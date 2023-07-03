using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    internal class EmptyFileStore : IFileStore
    {
        private static List<string> s_emptyList = new List<string>(0);

        public EmptyFileStore()
        {
        }

        public void ReloadIndex()
        {
        }

        public bool FileExists(string path)
        {
            return false;
        }

        public Stream OpenFile(string path)
        {
            return null;
        }

        public IEnumerable<string> EnumerateFiles()
        {
            return s_emptyList;
        }
    }
}

