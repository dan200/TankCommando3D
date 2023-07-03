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
            return '[' + axis.GetPromptImagePath(type) + ']';
        }

        private static string GetPromptImagePath(this GamepadAxis axis, GamepadType type)
        {
            if (type == GamepadType.Unknown)
            {
                type = GamepadType.Xbox360;
            }

            string buttonName;
            switch (axis)
            {
                case GamepadAxis.LeftStickUp:
                case GamepadAxis.LeftStickDown:
                case GamepadAxis.LeftStickLeft:
                case GamepadAxis.LeftStickRight:
                    {
                        buttonName = "left_stick";
                        break;
                    }
                case GamepadAxis.RightStickUp:
                case GamepadAxis.RightStickDown:
                case GamepadAxis.RightStickLeft:
                case GamepadAxis.RightStickRight:
                    {
                        buttonName = "right_stick";
                        break;
                    }
                default:
                    {
                        buttonName = axis.ToString().ToLowerUnderscored();
                        break;
                    }
            }

            return AssetPath.Combine(
                "gui/prompts/" + type.ToString().ToLowerUnderscored(),
                buttonName + ".png"
            );
        }
    }
}

