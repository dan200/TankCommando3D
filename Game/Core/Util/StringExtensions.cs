using System.Text;
using System.Text.RegularExpressions;

namespace Dan200.Core.Util
{
    internal static class StringExtensions
    {
        private static Regex s_wordBoundaryRegex = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);

        public static string ToLowerUnderscored(this string str)
        {
            return s_wordBoundaryRegex.Replace(str, "$1_$2").ToLowerInvariant();
        }

        public static string ToProperSpaced(this string str)
        {
            return s_wordBoundaryRegex.Replace(str, "$1 $2");
        }

        public static string URLEncode(this string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            var result = new StringBuilder();
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte b = bytes[i];
                if ((b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z') || (b >= '0' && b <= '9') || b == '-' || b == '_' || b == '.')
                {
                    result.Append((char)b);
                }
                else if (b == ' ')
                {
                    result.Append('+');
                }
                else
                {
                    result.Append('%');
                    result.Append(b.ToString("X2"));
                }
            }
            return result.ToString();
        }

		public static int StableHash(this string s)
		{
			unchecked
			{
				int hash = 0;
				foreach (char c in s)
				{
					hash = hash * 31 + c;
				}
				return hash;
			}
		}
    }
}

