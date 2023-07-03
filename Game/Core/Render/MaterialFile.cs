using Dan200.Core.Assets;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Core.Render
{
    internal class MaterialFile : IBasicAsset
    {
        public static MaterialFile Get(string path)
        {
            return Assets.Assets.Get<MaterialFile>(path);
        }

        private string m_path;
        private Dictionary<string, Material> m_materials;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            var mtlFile = new MTLFile();
            var reader = new StreamReader(stream, Encoding.UTF8);
            mtlFile.Parse(reader);
            return mtlFile;
        }

        public MaterialFile(string path, object data)
        {
            m_path = path;
            m_materials = new Dictionary<string, Material>();
            Load(data);
        }

        public void Reload(object data)
        {
            m_materials.Clear();
            Load(data);
        }

        public void Dispose()
        {
        }

        private void Load(object data)
        {
            // Get the material infos
            var mtlFile = (MTLFile)data;

            // Store the materials
            var dir = AssetPath.GetDirectoryName(m_path);
            foreach (var mtl in mtlFile.Materials)
            {
                var material = new Material();
                if (mtl.DiffuseMap != null)
                {
                    material.DiffuseTexturePath = AssetPath.Combine(dir, mtl.DiffuseMap.Replace('\\', '/'));
                }
                if (mtl.SpecularMap != null)
                {
                    material.SpecularTexturePath = AssetPath.Combine(dir, mtl.SpecularMap.Replace('\\', '/'));
                }
                if (mtl.NormalMap != null)
                {
                    material.NormalTexturePath = AssetPath.Combine(dir, mtl.NormalMap.Replace('\\', '/'));
                }
                if (mtl.EmissiveMap != null)
                {
                    material.EmissiveTexturePath = AssetPath.Combine(dir, mtl.EmissiveMap.Replace('\\', '/'));
                }
                m_materials.Add(mtl.Name, material);
            }
        }

        public IMaterial GetMaterial(string name)
        {
            Material result;
            if (m_materials.TryGetValue(name, out result))
            {
                return result;
            }
            if (m_materials.TryGetValue("default", out result))
            {
                return result;
            }
            return Material.Default;
        }
    }
}
