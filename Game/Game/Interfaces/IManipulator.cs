using Dan200.Core.Input;
using Dan200.Core.Level;
using Dan200.Core.Render;

namespace Dan200.Game.Components.Editor
{
    internal interface IManipulator : IComponentInterface
    {
        bool HandleMouseInput(IMouse mouse, Camera camera);
    }
}
