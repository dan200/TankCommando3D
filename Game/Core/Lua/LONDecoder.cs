using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Dan200.Core.Main;
using Dan200.Core.Util;

namespace Dan200.Core.Lua
{
    internal delegate LuaValue LONMacro(in LuaArgs args);

    internal class LONDecoder : ILuaDecoder
    {
        private SimpleTextParser m_parser;
        private Dictionary<ByteString, LONMacro> m_macros;

        public LONDecoder(Stream input)
        {
            m_parser = new SimpleTextParser(input);
            m_macros = new Dictionary<ByteString, LONMacro>();
        }

        public void AddMacro(string name, LONMacro macro)
        {
            m_macros.Add(ByteString.Intern(name), macro);
        }

        public LuaValue DecodeValue()
        {
            ByteString tokenText;
            var token = ReadToken(out tokenText);
            switch (token)
            {
                case Token.Nil:
                case Token.True:
                case Token.False:
                case Token.Number:
                case Token.String:
                    return DecodeSingleTokenValue(token, tokenText);
                case Token.OpenCurly:
                    return DecodeTable();
                case Token.Symbol:
                    ExpectToken(Token.OpenParen);
                    return DecodeMacro(tokenText);
                default:
					throw InvalidToken(tokenText);
            }
        }

		private LuaValue DecodeSingleTokenValue(Token token, ByteString tokenText)
        {
            switch (token)
            {
                case Token.Nil:
                    return LuaValue.Nil;
                case Token.True:
                    return LuaValue.True;
                case Token.False:
                    return LuaValue.False;
                case Token.String:
                    return Unescape(tokenText.Substring(1, tokenText.Length - 2));
                case Token.Number:
                    if (tokenText.Length >= 3 && (tokenText[1] == 'x' || tokenText[1] == 'X'))
                    {
                        // hex int
                        return long.Parse(tokenText.Substring(2).ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    }
                    else if (tokenText.IndexOf((byte)'.') >= 0)
                    {
                        // float
                        return double.Parse(tokenText.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // decimal int
                        return long.Parse(tokenText.ToString(), NumberStyles.Integer | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
                    }
                default:
					throw InvalidToken(tokenText);
            }
        }

        private ByteString Unescape(ByteString str)
        {
            int slashIndex = str.IndexOf((byte)'\\');
            if (slashIndex >= 0)
            {
                // Escape needed
                var result = new ByteStringBuilder(str.Length); // Unescaped strings should always be shorter than the original
                result.Append(str.Substring(0, slashIndex));
                var pos = slashIndex + 1;
                while (true)
                {
                    // Unescape a character
                    var b = str[pos];
                    switch ((char)b)
                    {
                        case 'a':
                            result.Append((byte)'\a');
                            break;
                        case 'b':
                            result.Append((byte)'\b');
                            break;
                        case 'f':
                            result.Append((byte)'\f');
                            break;
                        case 'n':
                            result.Append((byte)'\n');
                            break;
                        case 'r':
                            result.Append((byte)'\r');
                            break;
                        case 't':
                            result.Append((byte)'\t');
                            break;
                        case 'v':
                            result.Append((byte)'\v');
                            break;
                        case '\\':
                        case '\"':
                        case '\'':
                        case '[':
                        case ']':
                            result.Append(b);
                            break;
                        default:
                            // \ddd
                            if (b >= '0' && b <= '9')
                            {
                                int num = b - '0';
                                for (int i = 0; i < 2; ++i)
                                {
                                    if (pos + 1 < str.Length && (str[pos + 1] >= '0' && str[pos + 1] <= '9'))
                                    {
                                        num = num * 10 + (str[pos + 1] - '0');
                                        pos = pos + 1;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (num > 255)
                                {
									throw new IOException("Escape sequence too large on line " + m_parser.Line + ": \\" + num);
                                }
                                result.Append((byte)num);
                                break;
                            }
                            else
                            {
								throw new IOException("Unrecognised escape code on line " + m_parser.Line + ": \\" + (char)b);
                            }
                    }

                    // Look for the next slash
                    slashIndex = str.IndexOf((byte)'\\', pos + 1);
                    if (slashIndex >= 0)
                    {
                        // Another escape sequence, insert the intermediate characters and continue
                        if (slashIndex > pos + 1)
                        {
                            result.Append(str.Substring(pos + 1, slashIndex - (pos + 1)));
                        }
                        pos = slashIndex + 1;
                    }
                    else
                    {
                        // No more escape sequences, finish the string
                        result.Append(str.Substring(pos + 1));
                        break;
                    }
                }
                return result.ToByteString();
            }
            else
            {
                // No escape codes in string, no unescaping needed
                return str;
            }
        }

        private LuaTable DecodeTable()
        {
            ByteString tokenText;
            var table = new LuaTable();
            int nextIntIndex = 1;
			var tableLine = m_parser.Line;
            while (true)
            {
                // Read the key-value pair
                var token = ReadToken(out tokenText);
                switch (token)
                {
                    case Token.CloseCurly:
                        break;
                    case Token.OpenSquare:
                        {
							// [ k ] = v
                            var key = DecodeValue();
                            ExpectToken(Token.CloseSquare);
                            ExpectToken(Token.Equals);
                            var value = DecodeValue();
							if (!table.IsNil(key))
							{
								throw new IOException("Multiple entries with key " + key.ToString() + " in table on line " + tableLine);
							}
                            table[key] = value;
                            break;
                        }
                    case Token.Symbol:
                        {
                            var name = tokenText;
                            token = ReadToken(out tokenText);
                            if (token == Token.Equals)
                            {
                                // k = v
                                var key = name;
                                var value = DecodeValue();
								if (!table.IsNil(key))
								{
									throw new IOException("Multiple entries with key " + key.ToString() + " in table on line " + tableLine);
								}
                                table[key] = value;
                            }
                            else if(token == Token.OpenParen)
                            {
                                // v()
                                var key = nextIntIndex++;
                                var value = DecodeMacro(name);
								if (!table.IsNil(key))
								{
									throw new IOException("Multiple entries with key " + key.ToString() + " in table on line " + tableLine);
								}
                                table[key] = value;
                            }
                            else
                            {
								throw InvalidToken(tokenText);
                            }
                            break;
                        }
                    case Token.True:
                    case Token.False:
                    case Token.Number:
                    case Token.String:
                        {
                            // v
                            var key = nextIntIndex++;
                            var value = DecodeSingleTokenValue(token, tokenText);
							if (!table.IsNil(key))
							{
								throw new IOException("Multiple entries with key " + key.ToString() + " in table on line " + tableLine);
							}
                            table[key] = value;
                            break;
                        }
                    case Token.OpenCurly:
                        {
                            // {v}
                            var key = nextIntIndex++;
                            var value = DecodeTable();
							if (!table.IsNil(key))
							{
								throw new IOException("Multiple entries with key " + key.ToString() + " in table on line " + tableLine);
							}
                            table[key] = value;
                            break;
                        }
                    default:
						throw InvalidToken(tokenText);
                }
                if (token == Token.CloseCurly)
                {
                    break;
                }

                // Read the , or }
                token = ReadToken(out tokenText);
                switch (token)
                {
                    case Token.CloseCurly:
                    case Token.Comma:
                    case Token.SemiColon:
                        break;
                    default:
						throw InvalidToken(tokenText);
                }
                if (token == Token.CloseCurly)
                {
                    break;
                }
            }
            return table;
        }

        private LuaValue ExecuteMacro(ByteString name, LuaArgs args)
        {
            LONMacro macro;
            if(m_macros.TryGetValue(name, out macro))
            {
                return macro.Invoke(args);
            }
            else
            {
                throw new IOException("Unrecognised macro: " + name.ToString());
            }
        }

        private LuaValue DecodeMacro(ByteString name)
        {
            // Read the first token
            ByteString tokenText;
            var token = ReadToken(out tokenText);
            if(token == Token.CloseParen)
            {
                return ExecuteMacro(name, LuaArgs.Empty);
            }

            // Read the arguments
            var values = new List<LuaValue>();
            while (true)
            {
                // Read the argument
                switch (token)
                {
                    case Token.Nil:
                    case Token.True:
                    case Token.False:
                    case Token.Number:
                    case Token.String:
                        values.Add(DecodeSingleTokenValue(token, tokenText));
                        break;
                    case Token.OpenCurly:
                        values.Add(DecodeTable());
                        break;
                    case Token.Symbol:
                        ExpectToken(Token.OpenParen);
                        values.Add(DecodeMacro(tokenText));
                        break;
                    default:
						throw InvalidToken(tokenText);
                }

                // Read the next token
                token = ReadToken(out tokenText);
                if (token == Token.CloseParen)
                {
                    break;
                }
                else if (token == Token.Comma)
                {
                    token = ReadToken(out tokenText);
                    continue;
                }
                else
                {
					throw InvalidToken(tokenText);
                }
            }

            // Execute the macro
            return ExecuteMacro(name, new LuaArgs(values.ToArray()));
        }

        private enum Token
        {
            Nil,
            True,
            False,
            Symbol,
            Number,
            String,
            Equals,
            Comma,
            SemiColon,
            OpenParen,
            CloseParen,
            OpenCurly,
            CloseCurly,
            OpenSquare,
            CloseSquare,
            EOF
        }

        private void ExpectToken(Token token)
        {
            ByteString tokenText;
            if (ReadToken(out tokenText) != token)
            {
				throw InvalidToken(tokenText);
            }
        }

        private bool SkipComment()
        {
            var b = m_parser.ReadByte();
            if (b == '-')
            {
                // Comment
                // --
                var next = m_parser.ReadByte();
                if ((next >= '0' && next <= '9') || next == '.')
                {
                    // Comment was actually the start of a negative number, abort!
                    m_parser.ReturnByte(next);
                    m_parser.ReturnByte(b);
                    return false;
                }
                else if (next != '-')
                {
                    throw m_parser.InvalidByte(b);
                }
                b = m_parser.ReadByte();
                if (b == '[')
                {
                    // --[[ style comment ]]
                    b = m_parser.ReadByte();
                    if (b != '[')
                    {
                        throw m_parser.InvalidByte(b);
                    }
                    var last = b;
                    while (true)
                    {
                        b = m_parser.ReadByte();
                        if (b == -1)
                        {
                            throw m_parser.InvalidByte(b);
                        }
                        else if (b == ']' && last != '\\')
                        {
                            b = m_parser.ReadByte();
                            if (b == ']')
                            {
                                break;
                            }
                        }
                        last = b;
                    }
                    return true;
                }
                else
                {
                    // -- style commment
                    while (b != -1 && b != '\n')
                    {
                        b = m_parser.ReadByte();
                    }
                    m_parser.ReturnByte(b);
                    return true;
                }
            }
            else
            {
                // Not a comment
                m_parser.ReturnByte(b);
                return false;
            }
        }

        private Token ReadToken(out ByteString o_string)
        {
            // Skip BOM
            m_parser.SkipBOM();

            // Skip whitespace and comments
            while (m_parser.SkipWhitespace() || SkipComment()) { }

            // Parse
            if (m_parser.ReadNumber(out o_string))
            {
                return Token.Number;
            }
            else if (m_parser.ReadQuotedString(out o_string))
            {
                return Token.String;
            }
            else if (m_parser.ReadIdentifier(out o_string))
            {
                if (o_string.Equals(ByteString.Intern("nil")))
                {
                    return Token.Nil;
                }
                else if (o_string.Equals(ByteString.Intern("true")))
                {
                    return Token.True;
                }
                else if (o_string.Equals(ByteString.Intern("false")))
                {
                    return Token.False;
                }
                else
                {
                    return Token.Symbol;
                }
            }
            else
            {
                var b = m_parser.ReadByte();
                if (b == -1)
                {
                    // EOF
                    o_string = ByteString.Intern("EOF");
                    return Token.EOF;
                }
                else if (b == ',')
                {
                    // ,
                    o_string = ByteString.Intern(",");
                    return Token.Comma;
                }
                else if (b == ';')
                {
                    // ;
                    o_string = ByteString.Intern(";");
                    return Token.SemiColon;
                }
                else if (b == '=')
                {
                    // =
                    o_string = ByteString.Intern("=");
                    return Token.Equals;
                }
                else if (b == '(')
                {
                    // (
                    o_string = ByteString.Intern("(");
                    return Token.OpenParen;
                }
                else if (b == ')')
                {
                    // )
                    o_string = ByteString.Intern(")");
                    return Token.CloseParen;
                }
                else if (b == '{')
                {
                    // {
                    o_string = ByteString.Intern("{");
                    return Token.OpenCurly;
                }
                else if (b == '}')
                {
                    // }
                    o_string = ByteString.Intern("}");
                    return Token.CloseCurly;
                }
                else if (b == '[')
                {
                    // [
                    o_string = ByteString.Intern("[");
                    return Token.OpenSquare;
                }
                else if (b == ']')
                {
                    // ]
                    o_string = ByteString.Intern("]");
                    return Token.CloseSquare;
                }
                else
                {
                    throw m_parser.InvalidByte(b);
                }
            }
        }

		private IOException InvalidToken(ByteString tokenText)
		{
			return new IOException("Unexpected token on line " + m_parser.Line + ": " + tokenText.ToString());
		}
    }
}

