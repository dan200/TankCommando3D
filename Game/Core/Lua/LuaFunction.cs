using Dan200.Core.Main;

namespace Dan200.Core.Lua
{
    internal class LuaFunction
    {
        public readonly LuaMachine Machine;
        internal readonly int ID;

        internal LuaFunction(LuaMachine machine, int id)
        {
            Machine = machine;
            ID = id;
        }

        ~LuaFunction()
        {
            if (!Machine.Disposed)
            {
                Machine.Release(this);
            }
        }

        public LuaArgs Call(in LuaArgs args)
        {
            if (Machine.Disposed)
            {
                throw new LuaError("Attempt to call dead function", 0);
            }
            return Machine.Call(this, args);
        }

        public LuaArgs CallAsync(in LuaArgs args, LuaContinuation continuation = null)
        {
            if (Machine.Disposed)
            {
                throw new LuaError("Attempt to call dead function", 0);
            }
            return Machine.CallAsync(this, args, continuation);
        }

        public void Invoke(in LuaArgs args)
        {
            if (Machine.Disposed)
            {
                throw new LuaError("Attempt to call dead function", 0);
            }
            try
            {
                Machine.Call(this, args);
            }
            catch(LuaError e)
            {
                App.LogError(e.Message);
            }
        }
    }
}
