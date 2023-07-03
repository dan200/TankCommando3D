#if ANDROID
using Android.Content.Res;
using Dan200.Core.Main;
using Dan200.Core.Assets;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets.Android
{
    internal class AndroidAssetsFileStore : IFileStore
    {
        private string m_rootPath;
        private HashSet<string> m_files;
        private Dictionary<string, string[]> m_directories;

        public string Path
        {
            get
            {
                return m_rootPath;
            }
        }

        public AndroidAssetsFileStore(string rootPath)
        {
            m_rootPath = rootPath;
            m_files = new HashSet<string>();
            m_directories = new Dictionary<string, string[]>();
            ReloadIndex();
        }

        private void CollectAssets(AssetManager mgr, string path, string localPath)
        {
            if (AssetPath.GetExtension(localPath) != "")
            {
                m_files.Add(localPath);
            }
            else
            {
                var files = mgr.List(path);
                if( files.Length > 0 )
                {
                    m_directories.Add(localPath, files);
                    foreach( var fileName in files )
                    {
                        CollectAssets(mgr, System.IO.Path.Combine(path, fileName), AssetPath.Combine(localPath, fileName));
                    }
                }
            }
        }

        public void ReloadIndex()
        {
			var mgr = App.AndroidWindow.Context.Assets;
            m_files.Clear();
            m_directories.Clear();
            CollectAssets(mgr, m_rootPath, "");
        }

        public bool FileExists(string path)
        {
            return m_files.Contains(path);
        }

        private class AssetStreamOrigin : SeekableStream.IStreamOrigin
        {
            private string m_path;

            public AssetStreamOrigin( string path )
            {
                m_path = path;
            }

            public Stream Open( out long o_length )
            {
				var mgr = App.AndroidWindow.Context.Assets;
                var stream = mgr.Open(m_path, Access.Random);
                using (var fd = mgr.OpenFd(m_path))
                {
                    o_length = fd.Length;
                    return stream;
                }
            }
        }

        public Stream OpenFile(string path)
        {
            var fullPath = Resolve(path);
			var mgr = App.AndroidWindow.Context.Assets;
	        if (AssetPath.GetExtension(path) == "ogg")
            {
                var stream = mgr.Open(fullPath, Access.Random);
                if (!stream.CanSeek)
                {
                    stream = new SeekableStream( new AssetStreamOrigin(fullPath), 300000 );
                }
                return stream;
            }
            else
            {
                return mgr.Open(fullPath, Access.Streaming);
            }
        }

        public TextReader OpenTextFile(string path)
        {
            var stream = OpenFile(path);
            if( stream != null )
            {
                return new StreamReader(stream);
            }
            return null;
        }

        public bool DirectoryExists(string path)
        {
            return m_directories.ContainsKey(path);
        }

        public IEnumerable<string> ListFiles(string path)
        {
            var files = m_directories[path];
            foreach (var fileName in files)
            {
                var candidate = AssetPath.Combine(path, fileName);
                if (m_files.Contains(candidate))
                {
                    yield return fileName;
                }
            }
        }

        public IEnumerable<string> ListDirectories(string path)
        {
            var files = m_directories[path];
            foreach (var fileName in files)
            {
                var candidate = AssetPath.Combine(path, fileName);
                if (m_directories.ContainsKey(candidate))
                {
                    yield return fileName;
                }
            }
        }

        private string Resolve(string path)
        {
            return System.IO.Path.Combine( m_rootPath, path );
        }
    }
}
#endif
