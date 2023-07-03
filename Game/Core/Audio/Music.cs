using System;
using Dan200.Core.Assets;

namespace Dan200.Core.Audio
{
    internal abstract class Music : IBasicAsset, IDisposable
    {
        public static Music Get(string path)
        {
            return Assets.Assets.Get<Music>(path);
        }

        public abstract string Path { get; }
        public abstract void Reload(object data);
        public abstract void Dispose();
    }
}
