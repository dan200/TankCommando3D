using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Lua;
using Dan200.Core.Serialisation;
using Dan200.Game.Components.Editor;

namespace Dan200.Core.Level
{
    internal abstract class EditableComponent<TComponentData> : Component<TComponentData>, IEditable, IResettable
        where TComponentData : struct
    {
        public void Reset(LuaTable properties)
        {
            Reset(LONSerialiser.Parse<TComponentData>(properties));
        }

        protected abstract void Reset(in TComponentData properties);

        public virtual void AddManipulators(EditorComponent editor)
        {
        }

        public virtual void RemoveManipulators(EditorComponent editor)
        {
        }
    }
}
