
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Render
{
    internal class MTLFile
    {
        internal class MTLMaterial
        {
            public string Name;
            public string DiffuseMap;
            public string SpecularMap;
            public string NormalMap;
            public string EmissiveMap;
        }

        public readonly List<MTLMaterial> Materials;

        public MTLFile()
        {
            Materials = new List<MTLMaterial>();
        }

        public void Parse(TextReader reader)
        {
            // For each line:
            string line;
            var whitespace = new char[] { ' ', '\t' };
            MTLMaterial currentMaterial = null;
            while ((line = reader.ReadLine()) != null)
            {
                // Strip comment
                var commentIdx = line.IndexOf('#');
                if (commentIdx >= 0)
                {
                    line = line.Substring(0, commentIdx);
                }

                // Segment
                var parts = line.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }

                // Parse
                var type = parts[0].ToLowerInvariant();
                switch (type)
                {
                    case "newmtl":
                        {
                            var name = parts[1];
                            currentMaterial = new MTLMaterial();
                            currentMaterial.Name = name;
                            Materials.Add(currentMaterial);
                            break;
                        }
                    case "map_kd":
                        {
                            var path = parts[1];
                            currentMaterial.DiffuseMap = path;
                            break;
                        }
                    case "map_ks":
                        {
                            var path = parts[1];
                            currentMaterial.SpecularMap = path;
                            break;
                        }
                    case "norm":
                        {
                            var path = parts[1];
                            currentMaterial.NormalMap = path;
                            break;
                        }
                    case "map_ke":
                        {
                            var path = parts[1];
                            currentMaterial.EmissiveMap = path;
                            break;
                        }
                }
            }
        }
    }
}
