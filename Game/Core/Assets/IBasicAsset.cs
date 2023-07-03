namespace Dan200.Core.Assets
{
    internal interface IBasicAsset : IAsset
    {
        // Must also have a constructor (string path, object data)
        void Reload(object data);
    }
}

