using Dan200.Core.GUI;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.GUI
{
    internal class ControlsDisplay : Element
    {
        public ControlsDisplay()
        {
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            var position = Position;
            var size = Size;
            builder.AddQuad(position, position + size, LowResUI.Blank);

            var numLines = 13;
            var font = LowResUI.TextFont;
            var fontSize = LowResUI.TextFontSize;
            var fontHeight = font.GetHeight(fontSize);

            var pos = new Vector2(
                position.X + 0.5f * size.X,
                position.Y + 0.5f * size.Y - 0.5f * numLines * fontHeight
            );

            builder.AddText(Screen.Language.Translate("controls.line1"), pos, font, fontSize, Colour.White, TextAlignment.Center);
            pos.Y += 2.0f * fontHeight;

            for(int i=2; i<=9; ++i)
            {
                builder.AddText(Screen.Language.Translate("controls.line" + i + ".1"), pos - new Vector2(16.0f, 0.0f), font, fontSize, Colour.White, TextAlignment.Right);
                builder.AddText(Screen.Language.Translate("controls.line" + i + ".2"), pos + new Vector2(16.0f, 0.0f), font, fontSize, Colour.White, TextAlignment.Left);
                pos.Y += fontHeight;
            }
            pos.Y += fontHeight;

            builder.AddText(Screen.Language.Translate("controls.line10"), pos, font, fontSize, Colour.White, TextAlignment.Center);
            pos.Y += fontHeight;

            builder.AddText(Screen.Language.Translate("controls.line11"), pos, font, fontSize, Colour.White, TextAlignment.Center);
            pos.Y += fontHeight;
        }
    }
}
