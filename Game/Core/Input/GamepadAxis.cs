using Dan200.Core.Assets;
using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    internal enum GamepadAxis
    {
        LeftStickUp,
        LeftStickDown,
        LeftStickLeft,
        LeftStickRight,
        RightStickUp,
        RightStickDown,
        RightStickLeft,
        RightStickRight,
        LeftTrigger,
        RightTrigger,
    }

    internal static class GamepadAxisExtensions
    {
        public static string GetPrompt(this GamepadAxis axis, GamepadType type)
        {
            // TODO: Incorporate gamepad type
            return "Inputs.Gamepad." + axis;
        }
    }
}

