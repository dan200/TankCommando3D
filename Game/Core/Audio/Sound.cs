using System;
using Dan200.Core.Assets;

namespace Dan200.Core.Audio
{
    internal abstract class Sound : IBasicAsset, IDisposable
    {
        public static Sound Get(string path)
        {
            return Assets.Assets.Get<Sound>(path);
        }

        public abstract string Path { get; }
        public abstract float Duration { get; }
        public abstract void Reload(object data);
        public abstract void Dispose();
    }
}

