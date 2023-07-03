using System;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Game.Components.Editor;

namespace Dan200.Core.Interfaces
{
    internal interface IEditable : IComponentInterface
    {
        void AddManipulators(EditorComponent editor);
        void RemoveManipulators(EditorComponent editor);
    }
}
