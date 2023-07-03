using System;
using System.Linq;
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.GUI;

namespace Dan200.Game.Game
{
    internal class LevelSelectState : GameState
    {
        private struct LevelSelectEventArgs
        {
            public LevelData SelectedLevel;
        }

        private class LevelSelectGUI : Element
        {
            public event StructEventHandler<LevelSelectGUI, LevelSelectEventArgs> OnSelection;

            private LevelData[] m_levels;
            private int m_selectedLevel;

            public LevelSelectGUI(LevelData[] levels)
            {
                m_levels = levels;
                m_selectedLevel = (m_levels.Length > 0) ? 0 : -1;
            }

            protected override void OnInit()
            {
            }

            protected override void OnRebuild(GUIBuilder builder)
            {
                var position = Position;
                var font = UIFonts.Default;
                var fontSize = 24;
                builder.AddText("SELECT LEVEL\n------------", position, font, fontSize, Colour.White);
                position.Y += 2.0f * font.GetHeight(fontSize);
                for (int i = 0; i < m_levels.Length; ++i)
                {
                    var level = m_levels[i];
                    if (i == m_selectedLevel)
                    {
                        builder.AddText("> " + level.Path, position, font, fontSize, Colour.Green);
                    }
                    else
                    {
                        builder.AddText(level.Path, position, font, fontSize, Colour.White);
                    }
                    position.Y += font.GetHeight(fontSize);
                }
            }

            protected override void OnUpdate(float dt)
            {
                // Gather inputs
                bool up = false;
                bool down = false;
                bool select = false;

                var keyboard = Screen.InputDevices.Keyboard;
                if (keyboard != null)
                {
                    up |= keyboard.GetInput(Key.Up).Pressed;
                    down |= keyboard.GetInput(Key.Down).Pressed;
                    select |= keyboard.GetInput(Key.Return).Pressed;
                }
                foreach (var gamepad in Screen.InputDevices.Gamepads)
                {
                    up |= gamepad.GetInput(GamepadButton.Up).Pressed;
                    up |= gamepad.GetInput(GamepadAxis.LeftStickUp).Pressed;
                    down |= gamepad.GetInput(GamepadButton.Down).Pressed;
                    down |= gamepad.GetInput(GamepadAxis.LeftStickDown).Pressed;
                    select |= gamepad.GetInput(GamepadButton.A).Pressed;
                    select |= gamepad.GetInput(GamepadButton.Start).Pressed;
                }

                // Navigate
                if(select)
                {
                    if (m_selectedLevel >= 0 && OnSelection != null)
                    {
                        var args = new LevelSelectEventArgs();
                        args.SelectedLevel = m_levels[m_selectedLevel];
                        OnSelection.Invoke(this, args);
                    }
                }
                else if (up)
                {
                    if (m_levels.Length > 0)
                    {
                        m_selectedLevel = (m_selectedLevel + m_levels.Length - 1) % m_levels.Length;
                        RequestRebuild();
                    }
                }
                else if (down)
                {
                    if (m_levels.Length > 0)
                    {
                        m_selectedLevel = (m_selectedLevel + 1) % m_levels.Length;
                        RequestRebuild();
                    }
                }
            }
        }

        private LevelSelectGUI m_gui;

        public LevelSelectState(Game game) : base(game)
        {
        }

        public override void Enter(GameState previous)
        {
            m_gui = new LevelSelectGUI(Assets.Find<LevelData>().ToArray());
            m_gui.Anchor = Anchor.TopLeft;
            m_gui.LocalPosition = new Vector2(10.0f, 10.0f);
            m_gui.OnSelection += delegate(LevelSelectGUI gui, LevelSelectEventArgs args) 
            {
                var levelPath = args.SelectedLevel.Path;
                Game.QueueState(new InGameState(Game, levelPath));
            };
            Game.Screen.Elements.Add(m_gui);
        }

        public override void Leave(GameState next)
        {
            Game.Screen.Elements.Remove(m_gui);
            m_gui.Dispose();
        }

        public override void OnConsoleCommand(string command)
        {
        }

        public override void Update(float dt)
        {
        }

        public override void PopulateCamera(View view)
        {
        }

        public override void Draw(IRenderer renderer, View view)
        {
        }
    }
}
