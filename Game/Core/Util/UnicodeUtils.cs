using System;
using System.Collections;
using System.Collections.Generic;
using Dan200.Core.Lua;

namespace Dan200.Core.Util
{
    internal struct UTF8Enumerator : IEnumerator<int>
    {
		private ByteString m_bytes;
        private int m_pos;
        private int m_current;

        public int Current
        {
            get
            {
                return m_current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public UTF8Enumerator(ByteString str)
        {
			m_bytes = str;
            m_pos = 0;
            m_current = -1;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
			if (m_pos < m_bytes.Length)
            {
                int codepoint, size;
                if (UnicodeUtils.ReadUTF8Char(m_bytes, m_pos, out codepoint, out size))
                {
                    m_current = codepoint;
                    m_pos += size;
                    return true;
                }
                else
                {
                    m_current = UnicodeUtils.REPLACEMENT_CHARACTER;
                    m_pos++;
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
			throw new NotImplementedException();
        }
    }

    internal struct UTF16Enumerator : IEnumerator<int>
    {
        private string m_chars;
        private int m_pos;
        private int m_current;

        public int Current
        {
            get
            {
                return m_current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public UTF16Enumerator(string str)
        {
            m_chars = str;
            m_pos = 0;
            m_current = -1;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (m_pos < m_chars.Length)
            {
                int codepoint, size;
                if (UnicodeUtils.ReadUTF16Char(m_chars, m_pos, out codepoint, out size))
                {
                    m_current = codepoint;
                    m_pos += size;
                    return true;
                }
                else
                {
                    m_current = UnicodeUtils.REPLACEMENT_CHARACTER;
                    m_pos++;
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
			throw new NotImplementedException();
        }
    }

    internal static class UnicodeUtils
    {
		public static readonly ByteString UTF8_BOM = new ByteString(0xEF, 0xBB, 0xBF);
        public const int REPLACEMENT_CHARACTER = 0xfffd;

		private static bool CheckUTF8Continuations(ByteString str, int pos, int count)
        {
			var space = str.Length - 1;
            if (space >= count)
            {
                for (int i = 1; i <= count; ++i)
                {
                    var b = str[pos + i];
                    if ((b & 0xC0) != 0x80)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

		public static bool ReadUTF8Char(ByteString str, int pos, out int o_codepoint, out int o_size)
        {
            var b0 = str[pos];
            if ((b0 & 0x80) == 0) // 0xxxxxxx
            {
                // 1 byte sequence (ASCII)
                o_codepoint =
                    (b0 & 0x7f);
                o_size = 1;
                return IsValidCodepoint(o_codepoint);
            }
            else if ((b0 & 0xE0) == 0xC0 && CheckUTF8Continuations(str, pos, 1)) // 110xxxxx 10xxxxxx
            {
                // 2 byte sequence
                o_codepoint =
                    ((b0 & 0x1F) << 6) +
                    (str[pos + 1] & 0x3F);
                o_size = 2;
                return IsValidCodepoint(o_codepoint);
            }
            else if ((b0 & 0xF0) == 0xE0 && CheckUTF8Continuations(str, pos, 2)) // 1110xxxx 10xxxxxx 10xxxxxx
            {
                // 3 byte sequence
                o_codepoint =
                    ((b0 & 0x0F) << 12) +
                    ((str[pos + 1] & 0x3F) << 6) +
                    (str[pos + 2] & 0x3F);
                o_size = 3;
                return IsValidCodepoint(o_codepoint);
            }
            else if ((b0 & 0xF8) == 0xF0 && CheckUTF8Continuations(str, pos, 3)) // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
            {
                // 4 byte sequence
                o_codepoint =
                    ((b0 & 0x07) << 18) +
                    ((str[pos + 1] & 0x3F) << 12) +
                    ((str[pos + 2] & 0x3F) << 6) +
                    (str[pos + 3] & 0x3F);
                o_size = 4;
                return IsValidCodepoint(o_codepoint);
            }
            else
            {
                // Invalid sequence
                o_codepoint = 0;
                o_size = 0;
                return false;
            }
        }

        public static bool IsValidCodepoint(int codepoint)
        {
            return
                codepoint >= 0 &&
                codepoint <= 0x10ffff &&
                (codepoint < 0xd800 || codepoint > 0xdfff);
        }

		internal struct UTF8Enumerable : IEnumerable<int>
		{
			private ByteString m_str;

			public UTF8Enumerable(ByteString str)
			{
				m_str = str;
			}

			public UTF8Enumerator GetEnumerator()
			{
				return new UTF8Enumerator(m_str);
			}

			IEnumerator<int> IEnumerable<int>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public static UTF8Enumerable ReadUTF8Characters(ByteString str)
		{
			return new UTF8Enumerable(str);
		}

        public static bool ReadUTF16Char(string str, int pos, out int o_codepoint, out int o_size)
        {
            var c0 = str[pos];
            var c1 = (pos < str.Length - 1) ? str[pos + 1] : '\0';
            if (char.IsHighSurrogate(c0) && char.IsLowSurrogate(c1))
            {
                // Surrogate pair
                o_codepoint = char.ConvertToUtf32(c0, c1);
                o_size = 2;
                return IsValidCodepoint(o_codepoint);
            }
            else if (!char.IsSurrogate(c0))
            {
                // Standalone character
                o_codepoint = (int)c0;
                o_size = 1;
                return IsValidCodepoint(o_codepoint);
            }
            else
            {
                // Lone surrogate
                o_codepoint = 0;
                o_size = 0;
                return false;
            }
        }

        public static int EncodeUTF16Char(int codepoint, out char o_first, out char o_second)
        {
            if (!IsValidCodepoint(codepoint))
            {
                codepoint = REPLACEMENT_CHARACTER;
            }

            if (codepoint > 0xffff)
            {
                codepoint = codepoint - 0x010000;
                o_first = (char)(((codepoint & 0xffc00) >> 10) + 0xd800);
                o_second = (char)((codepoint & 0x003ff) + 0xdc00);
                return 2;
            }
            else
            {
                o_first = (char)codepoint;
                o_second = '\0';
                return 1;
            }
        }

        public static int EncodeUTF8Char(int codepoint, out byte o_first, out byte o_second, out byte o_third, out byte o_fourth)
        {
            if (!IsValidCodepoint(codepoint))
            {
                codepoint = REPLACEMENT_CHARACTER;
            }

            if (codepoint <= 0x7f)
            {
                o_first = (byte)codepoint;
                o_second = 0;
                o_third = 0;
                o_fourth = 0;
                return 1;
            }
            else if (codepoint <= 0x7ff)
            {
                o_first = (byte)(0xC0 + ((codepoint >> 6) & 0x1f));
                o_second = (byte)(0x80 + (codepoint & 0x3f));
                o_third = 0;
                o_fourth = 0;
                return 2;
            }
            else if (codepoint <= 0xffff)
            {
                o_first = (byte)(0xE0 + ((codepoint >> 12) & 0x0f));
                o_second = (byte)(0x80 + ((codepoint >> 6) & 0x3f));
                o_third = (byte)(0x80 + (codepoint & 0x3f));
                o_fourth = 0;
                return 3;
            }
            else
            {
                o_first = (byte)(0xF0 + ((codepoint >> 18) & 0x07));
                o_second = (byte)(0x80 + ((codepoint >> 12) & 0x3f));
                o_third = (byte)(0x80 + ((codepoint >> 6) & 0x3f));
                o_fourth = (byte)(0x80 + (codepoint & 0x3f));
                return 4;
            }
        }
    }
}
