using Dan200.Core.Main;
using System;
using System.Linq.Expressions;

namespace Dan200.Core.Util
{	
    internal static class EnumConverter
    {
        private static class ConvertHelper<TEnum> where TEnum : struct, IConvertible
        {
            public static readonly Func<int, TEnum> Convert = GenerateConverter();
			public static readonly TEnum[] Values = (TEnum[])Enum.GetValues(typeof(TEnum));

            private static Func<int, TEnum> GenerateConverter()
            {
                var parameter = Expression.Parameter(typeof(int));
                var dynamicMethod = Expression.Lambda<Func<int, TEnum>>(
                    Expression.Convert(parameter, typeof(TEnum)),
                    parameter
                );
                return dynamicMethod.Compile();
            }
        }

		public static TEnum[] GetValues<TEnum>() where TEnum : struct, IConvertible
		{
			return ConvertHelper<TEnum>.Values;
		}

        public static int ToInt<TEnum>(TEnum e) where TEnum : struct, IConvertible
        {
            App.Assert(typeof(TEnum).IsEnum);
			App.Assert(Enum.IsDefined(typeof(TEnum), e));
            return e.ToInt32(null);
        }

		public static string ToString<TEnum>(TEnum e) where TEnum : struct, IConvertible
		{
			App.Assert(typeof(TEnum).IsEnum);
			App.Assert(Enum.IsDefined(typeof(TEnum), e));
			return e.ToString();
		}

        public static TEnum ToEnum<TEnum>(int i) where TEnum : struct, IConvertible
        {
            App.Assert(typeof(TEnum).IsEnum);
            var result = ConvertHelper<TEnum>.Convert(i);
			App.Assert(Enum.IsDefined(typeof(TEnum), result));
			return result;
        }

		public static bool TryParse<TEnum>(string s, out TEnum o_result) where TEnum : struct, IConvertible
		{
            App.Assert(typeof(TEnum).IsEnum);
            TEnum result;
            if(Enum.TryParse(s, out result) && Enum.IsDefined(typeof(TEnum), result))
            {
                o_result = result;
                return true;
            }
            o_result = default(TEnum);
            return false;
		}
    }
}
