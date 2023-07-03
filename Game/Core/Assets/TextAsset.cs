using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Core.Assets
{
    internal class TextAsset : IBasicAsset
    {
        public static TextAsset Get(string path)
        {
            return Assets.Get<TextAsset>(path);
        }

        private string m_path;
        private string[] m_lines;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string[] Lines
        {
            get
            {
                return m_lines;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            var lines = new List<string>();
            var reader = new StreamReader(stream, Encoding.UTF8);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
            return lines.ToArray();
        }

        public TextAsset(string path, object data)
        {
            m_path = path;
            Load(data);
        }

        public void Reload(object data)
        {
            Load(data);
        }

        public void Dispose()
        {
        }

        private void Load(object data)
        {
            var lines = (string[])data;
            m_lines = lines;
        }
    }
}

