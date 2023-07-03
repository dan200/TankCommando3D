using System.IO;

namespace Dan200.Core.Audio.OpenAL
{
    internal class OpenALMusic : Music
    {
        private string m_path;

        public override string Path
        {
            get
            {
                return m_path;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            return null;
        }

        public OpenALMusic(string path, object data)
        {
            m_path = path;
        }

        public override void Dispose()
        {
        }

        public override void Reload(object data)
        {
            OpenALAudio.Instance.StopMusic(this);
        }

        public Stream OpenForStreaming()
        {
            return Assets.Assets.OpenStreamingAsset(m_path);
        }
    }
}

