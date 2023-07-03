using System.IO;
using System.Text;
using Dan200.Core.Util;

namespace Dan200.Core.Util
{
    internal class ByteStringBuilder
    {
        private MemoryStream m_stream;

        public int Length
        {
            get
            {
                return (int)m_stream.Length;
            }
        }

        public ByteStringBuilder()
        {
            m_stream = new MemoryStream();
        }

        public ByteStringBuilder(int capacity)
        {
            m_stream = new MemoryStream(capacity);
        }

        public void Append(byte b)
        {
            m_stream.WriteByte(b);
        }

        public void Append(byte[] bytes)
        {
            m_stream.Write(bytes, 0, bytes.Length);
        }

        public void Append(byte[] bytes, int start, int count)
        {
            m_stream.Write(bytes, start, count);
        }

        public void Append(ByteString s)
        {
            m_stream.Write(s);
        }

		public void Append(string s)
		{
			Append(s, Encoding.UTF8);
		}

        public void Append(string s, Encoding encoding)
        {
            Append(ByteString.Temp(s, encoding));
        }

		public void Clear()
		{
			m_stream.Position = 0;
		}

        public ByteString ToByteString()
        {
            if (m_stream.Capacity >= 2 * m_stream.Length)
            {
				return new ByteString(m_stream.ToArray(), 0, (int)m_stream.Position); // Don't waste memory unnecessarilly
            }
            else
            {
				return new ByteString(m_stream.GetBuffer(), 0, (int)m_stream.Position);
            }
        }

		public override string ToString()
		{
			return ToString(Encoding.UTF8);
		}

		public string ToString(Encoding encoding)
		{
			return ToByteString().ToString(encoding);
        }
    }
}
