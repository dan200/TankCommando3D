using System.Collections.Generic;

namespace Dan200.Core.Input
{
    internal interface IGamepad : IDevice, IVibrator
    {
        GamepadType GamepadType { get; }
        Input GetInput(GamepadButton button);
        Input GetInput(GamepadAxis axis);
    }
}
