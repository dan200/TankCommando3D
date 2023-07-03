using Dan200.Core.Audio;
using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal class Button : ButtonBase
    {
        private Texture m_texture;

        private Quad m_region;
        private Quad m_hoverRegion;
        private Quad m_disabledRegion;
        private Quad m_heldRegion;

        private Colour m_colour;
        private Colour m_hoverColour;
        private Colour m_disabledColour;
        private Colour m_heldColour;

        public Texture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                m_texture = value;
            }
        }

        public Quad Region
        {
            get
            {
                return m_region;
            }
            set
            {
                m_region = value;
                RequestRebuild();
            }
        }

        public Quad HoverRegion
        {
            get
            {
                return m_hoverRegion;
            }
            set
            {
                m_hoverRegion = value;
                RequestRebuild();
            }
        }

        public Quad DisabledRegion
        {
            get
            {
                return m_disabledRegion;
            }
            set
            {
                m_disabledRegion = value;
                RequestRebuild();
            }
        }

        public Quad HeldRegion
        {
            get
            {
                return m_heldRegion;
            }
            set
            {
                m_heldRegion = value;
                RequestRebuild();
            }
        }

        public Colour Colour
        {
            get
            {
                return m_colour;
            }
            set
            {
                m_colour = value;
            }
        }

        public Colour HoverColour
        {
            get
            {
                return m_hoverColour;
            }
            set
            {
                m_hoverColour = value;
            }
        }

        public Colour DisabledColour
        {
            get
            {
                return m_disabledColour;
            }
            set
            {
                m_disabledColour = value;
            }
        }

        public Colour HeldColour
        {
            get
            {
                return m_heldColour;
            }
            set
            {
                m_heldColour = value;
            }
        }
        
        public Button(Texture texture, float width, float height) : base(width, height)
        {
            m_texture = texture;

            m_region = Quad.UnitSquare;
            m_hoverRegion = Quad.UnitSquare;
            m_disabledRegion = Quad.UnitSquare;
            m_heldRegion = Quad.UnitSquare;

            m_colour = Colour.White;
            m_hoverColour = Colour.White;
            m_disabledColour = Colour.White;
            m_heldColour = Colour.White;

            OnHoverEnter += delegate
            {
                if (!Held)
                {
                    PlayHoverSound();
                }
				RequestRebuild();
            };
			OnHoverLeave += delegate
			{
				RequestRebuild();
			};
            OnPressed += delegate
            {
                PlayDownSound();
				RequestRebuild();
            };
            OnReleased += delegate
            {
                PlayUpSound();
                RequestRebuild();
            };
			OnCancelled += delegate
			{
				RequestRebuild();
			};
        }

        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
			var origin = Position;
            if (Blocked || Disabled)
            {
				// Draw self (disabled)
				builder.AddQuad(origin, Size, m_texture, m_disabledRegion, m_disabledColour);
            }
            else
            {
                // Draw self
                switch (State)
                {
                    case ButtonState.Idle:
                    default:
                        {
							builder.AddQuad(origin, Size, m_texture, m_region, m_colour);
                            break;
                        }
                    case ButtonState.Hover:
                        {
							builder.AddQuad(origin, Size, m_texture, m_hoverRegion, m_hoverColour);
                            break;
                        }
                    case ButtonState.Held:
                        {
							builder.AddQuad(origin, Size, m_texture, m_heldRegion, m_heldColour);
                            break;
                        }
                }
            }
        }

        private void PlayHoverSound()
        {
//                Screen.Audio.PlaySound(Sound.Get("sound/menu_highlight.wav"));
        }

        private void PlayDownSound()
        {
  //              Screen.Audio.PlaySound(Sound.Get("sound/menu_down.wav"));
        }

        private void PlayUpSound()
        {
    //            Screen.Audio.PlaySound(Sound.Get("sound/menu_up.wav"));
        }
    }
}
