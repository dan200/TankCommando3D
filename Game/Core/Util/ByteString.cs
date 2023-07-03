using Dan200.Core.Main;
using Dan200.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Dan200.Core.Util
{
    internal unsafe struct ByteString : IEquatable<ByteString>, IEnumerable<byte>
    {		
        private const int TEMP_STRING_BUFFER_SIZE = 1024;
        private static Dictionary<string, ByteString> s_internedStrings = new Dictionary<string, ByteString>();
        [ThreadStatic]
        private static byte[] s_tempStringBuffer;

        public static ByteString Empty = Intern("");

        public static ByteString Intern(string str)
        {
            ByteString result;
            lock (s_internedStrings)
            {
                if (!s_internedStrings.TryGetValue(str, out result))
                {
					result = new ByteString(str, Encoding.UTF8);
                    s_internedStrings[str] = result;
                }
            }
            return result;
        }

        public static ByteString Temp(int length)
        {
			App.Assert(length >= 0);
            if (length <= TEMP_STRING_BUFFER_SIZE)
            {
                if (s_tempStringBuffer == null)
                {
                    s_tempStringBuffer = new byte[TEMP_STRING_BUFFER_SIZE];
                }
				return new ByteString(s_tempStringBuffer, 0, length);
            }
            else
            {
                var bytes = new byte[length];
	            return new ByteString(bytes, 0, length);
            }
        }

		public static ByteString Temp(string str)
		{
			return Temp(str, Encoding.UTF8);
		}

		public static ByteString Temp(string str, Encoding encoding)
        {
            int bytesNeeded = encoding.GetMaxByteCount(str.Length);
            if (bytesNeeded > TEMP_STRING_BUFFER_SIZE)
            {
                bytesNeeded = encoding.GetByteCount(str);
            }
            var buffer = Temp(bytesNeeded);
			App.Assert(buffer.Array != null);
			int bytesUsed = encoding.GetBytes(str, 0, str.Length, buffer.Array, (int)buffer.Offset);
            return buffer.Substring(0, bytesUsed);
        }

		public struct Bytes : IDisposable
		{
			private readonly GCHandle m_handle;
			public readonly byte* Data;
			public readonly int Length;

			public Bytes(byte[] bytes) : this(bytes, 0, bytes.Length)
			{
			}

			public Bytes(byte[] bytes, int start, int length)
			{
				App.Assert(bytes != null && start >= 0 && length >= 0 && start + length <= bytes.Length);
				m_handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				Data = (byte*)m_handle.AddrOfPinnedObject() + start;
				Length = length;
			}

			public Bytes(IntPtr bytes, int length)
			{
                m_handle = default(GCHandle);
				Data = (byte*)bytes;
				Length = length;
			}

			public void Dispose()
			{
				if (m_handle.IsAllocated)
				{
					m_handle.Free();
				}
			}
		}

        public struct ByteEnumerator : IEnumerator<byte>
        {
            private ByteString m_string;
            private int m_pos;
            private byte m_current;

            public byte Current
            {
                get
                {
                    return m_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public ByteEnumerator(ByteString str)
            {
                m_string = str;
                m_pos = -1;
                m_current = 0;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                m_pos++;
                if (m_pos < m_string.Length)
                {
                    m_current = m_string[m_pos];
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                m_pos = -1;
            }
        }

		public readonly byte[] Array;
		public readonly IntPtr Offset;
		public readonly int Length;

        public byte this[int i]
        {
            get
            {
                App.Assert(i >= 0 && i < Length);
				if (Array != null)
				{
					return Array[(int)Offset + i];
				}
				else
				{
					return *((byte*)Offset + i);
				}
            }
            set
            {
                App.Assert(i >= 0 && i < Length);
                if (Array != null)
                {
                    Array[(int)Offset + i] = value;
                }
                else
                {
                    *((byte*)Offset + i) = value;
                }
            }
        }

		public ByteString(string str) : this(str, Encoding.UTF8)
		{
		}

		public ByteString(string str, Encoding encoding)
        {
			App.Assert(str != null);
			Array = encoding.GetBytes(str);
			Offset = IntPtr.Zero;
            Length = Array.Length;
        }

		public ByteString(params byte[] bytes) : this(bytes, 0, bytes.Length)
        {
        }

        public ByteString(byte[] bytes, int start, int length)
        {
            App.Assert(bytes != null && start >= 0 && length >= 0 && start + length <= bytes.Length);
            Array = bytes;
			Offset = (IntPtr)start;
            Length = length;
        }

        public ByteString(byte* bytes)
        {
            int len = 0;
            while (bytes[len] != 0)
            {
                ++len;
            }
			Array = null;
			Offset = (IntPtr)bytes;
			Length = len;
        }

        public ByteString(byte* bytes, int length)
        {
			App.Assert(bytes != null && length >= 0);
			Array = null;
			Offset = (IntPtr)bytes;
			Length = length;
        }

		public Bytes Lock()
		{
			if (Array != null)
			{
				return new Bytes(Array, (int)Offset, Length);
			}
			else
			{
				return new Bytes(Offset, Length);
			}
		}

        public bool IsNullTerminated()
        {
            return IndexOf((byte)'\0') == Length - 1;
        }

        public int IndexOf(byte b)
        {
            return IndexOf(b, 0, Length);
        }

        public int IndexOf(byte b, int start)
        {
            return IndexOf(b, start, Length - start);
        }

        public int IndexOf(byte b, int start, int length)
        {
            App.Assert(start >= 0 && length >= 0 && start + length <= Length);
			using (var bytes = Lock())
			{
				for (int i = start; i < length; ++i)
				{
					if (bytes.Data[i] == b)
					{
						return i;
					}
				}
				return -1;
			}
        }

		public bool StartsWith(ByteString other)
		{
			if (other.Length <= Length)
			{
				using (var bytes = Lock())
				using (var obytes = other.Lock())
				{
					byte* data = bytes.Data;
					byte* odata = obytes.Data;
					for (int i = 0; i < obytes.Length; ++i)
					{
						if (data[i] != odata[i])
						{
							return false;
						}
					}
				}
				return true;
			}
			return false;
		}

        public ByteString Substring(int start)
        {
            return Substring(start, Length - start);
        }

        public ByteString Substring(int start, int length)
        {
            App.Assert(start >= 0 && length >= 0 && start + length <= Length);
			if (Array != null)
			{
				return new ByteString(Array, (int)Offset + start, length);
			}
			else
			{
				return new ByteString((byte*)Offset + start, length);
			}
        }

        public ByteString Copy()
        {
            var array = new byte[Length];
            if (Array != null)
            {
                Buffer.BlockCopy(Array, (int)Offset, array, 0, array.Length);
            }
            else
            {
                Marshal.Copy(Offset, array, 0, array.Length);
            }
            return new ByteString(array);
        }

        public ByteString MakeCompact()
        {
            if(Array == null || (Array != null && (int)Offset == 0 && Length == Array.Length))
            {
                return this;
            }
            else
            {
                return Copy();
            }
        }

        public ByteString MakePermanent()
        {
            if(Array == s_tempStringBuffer || Array == null)
            {
                return Copy();
            }
            else
            {
                return this;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ByteString)
            {
                return Equals((ByteString)obj);
            }
            return false;
        }

		public override string ToString()
		{
			return ToString(Encoding.UTF8);
		}

		public string ToString(Encoding encoding)
        {
			if (Array != null)
			{
				return encoding.GetString(Array, (int)Offset, Length);
			}
			else
			{
				var temp = ByteString.Temp(Length);
				App.Assert(temp.Array != null);
				Marshal.Copy(Offset, temp.Array, (int)temp.Offset, Length);
				return encoding.GetString(temp.Array, (int)temp.Offset, temp.Length);
			}
        }

        public override int GetHashCode()
        {
            int hash = 0;
			using (var bytes = Lock())
			{
				var pData = bytes.Data;
				for (int i = 0; i < bytes.Length; ++i)
				{
					hash = hash * 31 + pData[i];
				}
			}
            return hash;
        }

        public bool Equals(ByteString other)
        {
            if (other.Length == Length)
            {
				using (var bytes = Lock())
				using (var obytes = other.Lock())
				{
					int length = bytes.Length;
					byte* data = bytes.Data;
					byte* odata = obytes.Data;
					if (data != odata)
					{
	                    for (int i=0; i<length; ++i)
	                    {
	                        if (data[i] != odata[i])
							{
								return false;
							}
						}
					}
                }
                return true;
            }
            return false;
        }

        public static bool operator ==(ByteString a, ByteString b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ByteString a, ByteString b)
        {
            return !a.Equals(b);
        }

        public ByteEnumerator GetEnumerator()
        {
            return new ByteEnumerator(this);
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
