using Dan200.Core.Main;
using System;
using System.IO;

namespace Dan200.Core.Util
{
    internal class SeekBuffer
    {
        private Stream m_stream;
        private byte[] m_buffer;
        private long m_start;
        private int m_size;

        public long Start
        {
            get
            {
                return m_start;
            }
        }

        public long End
        {
            get
            {
                return m_start + m_size;
            }
        }

        public int Size
        {
            get
            {
                return m_size;
            }
        }

        public int Capacity
        {
            get
            {
                return m_buffer.Length;
            }
        }

        public SeekBuffer(Stream stream, int bufferSize)
        {
            m_stream = stream;
            m_buffer = new byte[bufferSize];
            m_start = 0;
            m_size = 0;
        }

        public void Reset(Stream stream)
        {
            m_stream = stream;
            m_start = 0;
            m_size = 0;
        }

        public long Advance(long count)
        {
            long bytesAdvanced = 0;
            while (bytesAdvanced < count)
            {
                int tail = (int)((m_start + m_size) % m_buffer.Length);
                int limit = (int)System.Math.Min(count - bytesAdvanced, (long)(m_buffer.Length - tail));
                int bytesRead = m_stream.ReadAll(m_buffer, tail, limit);
                m_size += bytesRead;
                bytesAdvanced += bytesRead;
                if (m_size > m_buffer.Length)
                {
                    long end = m_start + m_size;
                    m_start = end - m_buffer.Length;
                    m_size = m_buffer.Length;
                }
                if (bytesRead < limit)
                {
                    break;
                }
            }
            return bytesAdvanced;
        }

        public byte ReadByte(long srcPosition)
        {
            App.Assert(srcPosition >= Start && srcPosition < End);
            int pos = (int)(srcPosition % m_buffer.Length);
            return m_buffer[pos];
        }

        public void Read(byte[] buffer, int offset, int count, long srcPosition)
        {
            long srcEnd = srcPosition + count;
            App.Assert(srcPosition >= Start && srcEnd < End && srcEnd >= srcPosition);
            if (count > 0)
            {
                int head = (int)(srcPosition % m_buffer.Length);
                if ((m_buffer.Length - head) < count)
                {
                    int count0 = m_buffer.Length - head;
                    Buffer.BlockCopy(m_buffer, head, buffer, offset, count0);
                    Buffer.BlockCopy(m_buffer, 0, buffer, offset + count0, count - count0);
                }
                else
                {
                    Buffer.BlockCopy(m_buffer, head, buffer, offset, count);
                }
            }
        }
    }

    internal class SeekableStream : Stream
    {
        internal interface IStreamOrigin
        {
            Stream Open(out long o_length);
        }

        private IStreamOrigin m_origin;
        private Stream m_stream;
        private SeekBuffer m_buffer;
        private long m_position;
        private long m_length;

        public override bool CanRead
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

        public override long Length
        {
            get
            {
                return m_length;
            }
        }

        public override long Position
        {
            get
            {
                return m_position;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public SeekableStream(IStreamOrigin origin, int bufferSize)
        {
            m_origin = origin;
            m_stream = origin.Open(out m_length);
            m_position = 0;
            m_buffer = new SeekBuffer(m_stream, bufferSize);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                m_stream.Dispose();
            }
        }

        public override int ReadByte()
        {
            if (m_position < m_buffer.Start)
            {
                // If we read beyond the start of the buffer, we need to re-open the file and seek to the current position
                // This should be very rare!
                m_stream = m_origin.Open(out m_length);
                m_buffer.Reset(m_stream);
                m_buffer.Advance(m_position);
            }

            // If the read is beyond the end of the buffer, we need to advance it
            if (m_position >= m_buffer.End)
            {
                m_buffer.Advance((m_position + 1) - m_buffer.End);
            }

            // Read the byte from the buffer
            if (m_position < m_buffer.End)
            {
                return m_buffer.ReadByte(m_position);
            }
            return -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (m_position < m_buffer.Start)
            {
                // If we read beyond the start of the buffer, we need to re-open the file and seek to the current position
                // This should be very rare!
                m_stream = m_origin.Open(out m_length);
                m_buffer.Reset(m_stream);
                m_buffer.Advance(m_position);
            }

            int bytesRead = 0;
            while (bytesRead < count)
            {
                // For each buffer sized chunk of bytes...
                int limit = System.Math.Min(count - bytesRead, m_buffer.Capacity);

                // If the read is beyond the end of the buffer, advance the buffer
                if (m_position + limit > m_buffer.End)
                {
                    m_buffer.Advance((m_position + limit) - m_buffer.End);
                }

                // Read the bytes we need from the buffer
                var bytesAvailable = System.Math.Min(limit, (int)(m_buffer.End - m_position));
                m_buffer.Read(buffer, offset + bytesRead, bytesAvailable, m_position);
                m_position += bytesAvailable;
                bytesRead += bytesAvailable;

                // Break on EOF
                if (bytesAvailable < limit)
                {
                    break;
                }
            }
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                default:
                    break;
                case SeekOrigin.Current:
                    offset += Position;
                    break;
                case SeekOrigin.End:
                    offset += Length;
                    break;
            }

            // Just set the position, don't do anything until we actually read
            if (offset < 0)
            {
                offset = 0;
            }
            m_position = offset;
            return m_position;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
