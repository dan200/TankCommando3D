using Dan200.Core.Util;
using System.IO;
using System.Text;

namespace Dan200.Core.Lua
{
    internal class BLONDecoder : ILuaDecoder
    {
        private BinaryReader m_input;
        private ByteString[] m_stringCache;
        private byte m_stringCachePosition;

		public BLONDecoder(Stream input) : this(new BinaryReader(input, Encoding.UTF8))
		{
		}

        public BLONDecoder(BinaryReader input)
        {
            m_input = input;
            m_stringCache = new ByteString[256];
            m_stringCachePosition = 0;
        }

        public LuaValue DecodeValue()
        {
            var input = m_input;
            var type = (BLONValueType)input.ReadByte();
            switch (type)
            {
                case BLONValueType.Nil:
                    {
                        return LuaValue.Nil;
                    }
                case BLONValueType.False:
                    {
                        return LuaValue.False;
                    }
                case BLONValueType.True:
                    {
                        return LuaValue.True;
                    }
                case BLONValueType.Zero:
                    {
                        return new LuaValue(0L);
                    }
                case BLONValueType.One:
                    {
                        return new LuaValue(1L);
                    }
                case BLONValueType.UInt8:
                    {
                        return new LuaValue((long)input.ReadByte());
                    }
                case BLONValueType.UInt16:
                    {
                        return new LuaValue((long)input.ReadUInt16());
                    }
                case BLONValueType.UInt32:
                    {
                        return new LuaValue((long)input.ReadUInt32());
                    }
                case BLONValueType.UInt8_Negative:
                    {
                        return new LuaValue(-(long)input.ReadByte());
                    }
                case BLONValueType.UInt16_Negative:
                    {
                        return new LuaValue(-(long)input.ReadUInt16());
                    }
                case BLONValueType.UInt32_Negative:
                    {
                        return new LuaValue(-(long)input.ReadUInt32());
                    }
                case BLONValueType.Int64:
                    {
                        return new LuaValue(input.ReadInt64());
                    }
                case BLONValueType.Float32:
                    {
                        return new LuaValue(input.ReadSingle());
                    }
                case BLONValueType.Float64:
                    {
                        return new LuaValue(input.ReadDouble());
                    }
                case BLONValueType.EmptyString:
                    {
                        return ByteString.Empty;
                    }
                case BLONValueType.String:
                    {
                        var count = input.ReadCompactUInt();
                        return new LuaValue(DecodeString(input, count, false));
                    }
                case BLONValueType.String_Cache:
                    {
                        var count = input.ReadCompactUInt();
                        return new LuaValue(DecodeString(input, count, true));
                    }
                case BLONValueType.PreviouslyCachedString:
                    {
                        var index = input.ReadByte();
                        return new LuaValue(m_stringCache[index]);
                    }
                case BLONValueType.EmptyTable:
                    {
                        return new LuaValue(LuaTable.Empty);
                    }
                case BLONValueType.ArrayTable:
                    {
                        var count = input.ReadCompactUInt();
                        if (count > int.MaxValue)
                        {
                            throw new InvalidDataException("Table too large");
                        }
                        var t = new LuaTable((int)count);
                        for (uint k = 1; k <= count; ++k)
                        {
                            var v = DecodeValue();
                            t[k] = v;
                        }
                        return new LuaValue(t);
                    }
                case BLONValueType.Table:
                    {
                        var count = input.ReadCompactUInt();
                        if (count > int.MaxValue)
                        {
                            throw new InvalidDataException("Table too large");
                        }
                        var t = new LuaTable((int)count);
                        for (uint i = 0; i < count; ++i)
                        {
                            var k = DecodeValue();
                            var v = DecodeValue();
                            t[k] = v;
                        }
                        return new LuaValue(t);
                    }
                default:
                    {
                        throw new InvalidDataException(string.Format("Unrecognised type code: {0}", type));
                    }
            }
        }

        private ByteString DecodeString(BinaryReader input, uint size, bool cache)
        {
            if (size > int.MaxValue)
            {
                throw new InvalidDataException("String too large");
            }
            var bytes = input.ReadBytes((int)size);
            var result = new ByteString(bytes);
            if (cache)
            {
                m_stringCache[m_stringCachePosition] = result;
                m_stringCachePosition++;
            }
            return result;
        }
    }
}

