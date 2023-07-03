using Dan200.Core.Assets;
using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    // Values must match SDL_GameControllerButton
    internal enum GamepadButton
    {
        A = 0,
        B = 1,
        X = 2,
        Y = 3,
        Back = 4,
        Start = 6,
        LeftStick = 7,
        RightStick = 8,
        LeftBumper = 9,
        RightBumper = 10,
        Up = 11,
        Down = 12,
        Left = 13,
        Right = 14,
    }

    internal static class GamepadButtonExtensions
    {
        public static string GetPrompt(this GamepadButton button, GamepadType type)
        {
            // TODO: Incorporate gamepad type
            return "Inputs.Gamepad." + button;
        }
    }
}

