using System.Collections.Generic;

namespace Dan200.Core.Input
{
    internal interface IJoystick : IDevice, IVibrator
    {
        int NumButtons { get; }
        int NumAxes { get; }
        int NumHats { get; }
        Input GetButtonInput(int index);
        Input GetAxisInput(int index, JoystickAxisDirection direction);
        Input GetHatInput(int index, JoystickHatDirection direction);
    }
}
