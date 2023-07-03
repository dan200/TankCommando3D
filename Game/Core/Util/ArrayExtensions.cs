using Dan200.Core.Main;
using System;

namespace Dan200.Core.Util
{
    internal static class ArrayExtensions
    {
        public static int IndexOf<T>(this T[] array, T search) where T : IEquatable<T>
        {
            return array.IndexOf(search, 0, array.Length);
        }

        public static int IndexOf<T>(this T[] array, T search, int start) where T : IEquatable<T>
        {
            return array.IndexOf(search, start, array.Length - start);
        }

        public static int IndexOf<T>(this T[] array, T search, int start, int length) where T : IEquatable<T>
        {
            App.Assert(start >= 0 && length >= 0 && start + length <= array.Length);
            for (int i = start; i < start + length; ++i)
            {
                if (array[i].Equals(search))
                {
                    return i;
                }
            }
            return -1;
        }

		public static int LastIndexOf<T>(this T[] array, T search) where T : IEquatable<T>
		{
			return array.LastIndexOf(search, 0, array.Length);
		}

		public static int LastIndexOf<T>(this T[] array, T search, int start) where T : IEquatable<T>
		{
			return array.LastIndexOf(search, start, array.Length - start);
		}

		public static int LastIndexOf<T>(this T[] array, T search, int start, int length) where T : IEquatable<T>
		{
			App.Assert(start >= 0 && length >= 0 && start + length <= array.Length);
			for (int i = start + length - 1; i >= start; --i)
			{
				if (array[i].Equals(search))
				{
					return i;
				}
			}
			return -1;
		}
    }
}

