using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    internal interface IFileStore
    {
        void ReloadIndex();
        bool FileExists(string path);
        Stream OpenFile(string path);
        IEnumerable<string> EnumerateFiles();
    }
}
