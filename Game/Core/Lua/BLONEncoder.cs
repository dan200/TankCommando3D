using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Core.Lua
{
    internal class BLONEncoder : ILuaEncoder
    {
        private BinaryWriter m_output;
        private ByteString[] m_stringCache;
        private byte m_stringCachePosition;
        private Dictionary<ByteString, byte> m_reverseStringCache;

        public bool EncodeDoubleAsFloat = false;

		public BLONEncoder(Stream output) : this(new BinaryWriter(output, Encoding.UTF8))
		{
		}

        public BLONEncoder(BinaryWriter output)
        {
            m_output = output;
            m_stringCache = new ByteString[256];
            m_stringCachePosition = 0;
            m_reverseStringCache = new Dictionary<ByteString, byte>(StructComparer<ByteString>.Instance);
        }

        public void EncodeComment(string comment)
        {
            // No comments in binary files
        }

        public void Encode(LuaValue value)
        {
            WriteValue(value);
        }

        private void WriteValue(LuaValue value)
        {
            var output = m_output;
            if (value.IsNil())
            {
                output.Write((byte)BLONValueType.Nil);
            }
            else if (value.IsBool())
            {
                var b = value.GetBool();
                if (b)
                {
                    output.Write((byte)BLONValueType.True);
                }
                else
                {
                    output.Write((byte)BLONValueType.False);
                }
            }
            else if (value.IsNumber())
            {
                if (value.IsInteger())
                {
                    var n = value.GetLong();
                    if (n >= 0)
                    {
                        if (n == 0)
                        {
                            output.Write((byte)BLONValueType.Zero);
                        }
                        else if (n == 1)
                        {
                            output.Write((byte)BLONValueType.One);
                        }
                        else if (n <= Byte.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt8);
                            output.Write((byte)n);
                        }
                        else if (n <= UInt16.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt16);
                            output.Write((ushort)n);
                        }
                        else if (n <= UInt32.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt32);
                            output.Write((uint)n);
                        }
                        else
                        {
                            output.Write((byte)BLONValueType.Int64);
                            output.Write(n);
                        }
                    }
                    else
                    {
                        if (n >= -Byte.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt8_Negative);
                            output.Write((byte)-n);
                        }
                        else if (n >= -UInt16.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt16_Negative);
                            output.Write((ushort)-n);
                        }
                        else if (n >= -UInt32.MaxValue)
                        {
                            output.Write((byte)BLONValueType.UInt32_Negative);
                            output.Write((uint)-n);
                        }
                        else
                        {
                            output.Write((byte)BLONValueType.Int64);
                            output.Write(n);
                        }
                    }
                }
                else
                {
                    var d = value.GetDouble();
                    if (EncodeDoubleAsFloat && d >= float.MinValue && d <= float.MaxValue)
                    {
                        output.Write((byte)BLONValueType.Float32);
                        output.Write((float)d);
                    }
                    else
                    {
                        output.Write((byte)BLONValueType.Float64);
                        output.Write(d);
                    }
                }
            }
            else if (value.IsString())
            {
                byte cacheIndex;
                var str = value.IsByteString() ? value.GetByteString() : ByteString.Temp(value.GetString());
                if(str.Length == 0)
                {
                    output.Write((byte)BLONValueType.EmptyString);
                }
                else if (m_reverseStringCache.TryGetValue(str, out cacheIndex))
                {
                    output.Write((byte)BLONValueType.PreviouslyCachedString);
                    output.Write(cacheIndex);
                }
                else
                {
                    if (TryCacheString(str))
                    {
                        output.Write((byte)BLONValueType.String_Cache);
                    }
                    else
                    {
                        output.Write((byte)BLONValueType.String);
                    }
                    output.WriteCompactUInt((uint)str.Length);
                    output.Write(str);
                }
            }
            else if (value.IsTable())
            {
                var t = value.GetTable();
                if (t.Count == 0)
                {
                    output.Write((byte)BLONValueType.EmptyTable);
                }
                else if(t.Count == t.ArrayLength)
                {
                    output.Write((byte)BLONValueType.ArrayTable);
                    output.WriteCompactUInt((uint)t.ArrayLength);
                    for (uint n=1; n<=t.ArrayLength; ++n)
                    {
                        WriteValue(t[n]);
                    }
                }
                else
                {
                    output.Write((byte)BLONValueType.Table);
                    output.WriteCompactUInt((uint)t.Count);
                    foreach (var pair in t)
                    {
                        WriteValue(pair.Key);
                        WriteValue(pair.Value);
                    }
                }
            }
            else
            {
                throw new InvalidDataException(string.Format("Cannot encode type {0}", value.GetTypeName()));
            }
        }

        private bool TryCacheString(ByteString s)
        {
            if (s.Length <= 32)
            {
                var existing = m_stringCache[m_stringCachePosition];
                m_reverseStringCache.Remove(existing);
                m_stringCache[m_stringCachePosition] = s.MakePermanent();
                m_reverseStringCache.Add(m_stringCache[m_stringCachePosition], m_stringCachePosition);
                m_stringCachePosition++;
                return true;
            }
            return false;
        }
    }
}
