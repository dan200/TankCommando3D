using Dan200.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dan200.Core.Lua
{
    internal class LuaTable : IEnumerable<KeyValuePair<LuaValue, LuaValue>>
    {
        public static LuaTable Empty = new LuaTable(0);

        private Dictionary<LuaValue, LuaValue> m_values;
        private int m_arrayLength;

        public int Count
        {
            get
            {
                return m_values.Count;
            }
        }

        public int ArrayLength
        {
            get
            {
                return m_arrayLength;
            }
        }

        public LuaValue this[LuaValue key]
        {
            get
            {
                if (m_values.ContainsKey(key))
                {
                    return m_values[key];
                }
                return LuaValue.Nil;
            }
            set
            {
                if (key.IsNil())
                {
                    throw new LuaError("table index is nil");
                }
                else if (!value.IsNil())
                {
                    m_values[key] = value;
                    if(key.IsInteger())
                    {
                        var n = key.GetInt();
                        if(n == m_arrayLength + 1)
                        {
                            do
                            {
                                ++m_arrayLength;
                            }
                            while (m_values.ContainsKey(m_arrayLength + 1));
                        }
                    }
                }
                else
                {
                    m_values.Remove(key);
                    if (key.IsInteger())
                    {
                        var n = key.GetInt();
                        if(n >= 1 && n <= m_arrayLength)
                        {
                            m_arrayLength = n-1;
                        }
                    }
                }
            }
        }

        public LuaTable()
        {
            m_values = new Dictionary<LuaValue, LuaValue>(StructComparer<LuaValue>.Instance);
            m_arrayLength = 0;
        }

        public LuaTable(int initialCapacity)
        {
            m_values = new Dictionary<LuaValue, LuaValue>(initialCapacity, StructComparer<LuaValue>.Instance);
            m_arrayLength = 0;
        }

		public void Insert(LuaValue value)
		{
			m_values[m_arrayLength + 1] = value;
            m_arrayLength++;
		}

        public void Insert(LuaValue value, int index)
        {
            if (index < 1 || index > m_arrayLength + 1)
            {
                throw new LuaError("Index out of range");
            }

            for (int i = m_arrayLength + 1; i > index; --i)
            {
                m_values[i] = m_values[i - 1];
            }
            m_values[index] = value;
            m_arrayLength++;
        }

        public void Remove(int index)
        {
            if (index < 1 || index > m_arrayLength)
            {
                throw new LuaError("Index out of range");
            }

            m_values.Remove(index);
            if (index < m_arrayLength)
            {
                for (int i = index; i < m_arrayLength; ++i)
                {
                    m_values[i] = m_values[i + 1];
                }
                m_values.Remove(m_arrayLength);
            }
            m_arrayLength--;
        }

        public LuaTable Copy()
        {
            var copy = new LuaTable(m_values.Count);
            foreach(var pair in this)
            {
                copy[pair.Key] = pair.Value;
            }
            return copy;
        }

        public Dictionary<LuaValue, LuaValue>.Enumerator GetEnumerator()
        {
            return m_values.GetEnumerator();
        }

        IEnumerator<KeyValuePair<LuaValue, LuaValue>> IEnumerable<KeyValuePair<LuaValue, LuaValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string GetTypeName(LuaValue key)
        {
            LuaValue value;
            if (m_values.TryGetValue(key, out value))
            {
                return value.GetTypeName();
            }
            return "nil";
        }

        public bool IsNil(LuaValue key)
        {
            return this[key].IsNil();
        }

        public bool IsBool(LuaValue key)
        {
            return this[key].IsBool();
        }

        public bool GetBool(LuaValue key)
        {
            ExpectType(key, LuaValueType.Boolean);
            return this[key].GetBool();
        }

        public bool GetOptionalBool(LuaValue key, bool _default)
        {
            return IsNil(key) ? _default : GetBool(key);
        }

        public bool IsNumber(LuaValue key)
        {
            return this[key].IsNumber();
        }

        public bool IsInteger(int index)
        {
            return this[index].IsInteger();
        }

        public double GetDouble(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetDouble();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public float GetFloat(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetFloat();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public long GetLong(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetLong();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public int GetInt(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetInt();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public byte GetByte(LuaValue key)
        {
            var value = this[key];
            if (value.IsNumber())
            {
                return value.GetByte();
            }
            else
            {
                throw GenerateTypeError("number", key);
            }
        }

        public double GetOptionalDouble(LuaValue key, double _default)
        {
            return IsNil(key) ? _default : GetDouble(key);
        }

        public float GetOptionalFloat(LuaValue key, float _default)
        {
            return IsNil(key) ? _default : GetFloat(key);
        }

        public long GetOptionalLong(LuaValue key, long _default)
        {
            return IsNil(key) ? _default : GetLong(key);
        }

        public int GetOptionalInt(LuaValue key, int _default)
        {
            return IsNil(key) ? _default : GetInt(key);
        }

        public byte GetOptionalByte(LuaValue key, byte _default)
        {
            return IsNil(key) ? _default : GetByte(key);
        }

        public bool IsString(LuaValue key)
        {
            return this[key].IsString();
        }

        public bool IsByteString(LuaValue key)
        {
            return this[key].IsByteString();
        }

        public string GetString(LuaValue key)
        {
            var value = this[key];
            if (value.IsString())
            {
                return value.GetString();
            }
            else
            {
                throw GenerateTypeError("string", key);
            }
        }

        public ByteString GetByteString(LuaValue key)
        {
            var value = this[key];
            if (value.IsString())
            {
                return value.GetByteString();
            }
            else
            {
                throw GenerateTypeError("string", key);
            }
        }

        public string GetOptionalString(LuaValue key, string _default=null)
        {
            return IsNil(key) ? _default : GetString(key);
        }

        public ByteString GetOptionalByteString(LuaValue key, ByteString _default)
        {
            return IsNil(key) ? _default : GetByteString(key);
        }

        public bool IsTable(LuaValue key)
        {
            return this[key].IsTable();
        }

        public LuaTable GetTable(LuaValue key)
        {
            ExpectType(key, LuaValueType.Table);
            return this[key].GetTable();
        }

		public LuaTable GetOptionalTable(LuaValue key, LuaTable _default=null)
        {
            return IsNil(key) ? _default : GetTable(key);
        }

		public TEnum GetEnum<TEnum>(LuaValue key) where TEnum : struct, IConvertible
		{
			var s = GetString(key);
			TEnum result;
			if (EnumConverter.TryParse(s, out result))
			{
				return result;
			}
			else
			{
				throw new LuaError(string.Format("Unrecognised value {0} at key {1}", s, key.ToString()));
			}
		}

		public TEnum GetOptionalEnum<TEnum>(LuaValue key, TEnum _default) where TEnum : struct, IConvertible
		{
			var s = GetOptionalString(key, null);
			if (s != null)
			{
				TEnum result;
				if (EnumConverter.TryParse(s, out result))
				{
					return result;
				}
				else
				{
					throw new LuaError(string.Format("Unrecognised value {0} at key {1}", s, key.ToString()));
				}
			}
			return _default;
		}

        public bool IsObject(LuaValue key)
        {
            return this[key].IsObject();
        }

        public LuaObject GetObject(LuaValue key)
        {
            ExpectType(key, LuaValueType.Object);
            return this[key].GetObject();
        }

        public bool IsObject(LuaValue key, Type type)
        {
            return this[key].IsObject(type);
        }

        public LuaObject GetObject(LuaValue key, Type type)
        {
            if (!this[key].IsObject(type))
            {
                throw GenerateTypeError(LuaObject.GetTypeName(type), key);
            }
            return this[key].GetObject(type);
        }

        public bool IsObject<T>(LuaValue key) where T : LuaObject
        {
            return this[key].IsObject<T>();
        }

        public T GetObject<T>(LuaValue key) where T : LuaObject
        {
            if (!this[key].IsObject<T>())
            {
                throw GenerateTypeError(LuaObject.GetTypeName(typeof(T)), key);
            }
            return this[key].GetObject<T>();
        }

        public bool IsUserdata(LuaValue key)
        {
            return this[key].IsUserdata();
        }

        public IntPtr GetUserdata(LuaValue key)
        {
            ExpectType(key, LuaValueType.Userdata);
            return this[key].GetUserdata();
        }

        public string ToString(LuaValue key)
        {
            return this[key].ToString();
        }

        private void ExpectType(LuaValue key, LuaValueType type)
        {
            var value = this[key];
            if (value.Type != type)
            {
                throw GenerateTypeError(type.GetTypeName(), key);
            }
        }

        private LuaError GenerateTypeError(string expectedType, LuaValue key)
        {
            throw new LuaError(string.Format("Expected {0} for key {1}, got {2}", expectedType, key.ToString(), GetTypeName(key)));
        }
    }
}
