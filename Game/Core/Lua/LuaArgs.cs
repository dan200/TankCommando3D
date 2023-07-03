using Dan200.Core.Main;
using Dan200.Core.Util;
using System;
using System.Text;

namespace Dan200.Core.Lua
{
    internal readonly struct LuaArgs
    {
        public static LuaArgs Empty = new LuaArgs(); // TODO: Make me readonly again
        public static LuaArgs Nil = new LuaArgs(LuaValue.Nil); // TODO: Make me readonly again

        public static LuaArgs Concat(LuaArgs a, LuaArgs b)
        {
            if (b.Length == 0)
            {
                return a;
            }
            else if (b.Length == 0)
            {
                return b;
            }

            var totalLength = a.Length + b.Length;
            if (totalLength == 0)
            {
                return LuaArgs.Empty;
            }
            else if (totalLength == 1)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                return new LuaArgs(arg0);
            }
            else if (totalLength == 2)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                return new LuaArgs(arg0, arg1);
            }
            else if (totalLength == 3)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                var arg2 = (a.Length > 2) ? a[2] : b[2 - a.Length];
                return new LuaArgs(arg0, arg1, arg2);
            }
            else if (totalLength == 4)
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                var arg2 = (a.Length > 2) ? a[2] : b[2 - a.Length];
                var arg3 = (a.Length > 3) ? a[3] : b[3 - a.Length];
                return new LuaArgs(arg0, arg1, arg2, arg3);
            }
            else
            {
                var arg0 = (a.Length > 0) ? a[0] : b[0];
                var arg1 = (a.Length > 1) ? a[1] : b[1 - a.Length];
                var arg2 = (a.Length > 2) ? a[2] : b[2 - a.Length];
                var arg3 = (a.Length > 3) ? a[3] : b[3 - a.Length];
                var extraArgs = new LuaValue[totalLength - 4];
                for (int i = 0; i < extraArgs.Length; ++i)
                {
                    var n = i + 4;
                    extraArgs[i] = (a.Length > n) ? a[n] : b[n - a.Length];
                }
                return new LuaArgs(arg0, arg1, arg2, arg3, extraArgs);
            }
        }

        private readonly LuaValue m_arg0;
        private readonly LuaValue m_arg1;
        private readonly LuaValue m_arg2;
        private readonly LuaValue m_arg3;
        private readonly LuaValue[] m_extraArgs;
        private readonly int m_start;
        private readonly int m_length;

        public int Length
        {
            get
            {
                return m_length;
            }
        }

        public LuaValue this[int index]
        {
            get
            {
                if (index >= 0 && index < m_length)
                {
                    var realIndex = m_start + index;
                    if (realIndex == 0)
                    {
                        return m_arg0;
                    }
                    else if (realIndex == 1)
                    {
                        return m_arg1;
                    }
                    else if (realIndex == 2)
                    {
                        return m_arg2;
                    }
                    else if (realIndex == 3)
                    {
                        return m_arg3;
                    }
                    else
                    {
                        return m_extraArgs[realIndex - 4];
                    }
                }
                return LuaValue.Nil;
            }
        }

        public LuaArgs(LuaValue arg0)
        {
            m_arg0 = arg0;
            m_arg1 = LuaValue.Nil;
            m_arg2 = LuaValue.Nil;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = null;
            m_start = 0;
            m_length = 1;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = LuaValue.Nil;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = null;
            m_start = 0;
            m_length = 2;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = arg2;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = null;
            m_start = 0;
            m_length = 3;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = arg2;
            m_arg3 = arg3;
            m_extraArgs = null;
            m_start = 0;
            m_length = 4;
        }

        public LuaArgs(LuaValue arg0, LuaValue arg1, LuaValue arg2, LuaValue arg3, params LuaValue[] extraArgs)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = arg2;
            m_arg3 = arg3;
            m_extraArgs = extraArgs;
            m_start = 0;
            m_length = 4 + extraArgs.Length;
        }

        private LuaArgs(LuaValue arg0, LuaValue arg1, LuaValue arg2, LuaValue arg3, LuaValue[] extraArgs, int start, int length)
        {
            m_arg0 = arg0;
            m_arg1 = arg1;
            m_arg2 = arg2;
            m_arg3 = arg3;
            m_extraArgs = extraArgs;
            m_start = start;
            m_length = length;
        }

        public LuaArgs(LuaValue[] args)
        {
            m_arg0 = LuaValue.Nil;
            m_arg1 = LuaValue.Nil;
            m_arg2 = LuaValue.Nil;
            m_arg3 = LuaValue.Nil;
            m_extraArgs = args;
            m_start = 4;
            m_length = args.Length;
        }

        public LuaArgs Select(int start)
        {
            return Select(start, System.Math.Max(m_length - start, 0));
        }

        public LuaArgs Select(int start, int length)
        {
            App.Assert(start >= 0 && length >= 0 && start + length <= m_length);
            return new LuaArgs(m_arg0, m_arg1, m_arg2, m_arg3, m_extraArgs, start, length);
        }

        public string GetTypeName(int index)
        {
            return this[index].GetTypeName();
        }

        public bool IsNil(int index)
        {
            return this[index].IsNil();
        }

        public bool IsBool(int index)
        {
            return this[index].IsBool();
        }

        public bool GetBool(int index)
        {
            ExpectType(LuaValueType.Boolean, index);
            return this[index].GetBool();
        }

		public bool GetOptionalBool(int index, bool _default)
		{
			return IsNil(index) ? _default : GetBool(index);
		}

        public bool IsNumber(int index)
        {
            return this[index].IsNumber();
        }

        public bool IsInteger(int index)
        {
            return this[index].IsInteger();
        }

        public double GetDouble(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetDouble();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

		public double GetOptionalDouble(int index, double _default)
		{
			return IsNil(index) ? _default : GetDouble(index);
		}

        public float GetFloat(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetFloat();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

		public float GetOptionalFloat(int index, float _default)
		{
			return IsNil(index) ? _default : GetFloat(index);
		}

        public long GetLong(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetLong();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

		public long GetOptionalLong(int index, long _default)
		{
			return IsNil(index) ? _default : GetLong(index);
		}

        public int GetInt(int index)
        {
            var value = this[index];
            if (value.IsNumber())
            {
                return value.GetInt();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

		public int GetOptionalInt(int index, int _default)
		{
			return IsNil(index) ? _default : GetInt(index);
		}

        public byte GetByte(int index)
        {
            var value = this[index];
            if (value.Type == LuaValueType.Integer || value.Type == LuaValueType.Number)
            {
                return value.GetByte();
            }
            else
            {
                throw GenerateTypeError("number", index);
            }
        }

		public byte GetOptionalByte(int index, byte _default)
		{
			return IsNil(index) ? _default : GetByte(index);
		}

	    public bool IsString(int index)
        {
            return this[index].IsString();
        }

        public bool IsByteString(int index)
        {
            return this[index].IsByteString();
        }

        public string GetString(int index)
        {
            var value = this[index];
            if (value.IsString())
            {
                return value.GetString();
            }
            else
            {
                throw GenerateTypeError("string", index);
            }
        }

		public string GetOptionalString(int index, string _default=null)
		{
			return IsNil(index) ? _default : GetString(index);
		}

        public ByteString GetByteString(int index)
        {
            var value = this[index];
            if (value.IsString())
            {
                return value.GetByteString();
            }
            else
            {
                throw GenerateTypeError("string", index);
            }
        }

		public ByteString GetOptionalByteString(int index, ByteString _default)
		{
			return IsNil(index) ? _default : GetByteString(index);
		}

		public TEnum GetEnum<TEnum>(int index) where TEnum : struct, IConvertible
		{
			var s = GetString(index);
			TEnum result;
			if (EnumConverter.TryParse(s, out result))
			{
				return result;
			}
			else
			{
				throw new LuaError(string.Format("Unrecognised value {0} at index {1}", s, index + m_start + 1));
			}
		}

		public TEnum GetOptionalEnum<TEnum>(int index, TEnum _default) where TEnum : struct, IConvertible
		{
			var s = GetOptionalString(index, null);
			if (s != null)
			{
				TEnum result;
				if (EnumConverter.TryParse(s, out result))
				{
					return result;
				}
				else
				{
					throw new LuaError(string.Format("Unrecognised value {0} at index {1}", s, index + m_start + 1));
				}
			}
			return _default;
		}

		public bool IsTable(int index)
        {
            return this[index].IsTable();
        }

        public LuaTable GetTable(int index)
        {
            ExpectType(LuaValueType.Table, index);
            return this[index].GetTable();
        }

		public LuaTable GetOptionalTable(int index, LuaTable _default = null)
		{
			return IsNil(index) ? _default : GetTable(index);
		}

        public bool IsObject(int index)
        {
            return this[index].IsObject();
        }

        public LuaObject GetObject(int index)
        {
            ExpectType(LuaValueType.Object, index);
            return this[index].GetObject();
        }

        public bool IsObject(int index, Type type)
        {
            return this[index].IsObject(type);
        }

        public LuaObject GetObject(int index, Type type)
        {
            if (!this[index].IsObject(type))
            {
                throw GenerateTypeError(LuaObject.GetTypeName(type), index);
            }
            return this[index].GetObject(type);
        }

		public LuaObject GetOptionalObject(int index, Type type)
		{
			return IsNil(index) ? null : GetObject(index, type);
		}

        public bool IsObject<T>(int index) where T : LuaObject
        {
            return this[index].IsObject<T>();
        }

        public T GetObject<T>(int index) where T : LuaObject
        {
            if (!this[index].IsObject<T>())
            {
                throw GenerateTypeError(LuaObject.GetTypeName(typeof(T)), index);
            }
            return this[index].GetObject<T>();
        }

		public T GetOptionalObject<T>(int index) where T : LuaObject
		{
			return IsNil(index) ? null : GetObject<T>(index);
		}

        public bool IsFunction(int index)
        {
            return this[index].IsFunction();
        }

        public LuaFunction GetFunction(int index)
        {
            ExpectType(LuaValueType.Function, index);
            return this[index].GetFunction();
        }

        public bool IsCFunction(int index)
        {
            return this[index].IsCFunction();
        }

        public LuaCFunction GetCFunction(int index)
        {
            ExpectType(LuaValueType.CFunction, index);
            return this[index].GetCFunction();
        }

        public bool IsCoroutine(int index)
        {
            return this[index].IsCoroutine();
        }

        public LuaCoroutine GetCoroutine(int index)
        {
            ExpectType(LuaValueType.Coroutine, index);
            return this[index].GetCoroutine();
        }

        public bool IsUserdata(int index)
        {
            return this[index].IsUserdata();
        }

        public IntPtr GetUserdata(int index)
        {
            ExpectType(LuaValueType.Userdata, index);
            return this[index].GetUserdata();
        }

        public string ToString(int index)
        {
            return this[index].ToString();
        }

        public override string ToString()
        {
            var builder = new StringBuilder("[");
            for (int i = 0; i < Length; ++i)
            {
                builder.Append(ToString(i));
                if (i < Length - 1)
                {
                    builder.Append(",");
                }
            }
            builder.Append("]");
            return builder.ToString();
        }

        private void ExpectType(LuaValueType type, int index)
        {
            var foundType = this[index].Type;
            if (foundType != type)
            {
                throw GenerateTypeError(type.GetTypeName(), index);
            }
        }

        private LuaError GenerateTypeError(string expectedTypeName, int index)
        {
            return new LuaError(string.Format("Expected {0} at argument #{1}, got {2}", expectedTypeName, index + m_start + 1, GetTypeName(index)));
        }
    }
}
