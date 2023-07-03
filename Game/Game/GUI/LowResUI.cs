using Dan200.Core.GUI;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.GUI
{
    internal static class LowResUI
    {
        public static Font TextFont
        {
            get
            {
                return Font.Get("fonts/bitmap.fnt");
            }
        }

        public static int TextFontSize
        {
            get
            {
                return 33;
            }
        }

        public static Font NumbersFont
        {
            get
            {
                return Font.Get("fonts/numbers.fnt");
            }
        }

        public static int NumbersFontSize
        {
            get
            {
                return 60;
            }
        }

        public static Texture Blank
        {
            get
            {
                return Texture.Get("gui/blank.png", false);
            }
        }
    }
}
