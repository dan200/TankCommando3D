using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    // Buttons physically mounted to a touchscreen device (or part of standard system UI)
    internal enum TouchscreenButton
    {
        Back,
    }

    internal static class TouchscreenButtonExtensions
    {
        public static string GetPrompt(this TouchscreenButton button)
        {
            var imagePath = button.GetPromptImagePath();
            if (imagePath != null)
            {
                return '[' + imagePath + ']';
            }
            else
            {
                return button.ToString().ToProperSpaced();
            }
        }

        private static string GetPromptImagePath(this TouchscreenButton button)
        {
            return null;
        }
    }
}

