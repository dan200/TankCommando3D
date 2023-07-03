namespace Dan200.Core.Render
{
    internal interface IMaterial
    {
        ITexture DiffuseTexture { get; }
        ITexture SpecularTexture { get; }
        ITexture EmissiveTexture { get; }
        ITexture NormalTexture { get; }
    }
}

