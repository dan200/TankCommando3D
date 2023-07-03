#if STEAM
using Dan200.Core.Assets;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Core.Network.Steamworks
{
    internal class SteamRemoteStorageFileStore : IWritableFileStore
    {
        public SteamRemoteStorageFileStore()
        {
        }

        public void ReloadIndex()
        {
        }

        public bool FileExists(string path)
        {
            return SteamRemoteStorage.FileExists(path);
        }

        public Stream OpenFile(string path)
        {
            byte[] bytes = new byte[SteamRemoteStorage.GetFileSize(path)];
            int bytesRead = SteamRemoteStorage.FileRead(path, bytes, bytes.Length);
            return new MemoryStream(bytes, 0, bytesRead);
        }

        public IEnumerable<string> EnumerateFiles()
        {
            int count = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < count; ++i)
            {
                int size;
                string file = SteamRemoteStorage.GetFileNameAndSize(i, out size);
                yield return file;
            }
        }

        public void SaveFile(string path, byte[] bytes)
        {
            SteamRemoteStorage.FileWrite(path, bytes, bytes.Length);
        }

        public void DeleteFile(string path)
        {
            SteamRemoteStorage.FileDelete(path);
        }
    }
}
#endif
