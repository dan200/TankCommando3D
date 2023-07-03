namespace Dan200.Core.Input
{
    internal enum MouseWheelDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    internal static class MouseWheelDirectionExtensions
    {
        public static string GetPrompt(this MouseWheelDirection direction)
        {
            return "Inputs.Mouse.Wheel" + direction;
        }
    }
}
