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
    internal class NetworkWriter
    {
        private BinaryWriter m_writer;
        private BLONEncoder m_blon;

        public long Position
        {
            get
            {
                return m_writer.BaseStream.Position;
            }
            set
            {
                m_writer.Seek((int)value, SeekOrigin.Begin);
            }
        }

        public NetworkWriter(Stream output)
        {
			App.Assert(output.CanWrite);
            m_writer = new BinaryWriter(output, Encoding.UTF8);
            m_blon = new BLONEncoder(m_writer);
            m_blon.EncodeDoubleAsFloat = true;
        }

        public void Write(bool b)
        {
            m_writer.Write(b);
        }

        public void Write(byte n)
        {
            m_writer.Write(n);
        }

        public void Write(char c)
        {
            m_writer.Write(c);
        }

        public void Write(short n)
        {
            m_writer.Write(n);
        }

        public void Write(int n)
        {
            m_writer.Write(n);
        }

        public void Write(long n)
        {
            m_writer.Write(n);
        }

        public void Write(ushort n)
        {
            m_writer.Write(n);
        }

        public void Write(uint n)
        {
            m_writer.Write(n);
        }

        public void Write(ulong n)
        {
            m_writer.Write(n);
        }

        public void Write(float f)
        {
            m_writer.Write(f);
        }

        public void Write(double d)
        {
            m_writer.Write(d);
        }

        public void WriteCompact(int n)
        {
            m_writer.WriteCompactInt(n);
        }

        public void WriteCompact(uint n)
        {
            m_writer.WriteCompactUInt(n);
        }

        public void WriteEnum<TEnum>(TEnum e) where TEnum : struct, IConvertible
        {
            WriteCompact(EnumConverter.ToInt(e));
        }

        public void Write(byte[] b)
        {
            Write(b, 0, b.Length);
        }

        public void Write(byte[] b, int start, int length)
        {
            m_writer.Write(b, start, length);
        }

        public void Write(string str)
        {
            var luaStr = ByteString.Temp(str);
            m_writer.WriteCompactUInt((uint)luaStr.Length);
            m_writer.Write(luaStr);
        }

        public void Write(Vector2 v)
        {
            m_writer.Write(v.X);
            m_writer.Write(v.Y);
        }

        public void Write(Vector3 v)
        {
            m_writer.Write(v.X);
            m_writer.Write(v.Y);
            m_writer.Write(v.Z);
        }

        public void Write(UnitVector3 v)
        {
            m_writer.Write(v.X);
            m_writer.Write(v.Y);
            m_writer.Write(v.Z >= 0.0f);
            // Z is derived by the reader
        }

        public void Write(Matrix4 trans)
        {
            Write(trans.Forward);
            Write(trans.Right);
            // Up is derived by the reader
            Write(trans.Position);
        }

        public void Write(Colour c)
        {
            m_writer.Write(c.RGBA);
        }

        public void Write(ByteString str)
        {
            WriteCompact((uint)str.Length);
			m_writer.Write(str);
        }

        public void Write(LuaValue value)
        {
            m_blon.Encode(value);
        }

        public void Write(Version version)
        {
            m_writer.Write((byte)version.Major);
            m_writer.Write((byte)version.Minor);
            m_writer.Write((byte)version.Build);
        }
    }
}
