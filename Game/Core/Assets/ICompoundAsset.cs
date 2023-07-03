namespace Dan200.Core.Assets
{
    internal interface ICompoundAsset : IAsset
    {
        // Must also have a constructor (string path)
        void Reset();
        void AddLayer(object data);
    }
}

