using System.Collections.Generic;
using System.IO;
using Dan200.Core.Util;

namespace Dan200.Core.Util
{
    internal class SimpleTextParser
    {
        private Stream m_input;
        private int m_line;
        private List<int> m_returnedBytes;
        private bool m_bomSkipped;

        public int Line
        {
            get
            {
                return m_line;
            }
        }

        public SimpleTextParser(Stream input)
        {
            m_input = input;
            m_line = 0;
            m_returnedBytes = new List<int>();
            m_bomSkipped = (input.Position != 0);
        }

        public int ReadByte()
        {
            int b;
            if(m_returnedBytes.Count > 0)
            {
                int last = m_returnedBytes.Count - 1;
                b = m_returnedBytes[last];
                m_returnedBytes.RemoveAt(last);
            }
            else
            {
                b = m_input.ReadByte();
            }
            if (b == '\n')
            {
                m_line++;
            }
            return b;
        }

        public void ReturnByte(int b)
        {
            if (b == '\n')
            {
                m_line--;
            }
            m_returnedBytes.Add(b);
        }

        public bool SkipBOM()
        {
            // Skip the BOM
            if (!m_bomSkipped)
            {
                var b = ReadByte();
                if (b == 0xEF)
                {
                    b = ReadByte();
                    if (b == 0xBB)
                    {
                        b = ReadByte();
                        if (b == 0xBF)
                        {
                            m_bomSkipped = true;
                            return true;
                        }
                        else
                        {
                            throw new IOException("Partial BOM at start of file");
                        }
                    }
                    else
                    {
                        throw new IOException("Partial BOM at start of file");
                    }
                }
                else
                {
                    ReturnByte(b);
                    return false;
                }
            }
            return false;
        }

        public bool SkipWhitespace()
        {
            var b = ReadByte();
            if (b == ' ' || b == '\t' || b == '\r' || b == '\n')
            {
                do
                {
                    b = ReadByte();
                }
                while (b == ' ' || b == '\t' || b == '\r' || b == '\n');
                ReturnByte(b);
                return true;
            }
            else
            {
                ReturnByte(b);
                return false;
            }
        }

        public bool ReadQuotedString(out ByteString o_text)
        {
            // String
            var b = ReadByte();
            if (b == '\"' || b == '\'')
            {
                int first = b;
                int last = b;
                var builder = new ByteStringBuilder();
                builder.Append((byte)b);
                while (true)
                {
                    b = ReadByte();
                    if (b < 0 || b == '\n')
                    {
                        throw InvalidByte(b);
                    }
                    builder.Append((byte)b);
                    if (b == first && last != '\\')
                    {
                        break;
                    }
                    last = b;
                }
                o_text = builder.ToByteString();
                return true;
            }
            else
            {
                ReturnByte(b);
                o_text = default(ByteString);
                return false;
            }
        }

        public bool ReadNumber(out ByteString o_text)
        {
            var b = ReadByte();
            if ((b >= '0' && b <= '9') || b == '.' || b == '-')
            {
                // Number
                var builder = new ByteStringBuilder();
                builder.Append((byte)b);

                var b0 = b;
                b = ReadByte();
                if (b0 == '0' && (b == 'x' || b == 'X'))
                {
                    // Hex integer
                    builder.Append((byte)b);
                    b = ReadByte();
                    int numDigits = 0;
                    while (true)
                    {
                        if ((b >= '0' && b <= '9') || (b >= 'a' && b <= 'f') || (b >= 'A' && b <= 'F'))
                        {
                            builder.Append((byte)b);
                            b = ReadByte();
                            numDigits++;
                        }
                        else
                        {
                            ReturnByte(b);
                            break;
                        }
                    }
                    if (numDigits == 0)
                    {
                        throw InvalidByte(b); // 0x or 0X on it's own
                    }
                }
                else
                {
                    // Decimal integer or float
                    bool dotSeen = (b0 == '.');
                    bool numSeem = (b0 >= '0' && b0 <= '9');
                    while (true)
                    {
                        if ((b >= '0' && b <= '9') || (b == '.' && !dotSeen))
                        {
                            builder.Append((byte)b);
                            if (b == '.')
                            {
                                dotSeen = true;
                            }
                            else
                            {
                                numSeem = true;
                            }
                            b = ReadByte();
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (!numSeem)
                    {
                        throw InvalidByte(b); // . on it's own
                    }

                    // Exponential part
                    if (b == 'e' || b == 'E')
                    {
                        builder.Append((byte)b);
                        b = ReadByte();

                        if(b == '-')
                        {
                            builder.Append((byte)b);
                            b = ReadByte();
                        }

                        numSeem = false;
                        while(true)
                        {
                            if(b >= '0' && b <= '9')
                            {
                                numSeem = true;
                                builder.Append((byte)b);
                                b = ReadByte();
                            }
                            else
                            {
                                ReturnByte(b);
                                break;
                            }
                        }
                        if (!numSeem)
                        {
                            throw InvalidByte(b); // no digits after e
                        }
                    }
                    else
                    {
                        ReturnByte(b);
                    }
                }
                o_text = builder.ToByteString();
                return true;
            }
            else
            {
                ReturnByte(b);
                o_text = default(ByteString);
                return false;
            }
        }

        public bool ReadIdentifier(out ByteString o_text)
        {
            var b = ReadByte();
            if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || b == '_')
            {
                var builder = new ByteStringBuilder();
                builder.Append((byte)b);
                while (true)
                {
                    b = ReadByte();
                    if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9') || b == '_')
                    {
                        builder.Append((byte)b);
                    }
                    else
                    {
                        ReturnByte(b);
                        break;
                    }
                }
                o_text = builder.ToByteString();
                return true;
            }
            else
            {
                ReturnByte(b);
                o_text = default(ByteString);
                return false;
            }
        }

        public IOException InvalidByte(int b)
        {
            if (b < 0)
            {
                throw new IOException("Unexpected EOF on line " + m_line);
            }
            else if (b == '\n')
            {
                throw new IOException("Unexpected NewLine on line " + (m_line - 1));
            }
            else if (b >= 0 && b <= 127 && !char.IsControl((char)b))
            {
                throw new IOException("Unexpected character on line " + m_line + ": " + (char)b);
            }
            else
            {
                throw new IOException("Unexpected character on line " + m_line + ": " + b);
            }
        }
    }
}

