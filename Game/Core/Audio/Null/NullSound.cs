using System.IO;

namespace Dan200.Core.Audio.Null
{
    internal class NullSound : Sound
    {
        private string m_path;

        public override string Path
        {
            get
            {
                return m_path;
            }
        }

        public override float Duration
        {
            get
            {
                return 0.0f;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            return null;
        }

        public NullSound(string path, object data)
        {
            m_path = path;
        }

        public override void Dispose()
        {
        }

        public override void Reload(object data)
        {
        }
    }
}

