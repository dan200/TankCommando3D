#if SDL
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Core.Window.SDL2;
using Dan200.Core.Platform.SDL2;
using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dan200.Core.Input.SDL2
{
    internal class SDL2Keyboard : IKeyboard
    {
        private SDL2Window m_window;

        private Dictionary<Key, Input> m_keys;
        private StringBuilder m_lastText;
        private StringBuilder m_pendingText;

        public DeviceCategory Category
        {
            get
            {
                return DeviceCategory.Keyboard;
            }
        }

        public bool Connected
        {
            get
            {
                return true;
            }
        }

        public IEnumerable<Input> Inputs
        {
            get
            {
                return m_keys.Values;
            }
        }

        public string Text
        {
            get
            {
                return m_lastText.ToString();
            }
        }

        public SDL2Keyboard(SDL2Window window)
        {
            m_window = window;
            m_keys = new Dictionary<Key, Input>();
            m_lastText = new StringBuilder();
            m_pendingText = new StringBuilder();
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                m_keys.Add(key, new Input(key.ToString(), key.GetPrompt()));
            }
            Update();
        }

        public Input GetInput(string name)
        {
            Key key;
            if(EnumConverter.TryParse(name, out key))
            {
                return GetInput(key);
            }
            return null;
        }

        public Input GetInput(Key key)
        {
            App.Assert(m_keys.ContainsKey(key));
            return m_keys[key];
        }

        public void SetClipboardText(string text)
        {
            try
            {
                SDLUtils.CheckResult("SDL_SetClipboardText", SDL.SDL_SetClipboardText(text));
            }
            catch(SDLException e)
            {
                App.LogError(e.Message);
            }
        }

        public unsafe void HandleEvent(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    {
                        // Typed text
                        if (m_window.Focus)
                        {
							fixed(byte* text = e.text.text)
							{
								var str = new ByteString(text);
								foreach (var codepoint in UnicodeUtils.ReadUTF8Characters(str))
								{
									char first, second;
									int count = UnicodeUtils.EncodeUTF16Char(codepoint, out first, out second);
									if (count == 1)
									{
                                        if (!char.IsControl(first))
										{
											m_pendingText.Append(first);
										}
									}
									else
									{
										m_pendingText.Append(first);
										m_pendingText.Append(second);
									}
								}
							}
                        }
                        break;
                    }
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    {
                        if (m_window.Focus)
                        {
                            if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_v)
                            {
                                // Pasted text
                                SDL.SDL_Keymod pasteModifier = (SDL.SDL_GetPlatform() == "Mac OS X") ?
                                    SDL.SDL_Keymod.KMOD_GUI :
                                    SDL.SDL_Keymod.KMOD_CTRL;
                                if (((int)e.key.keysym.mod & (int)pasteModifier) != 0)
                                {
                                    var clipboard = SDL.SDL_GetClipboardText();
                                    if (clipboard != null)
                                    {
                                        m_pendingText.Append(clipboard);
                                    }
                                }
                            }
                            else if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_BACKSPACE)
                            {
                                // Backspace
                                m_pendingText.Append('\b');
                            }
                            else if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_RETURN ||
                                     e.key.keysym.sym == SDL.SDL_Keycode.SDLK_KP_ENTER)
                            {
                                // Return
                                m_pendingText.Append('\n');
                            }
                        }
                        break;
                    }
            }
        }

        public void Update()
        {
            // Get keys
            int numKeys;
            IntPtr state = SDL.SDL_GetKeyboardState(out numKeys);
            bool focus = m_window.Focus;
            for (int i = 0; i < numKeys; ++i)
            {
                var scancode = (SDL.SDL_Scancode)i;
                var keycode = SDL.SDL_GetKeyFromScancode(scancode);

                Input input;
                if (m_keys.TryGetValue((Key)keycode, out input))
                {
                    byte stateByte = Marshal.ReadByte(state, i);
                    bool pressed = (stateByte != 0);
                    input.Update((focus && pressed) ? 1.0f : 0.0f);
                }
            }

            // Swap text buffers
            var lastText = m_lastText;
            m_lastText = m_pendingText;
            m_pendingText = lastText;
            m_pendingText.Clear();
        }
    }
}
#endif
