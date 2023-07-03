using System;

namespace Dan200.Core.GUI
{
    [Flags]
    internal enum Anchor
    {
        Top = 1,
        Left = 2,
        Bottom = 4,
        Right = 8,

        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right,
        Centre = 0,
    }
}
