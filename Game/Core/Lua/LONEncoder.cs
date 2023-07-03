using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Dan200.Core.Lua
{
    internal class LONEncoder : ILuaEncoder
    {
        public Stream m_output;
        private int m_indent;
        private bool m_lineStarted;

        public LONEncoder(Stream output)
        {
            m_output = output;
            m_indent = 0;
            m_lineStarted = false;
        }

        public void Dispose()
        {
            m_output.Dispose();
        }

        public void EncodeComment(string comment)
        {
            Write(ByteString.Intern("-- "));
            WriteLine(new ByteString(comment));
        }

        public void Encode(LuaValue value)
        {
            WriteValue(value);
        }

        private void WriteString(ByteString str)
        {
            // Write an escaped string
            Write((byte)'\"');
            for (int i = 0; i < str.Length; ++i)
            {
                int codepoint;
                int size;
                if (UnicodeUtils.ReadUTF8Char(str, i, out codepoint, out size))
                {
                    if (size == 1)
                    {
                        // Write an ascii character
                        var b = str[i];
                        switch ((char)b)
                        {
                            case '\a':
                                Write((byte)'\\');
                                Write((byte)'a');
                                break;
                            case '\b':
                                Write((byte)'\\');
                                Write((byte)'b');
                                break;
                            case '\f':
                                Write((byte)'\\');
                                Write((byte)'f');
                                break;
                            case '\n':
                                Write((byte)'\\');
                                Write((byte)'n');
                                break;
                            case '\r':
                                Write((byte)'\\');
                                Write((byte)'r');
                                break;
                            case '\t':
                                Write((byte)'\\');
                                Write((byte)'t');
                                break;
                            case '\v':
                                Write((byte)'\\');
                                Write((byte)'v');
                                break;
                            case '\\':
                            case '\"':
                            case '\'':
                            case '[':
                            case ']':
                                Write((byte)'\\');
                                Write(b);
                                break;
                            default:
                                if (char.IsControl((char)b))
                                {
                                    if (i + size < str.Length && (str[i + size] >= '0' && str[i + size] <= '9'))
                                    {
                                        // Next character is a digit, so encode full sized
                                        Write((byte)'\\');
                                        Write(ByteString.Temp(((int)b).ToString("D3", CultureInfo.InvariantCulture)));
                                    }
                                    else
                                    {
                                        // Next character is something else, so encode compacted
                                        Write((byte)'\\');
                                        Write(ByteString.Temp(((int)b).ToString(CultureInfo.InvariantCulture)));
                                    }
                                }
                                else
                                {
                                    Write(b);
                                }
                                break;
                        }
                    }
                    else
                    {
                        // Write a non-ascii character
                        Write(str.Substring(i, size));
                        i += (size - 1);
                    }
                }
                else
                {
                    // Write a byte
                    var b = str[i];
                    Write((byte)'\\');
                    Write(ByteString.Temp(((int)b).ToString("D3", CultureInfo.InvariantCulture)));
                }
            }
            Write((byte)'\"');
        }

        private void WriteValue(LuaValue value)
        {
            if (value.IsNil())
            {
                // Write nil
                Write(ByteString.Intern("nil"));
            }
            else if (value.IsBool())
            {
                // Write a boolean
                var b = value.GetBool();
                if (b)
                {
                    Write(ByteString.Intern("true"));
                }
                else
                {
                    Write(ByteString.Intern("false"));
                }
            }
            else if (value.IsNumber())
            {
                // Write a number
                if (value.IsInteger())
                {
                    var l = value.GetLong();
                    Write(ByteString.Temp(l.ToString(CultureInfo.InvariantCulture)));
                }
                else
                {
                    var d = value.GetDouble();
                    Write(ByteString.Temp(d.ToString("G7", CultureInfo.InvariantCulture)));
                }
            }
            else if (value.IsString())
            {
                // Write an escaped string
                var str = value.IsByteString() ? value.GetByteString() : ByteString.Temp(value.GetString());
                WriteString(str);
            }
            else if (value.IsTable())
            {
                // Write a table
                var t = value.GetTable();
                if (t.Count == 0)
                {
                    Write((byte)'{');
                    Write((byte)'}');
                }
                else
                {
                    // Start the table
                    WriteLine((byte)'{');
                    Indent();

                    // Emit array-like entries
                    for (int i = 1; i <= t.ArrayLength; ++i)
                    {
                        var v = t[i];
                        WriteValue(v);
                        WriteLine((byte)',');
                    }

                    // Emit the rest of the entries
                    if (t.Count > t.ArrayLength)
                    {
                        foreach (var pair in t)
                        {
                            var k = pair.Key;
                            if (k.IsInteger() && k.GetInt() >= 1 && k.GetInt() <= t.ArrayLength)
                            {
                                continue;
                            }
                            else if (k.IsString())
                            {
                                var str = k.IsByteString() ? k.GetByteString() : ByteString.Temp(k.GetString());
                                if (IsValidIdentifier(str))
                                {
                                    Write(str);
                                    Write(ByteString.Intern(" = "));
                                    WriteValue(pair.Value);
                                }
                                else
                                {
                                    Write((byte)'[');
                                    WriteString(str);
                                    Write(ByteString.Intern("] = "));
                                    WriteValue(pair.Value);
                                }
                            }
                            else
                            {
                                Write((byte)'[');
                                WriteValue(k);
                                Write(ByteString.Intern("] = "));
                                WriteValue(pair.Value);
                            }
                            WriteLine((byte)',');
                        }
                    }

                    // Finish the table
                    Outdent();
                    Write((byte)'}');
                }
            }
            else
            {
                throw new InvalidDataException(string.Format("Cannot encode type {0}", value.GetTypeName()));
            }
        }

        private List<string> s_keywords = new List<string>{
            "and", "break", "do", "else", "elseif", "end",
            "false", "for", "function", "goto", "if", "in",
            "local", "nil", "not", "or", "repeat", "return",
            "then", "true", "until", "while"
        };

        private bool IsValidIdentifier(ByteString str)
        {
            if (str.Length > 0)
            {
                // Check the first byte is valid
                var b0 = str[0];
                if ((b0 >= 'a' && b0 <= 'z') ||
                    (b0 >= 'A' && b0 <= 'Z') ||
                    (b0 == '_'))
                {
                    // Check the rest of bytes are valid
                    for (int i = 1; i < str.Length; ++i)
                    {
                        var b = str[i];
                        if ((b >= 'a' && b <= 'z') ||
                            (b >= 'A' && b <= 'Z') ||
                            (b >= '0' && b <= '9') ||
                            (b == '_'))
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    // Check the string doesn't match a keyword
                    if (str.Length <= 8 && b0 >= 'a' && b0 <= 'z')
                    {
                        for (int i = 0; i < s_keywords.Count; ++i)
                        {
                            var keyword = s_keywords[i];
                            if (keyword[0] > b0) // Array is alphabetical, so break once we pass the desired letter
                            {
                                break;
                            }
                            if (keyword.Length == str.Length)
                            {
                                int j;
                                for (j = 0; j < str.Length; ++j)
                                {
                                    if (str[j] != keyword[j])
                                    {
                                        break;
                                    }
                                }
                                if (j >= keyword.Length)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void Indent()
        {
            ++m_indent;
        }

        private void Outdent()
        {
            --m_indent;
        }

        private void StartLine()
        {
            if (!m_lineStarted)
            {
                var tab = ByteString.Intern("    ");
                for (int i = 0; i < m_indent; ++i)
                {
                    m_output.Write(tab);
                }
                m_lineStarted = true;
            }
        }

        private void Write(ByteString text)
        {
            StartLine();
            m_output.Write(text);
        }

        private void Write(byte b)
        {
            StartLine();
            m_output.WriteByte(b);
        }

        private void WriteLine(ByteString text)
        {
            Write(text);
            WriteLine();
        }

        private void WriteLine(byte b)
        {
            Write(b);
            WriteLine();
        }

        private void WriteLine()
        {
            var newLine = ByteString.Intern(Environment.NewLine);
			m_output.Write(newLine);
            m_lineStarted = false;
        }
    }
}
