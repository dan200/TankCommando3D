using System;
using System.IO;
using System.Runtime.InteropServices;
using Dan200.Core.Lua;

namespace Dan200.Core.Util
{
    internal static class StreamExtensions
    {
        public static int ReadAll(this Stream stream, byte[] buffer)
        {
            return stream.ReadAll(buffer, 0, buffer.Length);
        }

        public static int ReadAll(this Stream stream, byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                var bytesRead = stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    break;
                }
                else
                {
                    totalBytesRead += bytesRead;
                }
            }
            return totalBytesRead;
        }

        public static byte[] ReadToEnd(this Stream stream)
        {
            if (stream.CanSeek)
            {
                var bytes = new byte[stream.Length - stream.Position];
                stream.ReadAll(bytes, 0, bytes.Length);
                return bytes;
            }
            else
            {
                var memory = new MemoryStream(4096);
                var buffer = new byte[4096];
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        memory.Write(buffer, 0, bytesRead);
                    }
                    else
                    {
                        break;
                    }
                }
                return memory.ToArray();
            }
        }

		public unsafe static void Write(this Stream stream, ByteString str)
		{
			if (str.Array != null)
			{
				stream.Write(str.Array, (int)str.Offset, str.Length);
			}
			else
			{
				var temp = ByteString.Temp(str.Length);
				Marshal.Copy(str.Offset, temp.Array, (int)temp.Offset, temp.Length);
				stream.Write(temp.Array, (int)temp.Offset, temp.Length);
			}
		}
    }
}

