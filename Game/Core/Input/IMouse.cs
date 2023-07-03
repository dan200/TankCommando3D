using System.Collections.Generic;
using Dan200.Core.Math;

namespace Dan200.Core.Input
{
    internal interface IMouse : IDevice
    {
        Vector2I Position { get; }
        Vector2I Delta { get; }
        bool Locked { get; set; }
        bool ShowCursor { get; set; }
        Input GetInput( MouseButton button );
        Input GetInput( MouseWheelDirection direction );
    }
}
