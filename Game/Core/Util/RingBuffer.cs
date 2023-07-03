using System;
using System.IO;
using System.Runtime.InteropServices;
using Dan200.Core.Main;

namespace Dan200.Core.Util
{
    internal class RingBuffer
    {
        internal class Reader : Stream
        {
            private RingBuffer m_owner;

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public Reader(RingBuffer owner)
            {
                m_owner = owner;
            }

            public override int ReadByte()
            {
                return m_owner.ReadByte();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return m_owner.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void WriteByte(byte value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
        }

        internal class Writer : Stream
        {
            private RingBuffer m_owner;

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public Writer(RingBuffer owner)
            {
                m_owner = owner;
            }

            public override int ReadByte()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void WriteByte(byte value)
            {
                m_owner.WriteByte(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                m_owner.Write(buffer, offset, count);
            }

            public override void Flush()
            {
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
        }

        private byte[] m_data;
        private int m_head;
        private int m_tail;

        public int BytesAvailable
        {
            get
            {
                return (m_head >= m_tail) ? (m_head - m_tail) : (m_head + (m_data.Length - m_tail));
            }
        }

        public int Capacity
        {
            get
            {
                return m_data.Length - 1;
            }
        }

        public RingBuffer(int initialCapacity = 4095)
        {
            m_data = new byte[initialCapacity + 1];
            m_head = 0;
            m_tail = 0;
        }

        public Reader OpenForRead()
        {
            return new Reader(this);
        }

        public Writer OpenForWrite()
        {
            return new Writer(this);
        }

        public int ReadByte()
        {
            if (m_head != m_tail)
            {
                var b = m_data[m_tail];
                m_tail = (m_tail + 1) % m_data.Length;
                return b;
            }
            return -1;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            App.Assert(offset >= 0 && count >= 0 && offset + count <= buffer.Length);

            // Calculate how many bytes we can read
            count = System.Math.Min(count, BytesAvailable);
            if (count == 0)
            {
                return 0;
            }

            // Read from the array and move the tail
            int newTail = (m_tail + count) % m_data.Length;
            if (newTail > m_tail)
            {
                Buffer.BlockCopy(m_data, m_tail, buffer, offset, count);
            }
            else
            {
                int count0 = m_data.Length - m_tail;
                int count1 = newTail;
                Buffer.BlockCopy(m_data, m_tail, buffer, offset, count0);
                Buffer.BlockCopy(m_data, 0, buffer, offset + count0, count1);
            }
            m_tail = newTail;
            return count;
        }

        private void Expand(int newSize)
        {
            var newData = new byte[newSize];
            if (m_head >= m_tail)
            {
                Buffer.BlockCopy(m_data, m_tail, newData, m_tail, m_head - m_tail);
                m_data = newData;
            }
            else
            {
                int newTail = newData.Length - (m_data.Length - m_tail);
                Buffer.BlockCopy(m_data, m_tail, newData, newTail, m_data.Length - m_tail);
                Buffer.BlockCopy(m_data, 0, newData, 0, m_head);
                m_data = newData;
                m_tail = newTail;
            }
        }

        private void MakeSpaceForBytes(int count)
        {
            // Calculate the space needed
            if (BytesAvailable + count >= m_data.Length) // Always keep at least 1 byte free so we can tell the difference between full and empty
            {
                // Expand the buffer
                int newSize = m_data.Length * 2;
                while (BytesAvailable + count >= newSize)
                {
                    newSize *= 2;
                }
                Expand(newSize);
            }
        }

        public void WriteByte(byte value)
        {
            // Make space
            MakeSpaceForBytes(1);

            // Write to the head
            m_data[m_head] = value;
            m_head = (m_head + 1) % m_data.Length;
        }

        public void Write(ByteString str)
        {
            if(str.Array != null)
            {
                Write(str.Array, (int)str.Offset, str.Length);
            }
            else
            {
                var temp = ByteString.Temp(str.Length);
                Marshal.Copy(str.Offset, temp.Array, (int)temp.Offset, temp.Length);
                Write(temp.Array, (int)temp.Offset, temp.Length);
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            App.Assert(offset >= 0 && count >= 0 && offset + count <= buffer.Length);
            if (count == 0)
            {
                return;
            }

            // Make space
            MakeSpaceForBytes(count);

            // Write to the array and move the head
            var newHead = (m_head + count) % m_data.Length;
            if (newHead > m_head)
            {
                Buffer.BlockCopy(buffer, offset, m_data, m_head, count);
            }
            else
            {
                int count0 = m_data.Length - m_head;
                int count1 = newHead;
                Buffer.BlockCopy(buffer, offset, m_data, m_head, count0);
                Buffer.BlockCopy(buffer, offset + count0, m_data, 0, count1);
            }
            m_head = newHead;
        }

        public int IndexOf(byte b)
        {
            return IndexOf(b, 0, BytesAvailable);
        }

        public int IndexOf(byte b, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= BytesAvailable);
            if (m_head >= m_tail)
            {
                // Data is in one contiguous section
                int end = m_tail + start + count;
                for (int i = m_tail + start; i < end; ++i)
                {
                    if (m_data[i] == b)
                    {
                        return i - m_tail;
                    }
                }
            }
            else
            {
                // Data is split between two sections
                int firstPartLength = m_data.Length - m_tail;
                if (start < firstPartLength)
                {
                    // First section
                    int end = System.Math.Min(m_tail + start + count, m_data.Length);
                    for (int i = m_tail + start; i < end; ++i)
                    {
                        if (m_data[i] == b)
                        {
                            return i - m_tail;
                        }
                    }
                }
                if (start + count > firstPartLength)
                {
                    // Second section
                    int end = start + count - firstPartLength;
                    for (int i = start - firstPartLength; i < end; ++i)
                    {
                        if (m_data[i] == b)
                        {
                            return i + firstPartLength;
                        }
                    }
                }
            }
            return -1;
        }

        public void Clear()
        {
            m_head = 0;
            m_tail = 0;
        }
    }
}
