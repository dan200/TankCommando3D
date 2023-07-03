using Dan200.Core.Lua;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;
using System;
using System.IO;
using System.Text;
using Dan200.Core.Main;

namespace Dan200.Core.Multiplayer
{
    internal class NetworkReader
    {
        private BinaryReader m_reader;
        private BLONDecoder m_blon;

        public long Position
        {
            get
            {
                return m_reader.BaseStream.Position;
            }
            set
            {
                m_reader.BaseStream.Seek((int)value, SeekOrigin.Begin);
            }
        }

        public NetworkReader(Stream input)
        {
			App.Assert(input.CanRead);
            m_reader = new BinaryReader(input, Encoding.UTF8);
            m_blon = new BLONDecoder(m_reader);
        }

        public bool ReadBool()
        {
            return m_reader.ReadBoolean();
        }

        public byte ReadByte()
        {
            return m_reader.ReadByte();
        }

        public char ReadChar()
        {
            return m_reader.ReadChar();
        }

        public short ReadShort()
        {
            return m_reader.ReadInt16();
        }

        public int ReadInt()
        {
            return m_reader.ReadInt32();
        }

        public long ReadLong()
        {
            return m_reader.ReadInt64();
        }

        public ushort ReadUShort()
        {
            return m_reader.ReadUInt16();
        }

        public uint ReadUInt()
        {
            return m_reader.ReadUInt32();
        }

        public ulong ReadULong()
        {
            return m_reader.ReadUInt64();
        }

        public float ReadFloat()
        {
            return m_reader.ReadSingle();
        }

        public double ReadDouble()
        {
            return m_reader.ReadDouble();
        }

        public int ReadCompactInt()
        {
            return m_reader.ReadCompactInt();
        }

        public uint ReadCompactUInt()
        {
            return m_reader.ReadCompactUInt();
        }

        public TEnum ReadEnum<TEnum>(TEnum e) where TEnum : struct, IConvertible
        {
            return EnumConverter.ToEnum<TEnum>(ReadCompactInt());
        }

        public byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            ReadBytes(bytes, 0, count);
            return bytes;
        }

        public void ReadBytes(byte[] b, int start, int count)
        {
            int pos = start;
            int end = start + count;
            while (pos < end)
            {
                var numRead = m_reader.Read(b, pos, end - pos);
                if (numRead < 0)
                {
                    throw new IOException("Reached end of stream");
                }
                pos += numRead;
            }
        }

        public string ReadString()
        {
            var length = (int)m_reader.ReadCompactUInt();
            var buffer = ByteString.Temp(length);
            ReadBytes(buffer.Array, (int)buffer.Offset, length);
            return Encoding.UTF8.GetString(buffer.Array, (int)buffer.Offset, length);
        }

        public Vector2 ReadVector2()
        {
            var x = m_reader.ReadSingle();
            var y = m_reader.ReadSingle();
            return new Vector2(x, y);
        }

        public Vector3 ReadVector3()
        {
            var x = m_reader.ReadSingle();
            var y = m_reader.ReadSingle();
            var z = m_reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        public UnitVector3 ReadUnitVector3()
        {
            var x = m_reader.ReadSingle();
            var y = m_reader.ReadSingle();
            var zPositive = m_reader.ReadBoolean();
            var zSquared = 1.0f - (x * x + y * y);
            if (zSquared >= 0.0f)
            {
                var z = Mathf.Sqrt(zSquared) * (zPositive ? 1.0f : -1.0f);
                return UnitVector3.ConstructUnsafe(x, y, z);
            }
            else
            {
                return new Vector3(x, y, 0.0f).Normalise();
            }
        }

        public Matrix4 ReadTransform()
        {
            var fwd = ReadUnitVector3();
            var right = ReadUnitVector3();
            var up = fwd.Cross(right);
            var pos = ReadVector3();
            return new Matrix4(
                new Vector4(right, 0.0f),
                new Vector4(up, 0.0f),
                new Vector4(fwd, 0.0f),
                new Vector4(pos, 1.0f)
            );
        }

        public Colour ReadColour()
        {
            return new Colour(m_reader.ReadUInt32());
        }

        public ByteString ReadByteString()
        {
            var length = (int)ReadCompactUInt();
            var bytes = new byte[length];
            ReadBytes(bytes, 0, length);
            return new ByteString(bytes, 0, length);
        }

        public LuaValue ReadLuaValue()
        {
            return m_blon.DecodeValue();
        }

        public Version ReadVersion()
        {
            var major = m_reader.ReadByte();
            var minor = m_reader.ReadByte();
            var build = m_reader.ReadByte();
            return new Version(major, minor, build);
        }
    }
}
