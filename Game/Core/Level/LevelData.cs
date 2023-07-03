using Dan200.Core.Assets;
using Dan200.Core.Lua;
using Dan200.Core.Util;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Level
{
    internal class LevelData : IBasicAsset
    {
        public static LevelData Get(string path)
        {
            return Assets.Assets.Get<LevelData>(path);
        }

        private string m_path;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string MusicPath { get; set; }
        public string SkyPath { get; set; }
        public string ScriptPath { get; set; }

        internal struct EntityData
        {
            public string Type;
            public LuaTable Properties;
        }
        public readonly List<EntityData> Entities = new List<EntityData>();

        public static object LoadData(Stream stream, string path)
        {
            var decoder = new LONDecoder(stream);
            decoder.AddMacro("Vector2", LONMacros.Vector2);
            decoder.AddMacro("Vector3", LONMacros.Vector3);
            decoder.AddMacro("Colour", LONMacros.Colour);
            return decoder.DecodeValue().GetTable();
        }

        public LevelData(string path)
        {
            m_path = path;
        }

        public LevelData(string path, object data)
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

        public void Save(string path)
        {
            // Construct the table
            var table = new LuaTable();
            table["Music"] = MusicPath;
            table["Sky"] = SkyPath;
            table["Script"] = ScriptPath;

            var entitiesTables = new LuaTable(Entities.Count);
            foreach(var entity in Entities)
            {
                var entityTable = new LuaTable();
                entityTable["Type"] = entity.Type;

                var name = entity.Properties.GetOptionalString("Name");
                foreach(var pair in entity.Properties)
                {
                    if(pair.Key != "Name")
                    {
                        entityTable[pair.Key] = pair.Value;
                    }
                }

                if(name != null)
                {
                    entitiesTables[name] = entityTable;
                }
                else
                {
                    entitiesTables.Insert(entityTable);
                }
            }
            table["Entities"] = entitiesTables;

            // Save
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            using (var stream = File.Open(path, FileMode.Create))
            {
                var encoder = new LONEncoder(stream);
                encoder.Encode(table);
            }
        }

        private void Load(object data)
        {
            var table = (LuaTable)data;
            MusicPath = table.GetOptionalString("Music");
            SkyPath = table.GetOptionalString("Sky");
            ScriptPath = table.GetOptionalString("Script");

            Entities.Clear();
            if (!table.IsNil("Entities"))
            {
                var entities = table.GetTable("Entities");
                Entities.Capacity = entities.Count;
                foreach (var pair in entities)
                {
                    var entity = new EntityData();
                    entity.Properties = pair.Value.GetTable();
					if (pair.Key.IsString())
					{
						entity.Properties["Name"] = pair.Key.GetString();
					}
                    entity.Type = entity.Properties.GetString("Type");
                    entity.Properties["Type"] = LuaValue.Nil;
                    Entities.Add(entity);
                }
            }
        }

        private void Unload()
        {
        }
    }
}

