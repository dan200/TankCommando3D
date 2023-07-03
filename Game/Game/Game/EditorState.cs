using Dan200.Core.Components;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Multiplayer;
using Dan200.Core.Network;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Game.Level;
using Dan200.Game.Script;
using Dan200.Game.User;
using System;
using System.Collections.Generic;
using System.Linq;
using Dan200.Game.Components;
using Dan200.Game.Components.Player;
using System.IO;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.GUI;
using System.Text;
using Dan200.Game.Options;
using Dan200.Core.Audio;
using Dan200.Core.Interfaces;
using Dan200.Core.Components.Misc;
using Dan200.Game.Components.Editor;
using Dan200.Core.Geometry;
using Dan200.Core.Serialisation;
using Dan200.Core.Assets;
using Dan200.Core.Components.Core;
using Dan200.Game.Systems.AI;
using Dan200.Core.Components.Physics;

namespace Dan200.Game.Game
{
    internal class EditorState : LevelState
    {
        private string m_levelSavePath;

        private PhysicsWorldComponent m_physics;
        private DebugCameraController m_camera;
        private EditorComponent m_selectedEntity;
        private EditorComponent m_hoverEntity;

        private class InspectorWindow : DebugWindow
        {
            public EditorComponent m_entity;

            public EditorComponent Entity
            {
                get
                {
                    return m_entity;
                }
                set
                {
                    m_entity = value;
                }
            }

            public InspectorWindow()
            {
                m_entity = null;
                Title = "Entity Properties";
            }

