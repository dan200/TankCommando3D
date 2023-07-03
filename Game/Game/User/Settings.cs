using Dan200.Core.Assets;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Render;
using System.IO;
using System.Text;
using Dan200.Core.Util;
using Dan200.Core.Audio;
using Dan200.Core.Lua;
using System;
using System.Collections.Generic;
using Dan200.Core.Platform;

namespace Dan200.Game.User
{
    internal class Settings
    {
		public readonly string Path;

		// Audio
		public readonly Dictionary<AudioCategory, float> Volume;

        // Video
        public bool Fullscreen;
        public int FullscreenWidth;
        public int FullscreenHeight;
        public bool VSync;
        public int WindowWidth;
        public int WindowHeight;
        public bool WindowMaximised;
        public float Gamma;
        public bool EnableShadows;
        public AntiAliasingMode AntiAliasingMode;

        // Input
        public bool EnableGamepad;
        public bool EnableGamepadRumble;
		public float MouseSensitivity;
        public bool InvertMouseY;
        public readonly Dictionary<string, InputOrigin[]> InputMappings;

        // Text
        public string Language;

		// Game
		public float FOV;

		public Settings(string path=null)
		{
            Path = path;
			Volume = new Dictionary<AudioCategory, float>();
            InputMappings = new Dictionary<string, InputOrigin[]>();
			Reset();
		}

        public void Reset()
        {            
			// Audio
			foreach(var category in EnumConverter.GetValues<AudioCategory>())
			{
				Volume[category] = 0.7f;
			}

			// Video
			if (App.PlatformID.IsMobile())
            {
                Fullscreen = true;
                FullscreenWidth = 0;
                FullscreenHeight = 0;
                EnableShadows = false;
                AntiAliasingMode = AntiAliasingMode.None;
                VSync = true;
                Gamma = 2.2f;
            }
            else
            {
                Fullscreen = App.Debug ? false : true;
                FullscreenWidth = 0;
                FullscreenHeight = 0;
                EnableShadows = true;
                AntiAliasingMode = AntiAliasingMode.FXAA;
                VSync = true;
                Gamma = 2.2f;
            }
            WindowWidth = 910;
            WindowHeight = 540;
            WindowMaximised = false;

			// Input
            EnableGamepad = true;
            EnableGamepadRumble = true;
			MouseSensitivity = 3.0f;
            InvertMouseY = false;
            InputMappings.Clear(); // TODO: Populate

			// Text
            Language = "system";

			// Game
			FOV = 60.0f;
        }

        public void ApplyInputMappings(InputMapper mapper)
        {
            mapper.UnmapAllInputs();
            foreach(var pair in InputMappings)
            {
                var inputName = pair.Key;
                foreach(var origin in pair.Value)
                {
                    mapper.MapInput(inputName, origin);
                }
            }
        }

        private bool TryParseInputOrigin(string originString, out InputOrigin o_origin)
        {
            var dot = originString.IndexOf('.');
            if (dot >= 0)
            {
                var categoryString = originString.Substring(0, dot);
                DeviceCategory category;
                if (EnumConverter.TryParse(categoryString, out category))
                {
                    var inputName = originString.Substring(dot + 1);
                    o_origin = new InputOrigin(category, inputName);
                    return true;
                }
            }
            App.LogError("Invalid input origin: {0}", originString);
            o_origin = default(InputOrigin);
            return false;
        }

        public bool Load()
        {
            return Load(Path);
        }

