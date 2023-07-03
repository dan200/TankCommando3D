using Dan200.Core.Main;

namespace Dan200.Core.Math
{
    internal static class Mathf
    {
        public const float PI = (float)System.Math.PI;
		public const float TWO_PI = (float)(2.0 * System.Math.PI);
		public const float DEGREES_TO_RADIANS = (float)(System.Math.PI / 180.0);
		public const float RADIANS_TO_DEGREES = (float)(180.0 / System.Math.PI);
        public const float LOG_2 = 0.69314718056f;

		public static bool IsFinite(float f)
		{
			return !float.IsInfinity(f) && !float.IsNaN(f);
		}

		public static float Max(float a, float b)
		{
			return System.Math.Max(a, b);
		}

		public static float Min(float a, float b)
		{
			return System.Math.Min(a, b);
		}

        public static float Sign(float f)
        {
            return (float)System.Math.Sign(f);
        }

        public static float Abs(float f)
        {
            return System.Math.Abs(f);
        }

        public static float Clamp(float f, float min, float max)
        {
			App.Assert(max >= min);
            return System.Math.Min(System.Math.Max(f, min), max);
        }

		public static float Saturate(float f)
		{
			return Clamp(f, 0.0f, 1.0f);
		}

        public static float Ease(float f)
        {
            f = Clamp(f, 0.0f, 1.0f);
            return (3.0f - 2.0f * f) * f * f;
        }

        public static float Lerp(float a, float b, float f)
        {
            return a + (b - a) * f;
        }

        public static float ToRadians(float f)
        {
            return f * DEGREES_TO_RADIANS;
        }

        public static float ToDegrees(float f)
        {
            return f * RADIANS_TO_DEGREES;
        }

        public static float Pow(float a, float b)
        {
            return (float)System.Math.Pow((double)a, (double)b);
        }

        public static float Exp(float f)
        {
            return (float)System.Math.Exp((double)f);
        }

        public static float Log(float f)
        {
            return (float)System.Math.Log((double)f);
        }

		public static float Square(float f)
		{
			return f * f;
		}

		public static float Cube(float f)
		{
			return f * f * f;
		}

        public static float Sqrt(float f)
        {
            return (float)System.Math.Sqrt((double)f);
        }

        public static float Floor(float f)
        {
            return (float)System.Math.Floor((double)f);
        }

		public static float Ceil(float f)
		{
			return (float)System.Math.Ceiling((double)f);
		}

        public static float Round(float f)
        {
            return (float)System.Math.Floor((double)f + 0.5);
        }

        public static float Round(float f, float step)
        {
            App.Assert(step > 0.0f);
            return step * Round(f / step);
        }

        public static float Sin(float f)
        {
            return (float)System.Math.Sin((double)f);
        }

		public static float Cos(float f)
        {
            return (float)System.Math.Cos((double)f);
        }

        public static float Tan(float f)
        {
            return (float)System.Math.Tan((double)f);
        }

        public static float ASin(float f)
        {
            return (float)System.Math.Asin((double)f);
        }

		public static float ACos(float f)
        {
            return (float)System.Math.Acos((double)f);
        }

		public static float ATan(float f)
        {
            return (float)System.Math.Atan((double)f);
        }

        public static float ATan2(float y, float x)
        {
            return (float)System.Math.Atan2((double)y, (double)x);
        }

		public static float AngleWrap(float a)
		{
			return a - Floor(a / TWO_PI) * TWO_PI;
		}

		public static float AngleDiff(float a, float b)
		{
			var diff = a - b;
			return AngleWrap(diff + PI) - PI;
		}

        // Exponential decay
        // Rate: the number of times the value will halve per second
        public static float Decay(float f, float dt, float rate)
        {
            App.Assert(dt >= 0.0f);
            App.Assert(rate >= 0.0f);
            return f * Exp(-LOG_2 * rate * dt);
        }

        // Exponential decay towards a target value
        // Rate: the number of times the distance to the target will halve per second
        public static float ApproachDecay(float f, float target, float dt, float rate)
        {
            App.Assert(dt >= 0.0f);
            App.Assert(rate >= 0.0f);
            return target + (f - target) * Exp(-LOG_2 * rate * dt);
        }

        // Linearly move towards a target value
        public static float ApproachLinear(float f, float target, float dt, float rate)
        {
            App.Assert(dt >= 0.0f);
            App.Assert(rate >= 0.0f);
            var diff = target - f;
            if (dt * rate >= Mathf.Abs(diff))
            {
                return target;
            }
            else
            {
                return f + Mathf.Sign(diff) * dt * rate;
            }
        }
    }
}

