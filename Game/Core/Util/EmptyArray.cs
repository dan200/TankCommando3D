using System;
using System.Collections.Generic;

namespace Dan200.Core.Util
{
	internal static class EmptyArray<T>
	{
		public static T[] Instance = new T[0];
	}

	internal static class EmptyList<T>
	{
		public static IReadOnlyList<T> Instance = new List<T>(0);
	}
}