		public bool Load(string path)
        {
			App.Assert(path != null);
			if (File.Exists(path))
			{
				try
				{
					// Start from the defaults
		            Reset();

					// Parse the file
					LuaTable table;
					using (var stream = File.OpenRead(path))
					{
						var lon = new LONDecoder(stream);
						table = lon.DecodeValue().GetTable();
					}

					// Read the settings:
					// Audio
					foreach (var category in EnumConverter.GetValues<AudioCategory>())
					{
						Volume[category] = table.GetOptionalFloat("Audio.Volume." + category, Volume[category]);
					}

					// Video
					Fullscreen = table.GetOptionalBool("Video.Fullscreen", Fullscreen);
					FullscreenWidth = table.GetOptionalInt("Video.FullscreenWidth", FullscreenWidth);
					FullscreenHeight = table.GetOptionalInt("Video.FullscreenHeight", FullscreenHeight);
					EnableShadows = table.GetOptionalBool("Video.EnableShadows", EnableShadows);
					AntiAliasingMode = table.GetOptionalEnum("Video.AntiAliasingMode", AntiAliasingMode);
					VSync = table.GetOptionalBool("Video.VSync", VSync);
					Gamma = table.GetOptionalFloat("Video.Gamma", Gamma);
					WindowWidth = table.GetOptionalInt("Video.WindowWidth", WindowWidth);
					WindowHeight = table.GetOptionalInt("Video.WindowHeight", WindowHeight);
					WindowMaximised = table.GetOptionalBool("Video.WindowMaximised", WindowMaximised);

					// Input
					EnableGamepad = table.GetOptionalBool("Input.EnableGamepad", EnableGamepad);
					EnableGamepadRumble = table.GetOptionalBool("Input.EnableGamepadRumble", EnableGamepadRumble);
					MouseSensitivity = table.GetOptionalFloat("Input.MouseSensitivity", MouseSensitivity);

                    InputMappings.Clear();
                    var mappingsTable = table.GetOptionalTable("Input.Mappings", LuaTable.Empty);
                    foreach(var pair in mappingsTable)
                    {
                        var inputName = pair.Key.ToString();
                        var originsTable = pair.Value.GetTable();
                        var origins = new List<InputOrigin>(originsTable.Count);
                        foreach (var originPair in originsTable)
                        {
                            InputOrigin origin;
                            if(TryParseInputOrigin(originPair.Value.GetString(), out origin))
                            {
                                origins.Add(origin);
                            }
                            else
                            {
                                origins.Add(InputOrigin.Invalid);
                            }
                        }
                        InputMappings[inputName] = origins.ToArray();
                    }

					// Text
					Language = table.GetOptionalString("Text.Language", Language);

					// Game
					FOV = table.GetOptionalFloat("Game.FOV", FOV);
				}
				catch (Exception e)
				{
					App.LogError("Error parsing {0}: {1}", System.IO.Path.GetFileName(path), e.Message);
					App.LogError("Using default settings");
					Reset();
				}
				return true;
			}
			return false;
        }

		public void Save()
		{
			App.Assert(Path != null);

			// Build the settings table
			var table = new LuaTable();

			// Audio
			foreach (var category in EnumConverter.GetValues<AudioCategory>())
			{
				table["Audio.Volume." + category] = Volume[category];
			}

			// Video
			table["Video.Fullscreen"] = Fullscreen;
			table["Video.FullscreenWidth"] = FullscreenWidth;
			table["Video.FullscreenHeight"] = FullscreenHeight;
			table["Video.EnableShadows"] = EnableShadows;
			table["Video.AntiAliasingMode"] = AntiAliasingMode.ToLuaValue();
			table["Video.VSync"] = VSync;
			table["Video.Gamma"] = Gamma;
			table["Video.WindowWidth"] = WindowWidth;
			table["Video.WindowHeight"] = WindowHeight;
			table["Video.WindowMaximised"] = WindowMaximised;

			// Input
			table["Input.EnableGamepad"] = EnableGamepad;
			table["Input.EnableGamepadRumble"] = EnableGamepadRumble;
			table["Input.MouseSensitivity"] = MouseSensitivity;
            table["Input.InvertMouseY"] = InvertMouseY;

            var mappingsTable = new LuaTable(InputMappings.Count);
            foreach (var pair in InputMappings)
			{
                var inputName = pair.Key;
                var origins = pair.Value;
                var originsTable = new LuaTable(origins.Length);
                for (int i = 0; i < origins.Length; ++i)
                {
                    var origin = origins[i];
                    if (origin.IsValid)
                    {
                        var originString = origin.ToString();
                        originsTable[i + 1] = originString;
                    }
                }
                mappingsTable[inputName] = originsTable;
			}
            table["Input.Mappings"] = mappingsTable;

			// Text
			table["Text.Language"] = Language;

			// FOV
			table["Game.FOV"] = FOV;

            // Write the table out
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            using (var output = File.Open(Path, FileMode.Create))
			{
				var lon = new LONEncoder(output);
				lon.EncodeComment("Game Settings");
				lon.Encode(table);
			}
        }
    }
}
