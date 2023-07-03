using Dan200.Core.Assets;
using Dan200.Core.Lua;
using Dan200.Core.Render;
using System;
using System.IO;
using System.Text;

namespace Dan200.Core.Modding
{
    internal enum ModSource
    {
        Local,
        Editor,
        Workshop
    }

    internal class Mod
    {
        public readonly string Path;
        public readonly ModSource Source;

        public string Title;
        public Version Version;
        public Version MinimumGameVersion;
        public string Author;
        public bool AutoLoad;
        public bool Loaded;
        public ulong? SteamWorkshopID;
        public ulong? SteamUserID;

        private IFileStore m_contents;
        private AssetSource m_assets;

        public AssetSource Assets
        {
            get
            {
                return m_assets;
            }
        }

        public Mod(string path, ModSource source)
        {
            Path = path;
            Source = source;
#if ZIP
			if (File.Exists(Path) && Path.EndsWith(".zip", StringComparison.InvariantCulture))
            {
                m_contents = new ZipArchiveFileStore(Path, "");
                m_assets = new AssetSource("Untitled Mod", new ZipArchiveFileStore(Path, "assets"), this);
            }
            else
#endif
            if (Directory.Exists(Path))
            {
                m_contents = new FolderFileStore(Path);
                m_assets = new AssetSource("Untitled Mod", new FolderFileStore(System.IO.Path.Combine(Path, "assets")), this);
            }
            else
            {
                m_contents = null;
                m_assets = new AssetSource("Untitled Mod", new EmptyFileStore(), this);
            }
            ReloadInfo();
            Loaded = false;
        }

        public ITexture LoadIcon(bool filter)
        {
            // Load Icon
            if (m_contents != null && m_contents.FileExists("icon.png"))
            {
                using (var file = m_contents.OpenFile("icon.png"))
                {
					var bitmap = new Bitmap(file);
					var texture = new BitmapTexture(bitmap);
                    texture.Filter = filter;
                    return texture;
                }
            }
            return null;
        }

        public void ReloadInfo()
        {
            // Set default info
            Title = "Untitled Mod";
            Version = new Version(1, 0, 0);
            MinimumGameVersion = new Version(0, 0, 0);
            Author = null;
            AutoLoad = false;
            SteamWorkshopID = null;
            SteamUserID = null;

            // Reload the index
            if (m_contents != null)
            {
                m_contents.ReloadIndex();
            }

            // Parse info.txt
            var infoPath = "info.txt";
            if (m_contents != null && m_contents.FileExists(infoPath))
            {
                LuaTable table;
                using (var stream = m_contents.OpenFile(infoPath))
                {
                    var lon = new LONDecoder(stream);
                    table = lon.DecodeValue().GetTable();
                }

                Title = table.GetOptionalString("Title", Title);
                Version = Version.Parse( table.GetOptionalString("Version", Version.ToString()) );
                MinimumGameVersion = Version.Parse( table.GetOptionalString("GameVersion", MinimumGameVersion.ToString()) );
                Author = table.GetOptionalString("Author", null);
                AutoLoad = table.GetOptionalBool("AutoLoad", false);
                if (!table.IsNil("SteamWorkshopID"))
                {
                    SteamWorkshopID = ulong.Parse( table.GetString("SteamWorkshopID") );
                }
                if (!table.IsNil("SteamUserID"))
                {
                    SteamUserID = ulong.Parse( table.GetString("SteamUserID") );
                }
            }

            // Reload the index
            m_assets.Name = Title;
            m_assets.FileStore.ReloadIndex();
        }

        public void SaveInfo()
        {
            // Populate the info table
            var table = new LuaTable();
            table["Title"] = Title;
            table["Version"] = Version.ToString();
            table["GameVersion"] = MinimumGameVersion.ToString();
            table["Author"] = Author;
            table["AutoLoad"] = AutoLoad;
            if (SteamWorkshopID.HasValue)
            {
                table["SteamWorkshopID"] = SteamWorkshopID.Value.ToString();
            }
            if (SteamUserID.HasValue)
            {
                table["SteamUserID"] = SteamUserID.Value.ToString();
            }

            // Save the table out
            var infoPath = System.IO.Path.Combine(Path, "info.txt");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(infoPath));
            using (var output = File.Open(infoPath, FileMode.Create))
            {
                var lon = new LONEncoder(output);
                lon.Encode(table);
            }
        }
    }
}