            private bool EditProperty(ref DebugGUIBuilder builder, string name, ref LuaValue io_value, PropertyOptions options)
            {
                bool changed = false;

                // Cache some colours
                var oldTheme = builder.Theme;
                var greyTheme = builder.Theme;
                greyTheme.TextColour.A = 128;
                greyTheme.HighlightColour.A = 128;
                greyTheme.HoverColour.A = 128;
                greyTheme.BoxColour.A = 128;

                // Start the row
                builder.Columns(0.0f, 24.0f * DebugGUI.DEBUG_GUI_SCALE);
                if (options.Optional)
                {
                    if (io_value.IsNil())
                    {
                        builder.Theme = greyTheme;
                    }
                }

                // Draw the main element
                bool sectionOpen = false;
                if (options.IsArray)
                {
                    if(builder.BeginSection(name))
                    {
                        sectionOpen = true;
                    }
                }
                else
                {
                    switch (options.ElementType)
                    {
                        case PropertyType.Bool:
                            {
                                bool boolValue;
                                if (!io_value.IsNil())
                                {
                                    boolValue = io_value.GetBool();
                                }
                                else if (options.CustomDefault != null)
                                {
                                    boolValue = (bool)options.CustomDefault;
                                }
                                else
                                {
                                    boolValue = false;
                                }
                                if (builder.Checkbox(name, ref boolValue))
                                {
                                    io_value = boolValue;
                                    changed = true;
                                }
                                break;
                            }
                        case PropertyType.Byte:
                            {
                                byte byteValue;
                                if (!io_value.IsNil())
                                {
                                    byteValue = io_value.GetByte();
                                }
                                else if (options.CustomDefault != null)
                                {
                                    byteValue = (byte)options.CustomDefault;
                                }
                                else
                                {
                                    byteValue = 0;
                                }
                                int intValue = byteValue;
                                if (builder.IntSlider(name, ref intValue, 0, 255))
                                {
                                    io_value = intValue;
                                    changed = true;
                                }
                                break;
                            }
                        case PropertyType.Int:
                            {
                                int value;
                                int intMin = (int)Math.Max(options.Min, int.MinValue);
                                int intMax = (int)Math.Min(options.Max, int.MaxValue);
                                if (!io_value.IsNil())
                                {
                                    value = io_value.GetInt();
                                }
                                else if (options.CustomDefault != null)
                                {
                                    value = (int)options.CustomDefault;
                                }
                                else
                                {
                                    value = Math.Min(Math.Max(0, intMin), intMax);
                                }
                                if (intMin > int.MinValue && intMax < int.MaxValue)
                                {
                                    if (builder.IntSlider(name, ref value, intMin, intMax))
                                    {
                                        io_value = value;
                                        changed = true;
                                    }
                                }
                                else
                                {
                                    if (builder.Int(name, ref value, intMin, intMax))
                                    {
                                        io_value = value;
                                        changed = true;
                                    }
                                }
                                break;
                            }
                        case PropertyType.Float:
                            {
                                float value;
                                float floatMin = (float)Math.Max(options.Min, float.MinValue);
                                float floatMax = (float)Math.Min(options.Max, float.MaxValue);
                                if (!io_value.IsNil())
                                {
                                    value = io_value.GetFloat();
                                }
                                else if (options.CustomDefault != null)
                                {
                                    value = (float)options.CustomDefault;
                                }
                                else
                                {
                                    value = Mathf.Clamp(0.0f, floatMin, floatMax);
                                }
                                if (floatMin > float.MinValue && floatMax < float.MaxValue)
                                {
                                    if (builder.FloatSlider(name, ref value, floatMin, floatMax))
                                    {
                                        io_value = value;
                                        changed = true;
                                    }
                                }
                                else
                                {
                                    if (builder.Float(name, ref value, floatMin, floatMax))
                                    {
                                        io_value = value;
                                        changed = true;
                                    }
                                }
                                break;
                            }
                        case PropertyType.String:
                            {
                                string value;
                                if (!io_value.IsNil())
                                {
                                    value = io_value.GetString();
                                }
                                else if (options.CustomDefault != null)
                                {
                                    value = (string)options.CustomDefault;
                                }
                                else
                                {
                                    value = "";
                                }
                                if(builder.Textbox(name, ref value))
                                {
                                    io_value = value;
                                    changed = true;
                                }
                                break;
                            }
                        case PropertyType.LuaTable:
                            {
                                builder.Label("Error: LuaTable properties are not yet editable");
                                break;
                            }
                        case PropertyType.Enum:
                            {
                                object value;
                                object[] values = Enum.GetValues(options.InnerType).OfType<object>().ToArray();
                                if (!io_value.IsNil())
                                {
                                    value = Enum.Parse(options.InnerType, io_value.GetString());
                                }
                                else if(options.CustomDefault != null)
                                {
                                    value = options.CustomDefault;
                                }
                                else
                                {
                                    value = values[0];
                                }
                                if(builder.DropDown(name, ref value, values))
                                {
                                    io_value = value.ToString();
                                    changed = true;
                                }
                                break;
                            }
                        case PropertyType.Struct:
                            {
                                if (builder.BeginSection(name))
                                {
                                    sectionOpen = true;
                                }
                                break;
                            }
                        default:
                            throw new Exception("Unhandled type");
                    }
                }

                // Draw the add/remove button
                if (options.Optional)
                {
                    builder.Theme = oldTheme;
                    if (!io_value.IsNil())
                    {
                        if (builder.Button("x"))
                        {
                            io_value = LuaValue.Nil;
                            changed = true;
                        }
                    }
                    else
                    {
                        if(builder.Button("+"))
                        {
                            io_value = LONSerialiser.MakeDefault(options);
                            changed = true;
                        }
                    }
                    if(io_value.IsNil())
                    {
                        builder.Theme = greyTheme;
                    }
                }
                else
                {
                    builder.EndRow();
                }

                // Draw the sub-contents
                if(sectionOpen)
                {
                    builder.Indent();

                    if(options.IsArray)
                    {
                        LuaTable arrayTable;
                        if (!io_value.IsNil())
                        {
                            arrayTable = io_value.GetTable();
                        }
                        else
                        {
                            arrayTable = LONSerialiser.MakeDefault(options).GetTable();
                        }

                        var elementOptions = options;
                        elementOptions.IsArray = false;
                        elementOptions.Optional = true;
                        elementOptions.CustomDefault = null;

                        for (int i = 1; i <= arrayTable.ArrayLength; ++i)
                        {
                            var arrayValue = arrayTable[i];
                            if(EditProperty(ref builder, i.ToString(), ref arrayValue, elementOptions))
                            {
                                if (arrayValue.IsNil())
                                {
                                    arrayTable.Remove(i);
                                    i--;
                                }
                                else
                                {
                                    arrayTable[i] = arrayValue;
                                }
                                io_value = arrayTable;
                                changed = true;
                            }
                        }

                        if(builder.Button("Add"))
                        {
                            arrayTable.Insert(LONSerialiser.MakeDefault(elementOptions));
                            io_value = arrayTable;
                            changed = true;
                        }
                        builder.EndRow();
                    }
                    else
                    {
                        switch (options.ElementType)
                        {
                            case PropertyType.Struct:
                                {
                                    var layout = StructLayout.Get(options.InnerType);
                                    if (!io_value.IsNil())
                                    {
                                        // Edit an existing struct table
                                        var structTable = io_value.GetTable();
                                        foreach (var pair in layout.Properties)
                                        {
                                            var propertyName = pair.Key;
                                            var property = pair.Value;
                                            var structValue = structTable[propertyName];
                                            if (EditProperty(ref builder, propertyName, ref structValue, property.Options))
                                            {
                                                structTable[propertyName] = structValue;
                                                changed = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Create a new default struct table, edit it, then store it if the player modifies it
                                        var defaultTable = (options.CustomDefault != null) ?
                                            LONSerialiser.Save(options.InnerType, options.CustomDefault) :
                                            LONSerialiser.MakeDefault(options.InnerType);
                                        foreach (var pair in layout.Properties)
                                        {
                                            var propertyName = pair.Key;
                                            var property = pair.Value;
                                            var structValue = defaultTable[propertyName];
                                            if (EditProperty(ref builder, propertyName, ref structValue, property.Options))
                                            {
                                                defaultTable[propertyName] = structValue;
                                                io_value = defaultTable;
                                                changed = true;
                                            }
                                        }
                                    }
                                    break;
                                }
                            default:
                                throw new Exception("Unhandled type");
                        }
                    }

                    builder.Outdent();
                    builder.EndSection();
                }

                if (options.Optional)
                {
                    builder.Theme = oldTheme;
                }
                return changed;
            }

            protected override void OnGUI(ref DebugGUIBuilder builder)
            {
                if (m_entity != null)
                {
                    builder.Label(m_entity.Prefab.Path);
                    builder.Columns(0.0f, 24.0f * DebugGUI.DEBUG_GUI_SCALE);
                    bool changed = false;

                    if (m_entity.Entity.GetComponent<NameComponent>() != null)
                    {
                        var namePropertyOptions = new PropertyOptions();
                        namePropertyOptions.Optional = true;
                        namePropertyOptions.ElementType = PropertyType.String;
                        var namePropertyValue = m_entity.Properties["Name"];
                        if (EditProperty(ref builder, "Name", ref namePropertyValue, namePropertyOptions))
                        {
                            m_entity.Properties["Name"] = namePropertyValue;
                            changed = true;
                        }
                    }
                    
                    foreach(var property in m_entity.Prefab.Properties)
                    {
                        var propertyValue = m_entity.Properties[property.Key];
                        if(EditProperty(ref builder, property.Key, ref propertyValue, property.Value))
                        {
                            m_entity.Properties[property.Key] = propertyValue;
                            changed = true;
                        }
                    }
                    if(changed)
                    {
                        m_entity.ResetFromProperties();
                    }
                }
                else
                {
                    builder.Label("No Entity selected");
                }
            }
        }

        private class LevelWindow : DebugWindow
        {
            private EditorState m_parent;

            public LevelWindow(EditorState parent)
            {
                m_parent = parent;
                Title = "Level";
            }

            protected override void OnGUI(ref DebugGUIBuilder builder)
            {
                builder.Label(m_parent.Level.Data.Path);
                if(builder.Button("Save"))
                {
                    m_parent.Save();
                }
                if (builder.Button("Test"))
                {
                    m_parent.Test();
                }
                if (builder.Button("Reload"))
                {
                    m_parent.Reload();
                }
            }
        }

        private InspectorWindow m_inspector;
        private LevelWindow m_level;

        public EditorState(Game game, string levelLoadPath, string levelSavePath) : base(game, levelLoadPath, LevelLoadFlags.Editor)
        {
            m_levelSavePath = levelSavePath;
            m_camera = new DebugCameraController(Game);

            var margin = 16.0f * DebugGUI.DEBUG_GUI_SCALE;
            m_inspector = new InspectorWindow();
            m_inspector.Anchor = Anchor.TopLeft;
            m_inspector.LocalPosition = new Vector2(margin, margin);
            m_inspector.Size = new Vector2(300.0f, 400.0f) * DebugGUI.DEBUG_GUI_SCALE;

            m_level = new LevelWindow(this);
            m_level.Anchor = Anchor.TopRight;
            m_level.Size = new Vector2(200.0f, 150.0f) * DebugGUI.DEBUG_GUI_SCALE;
            m_level.LocalPosition = new Vector2(-margin - m_level.Size.X, margin);

            var editorCamera = Level.GetSystem<NameSystem>().Lookup("./EditorCamera", RootEntity);
            if (editorCamera != null)
            {
                m_camera.Transform = editorCamera.GetComponent<TransformComponent>().Transform;
            }
        }

		public override void Enter(GameState previous)
        {
			base.Enter(previous);
            CameraProvider = m_camera;

            m_physics = RootEntity.GetComponent<PhysicsWorldComponent>();
            m_selectedEntity = null;
            m_hoverEntity = null;

            Game.Screen.Elements.Add(m_inspector);
            Game.Screen.Elements.Add(m_level);
        }

        private void Save()
        {
            SaveAs(m_levelSavePath);
        }

        private void SaveAs(string savePath)
        {
            var levelData = new LevelData(Level.Data.Path);
            levelData.MusicPath = Level.Data.MusicPath;
            levelData.SkyPath = Level.Data.SkyPath;
            levelData.ScriptPath = Level.Data.ScriptPath;
            foreach(var entity in Level.GetComponents<EditorComponent>())
            {
                var entityData = new LevelData.EntityData();
                entityData.Type = entity.Prefab.Path;
                entityData.Properties = entity.Properties;
                levelData.Entities.Add(entityData);
            }

            var fullPath = Path.Combine(App.AssetPath, "main/" + savePath); // TODO: Pick a location in the user's save directory!
            levelData.Save(fullPath);
            Assets.Reload(savePath);
        }

        private void Test()
        {
            var testPath = "editor/test.level";
            SaveAs(testPath);
            Game.QueueState(new TestState(Game, testPath, m_levelSavePath));
        }

        private void Reload()
        {
            Assets.Reload(m_levelSavePath);
            Game.QueueState(new EditorState(Game, m_levelSavePath, m_levelSavePath));
        }

        private EditorComponent GetEntityUnderCursor()
        {
            var screenPosition = Game.Screen.MousePosition;
            if (!m_inspector.Area.Contains(screenPosition))
            {
                // Convert screen coords to camera space direction
                float aspect = Game.Screen.Width / Game.Screen.Height;
                float x = (screenPosition.X / (0.5f * Game.Screen.Width)) - 1.0f;
                float y = (screenPosition.Y / (0.5f * Game.Screen.Height)) - 1.0f;

                var dirCS = new Vector3(
                    (float)(Math.Tan(0.5f * m_camera.FOV)) * (x * aspect),
                    -(float)(Math.Tan(0.5f * m_camera.FOV)) * y,
                    1.0f
                ).Normalise();

                // Convert camera space direction to world space ray
                var pos = m_camera.Transform.Position;
                var dir = m_camera.Transform.ToWorldDir(dirCS);

                // Peform the raycast
                RaycastResult result;
                if (m_physics.World.Raycast(
                    new Ray(pos, dir, 1000.0f),
                    CollisionGroup.EditorSelectable,
                    out result
                ))
                {
                    var entity = result.Shape.UserData as Entity;
                    if (entity != null)
                    {
                        var editable = entity.GetComponent<EditorComponent>();
                        if (editable != null)
                        {
                            return editable;
                        }
                    }
                }
            }
            return null;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            m_camera.Update(dt);
            HandleInput();
        }

        private void HandleInput()
        {
            var mouse = Game.InputDevices.Mouse;
            if (mouse != null)
            {
                if (m_selectedEntity != null)
                {
                    foreach(var manipulator in m_selectedEntity.Entity.GetComponentsWithInterface<IManipulator>())
                    {
                        if(manipulator.HandleMouseInput(mouse, Game.MainView.Camera))
                        {
                            return;
                        }
                    }
                }

                var entityUnderCursor = GetEntityUnderCursor();
                Hover(entityUnderCursor);
                if (mouse.GetInput(MouseButton.Left).Pressed)
                {
                    if (entityUnderCursor != null)
                    {
                        Select(entityUnderCursor);
                    }
                }
            }

            var keyboard = Game.InputDevices.Keyboard;
            if(keyboard != null)
            {
                if(m_selectedEntity != null)
                {
                    if(keyboard.GetInput(Key.Delete).Pressed)
                    {
                        Delete(m_selectedEntity);
                    }
                    if (keyboard.GetInput(Key.Space).Pressed)
                    {
                        var duplicate = Duplicate(m_selectedEntity);
                        Select(duplicate);
                    }
                }
            }
        }

        private void Hover(EditorComponent entity)
        {
            if (entity != m_hoverEntity)
            {
                if (m_hoverEntity != null)
                {
                    m_hoverEntity.Hover = false;
                }
                m_hoverEntity = entity;
                if (m_hoverEntity != null)
                {
                    m_hoverEntity.Hover = true;
                }
            }
        }

        private void Select(EditorComponent entity)
        {
            if(entity != m_selectedEntity)
            {
                if(m_selectedEntity != null)
                {
                    m_selectedEntity.Selected = false;
                }
                m_selectedEntity = entity;
                m_inspector.Entity = entity;
                if(m_selectedEntity != null)
                {
                    m_selectedEntity.Selected = true;
                }
            }
        }

        private void Delete(EditorComponent entity)
        {
            if (entity == m_selectedEntity)
            {
                Select(null);
            }
            Level.Entities.Destroy(entity.Entity);
        }

        private EditorComponent Create(EntityPrefab prefab, LuaTable properties)
        {
            // Setup creation info
            var creationInfo = new List<EntityCreationInfo>();
            SetupEntityCreationInfoForEditor(prefab, properties, creationInfo, true, RootEntity.ID);
            App.Assert(creationInfo.Count > 0);

            // Create the entities
            Level.Entities.Create(creationInfo);
            Level.PromoteNewComponents();

            // Return the entity
            var entity = Level.Entities.Lookup(creationInfo[0].ID);
            App.Assert(entity != null);
            var editor = entity.GetComponent<EditorComponent>();
            App.Assert(editor != null);
            return editor;
        }

        private EditorComponent Duplicate(EditorComponent entity)
        {
            var properties = entity.Properties.Copy();
            properties["Name"] = LuaValue.Nil;
            return Create(entity.Prefab, properties);
        }

        public override void Leave(GameState next)
        {
			base.Leave(next);

            Game.Screen.Elements.Remove(m_inspector);
            m_inspector.Dispose();
            m_inspector = null;

            Game.Screen.Elements.Remove(m_level);
            m_level.Dispose();
            m_level = null;
        }
    }
}
