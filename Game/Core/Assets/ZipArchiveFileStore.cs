#if ZIP
using Dan200.Core.Util;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    internal class ZipArchiveFileStore : IFileStore
    {
        private string m_zipFilePath;
        private string m_rootPath;

        private DateTime m_modifiedTime;
        private ISet<string> m_files;

        public ZipArchiveFileStore(string zipFilePath, string rootPath)
        {
            m_zipFilePath = zipFilePath;
            m_rootPath = rootPath;

            m_files = new HashSet<string>();
            LoadIndex();
        }

        public void ReloadIndex()
        {
            m_files.Clear();
            if (File.Exists(m_zipFilePath))
            {
                LoadIndex();
            }
        }

        private void LoadIndex()
        {
            m_modifiedTime = new FileInfo(m_zipFilePath).LastWriteTime;
            using (var stream = File.OpenRead(m_zipFilePath))
            {
                using (var file = ZipFile.Read(stream))
                {
                    foreach (var entry in file.Entries)
                    {
                        string sanePath = Sanitize(entry.FileName);
                        if (!entry.IsDirectory)
                        {
                            m_files.Add(sanePath);
                        }
                    }
                }
            }
        }

        public bool FileExists(string path)
        {
            string fullPath = Resolve(path);
            return m_files.Contains(fullPath);
        }

        public Stream OpenFile(string path)
        {
            string fullPath = Resolve(path);
            using (var stream = File.OpenRead(m_zipFilePath))
            {
                using (var file = ZipFile.Read(stream))
                {
                    foreach (var entry in file.Entries)
                    {
                        if (!entry.IsDirectory && Sanitize(entry.FileName) == fullPath)
                        {
                            return new MemoryStream(
                                entry.OpenReader().ReadToEnd()
                            );
                        }
                    }
                }
            }
            return null;
        }

        public IEnumerable<string> EnumerateFiles()
        {
            return m_files;
        }

        private string Resolve(string path)
        {
            return AssetPath.Combine(m_rootPath, path);
        }

        private string Sanitize(string path)
        {
            string saneName = path.Replace('\\', '/');
            if (saneName.EndsWith("/"))
            {
                saneName = saneName.Substring(0, saneName.Length - 1);
            }
            return saneName;
        }
    }
}
#endif
