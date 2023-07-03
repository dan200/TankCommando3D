using Dan200.Core.GUI;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.GUI
{
    internal class GameOverDisplay : Element
    {
        private Texture m_texture;
        private int m_tanksKilled;

        public int TanksKilled
        {
            get
            {
                return m_tanksKilled;
            }
            set
            {
                if(m_tanksKilled != value)
                {
                    m_tanksKilled = value;
                    RequestRebuild();
                }
            }
        }

        public GameOverDisplay()
        {
            m_texture = Texture.Get("gui/gameover.png", true);
            m_tanksKilled = 0;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            var centre = Center;
            centre.X = Mathf.Round(centre.X);
            centre.Y = Mathf.Round(centre.Y);

            var imageSize = new Vector2(m_texture.Width, m_texture.Height);
            builder.AddQuad(
                centre - new Vector2(0.5f * imageSize.X, imageSize.Y),
                centre + new Vector2(0.5f * imageSize.X, 0.0f),
                m_texture
            );

            var font = LowResUI.TextFont;
            var fontSize = LowResUI.TextFontSize;
            var fontHeight = font.GetHeight(fontSize);
            builder.AddText(
                Screen.Language.TranslateCount("gameover.tanks_killed", m_tanksKilled),
                centre + new Vector2(0.0f, 1.0f * fontHeight),
                font,
                fontSize,
                Colour.White,
                TextAlignment.Center
            );
            builder.AddText(
                Screen.Language.Translate("gameover.press_restart"),
                centre + new Vector2(0.0f, 2.0f * fontHeight),
                font,
                fontSize,
                Colour.White,
                TextAlignment.Center
            );
        }
    }
}
