using Dan200.Core.Modding;

namespace Dan200.Core.Assets
{
    internal class AssetSource
    {
        public string Name;
        public readonly IFileStore FileStore;
        public readonly Mod Mod;

        public AssetSource(string name, IFileStore fileStore, Mod mod = null)
        {
            Name = name;
            FileStore = fileStore;
            Mod = mod;
        }
    }
}
