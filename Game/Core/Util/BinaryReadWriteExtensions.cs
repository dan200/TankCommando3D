using System;
using System.IO;
using System.Runtime.InteropServices;
using Dan200.Core.Lua;
using Dan200.Core.Main;

namespace Dan200.Core.Util
{
    internal static class BinaryReadWriteExtensions
    {
        public static void WriteCompactInt(this BinaryWriter writer, int value)
        {
            uint v = (uint)value;
            writer.WriteCompactUInt(v);
        }

        public static int ReadCompactInt(this BinaryReader reader)
        {
            uint result = ReadCompactUInt(reader);
            return (int)result;
        }

        public static void WriteCompactUInt(this BinaryWriter writer, uint v)
        {
            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }
            writer.Write((byte)v);
        }

        public static uint ReadCompactUInt(this BinaryReader reader)
        {
            uint result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = reader.ReadByte();
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return result;
        }

        public unsafe static void Write(this BinaryWriter writer, ByteString str)
        {
            writer.Write(str, 0, str.Length);
        }

		public unsafe static void Write(this BinaryWriter writer, ByteString str, int start, int count)
		{
            App.Assert(start >= 0);
            App.Assert(count >= 0);
            App.Assert(start + count <= str.Length);
			if (str.Array != null)
			{
                writer.Write(str.Array, (int)str.Offset + start, count);
			}
			else
			{
                var temp = ByteString.Temp(count);
                Marshal.Copy(str.Offset + start, temp.Array, (int)temp.Offset, temp.Length);
				writer.Write(temp.Array, (int)temp.Offset, temp.Length);
			}
		}
    }
}
