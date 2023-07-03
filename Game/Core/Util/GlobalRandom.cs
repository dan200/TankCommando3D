using System;
using Dan200.Core.Main;
using Dan200.Core.Math;

namespace Dan200.Core.Util
{
	internal static class GlobalRandom
	{
		[ThreadStatic]
		private static Random s_random;

		private static Random GetRandom()
		{
			if (s_random == null)
			{
				s_random = new Random();
			}
			return s_random;
		}

		public static int Int(int min, int max)
		{
            App.Assert(max >= min);
			return min + GetRandom().Next((max - min) + 1);
		}

		public static float Float()
		{
			return (float)GetRandom().NextDouble();
		}

		public static float Float(float min, float max)
		{
			return min + Float() * (max - min);
		}

		public static bool Bool()
		{
			return (Int(0, 1) == 1);
		}

		public static UnitVector3 Direction()
		{
			return new Vector3(
				Float(-1.0f, 1.0f),
				Float(-1.0f, 1.0f),
				Float(-1.0f, 1.0f)
			).SafeNormalise(Vector3.YAxis);
		}
	}
}
