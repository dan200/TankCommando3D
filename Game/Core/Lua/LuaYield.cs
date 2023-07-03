using System;

namespace Dan200.Core.Lua
{
    internal delegate LuaArgs LuaContinuation(in LuaArgs resumeArgs);

    internal class LuaYield : Exception
    {
        public LuaArgs Results; // TODO: Make me readonly again
        public readonly LuaContinuation Continuation;

        public LuaYield(LuaArgs results, LuaContinuation continuation) : base("Unhandled lua yield")
        {
            Results = results;
            Continuation = continuation;
        }
    }

    internal class LuaAsyncCall : Exception
    {
        public readonly LuaFunction Function;
        public LuaArgs Arguments; // TODO: Make me readonly again
        public readonly LuaContinuation Continuation;

        public LuaAsyncCall(LuaFunction function, LuaArgs arguments, LuaContinuation continuation) : base("Unhandled lua async call")
        {
            Function = function;
            Arguments = arguments;
            Continuation = continuation;
        }
    }
}
