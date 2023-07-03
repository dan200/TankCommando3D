using Dan200.Core.Assets;
using System.IO;
using System.Text;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Render.OpenGL;

namespace Dan200.Core.Render
{
    internal class Effect : IBasicAsset
    {
        public static Effect Get(string path)
        {
            return Assets.Assets.Get<Effect>(path);
        }

        private string m_path;
        private string m_vertexShaderPath;
        private string m_fragmentShaderPath;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string VertexShaderPath
        {
            get
            {
                return m_vertexShaderPath;
            }
        }

        public string FragmentShaderPath
        {
            get
            {
                return m_fragmentShaderPath;
            }
        }

		public static object LoadData(Stream stream, string path)
		{
			var decoder = new LONDecoder(stream);
			return decoder.DecodeValue().GetTable();
		}

        public Effect(string path, object data)
        {
            m_path = path;
            Load(data);
        }

        public void Reload(object data)
        {
            Unload();
            Load(data);
        }

        public void Dispose()
        {
            Unload();
        }

        private void Load(object data)
        {
            // Load details
			var table = (LuaTable)data;
			m_vertexShaderPath = table.GetString("VertexShader");
			m_fragmentShaderPath = table.GetString("FragmentShader");
        }

        private void Unload()
        {
        }
    }
}
