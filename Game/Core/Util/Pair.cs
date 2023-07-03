using System;
using System.Collections.Generic;

namespace Dan200.Core.Util
{
	internal static class Pair
	{
		public static Pair<T1, T2> Create<T1, T2>(T1 first, T2 second)
		{
			return new Pair<T1, T2>(first, second);
		}

		public static int CompareByFirst<T1, T2>(Pair<T1, T2> x, Pair<T1, T2> y) where T1 : IComparable<T1>
		{
			return x.First.CompareTo(y.First);
		}

		public static int CompareBySecond<T1, T2>(Pair<T1, T2> x, Pair<T1, T2> y) where T2 : IComparable<T2>
		{
			return x.Second.CompareTo(y.Second);
		}
	}

	internal struct Pair<T1, T2> : IEquatable<Pair<T1, T2>>
	{		
		public T1 First;
		public T2 Second;

		public Pair(T1 first, T2 second)
		{
			First = first;
			Second = second;
		}

		public bool Equals(Pair<T1, T2> other)
		{
			return
				EqualityComparer<T1>.Default.Equals(First, other.First) &&
				EqualityComparer<T2>.Default.Equals(Second, other.Second);
		}

		public override int GetHashCode()
		{
			return
				First.GetHashCode() * 31 +
				Second.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is Pair<T1, T2>)
			{
				return Equals((Pair<T1, T2>)obj);
			}
			return false;
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", First, Second);
		}
	}
}
