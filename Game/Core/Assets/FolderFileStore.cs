using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    internal class FolderFileStore : IWritableFileStore
    {
        private string m_rootPath;

        public string Path
        {
            get
            {
                return m_rootPath;
            }
        }

        public FolderFileStore(string rootPath)
        {
            m_rootPath = rootPath;
        }

        public void ReloadIndex()
        {
        }

        public bool FileExists(string path)
        {
            string fullPath = Resolve(path);
            return File.Exists(fullPath);
        }

        public Stream OpenFile(string path)
        {
            string fullPath = Resolve(path);
            return File.OpenRead(fullPath);
        }

        private IEnumerable<string> EnumerateFiles(string fullPath, string localPath)
        {
            foreach (var fullSubPath in Directory.GetFileSystemEntries(fullPath))
            {
                var fullLocalPath = AssetPath.Combine(localPath, System.IO.Path.GetFileName(fullSubPath));
                if (Directory.Exists(fullSubPath))
                {
                    foreach (var filePath in EnumerateFiles(fullSubPath, fullLocalPath))
                    {
                        yield return filePath;
                    }
                }
                else
                {
                    yield return fullLocalPath;
                }
            }
        }

        public IEnumerable<string> EnumerateFiles()
        {
			if (Directory.Exists(m_rootPath))
			{
				foreach (var filePath in EnumerateFiles(m_rootPath, ""))
				{
					yield return filePath;
				}
			}
        }

        public void SaveFile(string path, byte[] bytes)
        {
            string fullPath = Resolve(path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, bytes);
        }

        public void DeleteFile(string path)
        {
            string fullPath = Resolve(path);
            File.Delete(fullPath);
        }

        private string Resolve(string path)
        {
            return System.IO.Path.Combine(
                m_rootPath,
                path.Replace('/', System.IO.Path.DirectorySeparatorChar)
            );
        }
    }
}

