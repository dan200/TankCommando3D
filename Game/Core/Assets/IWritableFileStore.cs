namespace Dan200.Core.Assets
{
    internal interface IWritableFileStore : IFileStore
    {
        void SaveFile(string path, byte[] bytes);
        void DeleteFile(string path);
    }
}

