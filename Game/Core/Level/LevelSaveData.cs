using System;
using System.Collections.Generic;
using System.IO;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Util;

namespace Dan200.Core.Level
{
	internal class LevelSaveData
	{
		public static LevelSaveData Load(Stream input)
		{
			// Load the table
			ILuaDecoder lon;
			if (App.Debug)
			{
				lon = new LONDecoder(input);
			}
			else
			{
				lon = new BLONDecoder(input);
			}
			var root = lon.DecodeValue().GetTable();

			// Decode the table
			var data = new LevelSaveData();
			data.LevelPath = root.GetString("Level");
			var systems = root.GetOptionalTable("Systems");
			if (systems != null)
			{
				foreach (var pair in systems)
				{
					var name = pair.Key.GetString();
					var id = ComponentRegistry.GetSystemID(name);
					var properties = pair.Value.GetTable();
					data.SystemProperties[id] = properties;
				}
			}
			var entities = root.GetOptionalTable("Entities");
			if (entities != null)
			{
				foreach (var pair in entities)
				{
					var id = pair.Key.GetInt();
					var entity = pair.Value.GetTable();
					var type = entity.GetString("Type");
					var properties = entity.GetOptionalTable("Properties");
					var components = entity.GetOptionalTable("Components");

					var entityData = new EntitySaveData();
					entityData.ID = id;
					entityData.Type = type;
					entityData.Properties = properties;
					entityData.Components = new BitField();
					entityData.ComponentProperties = new Dictionary<int, LuaTable>();
					if (components != null)
					{
						foreach (var pair2 in components)
						{
							var componentName = pair2.Key.GetString();
							var componentProperties = pair2.Value.GetTable();
							var componentID = ComponentRegistry.GetComponentID(componentName);
							entityData.Components[componentID] = true;
							entityData.ComponentProperties.Add(componentID, componentProperties);
						}
					}						
					data.Entities.Add(entityData);
				}
			}
			return data;
		}

		public string LevelPath;
		public Dictionary<int, LuaTable> SystemProperties;

		internal struct EntitySaveData
		{
			public int ID;
			public string Type;
			public LuaTable Properties;
			public BitField Components;
			public Dictionary<int, LuaTable> ComponentProperties;
		}
		public List<EntitySaveData> Entities;

		public LevelSaveData()
		{
			LevelPath = null;
			SystemProperties = new Dictionary<int, LuaTable>();
			Entities = new List<EntitySaveData>();
		}

		public void Save(Stream output)
		{
			// Build the lua table
			var root = new LuaTable();
			root["Level"] = LevelPath;
			if (SystemProperties.Count > 0)
			{
				var systems = new LuaTable();
				foreach (var pair in SystemProperties)
				{
					var id = pair.Key;
					var name = ComponentRegistry.GetSystemName(id);
					var properties = pair.Value;
					systems[name] = properties;
				}
				root["Systems"] = systems;
			}
			if (Entities.Count > 0)
			{
				var entities = new LuaTable();
				foreach (var e in Entities)
				{
					var entity = new LuaTable();
					entity["Type"] = e.Type;
					if (e.Properties.Count > 0)
					{
						entity["Properties"] = e.Properties;
					}
					if (e.ComponentProperties.Count > 0)
					{
						var components = new LuaTable();
						foreach (var pair in e.ComponentProperties)
						{
							var id = pair.Key;
							var name = ComponentRegistry.GetComponentName(id);
							var properties = pair.Value;
							components[name] = properties;
						}
						entity["Components"] = components;
					}
					entities[e.ID] = entity;
				}
				root["Entities"] = entities;
			}

			// Emit it
			ILuaEncoder lon;
			if (App.Debug)
			{
				lon = new LONEncoder(output);
			}
			else
			{
				var blon = new BLONEncoder(output);
				blon.EncodeDoubleAsFloat = true;
				lon = blon;
			}
			lon.Encode(root);
		}
	}
}
