using Dan200.Core.Assets;
using Dan200.Core.Lua;
using Dan200.Core.Util;
using System.IO;

namespace Dan200.Core.Script
{
    internal class LuaScript : IBasicAsset
    {
        public static LuaScript Get(string path)
        {
            return Assets.Assets.Get<LuaScript>(path);
        }

        private string m_path;
        private ByteString m_chunkName;
        private ByteString m_byteCode;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public ByteString ChunkName
        {
            get
            {
                return m_chunkName;
            }
        }

        public ByteString ByteCode
        {
            get
            {
                return m_byteCode;
            }
        }

        private class ScriptData
        {
            public ByteString ChunkName;
            public ByteString ByteCode;
        }

        public static object LoadData(Stream stream, string path)
        {
			var source = new ByteString(stream.ReadToEnd());
			if (source.StartsWith(UnicodeUtils.UTF8_BOM))
			{
				source = source.Substring(UnicodeUtils.UTF8_BOM.Length);
			}
            var chunkName = new ByteString("@" + path + '\0');
            using (var machine = new LuaMachine())
            {
                var result = new ScriptData();
                result.ChunkName = chunkName;
                result.ByteCode = machine.Precompile(source, chunkName);
                return result;
            }
        }

        public LuaScript(string path, object data)
        {
            m_path = path;
            Load(data);
        }

        public void Dispose()
        {
            Unload();
        }

        public void Reload(object data)
        {
            Unload();
            Load(data);
        }

        private void Load(object data)
        {
            var scriptData = (ScriptData)data;
            m_chunkName = scriptData.ChunkName;
            m_byteCode = scriptData.ByteCode;
        }

        private void Unload()
        {
            m_chunkName = ByteString.Empty;
            m_byteCode = ByteString.Empty;
        }
    }
}
