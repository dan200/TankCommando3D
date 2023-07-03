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
            var imagePath = button.GetPromptImagePath(type);
            if (imagePath != null)
            {
                return '[' + imagePath + ']';
            }
            else
            {
                return button.ToString().ToProperSpaced();
            }
        }

        private static string GetPromptImagePath(this GamepadButton button, GamepadType type)
        {
            if (type == GamepadType.Unknown || type == GamepadType.XboxOne)
            {
                type = GamepadType.Xbox360;
            }
            else if (type == GamepadType.PS4)
            {
                type = GamepadType.PS3;
            }

            string buttonName;
            switch (button)
            {
                case GamepadButton.A:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "cross";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "a";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.B:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "circle";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "b";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.X:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "square";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "x";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.Y:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "triangle";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "y";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.Back:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                                {
                                    buttonName = "select";
                                    break;
                                }
                            case GamepadType.PS4:
                                {
                                    buttonName = "share";
                                    break;
                                }
                            case GamepadType.XboxOne:
                                {
                                    buttonName = "view";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "back";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.Start:
                    {
                        switch (type)
                        {
                            case GamepadType.PS4:
                                {
                                    buttonName = "options";
                                    break;
                                }
                            case GamepadType.XboxOne:
                                {
                                    buttonName = "menu";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "start";
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        buttonName = button.ToString().ToLowerUnderscored();
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

