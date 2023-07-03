
using Dan200.Core.Util;
using System.IO;

namespace Dan200.Core.Assets
{
    internal class BinaryAsset : IBasicAsset
    {
        public static BinaryAsset Get(string path)
        {
            return Assets.Get<BinaryAsset>(path);
        }

        private string m_path;
        private byte[] m_bytes;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public byte[] Bytes
        {
            get
            {
                return m_bytes;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            return stream.ReadToEnd();
        }

        public BinaryAsset(string path, object data)
        {
            m_path = path;
            Load(data);
        }

        public void Reload(object data)
        {
            m_bytes = null;
            Load(data);
        }

        public void Dispose()
        {
            m_bytes = null;
        }

        private void Load(object data)
        {
            var bytes = (byte[])data;
            m_bytes = bytes;
        }
    }
}

