using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dan200.Game.GUI
{
	internal struct ConsoleCommandEventArgs
	{
		public readonly string Command;

		public ConsoleCommandEventArgs(string command)
		{
			Command = command;
		}
	}

    internal class Console : Element
    {
        private const float BLINK_TIME = 0.4f;
        private const float OPEN_SPEED = 4.0f;

		private Game.Game m_game;
        private StringBuilder m_pendingCommand;
        private List<string> m_history;
        private int m_historyPos;

        private Dictionary<Key, string> m_bindings;
        private List<LogEntry> m_entries;
        private int m_scroll;
        private bool m_blink;
        private float m_blinkTimer;
        private float m_openness;
        private bool m_open;
		private int m_fps;

        public event StructEventHandler<Console, ConsoleCommandEventArgs> OnCommand;

        public bool IsOpen
        {
            get
            {
                return m_open;
            }
        }

		public Console(Game.Game game)
        {
			m_game = game;
			m_pendingCommand = new StringBuilder();
            m_history = new List<string>();
            m_historyPos = -1;

            m_bindings = new Dictionary<Key, string>();
            m_entries = new List<LogEntry>();
            foreach(var entry in App.RecentLog)
            {
                m_entries.Add(entry);
            }
            m_scroll = 0;
            m_blink = true;
            m_blinkTimer = BLINK_TIME;
            m_openness = 0.0f;
            m_open = false;
			m_fps = (int)App.FPS;

            App.OnLog += App_OnLog;
        }

		public override void Dispose()
		{
			App.OnLog -= App_OnLog;
		}

		private void App_OnLog(LogEntry e)
		{
            lock (m_entries)
            {
                m_entries.Add(e);
                if(m_scroll > 0)
                {
                    m_scroll++;
                }
            }
			RequestRebuild();
		}

        public void Open()
        {
            m_open = true;
            Screen.ModalDialog = this;
            RequestRebuild();
        }

        public void Close()
        {
            m_open = false;
            Screen.ModalDialog = null;
            RequestRebuild();
        }

        public void Toggle()
        {
            m_open = !m_open;
            Screen.ModalDialog = m_open ? this : null;
            RequestRebuild();
        }

        public void Clear()
        {
            lock (m_entries)
            {
                m_entries.Clear();
                m_scroll = 0;
            }
            RequestRebuild();
        }

        public void Bind(Key key, string command)
        {
            m_bindings[key] = command;
        }

        public void Unbind(Key key)
        {
            m_bindings.Remove(key);
        }

        public string GetBinding(Key key)
        {
            string result;
            if(m_bindings.TryGetValue(key, out result))
            {
                return result;
            }
            return null;
        }

        private void Exec(string command)
        {
            if (OnCommand != null)
            {
                OnCommand.Invoke(this, new ConsoleCommandEventArgs(command));
            }
        }

        protected override void OnInit()
        {
            Bind(Key.F1, "game.restartLevel()");
            Bind(Key.F2, "dev.screenshot()");
			Bind(Key.F3, "dev.setGUIVisible( not dev.getGUIVisible() )");
            Bind(Key.F4, "dev.setDebugCamera( not dev.getDebugCamera() )");
            Bind(Key.F5, "dev.reloadAssets()");
            Bind(Key.F12, "game.quit()");
            Bind(Key.P, "dev.toggleDebugDraw(\"Physics\")");
			Bind(Key.Equals, "level.setTimeScale(math.min(level.getTimeScale() * 2.0, 32.0))");
			Bind(Key.Minus, "level.setTimeScale(math.max(level.getTimeScale() * 0.5, 0.0625))");
        }

        protected override void OnUpdate(float dt)
		{
            // Update blink
            m_blinkTimer -= dt;
            if(m_blinkTimer <= 0.0f)
            {
                m_blink = !m_blink;
                m_blinkTimer = BLINK_TIME;
				if (m_openness > 0.0f)
				{
					RequestRebuild();
				}
            }

            // Update visibility
            bool ignoreText = false;
            var keyboard = Screen.InputDevices.Keyboard;
            if (keyboard != null && keyboard.GetInput(Key.BackQuote).Pressed)
			{
                Toggle();
                ignoreText = true;
            }

            // Update opening animation
            if(m_open)
            {
                if(m_openness < 1.0f)
                {
                    m_openness = Mathf.Min(m_openness + dt * OPEN_SPEED, 1.0f);
                    RequestRebuild();
                }
            }
            else
            {
                if (m_openness > 0.0f)
                {
                    m_openness = Mathf.Max(m_openness - dt * OPEN_SPEED, 0.0f);
                    RequestRebuild();
                }
            }

            // Update shortcuts
            if(!m_open)
            {
                if (keyboard != null)
                {
                    foreach (var binding in m_bindings)
                    {
                        var key = binding.Key;
                        var command = binding.Value;
                        if (keyboard.GetInput(key).Pressed)
                        {
                            Exec(command);
                            m_history.Add(command);
                            m_historyPos = -1;
                        }
                    }
                }
            }

			// Update FPS
			var fps = (int)App.FPS;
			if (fps != m_fps)
			{
				m_fps = fps;
				if (m_openness > 0.0f)
				{
					RequestRebuild();
				}
			}

            // Update input
            if (m_open && !ignoreText)
			{
                // Update scroll
                var mouse = Screen.InputDevices.Mouse;
                if(mouse != null)
                {
                    if(mouse.GetInput(MouseWheelDirection.Up).Held)
                    {
                        lock (m_entries)
                        {
                            m_scroll = m_scroll + 1;
                        }
                    }
                    else if(mouse.GetInput(MouseWheelDirection.Down).Held)
                    {
                        lock (m_entries)
                        {
                            m_scroll = Math.Max(m_scroll - 1, 0);
                        }
                    }
                }
                    
                // Update history navigation
                if(keyboard != null && keyboard.GetInput(Key.Up).Pressed)
                {
                    // Back
                    if(m_historyPos >= 0)
                    {
                        m_historyPos = Math.Max(m_historyPos - 1, 0);
                    }
                    else
                    {
                        m_historyPos = (m_history.Count > 0) ? m_history.Count - 1 : -1;
                    }
                    m_pendingCommand.Clear();
                    if (m_historyPos >= 0)
                    {
                        m_pendingCommand.Append(m_history[m_historyPos]);
                    }
					RequestRebuild();
                }
                if (keyboard != null && keyboard.GetInput(Key.Down).Pressed)
                {
                    // Forward
                    if (m_historyPos >= 0)
                    {
                        m_historyPos = m_historyPos + 1;
                        if(m_historyPos >= m_history.Count)
                        {
                            m_historyPos = -1;
                        }
                    }
                    m_pendingCommand.Clear();
                    if (m_historyPos >= 0)
                    {
                        m_pendingCommand.Append(m_history[m_historyPos]);
                    }
                    RequestRebuild();
                }

                // Update input
                var text = (keyboard != null) ? keyboard.Text : "";
				if (text.Length > 0)
				{
					foreach (var c in text)
					{
						switch (c)
						{
							case '\b':
								if (m_pendingCommand.Length > 0)
								{
									if (m_pendingCommand.Length >= 2 && char.IsSurrogate(m_pendingCommand[m_pendingCommand.Length - 1]))
									{
										m_pendingCommand.Remove(m_pendingCommand.Length - 2, 2);
									}
									else
									{
										m_pendingCommand.Remove(m_pendingCommand.Length - 1, 1);
									}
                                }
                                break;
							case '\n':
                                var command = m_pendingCommand.ToString();
                                m_pendingCommand.Clear();
                                m_history.Add(command);
                                m_historyPos = -1;
                                App.Log("> {0}", command);
                                Exec(command);
                                break;
							default:
								m_pendingCommand.Append(c);
                                break;
						}
					}
	                RequestRebuild();
				}
			}
		}

		protected override void OnRebuild(GUIBuilder builder)
		{
			var origin = Position;
			var font = UIFonts.Default;
			var fontSize = 20;
            var textHeight = font.GetHeight(fontSize);

			// Draw the background
			float width = Width;
            float height = Size.Y * Mathf.Ease(m_openness);
            builder.AddQuad(Position, Position + new Vector2(width, height), Texture.White, Quad.UnitSquare, new Colour(0, 0, 0, 196));

			// Draw FPS
            if(height >= 3.0f * textHeight)
			{
				Colour fpsColour;
				if (m_fps <= 29)
				{
					fpsColour = Colour.Red;
				}
				else if (m_fps <= 59)
				{
					fpsColour = Colour.Yellow;
				}
				else
				{
					fpsColour = Colour.White;
				}

				var renderStats = m_game.RenderStats;
				builder.AddText(string.Format("FPS: {0}Hz", m_fps), Position + new Vector2(Width, 0.0f), font, fontSize, fpsColour, TextAlignment.Right);
				builder.AddText(string.Format("Triangles: {0}", renderStats.Triangles), Position + new Vector2(width, textHeight), font, fontSize, Colour.White, TextAlignment.Right);
				builder.AddText(string.Format("Draw Calls: {0}", renderStats.DrawCalls), Position + new Vector2(width, 2.0f * textHeight), font, fontSize, Colour.White, TextAlignment.Right);
			}

			// Draw log entries
			float yPos = height - textHeight;
            lock (m_entries)
            {
                for (int i = m_entries.Count - m_scroll - 1; i >= 0; --i)
                {
                    var entry = m_entries[i];
                    Colour colour;
                    switch (entry.Level)
                    {
                        case LogLevel.Error:
                            colour = Colour.Red;
                            break;
                        case LogLevel.Warning:
                            colour = Colour.Yellow;
                            break;
                        case LogLevel.Debug:
                            colour = Colour.Cyan;
                            break;
                        default:
                            colour = Colour.White;
                            break;
                    }

                    yPos -= font.Measure(entry.Text, fontSize, false, width).Y;
                    builder.AddText(
                        entry.Text,
                        origin + new Vector2(0.0f, yPos),
                        font,
                        fontSize,
                        colour,
                        TextAlignment.Left,
                        false,
                        width
                    );

                    if (yPos <= origin.Y)
                    {
                        break;
                    }
                }

                if(m_scroll > 0)
                {
                    builder.AddText(
                        "V",
                        origin + new Vector2(width, height - textHeight),
                        font,
                        fontSize,
                        Colour.White,
                        TextAlignment.Right
                    );
                }
            }

			// Draw prompt
			builder.AddText(
				"> " + m_pendingCommand + (m_blink ? "_" : ""),
                origin + new Vector2(0.0f, height - textHeight),
                font,
                fontSize,
				Colour.White,
				TextAlignment.Left
			);
		}
    }
}
