using Dan200.Core.Main;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Util
{
    internal static class ListExtensions
    {
        public static bool UnorderedRemove<T>(this List<T> list, T item)
        {
            int index = list.IndexOf(item);
            if (index >= 0)
            {
                list.UnorderedRemoveAt(index);
                return true;
            }
            return false;
        }

        public static void UnorderedRemoveAt<T>(this List<T> list, int index)
        {
            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (index < list.Count - 1)
            {
                list[index] = list[list.Count - 1];
            }
            list.RemoveAt(list.Count - 1);
        }

		public static int BinarySearch<T>(this List<T> list, T value, Comparison<T> comparison)
		{
			return list.BinarySearch(0, list.Count, value, comparison);
		}

		public static int BinarySearch<T>(this List<T> list, int start, int length, T value, Comparison<T> comparison)
		{
			App.Assert(start >= 0 && length >= 0 && start + length <= list.Count);
			int low = start;
			int high = start + length - 1;
			while (low <= high)
			{
				int med = low + (high - low) / 2;
				int compareResult = comparison.Invoke(list[med], value);
				if (compareResult == 0)
				{
					return med;
				}
				else if (compareResult < 0)
				{
					low = med + 1;
				}
				else
				{
					high = med - 1;
				}
			}
			App.Assert(low >= 0 && low <= list.Count);
			return ~low;
		}

        public static void InsertSorted<T>(this List<T> list, T value, Comparison<T> comparison)
        {
            list.InsertSorted(0, list.Count, value, comparison);
        }

        public static void InsertSorted<T>(this List<T> list, int start, int length, T value, Comparison<T> comparison)
		{
			var searchResult = list.BinarySearch(start, length, value, comparison);
			if (searchResult >= 0)
			{
				list.Insert(searchResult, value);
			}
			else
			{
				list.Insert(~searchResult, value);
			}
		}

		public static bool IsSorted<T>(this List<T> list, Comparison<T> comparison)
		{
			for (int i = 0; i < list.Count-1; ++i)
			{
				if (comparison.Invoke(list[i], list[i + 1]) > 0)
				{
					return false;
				}
			}
			return true;
		}

        public static T First<T>(this List<T> list)
        {
            App.Assert(list.Count > 0);
            return list[0];
        }

        public static T Last<T>(this List<T> list)
        {
            App.Assert(list.Count > 0);
            return list[list.Count - 1];
        }

        public static void Shuffle<T>(this List<T> list, Random random)
        {
            list.Shuffle(0, list.Count, random);
        }

        public static void Shuffle<T>(this List<T> list, int start, int length, Random random)
        {
            App.Assert(start >= 0 && length >= 0 && start + length <= list.Count);
            for(int i=start; i<start+length; ++i)
            {
                var swapIndex = random.Next(i, start + length);
                T temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }
    }
}

