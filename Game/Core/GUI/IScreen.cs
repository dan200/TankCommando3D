using Dan200.Core.Math;

namespace Dan200.Core.GUI
{
    internal interface IScreen
    {
        Vector2 WindowToScreen(Vector2I windowPos);
    }
}
