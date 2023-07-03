using Dan200.Core.Assets;
using Dan200.Core.Lua;
using System.Collections.Generic;
using System.IO;
using Dan200.Core.Util;
using System.Text;
using System;
using System.Collections;
using Dan200.Core.Main;
using Dan200.Core.Components;
using System.Globalization;
using Dan200.Core.Components.Core;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Level
{
    internal class EntityPrefab : IBasicAsset
    {
        public static EntityPrefab Get(string path)
        {
            return Assets.Assets.Get<EntityPrefab>(path);
        }

        private class EntityData
        {
            public string BasePrefab;
            public int Parent;
            public string Name;
            public string DebugPath;

            public BitField Components;
            public Dictionary<int, LuaTable> ComponentProperties;
            public bool HasChildren;

            public EntityData()
            {
                BasePrefab = null;
                Parent = -1;
                Name = null;
                DebugPath = "<anonymous>";
                Components = new BitField();
                ComponentProperties = new Dictionary<int, LuaTable>();
                HasChildren = false;
            }

            public EntityData(EntityData original)
            {
                BasePrefab = original.BasePrefab;
                Parent = original.Parent;
                Name = original.Name;
                DebugPath = original.DebugPath;
                Components = original.Components;
                ComponentProperties = original.ComponentProperties;
                HasChildren = original.HasChildren;
            }
        }

        private string m_path;
        private List<EntityData> m_entities;

        private List<EntityData> m_mergedEntities;
        private Dictionary<string, PropertyOptions> m_properties;
        private HashSet<string> m_dependencies;
        private List<EntityCreationInfo> m_creationInfo;
        private int m_creationsInProgress;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public Dictionary<string, PropertyOptions> Properties
        {
            get
            {
                Merge();
                App.Assert(m_properties != null);
                return m_properties;
            }
        }

        private static void CheckComponentDependencies(EntityData data)
        {
            // Get the required components
            var requirements = new BitField();
            foreach (var componentID in data.Components)
            {
                requirements |= ComponentRegistry.GetRequiredComponents(componentID);
            }

            // Check the requirements exist
            var missingComponents = (~data.Components & requirements);
            if (!missingComponents.IsEmpty)
            {
                var errorMessage = new StringBuilder();
                errorMessage.Append("Entity " + data.DebugPath + " has missing components");
                foreach (var requiredID in missingComponents)
                {
                    errorMessage.Append(" " + ComponentRegistry.GetComponentName(requiredID));
                }
                errorMessage.Append(" required by components");
                foreach (var id in data.Components)
                {
                    if (!(ComponentRegistry.GetRequiredComponents(id) & missingComponents).IsEmpty)
                    {
                        errorMessage.Append(" " + ComponentRegistry.GetComponentName(id));
                    }
                }
                throw new Exception(errorMessage.ToString());
            }

            // Check there's a hierarchy component if we're part of a hierarchy
            if (data.Parent >= 0 || data.HasChildren)
            {
                var hierarchyID = ComponentRegistry.GetComponentID<HierarchyComponent>();
                if (!data.Components[hierarchyID])
                {
                    throw new Exception("Entity " + data.DebugPath + " has children or a parent but does not have a Hierarchy component.");
                }
            }

            // Check there's a name component if we have a name
            if (data.Name != null)
            {
                var nameID = ComponentRegistry.GetComponentID<NameComponent>();
                if (!data.Components[nameID])
                {
                    throw new Exception("Entity " + data.DebugPath + " has a name but does not have a Name component.");
                }
            }
        }

        private static int ParseEntity(List<EntityData> o_entities, LuaTable table, string name, string debugPath, int parentIndex, bool incomplete)
        {
            // Get all the components on this object
            var data = new EntityData();
            data.Parent = parentIndex;
            data.Name = name;
            data.DebugPath = debugPath;

            if (!table.IsNil("Base"))
            {
                data.BasePrefab = table.GetString("Base");
                incomplete = true;
            }

            var componentsTable = table.GetOptionalTable("Components");
            if (componentsTable != null)
            {
                foreach (var pair in componentsTable)
                {
                    var componentName = pair.Key.GetString();
                    var componentID = ComponentRegistry.GetComponentID(componentName);
                    if (componentID < 0)
                    {
                        App.LogError("Component " + componentName + " was not recognised in prefab " + debugPath + ". It will be ignored");
                        continue;
                    }

                    var properties = pair.Value.GetTable();
                    data.ComponentProperties[componentID] = properties;
                    data.Components[componentID] = true;
                }
            }

            // Add the entity
            var index = o_entities.Count;
            o_entities.Add(data);

            // Parse and add the children
            var childrenTable = table.GetOptionalTable("Children");
            if (childrenTable != null && childrenTable.Count > 0)
            {
                data.HasChildren = true;
                foreach (var pair in childrenTable)
                {
                    string childName, childDebugPath;
                    if (pair.Key.IsString())
                    {
                        childName = pair.Key.GetString();
                        childDebugPath = debugPath + "/" + childName;
                    }
                    else
                    {
                        childName = null;
                        childDebugPath = debugPath + "/" + pair.Key.GetInt().ToString(CultureInfo.InvariantCulture);
                    }
                    var child = pair.Value.GetTable();
                    ParseEntity(o_entities, child, childName, childDebugPath, index, incomplete);
                }
            }

            if (!incomplete)
            {
                // Check there's no missing dependencies
                CheckComponentDependencies(data);
            }

            return index;
        }

        public static object LoadData(Stream stream, string path)
        {
            // Parse the LON
            var decoder = new LONDecoder(stream);
            decoder.AddMacro("Vector3", LONMacros.Vector3);
            decoder.AddMacro("Colour", LONMacros.Colour);
            decoder.AddMacro("Property", LONMacros.Property);
            var table = decoder.DecodeValue().GetTable();

            // Decode the entities
            var entities = new List<EntityData>();
            int rootIndex = ParseEntity(entities, table, null, path, -1, false);
            App.Assert(entities.Count > 0);
            App.Assert(rootIndex == 0);
            return entities;
        }

        public EntityPrefab(string path, object data)
        {
            m_path = path;
            Load(data);
            Assets.Assets.OnAssetsReloaded += Assets_OnAssetsReloaded;
        }

        public void Reload(object data)
        {
            Unload();
            Load(data);
        }

        private void FindProperties(EntityData entity, Dictionary<string, PropertyOptions> o_options)
        {
            foreach (var pair in entity.ComponentProperties)
            {
                var componentID = pair.Key;
                var componentProperties = pair.Value;
                var layout = ComponentRegistry.GetComponentDataLayout(componentID);
                foreach (var pair2 in componentProperties)
                {
                    var componentPropertyName = pair2.Key.GetString();
                    if(pair2.Value.IsTable())
                    {
                        var table = pair2.Value.GetTable();
                        var prefabPropertyName = table.GetOptionalString("__property");
                        if(prefabPropertyName != null)
                        {
                            // Get the options for this field
                            StructLayout.Property componentProperty;
                            if(!layout.Properties.TryGetValue(componentPropertyName, out componentProperty))
                            {
                                throw new Exception("Component " + ComponentRegistry.GetComponentName(componentID) + " does not have a property named " + componentPropertyName);
                            }
                            PropertyOptions newOptions = componentProperty.Options;

                            // Parse the default value of the property
                            var prefabPropertyDefault = table["__default"];
                            if (!prefabPropertyDefault.IsNil())
                            {
                                newOptions.Optional = true;
                                newOptions.CustomDefault = LONSerialiser.ParseValue(prefabPropertyName, prefabPropertyDefault, componentProperty.Options);
                            }

                            // Store the property
                            PropertyOptions existingOptions;
                            PropertyOptions mergedOptions;
                            if(!o_options.TryGetValue(prefabPropertyName, out existingOptions))
                            {
                                o_options.Add(prefabPropertyName, newOptions);
                            }
                            else if(TryMergePropertyOptions(prefabPropertyName, existingOptions, newOptions, out mergedOptions))
                            {
                                o_options[prefabPropertyName] = mergedOptions;
                            }
                            else
                            {
                                throw new Exception("Could not merge two different properties named " + prefabPropertyName + " on prefab " + m_path);
                            }
                        }
                    }
                }
            }
        }

        private void Load(object data)
        {
            m_entities = (List<EntityData>)data;
            m_mergedEntities = null;
            m_properties = null;
            m_dependencies = null;
        }

        private void Unload()
        {
        }

        public void Dispose()
        {
            Assets.Assets.OnAssetsReloaded -= Assets_OnAssetsReloaded;
            Unload();
        }

        private void Assets_OnAssetsReloaded(AssetLoadEventArgs e)
        {
            if (e.Paths.Contains(Path))
            {
                m_mergedEntities = null;
                m_properties = null;
                m_dependencies = null;
                return;
            }
            if (m_dependencies != null)
            {
                foreach (var dependency in m_dependencies)
                {
                    if (e.Paths.Contains(dependency))
                    {
                        m_mergedEntities = null;
                        m_properties = null;
                        m_dependencies = null;
                        return;
                    }
                }
            }
        }

        private static EntityData Merge(EntityData baseEntity, EntityData childEntity)
        {
            var mergedEntity = new EntityData();
            mergedEntity.BasePrefab = null;
            mergedEntity.Parent = childEntity.Parent;
            mergedEntity.Name = childEntity.Name;
            mergedEntity.DebugPath = childEntity.DebugPath;
            mergedEntity.Components = baseEntity.Components | childEntity.Components;
            foreach (var pair in baseEntity.ComponentProperties)
            {
                mergedEntity.ComponentProperties[pair.Key] = pair.Value;
            }
            foreach (var pair in childEntity.ComponentProperties)
            {
                LuaTable rootProperties;
                if (mergedEntity.ComponentProperties.TryGetValue(pair.Key, out rootProperties))
                {
                    mergedEntity.ComponentProperties[pair.Key] = LuaTableUtils.Merge(rootProperties, pair.Value);
                }
                else
                {
                    mergedEntity.ComponentProperties[pair.Key] = pair.Value;
                }
            }
            mergedEntity.HasChildren = baseEntity.HasChildren || childEntity.HasChildren;
            return mergedEntity;
        }

        private int FindEquivalentEntity(List<EntityData> baseEntities, int baseEntityIndex, List<EntityData> childEntities, int rootChildEntity)
        {
            App.Assert(baseEntityIndex > 0);
            var searchEntity = baseEntities[baseEntityIndex];
            App.Assert(searchEntity.Parent >= 0);
            if (searchEntity.Name != null)
            {
                for (int i = rootChildEntity + 1; i < childEntities.Count; ++i)
                {
                    var baseEntity = searchEntity;
                    var childEntity = childEntities[i];
                    while (childEntity.Name == baseEntity.Name)
                    {
                        if (baseEntity.Parent == 0 && childEntity.Parent == rootChildEntity)
                        {
                            // Reached the root with matching names all the way down, return the index
                            return i;
                        }
                        else if (baseEntity.Parent >= 0 && childEntity.Parent >= rootChildEntity)
                        {
                            // The name matched, but we're not at the root yet, keep going
                            baseEntity = baseEntities[baseEntity.Parent];
                            childEntity = childEntities[childEntity.Parent];
                        }
                        else
                        {
                            // Reached one root but not the other, no match
                            break;
                        }
                    }
                }
            }
            return -1;
        }

        private bool TryMergePropertyOptions(string name, PropertyOptions first, PropertyOptions second, out PropertyOptions o_result)
        {
            o_result = first;

            if (first.ElementType != second.ElementType ||
                first.IsArray != second.IsArray ||
                first.InnerType != second.InnerType)
            {
                App.LogError("Prefab {0} contains multiple properties named {1} with different types", m_path, name);
                return false;
            }

            o_result.Min = System.Math.Max(first.Min, second.Min);
            o_result.Max = System.Math.Min(first.Max, second.Max);
            if (o_result.Max < o_result.Min)
            {
                App.LogError("Prefab {0} contains multiple properties named {1} with non-overlapping ranges", m_path, name);
                return false;
            }

            if(first.Optional && second.Optional)
            {
                if(!object.Equals(first.CustomDefault, second.CustomDefault))
                {
                    App.LogError("Prefab {0} contains multiple properties named {1} with different default values", m_path, name);
                    return false;
                }
                o_result.Optional = true;
                o_result.CustomDefault = first.CustomDefault;
            }
            else
            {
                o_result.Optional = false;
                o_result.CustomDefault = false;
            }

            return true;
        }

        private void Merge()
        {
            // Only do this once per prefab
            if (m_mergedEntities != null)
            {
                return;
            }

            // Copy all the entities first, so they have the same positions in the array
            m_mergedEntities = new List<EntityData>();
            m_dependencies = new HashSet<string>();
            int originalCount = m_entities.Count;
            for (int i = 0; i < originalCount; ++i)
            {
                var entity = m_entities[i];
                m_mergedEntities.Add(entity);
            }

            // Then merge in data from the base prefabs
            bool changed = false;
            for (int i = 0; i < originalCount; ++i)
            {
                var entity = m_mergedEntities[i];
                if (entity.BasePrefab != null)
                {
                    // Collapse the base prefab
                    var basePrefab = EntityPrefab.Get(entity.BasePrefab);
                    basePrefab.Merge();

                    // Record the dependencies
                    m_dependencies.Add(basePrefab.Path);
                    m_dependencies.UnionWith(basePrefab.m_dependencies);

                    // Merge the root entity with the base prefab's root entity
                    var baseEntities = basePrefab.m_mergedEntities;
                    var baseRootEntity = baseEntities[0];
                    var mergedEntity = Merge(baseRootEntity, entity);
                    m_mergedEntities[i] = mergedEntity;

                    // Merge in the base prefab's children
                    unsafe
                    {
                        int* newIndexes = stackalloc int[baseEntities.Count];
                        for (int j = 1; j < baseEntities.Count; ++j)
                        {
                            var baseChildEntity = baseEntities[j];
                            App.Assert(baseChildEntity.Parent >= 0 && baseChildEntity.Parent < j);

                            // Find out if the base prefab's child entity needs to be merged with one on this prefab
                            int equivalent = FindEquivalentEntity(baseEntities, j, m_entities, i);
                            if (equivalent >= 0)
                            {
                                // Merge the entity with an existing one
                                App.Assert(equivalent > 0);
                                var mergedChild = Merge(baseChildEntity, m_mergedEntities[equivalent]);
                                m_mergedEntities[equivalent] = mergedChild;
                                newIndexes[j] = equivalent;
                            }
                            else
                            {
                                // Add the entity to the end of the list
                                var baseChildCopy = new EntityData(baseChildEntity);
                                if (baseChildEntity.Parent > 0)
                                {
                                    baseChildCopy.Parent = newIndexes[baseChildEntity.Parent];
                                }
                                else
                                {
                                    baseChildCopy.Parent = i;
                                }
                                m_mergedEntities.Add(baseChildCopy);
                                m_mergedEntities[baseChildCopy.Parent].HasChildren = true;
                                newIndexes[j] = m_mergedEntities.Count - 1;
                            }
                        }
                    }

                    // We've made some changes, make sure we get re-verified
                    changed = true;
                }
            }

            // Find all the properties
            m_properties = new Dictionary<string, PropertyOptions>();
            foreach (var entity in m_mergedEntities)
            {
                FindProperties(entity, m_properties);
            }

            // Check the dependencies on the results
            if (changed)
            {
                for (int i = 0; i<m_mergedEntities.Count; ++i)
                {
                    var entity = m_mergedEntities[i];
                    CheckComponentDependencies(entity);
                }
            }
        }

        public void SetupCreationInfo(Level level, LuaTable properties, List<EntityCreationInfo> o_infos)
        {
            // Collapse the prefab
            Merge();

            // Assign the entity IDs
            int rootID = level.Entities.AssignIDs(m_mergedEntities.Count);

            // Setup the creation info
            SetupCreationInfo(rootID, properties, o_infos);
        }

        public void SetupCreationInfo(int rootEntityID, LuaTable properties, List<EntityCreationInfo> o_infos)
        {
            // Collapse the prefab
            Merge();

            // Check the name component exists if we want to set a name
            var nameID = ComponentRegistry.GetComponentID<NameComponent>();
            var name = properties.GetOptionalString("Name", null);
            if (name != null && !m_mergedEntities[0].Components[nameID])
            {
                throw new Exception("Attempt to name an entity " + name + " but prefab " + m_path + " does not have a name component");
            }

            // Setup the entity infos
            var hierarchyID = ComponentRegistry.GetComponentID<HierarchyComponent>();
            for (int i = 0; i < m_mergedEntities.Count; ++i)
            {
                var data = m_mergedEntities[i];
                App.Assert(data.BasePrefab == null); // Inheritance should be collapsed by now
                App.Assert((i == 0 && data.Parent < 0) || (i > 0 && data.Parent >= 0 && data.Parent < m_mergedEntities.Count));

                var info = new EntityCreationInfo();
                info.ID = rootEntityID + i;
                info.Components = data.Components;
                info.ComponentProperties = new Dictionary<int, LuaTable>(data.ComponentProperties.Count);
                foreach (var componentID in data.Components)
                {
                    var componentProperties = data.ComponentProperties[componentID];
                    var modifiedProperties = LuaTableUtils.InjectProperties(componentProperties, properties);
                    if (componentID == hierarchyID)
                    {
                        if (modifiedProperties == componentProperties)
                        {
                            modifiedProperties = componentProperties.Copy();
                        }
                        if (data.Parent >= 0)
                        {
                            App.Assert(data.Parent != i);
                            App.Assert(data.Parent >= 0 && data.Parent < m_mergedEntities.Count);
                            var parentID = rootEntityID + data.Parent;
                            modifiedProperties["Parent"] = parentID;
                        }
                        else
                        {
                            modifiedProperties["Parent"] = LuaValue.Nil;
                        }
                    }
                    if (componentID == nameID)
                    {
                        if (modifiedProperties == componentProperties)
                        {
                            modifiedProperties = componentProperties.Copy();
                        }
                        if (i == 0 && name != null)
                        {
                            App.Assert(data.Name == null);
                            modifiedProperties["Name"] = name;
                        }
                        else if (data.Name != null)
                        {
                            modifiedProperties["Name"] = data.Name;
                        }
                    }
                    info.ComponentProperties[componentID] = modifiedProperties;
                }
                o_infos.Add(info);
            }
        }

        public Entity Instantiate(Level level, LuaTable properties)
        {
            // Setup the entity info
            List<EntityCreationInfo> creationInfo;
            if (m_creationsInProgress > 0)
            {
                creationInfo = new List<EntityCreationInfo>(m_entities.Count);
            }
            else
            {
                if (m_creationInfo == null)
                {
                    m_creationInfo = new List<EntityCreationInfo>(m_entities.Count);
                }
                m_creationInfo.Clear();
                creationInfo = m_creationInfo;
            }
            SetupCreationInfo(level, properties, creationInfo);

            // Create the entities
            m_creationsInProgress++;
            try
            {
                level.Entities.Create(creationInfo);
            }
            finally
            {
                m_creationsInProgress--;
            }

            // Return the root entity
            return level.Entities.Lookup(creationInfo[0].ID);
        }
    }
}
