using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    // Values match SDL2 SDL_BUTTON_xxx constants
    internal enum MouseButton
    {
        Left = 1,
        Middle,
        Right,
        Mouse4,
        Mouse5,
        Mouse6,
        Mouse7,
        Mouse8,
        Mouse9,
        Mouse10,
        Mouse11,
        Mouse12,
        Mouse13,
        Mouse14,
        Mouse15,
        Mouse16,
    }

    internal static class MouseButtonExtensions
    {
        public static string GetPrompt(this MouseButton button)
        {
            var path = button.GetPromptPath();
            if (path != null)
            {
                return '[' + button.GetPromptPath() + ']';
            }
            else
            {
                return button.ToString();
            }
        }

        public static string GetPromptPath(this MouseButton button)
        {
            if (button >= MouseButton.Left && button <= MouseButton.Right)
            {
                var buttonName = button.ToString().ToLowerUnderscored();
                return "gui/prompts/mouse/" + buttonName + ".png";
            }
            return null;
        }
    }
}
