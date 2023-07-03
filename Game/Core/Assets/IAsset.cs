using System;

namespace Dan200.Core.Assets
{
    // Implement IBasicAsset or ICompoundAsset rather than this directly
    internal interface IAsset : IDisposable
    {
        string Path { get; }
    }
}
