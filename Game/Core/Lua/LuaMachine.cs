using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LUA = Lua.Lua;
using Dan200.Core.Main;

#if IOS
using MonoPInvokeCallbackAttribute = ObjCRuntime.MonoPInvokeCallbackAttribute;
#else
using MonoPInvokeCallbackAttribute = Lua.MonoPInvokeCallbackAttribute;
#endif

namespace Dan200.Core.Lua
{
    internal unsafe class LuaMachine : IDisposable
    {
        public static Version Version
        {
            get
            {
                return new Version(LUA.LUA_VERSION_MAJOR, LUA.LUA_VERSION_MINOR);
            }
        }

        private const int s_hookInterval = 1000;
        private const int s_firstTimeLimit = 5000000; // The number of instructions until the first timeout error is emitted
        private const int s_secondTimeLimit = 2000000; // The number of instructions in which the CPU must yield after the first timeout

        [ThreadStatic]
        private static int s_nextMemoryLookupID = 0;
        [ThreadStatic]
        private static Dictionary<IntPtr, LuaMachine> s_memoryLookup;
        [ThreadStatic]
        private static Dictionary<IntPtr, LuaMachine> s_machineLookup;

        private static readonly LUA.lua_Hook s_hookDelegate;
        private static readonly LUA.lua_Alloc s_allocDelegate;
        private static readonly LUA.lua_KFunction s_continueDelegate;
        private static readonly LUA.lua_Writer s_dumpDelegate;
        private static readonly Dictionary<string, LUA.lua_CFunction> s_cFunctionDelegates;

        private static readonly ByteString str_native_load = ByteString.Intern("native_load");
        private static readonly ByteString str_load = ByteString.Intern("load");
        private static readonly ByteString str_luafunction_metatable = ByteString.Intern("luafunction_metatable");
        private static readonly ByteString str___gc = ByteString.Intern("__gc");
        private static readonly ByteString str___type = ByteString.Intern("__type");
        private static readonly ByteString str___index = ByteString.Intern("__index");
        private static readonly ByteString str___tostring = ByteString.Intern("__tostring");
        private static readonly ByteString str_tables_seen = ByteString.Intern("tables_seen");
        private static readonly ByteString str_t = ByteString.Intern("t");
        private static readonly ByteString str_t_null = ByteString.Intern("t\0");
        private static readonly ByteString str_b_null = ByteString.Intern("b\0");
        private static readonly ByteString str_timeout = ByteString.Intern("timeout");

        static LuaMachine()
        {
            s_hookDelegate = new LUA.lua_Hook(Debug_Hook);
            s_allocDelegate = new LUA.lua_Alloc(Alloc);
            s_continueDelegate = new LUA.lua_KFunction(LuaFunction_Continue);
            s_dumpDelegate = new LUA.lua_Writer(Dump);
            s_cFunctionDelegates = new Dictionary<string, LUA.lua_CFunction>();
            s_cFunctionDelegates.Add("Load", Load);
            s_cFunctionDelegates.Add("LuaObject_GC", LuaObject_GC);
            s_cFunctionDelegates.Add("LuaFunction_Call", LuaFunction_Call);
            s_cFunctionDelegates.Add("LuaFunction_GC", LuaFunction_GC);
        }

        private class ObjectLookup
        {
            private static ByteString str_anchors = ByteString.Intern("anchors");
            private static ByteString str_strong_anchors = ByteString.Intern("strong_anchors");
            private static ByteString str___mode = ByteString.Intern("__mode");
            private static ByteString str_v = ByteString.Intern("v");

            private LuaMachine m_parent;
            private Dictionary<object, int> m_objectToID;
            private Dictionary<int, object> m_IDToObject;
            private List<int> m_releasedObjects;
            private int m_nextID;

            public LuaMachine Machine
            {
                get
                {
                    return m_parent;
                }
            }

            public IEnumerable<object> KnownObjects
            {
                get
                {
                    return m_objectToID.Keys;
                }
            }

            public ObjectLookup(LuaMachine parent, IntPtr state)
            {
                // Create ID table
                m_parent = parent;
                m_objectToID = new Dictionary<object, int>();
                m_IDToObject = new Dictionary<int, object>();
                m_releasedObjects = new List<int>();
                m_nextID = 1;

                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Create anchor table
					LUA.lua_pushlstring(state, str_anchors); // 1
                    LUA.lua_createtable(state, 0, 0); // 2
                    {
                        LUA.lua_createtable(state, 0, 1); // 3
						LUA.lua_pushlstring(state, str___mode); // 4
                        LUA.lua_pushlstring(state, str_v); // 54
                        LUA.lua_rawset(state, -3); // 3
                        LUA.lua_setmetatable(state, -2); // 2
                    }
                    LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 0

                    // Create permanent anchor table
                    LUA.lua_pushlstring(state, str_strong_anchors); // 1
                    LUA.lua_createtable(state, 0, 0); // 2
                    LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 0
                }
                finally
                {
                    // Allow OOM
                    parent.AllowOOM();
                }
            }

            public int StoreObjectAndValue(IntPtr state, object obj, int valueIndex, bool permanent)
            {
                var id = NewID();

                // Store object in ID table
                m_objectToID[obj] = id;
                m_IDToObject[id] = obj;

                // Store ID and value
                StoreValue(state, valueIndex, id, permanent);
                return id;
            }

            public int StoreValueOnly(IntPtr state, int valueIndex, bool permanent)
            {
                var id = NewID();
                StoreValue(state, valueIndex, id, permanent);
                return id;
            }

            private void StoreValue(IntPtr state, int valueIndex, int id, bool permanent)
            {
                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Store value in anchor table
                    LUA.lua_pushvalue(state, valueIndex); // 11

                    LUA.lua_pushlstring(state, str_anchors); // 2
                    LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                    LUA.lua_pushvalue(state, -2); // 3
                    LUA.lua_rawseti(state, -2, id); // 2
                    LUA.lua_pop(state, 1); // 1

                    if (permanent)
                    {
                        // Store value in permanent anchor table
                        LUA.lua_pushlstring(state, str_strong_anchors); // 2
                        LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                        LUA.lua_pushvalue(state, -2); // 3
                        LUA.lua_rawseti(state, -2, id); // 2
                        LUA.lua_pop(state, 1); // 1
                    }

                    LUA.lua_pop(state, 1); // 0
                }
                finally
                {
                    // Allow OOM
                    m_parent.AllowOOM();
                }
            }

            public void RemoveObjectAndValue(IntPtr state, int id)
            {
                // Remove object from ID table
                object obj;
                if (m_IDToObject.TryGetValue(id, out obj))
                {
                    m_IDToObject.Remove(id);
                    m_objectToID.Remove(obj);
                }

                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Remove value from anchor table
					LUA.lua_pushlstring(state, str_anchors); // 1
                    LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 1
                    LUA.lua_pushnil(state); // 2
                    LUA.lua_rawseti(state, -2, id); // 1
                    LUA.lua_pop(state, 1); // 0

                    // Remove value from permanent anchor table
					LUA.lua_pushlstring(state, str_strong_anchors); // 1
                    LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 1
                    LUA.lua_pushnil(state); // 2
                    LUA.lua_rawseti(state, -2, id); // 1
                    LUA.lua_pop(state, 1); // 0
                }
                finally
                {
                    // Allow OOM
                    m_parent.AllowOOM();
                }
            }

            public object GetObjectForID(int id)
            {
                object obj;
                if (m_IDToObject.TryGetValue(id, out obj))
                {
                    return obj;
                }
                else
                {
                    return null;
                }
            }

            public bool PushValueForObject(IntPtr state, object obj) // +1|0
            {
                // Get ID from ID table
                int id;
                if (m_objectToID.TryGetValue(obj, out id))
                {
                    return PushValueForID(state, id); // 1|0
                }
                return false; // 0
            }

            public bool PushValueForID(IntPtr state, int id) // +1|0
            {
                try
                {
                    // Prevent OOM
                    m_parent.PreventOOM();

                    // Push value from anchor table
                    LUA.lua_pushlstring(state, str_anchors); // 1
                    LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 1
                    LUA.lua_rawgeti(state, -1, id); // 2
                    var type = LUA.lua_type(state, -1);
                    if (type != LUA.LUA_TNIL)
                    {
                        LUA.lua_remove(state, -2); // 1
                        return true;
                    }
                    else
                    {
                        LUA.lua_pop(state, 2); // 0
                        return false;
                    }
                }
                finally
                {
                    // Allow OOM
                    m_parent.AllowOOM();
                }
            }

            public void RemoveReleasedObjects(IntPtr state)
            {
                lock (m_releasedObjects)
                {
                    foreach (var id in m_releasedObjects)
                    {
                        RemoveObjectAndValue(state, id);
                    }
                    m_releasedObjects.Clear();
                }
            }

            public void ReleaseObject(int id)
            {
                lock (m_releasedObjects)
                {
                    m_releasedObjects.Add(id);
                }
            }

            // Misc

            private int NewID()
            {
                return m_nextID++;
            }
        }

        private IntPtr m_mainState;
        private IntPtr m_runningState;
        private ObjectLookup m_objectLookup;

        private Dictionary<int, LuaContinuation> m_pendingContinuations;
        private int m_nextContinuationID;

        private int m_instructionsExecutedThisTimeout;
        private bool m_firstTimeoutEmitted;

        private MemoryTracker m_memoryTracker;
        private int m_forceAllocations;
        private IntPtr m_memoryTrackerID;
        private int m_instructionsExecuted;

        private ByteStringBuilder m_dumpStringBuilder;

        public bool AllowByteCodeLoading = true;

        public bool Disposed
        {
            get
            {
                return m_mainState == IntPtr.Zero;
            }
        }

        public int InstructionsExecuted
        {
            get
            {
                return m_instructionsExecuted;
            }
        }

        public LuaMachine(MemoryTracker memory = null, bool enforceTimeLimits = false)
        {
            // Create machine
            m_memoryTracker = memory;
            m_forceAllocations = 0;
            if (m_memoryTracker != null)
            {
                var id = new IntPtr(s_nextMemoryLookupID++);
                if (s_memoryLookup == null)
                {
                    s_memoryLookup = new Dictionary<IntPtr, LuaMachine>(IntPtrComparer.Instance);
                }
                s_memoryLookup.Add(id, this);
                m_memoryTrackerID = id;
                try
                {
                    PreventOOM();
                    m_mainState = LUA.lua_newstate(s_allocDelegate, id);
                }
                finally
                {
                    AllowOOM();
                }
            }
            else
            {
                m_memoryTrackerID = IntPtr.Zero;
                m_mainState = LUA.luaL_newstate();
            }
            if (s_machineLookup == null)
            {
                s_machineLookup = new Dictionary<IntPtr, LuaMachine>(IntPtrComparer.Instance);
            }
            s_machineLookup.Add(m_mainState, this);
            m_runningState = IntPtr.Zero;

            m_objectLookup = new ObjectLookup(this, m_mainState);
            m_pendingContinuations = new Dictionary<int, LuaContinuation>();
            m_nextContinuationID = 0;

            try
            {
                // Prevent OOM during init
                PreventOOM();

                // Install standard library
                LUA.luaL_openlibs(m_mainState);

                // Copy important globals into the registry
                // Copy load
                LUA.lua_pushlstring(m_mainState, str_native_load); // 1
                {
                    LUA.lua_pushglobaltable(m_mainState); // 2
                    LUA.lua_pushlstring(m_mainState, str_load); // 3
                    LUA.lua_rawget(m_mainState, -2); // 3
                    LUA.lua_remove(m_mainState, -2); // 2
                }
                LUA.lua_rawset(m_mainState, LUA.LUA_REGISTRYINDEX); // 0

                // Replace load with our wrapped version
                LUA.lua_pushglobaltable(m_mainState); // 1
                LUA.lua_pushlstring(m_mainState, str_load); // 2
                PushStaticCFunction(m_mainState, "Load"); // 3
                LUA.lua_rawset(m_mainState, -3); // 1
                LUA.lua_pop(m_mainState, 1); // 0

                // Create the function metatable
				LUA.lua_pushlstring(m_mainState, str_luafunction_metatable); // 1
                LUA.lua_createtable(m_mainState, 0, 1); // 2
                {
					LUA.lua_pushlstring(m_mainState, str___gc); // 3
                    PushStaticCFunction(m_mainState, "LuaFunction_GC"); // 4
                    LUA.lua_rawset(m_mainState, -3); // 2
                }
                LUA.lua_rawset(m_mainState, LUA.LUA_REGISTRYINDEX); // 0

                if (enforceTimeLimits)
                {
                    // Install hook function
                    ResetTimeoutTimer();
                    m_instructionsExecuted = 0;
                    LUA.lua_sethook(m_mainState, s_hookDelegate, LUA.LUA_MASKCOUNT | LUA.LUA_MASKCALL, s_hookInterval);
                }
            }
            finally
            {
                // Allow OOM again
                AllowOOM();
            }
        }

        public void CollectGarbage()
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }
                m_objectLookup.RemoveReleasedObjects(m_runningState);
                LUA.lua_gc(m_runningState, LUA.LUA_GCCOLLECT, 0);
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        public void RemoveUnsafeGlobals()
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Clear globals
                ClearGlobal(ByteString.Intern("collectgarbage"));
                DoString(ByteString.Intern("debug = { traceback = debug.traceback }"), ByteString.Intern("=LuaMachine.RemoveUnsafeGlobals\0"));
                ClearGlobal(ByteString.Intern("dofile"));
                ClearGlobal(ByteString.Intern("io"));
                ClearGlobal(ByteString.Intern("loadfile"));
                ClearGlobal(ByteString.Intern("package"));
                ClearGlobal(ByteString.Intern("require"));
                ClearGlobal(ByteString.Intern("os"));
                ClearGlobal(ByteString.Intern("print"));
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        public void Dispose()
        {
            CheckNotDisposed();
            App.Assert(m_runningState == IntPtr.Zero, "Attempt to Dispose LuaMachine during a CFunction");

            // Clear variables (prevents thread safety problems)
            var mainState = m_mainState;
            m_mainState = IntPtr.Zero;
            m_runningState = IntPtr.Zero;

            // Close the state
            LUA.lua_close(mainState);
            if (m_memoryTracker != null)
            {
                s_memoryLookup.Remove(m_memoryTrackerID);
                m_memoryTracker = null;
                m_memoryTrackerID = IntPtr.Zero;
            }
            s_machineLookup.Remove(mainState);

            // GC any dangling LuaObjects (if close did it's job, there shouldn't be any)
            foreach (var obj in m_objectLookup.KnownObjects)
            {
                if (obj is LuaObject)
                {
                    var luaObject = (LuaObject)obj;
                    if (luaObject.UnRef() == 0)
                    {
                        luaObject.Dispose();
                    }
                }
            }
            m_objectLookup = null;
        }

        public LuaCoroutine CreateCoroutine(LuaFunction function)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Create the coroutine
                var thread = LUA.lua_newthread(m_runningState); // 1

                // Push the function onto the coroutine stack, ready for resuming
                if (function.Machine != this || !m_objectLookup.PushValueForID(thread, function.ID)) // 1,1|0
                {
                    LUA.lua_pop(m_runningState, 1); // 0,0
                    throw new Exception("Could not find function");
                }

                // Store the coroutine in the registry
                var id = m_objectLookup.StoreValueOnly(m_runningState, -1, true); // This will be removed when LuaCoroutine is collected
                var coroutine = new LuaCoroutine(this, id);
                LUA.lua_pop(m_runningState, 1); // 0,1

                return coroutine;
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        internal bool IsFinished(LuaCoroutine co)
        {
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Get the coroutine
                if (co.Machine != this || !m_objectLookup.PushValueForID(m_runningState, co.ID)) // 1|0
                {
                    throw new Exception("Could not find coroutine");
                }

                // Get the status
                var thread = LUA.lua_tothread(m_runningState, -1);
                var status = LUA.lua_status(thread);
                switch (status)
                {
                    case LUA.LUA_OK:
                        {
                            // Running or finished
                            var finished = (LUA.lua_gettop(thread) == 0);
                            LUA.lua_pop(m_runningState, 1); // 0
                            return finished;
                        }
                    case LUA.LUA_YIELD:
                        {
                            // Suspended
                            LUA.lua_pop(m_runningState, 1); // 0
                            return false;
                        }
                    default:
                        {
                            // Errored
                            LUA.lua_pop(m_runningState, 1); // 0
                            return true;
                        }
                }
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        internal LuaArgs Resume(LuaCoroutine co, in LuaArgs args)
        {
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                IntPtr thread;
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Get the coroutine
                    if (co.Machine != this || !m_objectLookup.PushValueForID(m_runningState, co.ID)) // 1
                    {
                        LUA.lua_pop(m_runningState, 1); // 0
                        throw new Exception("Could not find coroutine");
                    }

                    // Push the arguments onto the coroutine stack
                    thread = LUA.lua_tothread(m_runningState, -1);
                    PushValues(thread, args, m_objectLookup); // 1, 1 + args.Length
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }

                // Resume the coroutine
                int resumeResult = LUA.lua_resume(thread, m_runningState, args.Length); // 1, numResults|??? + 1

                try
                {
                    // Prevent OOM
                    PreventOOM();

                    if (resumeResult == LUA.LUA_OK || resumeResult == LUA.LUA_YIELD)
                    {
                        // Return the results
                        var numResults = LUA.lua_gettop(thread);
                        var results = PopValues(thread, numResults, m_objectLookup); // 1, 0
                        LUA.lua_pop(m_runningState, 1); // 0, 0
                        return results;
                    }
                    else
                    {
                        // Throw the error
                        var e = PopValue(thread, m_objectLookup); // 1, ???
                        LUA.lua_settop(thread, 0); // 1, 0
                        LUA.lua_pop(m_runningState, 1); // 0, 0
                        throw new LuaError(e, 0);
                    }
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        public ByteString Precompile(ByteString lua, ByteString chunkName)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Load the function
                    var mode = str_t_null;
                    int loadError = LUA.luaL_loadbufferx(m_runningState, lua, chunkName, mode); // 1

                    // Check for errors
                    if (loadError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }

                    try
                    {
                        // Dump the function
                        m_dumpStringBuilder = new ByteStringBuilder();
                        int dumpError = LUA.lua_dump(m_runningState, s_dumpDelegate, IntPtr.Zero, 0); // 1
                        LUA.lua_pop(m_runningState, 1); // 0

                        // Check for errors
                        if (dumpError != 0)
                        {
                            throw new LuaError("Error dumping string", 0);
                        }

                        // Return the string
                        return m_dumpStringBuilder.ToByteString();
                    }
                    finally
                    {
                        m_dumpStringBuilder = null;
                    }
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        public LuaFunction LoadString(ByteString lua, ByteString chunkName, bool binary = false)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Load the function
                    var mode = binary ? str_b_null : str_t_null;
                    int loadError = LUA.luaL_loadbufferx(m_runningState, lua, chunkName, mode); // 1

                    // Check for errors
                    if (loadError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }

                    // Return the function
                    return PopValue(m_runningState, m_objectLookup).GetFunction(); // 0
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        internal void Release(LuaCoroutine coroutine)
        {
            var lookup = m_objectLookup;
            if (lookup != null)
            {
                // Release the coroutine
                lookup.ReleaseObject(coroutine.ID);
            }
        }

        internal LuaArgs Call(LuaFunction function, in LuaArgs args)
        {
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                int top = LUA.lua_gettop(m_runningState);
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Get the function
                    if (function.Machine != this || !m_objectLookup.PushValueForID(m_runningState, function.ID)) // 1|0
                    {
                        throw new Exception("Could not find function");
                    }

                    // Push the arguments
                    PushValues(m_runningState, args, m_objectLookup); // 1 + args.Length
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }

                // Call the function
                int callError = LUA.lua_pcall(m_runningState, args.Length, LUA.LUA_MULTRET, 0); // numResults|1

                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Check for errors
                    if (callError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }

                    // Return the results
                    int numResults = LUA.lua_gettop(m_runningState) - top;
                    return PopValues(m_runningState, numResults, m_objectLookup); // 0
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        internal LuaArgs CallAsync(LuaFunction function, in LuaArgs args, LuaContinuation continuation)
        {
            App.Assert(m_runningState != IntPtr.Zero, "Attempt to call a function asynchronously from outside a CFunction");
            throw new LuaAsyncCall(function, args, continuation);
        }

        internal void Release(LuaFunction function)
        {
            var lookup = m_objectLookup;
            if (lookup != null)
            {
                // Release the function
                lookup.ReleaseObject(function.ID);
            }
        }

        public LuaArgs DoString(ByteString lua, ByteString chunkName, bool binary = false)
        {
            CheckNotDisposed();
            App.Assert(chunkName.IsNullTerminated());
            var oldState = m_runningState;
            try
            {
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Load the function
                int top = LUA.lua_gettop(m_runningState);
                var mode = binary ? str_b_null : str_t_null;
                int loadError = LUA.luaL_loadbufferx(m_runningState, lua, chunkName, mode); // 1
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Handle errors
                    if (loadError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }

                // Call the function
                int callError = LUA.lua_pcall(m_runningState, 0, LUA.LUA_MULTRET, 0); // numResults|1
                try
                {
                    // Prevent OOM
                    PreventOOM();

                    // Handle errors
                    if (callError != LUA.LUA_OK)
                    {
                        var e = PopValue(m_runningState, m_objectLookup); // 0
                        throw new LuaError(e, 0);
                    }

                    // Return the results
                    int numResults = LUA.lua_gettop(m_runningState) - top;
                    return PopValues(m_runningState, numResults, m_objectLookup); // 0
                }
                finally
                {
                    // Allow OOM
                    AllowOOM();
                }
            }
            finally
            {
                m_runningState = oldState;
            }
        }

        public void SetGlobal(LuaValue key, LuaValue value)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Set the global
                LUA.lua_pushglobaltable(m_runningState); // 1
                PushValue(m_runningState, ref key, m_objectLookup); // 2
                PushValue(m_runningState, ref value, m_objectLookup); // 3
                LUA.lua_rawset(m_runningState, -3); // 1
                LUA.lua_pop(m_runningState, 1); // 0
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        public void ClearGlobal(LuaValue key)
        {
            SetGlobal(key, LuaValue.Nil);
        }

        public LuaValue GetGlobal(LuaValue key)
        {
            CheckNotDisposed();
            var oldState = m_runningState;
            try
            {
                PreventOOM();
                if (m_runningState == IntPtr.Zero)
                {
                    // Start execution
                    m_runningState = m_mainState;
                    ResetTimeoutTimer();
                }

                // Get the global
                LUA.lua_pushglobaltable(m_runningState); // 1
                PushValue(m_runningState, ref key, m_objectLookup); // 2
                LUA.lua_rawget(m_runningState, -2); // 2
                LUA.lua_remove(m_runningState, -2); // 1
                return PopValue(m_runningState, m_objectLookup); // 0
            }
            finally
            {
                m_runningState = oldState;
                AllowOOM();
            }
        }

        private void CheckNotDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("LuaMachine");
            }
        }

        private static void PushNil(IntPtr state)
        {
            LUA.lua_pushnil(state); // 1
        }

        private static void PushBool(IntPtr state, bool value)
        {
            LUA.lua_pushboolean(state, value); // 1
        }

#if LUA_32BITS
		private static void PushNumber (IntPtr state, float value)
		{
			LUA.lua_pushnumber (state, value); // 1
		}

		private static void PushInteger (IntPtr state, int value)
		{
			LUA.lua_pushinteger (state, value); // 1
		}
#else
        private static void PushNumber(IntPtr state, double value)
        {
            LUA.lua_pushnumber(state, value); // 1
        }

        private static void PushInteger(IntPtr state, long value)
        {
            LUA.lua_pushinteger(state, value); // 1
        }
#endif

        private static void PushString(IntPtr state, string value)
        {
            if (value != null)
            {
                var str = ByteString.Temp(value);
                LUA.lua_pushlstring(state, str); // 1
            }
            else
            {
                LUA.lua_pushnil(state); // 1
            }
        }

        private static void PushByteString(IntPtr state, ByteString value)
        {
            LUA.lua_pushlstring(state, value); // 1
        }

        private static void PushTable(IntPtr state, LuaTable table, ObjectLookup objectLookup)
        {
            if (table != null)
            {
                LUA.lua_createtable(state, 0, table.Count); // 1
                foreach (var pair in table)
                {
                    var k = pair.Key;
                    var v = pair.Value;
                    PushValue(state, ref k, objectLookup); // 2
                    PushValue(state, ref v, objectLookup); // 3
                    LUA.lua_rawset(state, -3); // 1
                }
            }
            else
            {
                LUA.lua_pushnil(state); // 1
            }
        }

        private static void PushUserdata(IntPtr state, IntPtr value)
        {
            LUA.lua_pushlightuserdata(state, value); // 1
        }

        private delegate LuaArgs LuaMethod<TObject>(TObject o, in LuaArgs args) where TObject : LuaObject;

        private static LuaCFunction CreateLuaMethodCaller<TObject>(MethodInfo method) where TObject : LuaObject
		{
            var methodCallDelegate = (LuaMethod<TObject>)Delegate.CreateDelegate(typeof(LuaMethod<TObject>), method);
			return (LuaCFunction)delegate (in LuaArgs args)
			{
				var o = args.GetObject<TObject>(0);
        		var subArgs = args.Select(1); // TODO: Surely we can do this more effeciently
        		return methodCallDelegate.Invoke(o, subArgs);
			};
		}

		private static Func<MethodInfo, LuaCFunction> CreateLuaMethodCallerCreator(Type type)
		{
			var thisType = typeof(LuaMachine);
			var createMethodCallerGeneric = thisType.GetMethod("CreateLuaMethodCaller", BindingFlags.Static | BindingFlags.NonPublic);
			var createMethodCallerConcrete = createMethodCallerGeneric.MakeGenericMethod(type);
			return (Func<MethodInfo, LuaCFunction>)Delegate.CreateDelegate(typeof(Func<MethodInfo, LuaCFunction>), createMethodCallerConcrete);
		}

        private static void PushTypeMetatable(IntPtr state, Type type, ObjectLookup objectLookup) // +1
        {
            // See if the metatable is already registered
            if (objectLookup.PushValueForObject(state, type)) // 1|0
            {
                return;
            }

            // Get the type attribute
            bool exposeType = false;
            var typeName = ByteString.Empty;
            var typeAttribute = type.GetCustomAttribute<LuaTypeAttribute>();
            if (typeAttribute != null)
            {
                exposeType = typeAttribute.ExposeType;
                if (exposeType)
                {
                    if (typeAttribute.CustomName != null)
                    {
                        typeName = ByteString.Intern(typeAttribute.CustomName);
                    }
                    else
                    {
                        typeName = ByteString.Intern(type.Name);
                    }
                }
            }
            else
            {
                throw new InvalidDataException("Type " + type.Name + " is missing LuaTypeAttribute");
            }

            // Create the metatable
            var metatable = new LuaTable(3);

            if (exposeType)
            {
                // Setup __type
                metatable[str___type] = typeName;
            }

            // Setup __index
            var indexTable = new LuaTable();
            {
                // Add methods
                var methods = type.GetMethods();
				var functionCreator = CreateLuaMethodCallerCreator(type);
                for (int i = 0; i < methods.Length; ++i)
                {
                    var method = methods[i];
                    var methodAttribute = method.GetCustomAttribute<LuaMethodAttribute>();
                    if (methodAttribute != null)
                    {
                        string name;
                        if (methodAttribute.CustomName != null)
                        {
                            name = methodAttribute.CustomName;
                        }
                        else
                        {
                            name = method.Name;
                        }
                        if (exposeType || name != "getType")
                        {
                            var str = ByteString.Intern(name);
							var function = functionCreator.Invoke(method);
                            indexTable[str] = function;
                        }
                    }
                }
            }
            metatable[str___index] = indexTable;

            // Setup __tostring
            metatable[str___tostring] = (LuaCFunction)LuaObject_ToString;

            // Add additional methods
            var customise = type.GetMethod("CustomiseMetatable", BindingFlags.Static | BindingFlags.Public);
            if (customise != null)
            {
                customise.Invoke(null, new object[] { metatable });
            }

            // Push the table
            PushTable(state, metatable, objectLookup); // 1

            // Add __gc
			LUA.lua_pushlstring(state, str___gc); // 2
            PushStaticCFunction(state, "LuaObject_GC"); // 3
            LUA.lua_rawset(state, -3); // 1

            // Store the metatable in the registry
            objectLookup.StoreObjectAndValue(state, type, -1, true);
        }

        private static void PushLuaObject(IntPtr state, LuaObject obj, ObjectLookup objectLookup) // +1
        {
            if (obj == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (!objectLookup.PushValueForObject(state, obj)) // 1|0
            {
                // Reference the object
                obj.Ref();

                // Create a new userdata
                var ud = LUA.lua_newuserdata(state, new IntPtr(sizeof(int))); // 1

                // Set the type metatable on the userdata
                PushTypeMetatable(state, obj.GetType(), objectLookup); // 2
                LUA.lua_setmetatable(state, -2); // 1

                // Store the userdata and store the ID in the userdata
                var id = objectLookup.StoreObjectAndValue(state, obj, -1, false);
                Marshal.WriteInt32(ud, id);
            }
        }

        private static void PushCFunction(IntPtr state, LuaCFunction function, ObjectLookup objectLookup) // +1
        {
            if (function == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (!objectLookup.PushValueForObject(state, function)) // 1|0
            {
                // Create a userdata
                var ud = LUA.lua_newuserdata(state, new IntPtr(sizeof(int))); // 1

                // Set the luafunction metatable on the userdata
				LUA.lua_pushlstring(state, str_luafunction_metatable); // 2
                LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                LUA.lua_setmetatable(state, -2); // 1

                // Create a closure
                PushStaticCClosure(state, "LuaFunction_Call", 1); // 1

                // Store the closure and store the ID in the userdata
                var id = objectLookup.StoreObjectAndValue(state, function, -1, false);
                Marshal.WriteInt32(ud, id);
            }
        }

        private static void PushFunction(IntPtr state, LuaFunction function, ObjectLookup objectLookup) // +1
        {
            if (function == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (function.Machine != objectLookup.Machine || !objectLookup.PushValueForID(state, function.ID)) // 1|0
            {
                LUA.lua_pushnil(state); // 1 (should never happen)
            }
        }

        private static void PushCoroutine(IntPtr state, LuaCoroutine coroutine, ObjectLookup objectLookup) // +1
        {
            if (coroutine == null)
            {
                LUA.lua_pushnil(state); // 1
            }
            else if (coroutine.Machine != objectLookup.Machine || !objectLookup.PushValueForID(state, coroutine.ID)) // 1|0
            {
                LUA.lua_pushnil(state); // 1
            }
        }

        private static LUA.lua_CFunction GetStaticCFunction(string name)
        {
            LUA.lua_CFunction result;
            if (s_cFunctionDelegates.TryGetValue(name, out result))
            {
                return result;
            }
            else
            {
                throw new Exception("Static method " + name + " is not registered!");
            }
        }

        private static void PushStaticCFunction(IntPtr state, string name)
        {
            LUA.lua_pushcfunction(state, GetStaticCFunction(name));
        }

        private static void PushStaticCClosure(IntPtr state, string name, int count)
        {
            LUA.lua_pushcclosure(state, GetStaticCFunction(name), count);
        }

        private static void PushValue(IntPtr state, ref LuaValue value, ObjectLookup objectLookup)
        {
            if (value.IsBool())
            {
                PushBool(state, value.GetBool());
            }
            else if (value.IsNumber())
            {
#if LUA_32BITS
				if (value.IsInteger ()) {
					PushInteger (state, value.GetInt ());
				} else {
					PushNumber (state, value.GetFloat ());
				}
#else
                if (value.IsInteger())
                {
                    PushInteger(state, value.GetLong());
                }
                else
                {
                    PushNumber(state, value.GetDouble());
                }
#endif
            }
            else if (value.IsString())
            {
                if (value.IsByteString())
                {
                    PushByteString(state, value.GetByteString());
                }
                else
                {
                    PushString(state, value.GetString());
                }
            }
            else if (value.IsTable())
            {
                PushTable(state, value.GetTable(), objectLookup);
            }
            else if (value.IsObject())
            {
                PushLuaObject(state, value.GetObject(), objectLookup);
            }
            else if (value.IsCFunction())
            {
                PushCFunction(state, value.GetCFunction(), objectLookup);
            }
            else if (value.IsUserdata())
            {
                PushUserdata(state, value.GetUserdata());
            }
            else if (value.IsFunction())
            {
                PushFunction(state, value.GetFunction(), objectLookup);
            }
            else if (value.IsCoroutine())
            {
                PushCoroutine(state, value.GetCoroutine(), objectLookup);
            }
            else
            {
                PushNil(state);
            }
        }

        private static void PushValues(IntPtr state, in LuaArgs args, ObjectLookup lookup)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                var v = args[i];
                PushValue(state, ref v, lookup); // i+1
            } // args.Length
        }

        private static LuaValue PopValue(IntPtr state, ObjectLookup objectLookup)
        {
            try
            {
                return GetValue(state, -1, objectLookup, true);
            }
            finally
            {
                LUA.lua_pop(state, 1); // -1
            }
        }

		private static LuaValue GetValue(IntPtr state, int index, ObjectLookup objectLookup, bool copyStrings)
        {
            // Get the value
            bool tableTrackerCreated = false;
            try
            {
                return GetValueImpl(state, index, objectLookup, copyStrings, ref tableTrackerCreated);
            }
            finally
            {
                if (tableTrackerCreated)
                {
                    // Flush the tracker table
                    LUA.lua_pushlstring(state, str_tables_seen); // 1
                    LUA.lua_pushnil(state); // 2
                    LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 0
                }
            }
        }

        private static LuaValue GetValueImpl(IntPtr state, int index, ObjectLookup objectLookup, bool copyStrings, ref bool io_tableTrackerCreated)
        {
            int type = LUA.lua_type(state, index);
            switch (type)
            {
                case LUA.LUA_TNIL:
                case LUA.LUA_TNONE:
                default:
                    {
                        return LuaValue.Nil;
                    }
                case LUA.LUA_TBOOLEAN:
                    {
                        var b = LUA.lua_toboolean(state, index);
                        return b ? LuaValue.True : LuaValue.False;
                    }
                case LUA.LUA_TNUMBER:
                    {
                        if (LUA.lua_isinteger(state, index))
                        {
                            var l = LUA.lua_tointeger(state, index);
                            return new LuaValue(l);
                        }
                        else
                        {
                            var d = LUA.lua_tonumber(state, index);
                            return new LuaValue(d);
                        }
                    }
                case LUA.LUA_TSTRING:
                    {
                        IntPtr len;
                        var ptr = LUA.lua_tolstring(state, index, out len);
						var str = new ByteString(ptr, (int)len);
						if (copyStrings)
						{
                            str = str.MakePermanent();
						}
                        return new LuaValue(str);
                    }
                case LUA.LUA_TTABLE:
                    {
                        // Get the table
                        LUA.lua_pushvalue(state, index); // 1

                        // Get or create a table to track tables seen
                        if (io_tableTrackerCreated)
                        {
                            LUA.lua_pushlstring(state, str_tables_seen); // 2
                            LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // 2
                        }
                        else
                        {
                            LUA.lua_createtable(state, 0, 1); // 2
                            LUA.lua_pushlstring(state, str_tables_seen); // 3
                            LUA.lua_pushvalue(state, -2); // 4
                            LUA.lua_rawset(state, LUA.LUA_REGISTRYINDEX); // 2
                            io_tableTrackerCreated = true;
                        }

                        // Check the table hasn't already been seen
                        LUA.lua_pushvalue(state, -2); // 3
                        if (LUA.lua_rawget(state, -2) != LUA.LUA_TNIL)
                        { // 3
                          // Return nil for recursed tables
                            LUA.lua_pop(state, 3); // 0
                            return LuaValue.Nil;
                        }
                        else
                        {
                            LUA.lua_pop(state, 1); // 2
                        }

                        // Remember the table
                        LUA.lua_pushvalue(state, -2); // 3
                        LUA.lua_pushboolean(state, true); // 4
                        LUA.lua_rawset(state, -3); // 2
                        LUA.lua_pop(state, 1); // 1

                        var table = new LuaTable();
                        LUA.lua_pushnil(state); // 2
                        while (LUA.lua_next(state, -2) != 0)
                        { // 3|1
							var k = GetValueImpl(state, -2, objectLookup, copyStrings, ref io_tableTrackerCreated);
							var v = GetValueImpl(state, -1, objectLookup, copyStrings, ref io_tableTrackerCreated);
                            if (!k.IsNil() && !v.IsNil())
                            {
                                table[k] = v;
                            }
                            LUA.lua_pop(state, 1); // 2
                        }
                        LUA.lua_pop(state, 1); // 0
                        return new LuaValue(table);
                    }
                case LUA.LUA_TUSERDATA:
                    {
                        var ud = LUA.lua_touserdata(state, index);
                        var id = Marshal.ReadInt32(ud);
                        var obj = objectLookup.GetObjectForID(id);
                        if (obj is LuaObject)
                        {
                            return new LuaValue((LuaObject)obj);
                        }
                        else
                        {
                            return new LuaValue(IntPtr.Zero); // Should never happen
                        }
                    }
                case LUA.LUA_TLIGHTUSERDATA:
                    {
                        var ud = LUA.lua_touserdata(state, index);
                        return new LuaValue(ud);
                    }
                case LUA.LUA_TFUNCTION:
                    {
                        var id = objectLookup.StoreValueOnly(state, index, true); // This will be removed when LuaFunction is collected
                        var function = new LuaFunction(objectLookup.Machine, id);
                        return new LuaValue(function);
                    }
                case LUA.LUA_TTHREAD:
                    {
                        var id = objectLookup.StoreValueOnly(state, index, true); // This will be removed when LuaCoroutine is collected
                        var coroutine = new LuaCoroutine(objectLookup.Machine, id);
                        return new LuaValue(coroutine);
                    }
            }
        }

        private static LuaArgs PopValues(IntPtr state, int count, ObjectLookup objectLookup)
        {
            try
            {
                return GetValues(state, count, objectLookup, true);
            }
            finally
            {
                LUA.lua_pop(state, count);
            }
        }

        private static LuaArgs GetValues(IntPtr state, int count, ObjectLookup objectLookup, bool copyStrings=false)
		{
			if (count == 0)
			{
				return LuaArgs.Empty;
			}
			else if (count == 1)
			{
                return new LuaArgs(
                    GetValue(state, -1, objectLookup, copyStrings)
                );
			}
			else if (count == 2)
			{
                return new LuaArgs(
                    GetValue(state, -2, objectLookup, copyStrings),
                    GetValue(state, -1, objectLookup, copyStrings)
                );
			}
			else if (count == 3)
			{
                return new LuaArgs(
                    GetValue(state, -3, objectLookup, copyStrings),
                    GetValue(state, -2, objectLookup, copyStrings),
                    GetValue(state, -1, objectLookup, copyStrings)
                );
			}
			else if (count == 4)
			{
                return new LuaArgs(
                    GetValue(state, -4, objectLookup, copyStrings),
                    GetValue(state, -3, objectLookup, copyStrings),
                    GetValue(state, -2, objectLookup, copyStrings),
                    GetValue(state, -1, objectLookup, copyStrings)
                );
			}
			else
			{
				var extraArgs = new LuaValue[count - 4];
				for (int i = extraArgs.Length - 1; i >= 0; --i)
				{
                    extraArgs[i] = GetValue(state, -count + 4 + i, objectLookup, copyStrings);
				}
                return new LuaArgs(
                    GetValue(state, -count,     objectLookup, copyStrings),
                    GetValue(state, -count + 1, objectLookup, copyStrings),
                    GetValue(state, -count + 2, objectLookup, copyStrings),
                    GetValue(state, -count + 3, objectLookup, copyStrings),
                    extraArgs
                );
			}
		}

        private static LuaMachine LookupMachine(IntPtr state)
        {
            // Lookup the machine from the current state
            LuaMachine result;
            if (s_machineLookup.TryGetValue(state, out result))
            {
                return result;
            }

            // If that fails, get the main state
            LUA.lua_rawgeti(state, LUA.LUA_REGISTRYINDEX, LUA.LUA_RIDX_MAINTHREAD); // 1
            var mainState = LUA.lua_tothread(state, -1);
            LUA.lua_pop(state, 1); // 0

            // Lookup the machine from the main state
            if (s_machineLookup.TryGetValue(mainState, out result))
            {
                return result;
            }
            return null;
        }

        [MonoPInvokeCallback(typeof(LUA.lua_CFunction))]
        private static int Load(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                // Get the machine
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                // Pass all arguments to the native function
                int argumentCount = LUA.lua_gettop(state);
                try
                {
                    // Prevent OOM
                    machine.PreventOOM();

                    // Repush arguments, with modifications
                    LUA.lua_pushlstring(state, str_native_load); // argumentCount + 1
                    LUA.lua_rawget(state, LUA.LUA_REGISTRYINDEX); // argumentCount + 1
                    if (argumentCount >= 1)
                    {
                        LUA.lua_pushvalue(state, 1); // argumentCount + 2
                    }
                    else
                    {
                        LUA.lua_pushnil(state); // argumentCount + 2
                    }
                    if (argumentCount >= 2)
                    {
                        LUA.lua_pushvalue(state, 2); // argumentCount + 3
                    }
                    else
                    {
                        LUA.lua_pushnil(state); // argumentCount + 3
                    }
                    if (argumentCount >= 3)
                    {
                        if (machine.AllowByteCodeLoading)
                        {
                            LUA.lua_pushvalue(state, 3); // argumentCount + 4
                        }
                        else
                        {
                            int type = LUA.lua_type(state, 3);
                            if (type == LUA.LUA_TNIL)
                            {
                                LUA.lua_pushlstring(state, str_t); // argumentCount + 4
                            }
                            else if (type == LUA.LUA_TSTRING)
                            {
                                IntPtr len;
                                var ptr = LUA.lua_tolstring(state, 3, out len);
                                var mode = new ByteString(ptr, (int)len);
                                if (mode != ByteString.Intern("t"))
                                {
                                    var err = ByteString.Intern("binary chunk loading prohibited");
									return LUA.luaL_error(state, err);
                                }
                                LUA.lua_pushlstring(state, str_t);  // argumentCount + 4
                            }
                            else
                            {
                                var prefix = ByteString.Intern("bad argument #3 to 'load' (string expected, got ");
                                var typeName = LUA.lua_typename(state, type);
                                var suffix = ByteString.Intern(")");
                                LUA.luaL_where(state, 1); // argumentCount + 4
                                LUA.lua_pushlstring(state, prefix);  // argumentCount + 5
                                LUA.lua_pushstring(state, typeName); // argumentCount + 6
                                LUA.lua_pushlstring(state, suffix);  // argumentCount + 7
                                LUA.lua_concat(state, 4); // argumentCount + 3;
                                return LUA.lua_error(state);
                            }
                        }
                    }
                    else
                    {
                        if (machine.AllowByteCodeLoading)
                        {
                            LUA.lua_pushnil(state); // argumentCount + 4
                        }
                        else
                        {
                            LUA.lua_pushlstring(state, str_t); // argumentCount + 4
                        }
                    }
                    if (argumentCount >= 4)
                    {
                        LUA.lua_pushvalue(state, 4); // argumentCount + 5
                    }
                }
                finally
                {
                    // Allow OOM
                    machine.AllowOOM();
                }

                // Call and propogate the error
                int loadError = LUA.lua_pcall(state, (argumentCount >= 4) ? 4 : 3, 2, 0); // argumentCount + 2|1
                if (loadError != LUA.LUA_OK)
                {
                    return LUA.lua_error(state); // 1
                }

                return 2;
            }
            catch (Exception e)
            {
                return EmitLuaError(state, e, objectLookup);
            }
        }

        private static LuaArgs LuaObject_ToString(in LuaArgs args)
        {
            var o = args.GetObject(0);
            return new LuaArgs(o.ToString());
        }

        [MonoPInvokeCallback(typeof(LUA.lua_CFunction))]
        private static int LuaObject_GC(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                // Get the object
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                int type = LUA.lua_type(state, 1);
                if (type == LUA.LUA_TUSERDATA)
                {
                    var ud = LUA.lua_touserdata(state, 1);
                    var id = Marshal.ReadInt32(ud);
                    var obj = objectLookup.GetObjectForID(id);
                    if (obj != null && obj is LuaObject)
                    {
                        // Unref and possibly dispose the object
                        var luaObject = (LuaObject)obj;
                        if (luaObject.UnRef() == 0)
                        {
                            var oldState = machine.m_runningState;
                            try
                            {
                                machine.m_runningState = state;
                                luaObject.Dispose();
                            }
                            finally
                            {
                                machine.m_runningState = oldState;
                            }
                        }
                        objectLookup.RemoveObjectAndValue(state, id);
                        return 0;
                    }
                }
                throw new LuaError("Expected object, got " + new ByteString(LUA.lua_typename(state, type)));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        [MonoPInvokeCallback(typeof(LUA.lua_CFunction))]
        private static int LuaFunction_Call(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                // Get the function
                int index = LUA.lua_upvalueindex(1);
                int type = LUA.lua_type(state, index);
                if (type == LUA.LUA_TUSERDATA)
                {
                    var ud = LUA.lua_touserdata(state, index);
                    var id = Marshal.ReadInt32(ud);
                    var obj = objectLookup.GetObjectForID(id);
                    if (obj != null && obj is LuaCFunction)
                    {
                        // Pop the arguments
                        var function = (LuaCFunction)obj;
						int argumentCount = LUA.lua_gettop(state);
                        LuaArgs arguments;
                        try
                        {
                            machine.PreventOOM();
							arguments = GetValues(state, argumentCount, objectLookup); // argumentCount
                        }
                        finally
                        {
                            machine.AllowOOM();
                        }

                        LuaArgs results;
                        try
                        {
                            // Call the function
                            var oldState = machine.m_runningState;
                            try
                            {
                                machine.m_runningState = state;
                                results = function.Invoke(arguments);
                            }
                            finally
                            {
                                machine.m_runningState = oldState;
                            }
                        }
                        catch (LuaAsyncCall c)
                        {
							// Clear the stack
							LUA.lua_settop(state, 0); // 0

                            // Get the function
                            if (c.Function.Machine != machine || !objectLookup.PushValueForID(state, c.Function.ID)) // 1|0
                            {
                                throw new Exception("Could not find function");
                            }

                            // Push the arguments
                            try
                            {
                                machine.PreventOOM();
                                PushValues(state, c.Arguments, objectLookup); // 1 + c.Arguments.Length
                            }
                            finally
                            {
                                machine.AllowOOM();
                            }

                            // Store the continuationn
                            int nextContinuationID;
                            if (c.Continuation != null)
                            {
                                nextContinuationID = machine.m_nextContinuationID++;
                                machine.m_pendingContinuations.Add(nextContinuationID, c.Continuation);
                            }
                            else
                            {
                                nextContinuationID = -1;
                            }

                            // Call the function
                            int callResult = LUA.lua_pcallk(state, c.Arguments.Length, LUA.LUA_MULTRET, 0, new IntPtr(nextContinuationID), s_continueDelegate); // numResults|1
                            return LuaFunction_Continue(state, callResult, new IntPtr(nextContinuationID));
                        }
                        catch (LuaYield y)
                        {
                            // Push the results
                            try
                            {
                                machine.PreventOOM();
                                PushValues(state, y.Results, objectLookup); // argumentCount + y.results.Length
                            }
                            finally
                            {
                                machine.AllowOOM();
                            }

                            // Store the continuation
                            var continuationID = machine.m_nextContinuationID++;
                            machine.m_pendingContinuations.Add(continuationID, y.Continuation);

                            // Yield
                            return LUA.lua_yieldk(state, y.Results.Length, new IntPtr(continuationID), s_continueDelegate);
                        }

                        // Push the results
                        try
                        {
                            machine.PreventOOM();
                            PushValues(state, results, objectLookup); // argumentCount + results.Length
                            return results.Length;
                        }
                        finally
                        {
                            machine.AllowOOM();
                        }
                    }
                }
                throw new LuaError("Expected function, got " + new ByteString(LUA.lua_typename(state, type)));
            }
            catch (Exception e)
            {
                return EmitLuaError(state, e, objectLookup);
            }
        }

        [MonoPInvokeCallback(typeof(LUA.lua_KFunction))]
        private static int LuaFunction_Continue(IntPtr state, int status, IntPtr ctx)
        {
            ObjectLookup objectLookup = null;
            try
            {
                // Get the machine
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                while(true) // This loop allows us to yield again without having recurse
                {
                    // Get the continuation
                    var continuationID = ctx.ToInt32();
                    LuaContinuation continuation;
                    if (continuationID >= 0)
                    {
                        continuation = machine.m_pendingContinuations[continuationID];
                        machine.m_pendingContinuations.Remove(continuationID);
                    }
                    else
                    {
                        continuation = null;
                    }

                    // Check the result
                    if (status == LUA.LUA_OK || status == LUA.LUA_YIELD)
                    {
                        int argumentCount = LUA.lua_gettop(state);
                        if (continuation == null)
                        {
                            // Return the values directly
                            return argumentCount;
                        }
                        else
                        {
                            // Pop the arguments
                            LuaArgs arguments;
                            try
                            {
                                machine.PreventOOM();
                                arguments = GetValues(state, argumentCount, objectLookup); // argumentCount
                            }
                            finally
                            {
                                machine.AllowOOM();
                            }

                            LuaArgs results;
                            try
                            {
                                // Call the continuation
                                var oldState = machine.m_runningState;
                                try
                                {
                                    machine.m_runningState = state;
                                    results = continuation.Invoke(arguments);
                                }
                                finally
                                {
                                    machine.m_runningState = oldState;
                                }
                            }
                            catch (LuaAsyncCall c)
                            {
                                // Clear the stack
                                LUA.lua_settop(state, 0); // 0

                                // Get the function
                                if (c.Function.Machine != machine || !objectLookup.PushValueForID(state, c.Function.ID)) // 1|0
                                {
                                    throw new Exception("Could not find function");
                                }

                                // Push the arguments
                                try
                                {
                                    machine.PreventOOM();
                                    PushValues(state, c.Arguments, objectLookup); // 1 + c.Arguments.Length
                                }
                                finally
                                {
                                    machine.AllowOOM();
                                }

                                // Store the next continuation
                                int nextContinuationID;
                                if (c.Continuation != null)
                                {
                                    nextContinuationID = machine.m_nextContinuationID++;
                                    machine.m_pendingContinuations.Add(nextContinuationID, c.Continuation);
                                }
                                else
                                {
                                    nextContinuationID = -1;
                                }

                                // Call the function
                                int callResult = LUA.lua_pcallk(state, c.Arguments.Length, LUA.LUA_MULTRET, 0, new IntPtr(nextContinuationID), s_continueDelegate); // numResults|1

                                // Continue by re-entering the loop (so we don't have to recurse and won't cause a stack overflow)
                                status = callResult;
                                ctx = new IntPtr(nextContinuationID);
                                continue;
                            }
                            catch (LuaYield y)
                            {
                                // Push the results
                                try
                                {
                                    machine.PreventOOM();
                                    PushValues(state, y.Results, objectLookup); // argumentCount + y.results.Length
                                }
                                finally
                                {
                                    machine.AllowOOM();
                                }

                                // Store the next continuation
                                int nextContinuationID = machine.m_nextContinuationID++;
                                machine.m_pendingContinuations.Add(nextContinuationID, y.Continuation);

                                // Yield
                                return LUA.lua_yieldk(state, y.Results.Length, new IntPtr(nextContinuationID), s_continueDelegate);
                            }

                            // Push the results
                            try
                            {
                                machine.PreventOOM();
                                PushValues(state, results, objectLookup); // argumentCount + results.Length
                                return results.Length;
                            }
                            finally
                            {
                                machine.AllowOOM();
                            }
                        }
                    }
                    else
                    {
                        // Propogate the error
                        return LUA.lua_error(state); // 0
                    }
                }
            }
            catch (Exception e)
            {
                return EmitLuaError(state, e, objectLookup);
            }
        }

        [MonoPInvokeCallback(typeof(LUA.lua_CFunction))]
        private static int LuaFunction_GC(IntPtr state)
        {
            ObjectLookup objectLookup = null;
            try
            {
                LuaMachine machine = LookupMachine(state);
                objectLookup = machine.m_objectLookup;

                // Get the function
                int type = LUA.lua_type(state, 1);
                if (type == LUA.LUA_TUSERDATA)
                {
                    var ud = LUA.lua_touserdata(state, 1);
                    var id = Marshal.ReadInt32(ud);
                    var obj = objectLookup.GetObjectForID(id);
                    if (obj != null && obj is LuaCFunction)
                    {
                        // Remove the function
                        objectLookup.RemoveObjectAndValue(state, id);
                        return 0;
                    }
                }
                throw new LuaError("Expected function, got " + new ByteString(LUA.lua_typename(state, type)));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static int EmitLuaError(IntPtr state, Exception e, ObjectLookup objectLookup)
        {
            if (e is LuaError)
            {
                var luaError = (LuaError)e;
                ref var value = ref luaError.Value;
                if (value.IsString())
                {
                    var str = value.IsByteString() ? value.GetByteString() : ByteString.Temp(value.GetString());
                    return LUA.luaL_error(state, str, luaError.Level); // 0
                }
                else
                {
                    try
                    {
                        objectLookup.Machine.PreventOOM();
                        if (objectLookup != null || value.IsNumber() || value.IsBool() || value.IsNil())
                        {
                            PushValue(state, ref value, objectLookup); // 1
                        }
                        else
                        {
                            PushNil(state); // 1
                        }
                    }
                    finally
                    {
                        objectLookup.Machine.AllowOOM();
                    }
                    return LUA.lua_error(state); // 0
                }
            }
            else
            {
                var message = "C# Exception Thrown: " + e.GetType().FullName;
                if (e.Message != null)
                {
                    message += "\n" + e.Message;
                }
				App.LogError(message);
                if (e.StackTrace != null)
                {
                    App.LogError(e.StackTrace);
                }
                var msg = ByteString.Temp(message);
                return LUA.luaL_error(state, msg); // 0
            }
        }

        [MonoPInvokeCallback(typeof(LUA.lua_Hook))]
        private static void Debug_Hook(IntPtr state, ref LUA.lua_Debug ar)
        {
            try
            {
                var machine = LookupMachine(state);
                if (machine != null)
                {
                    if (ar.eventCode == LUA.LUA_HOOKCOUNT)
                    {
                        machine.m_instructionsExecuted += s_hookInterval;
                        machine.m_instructionsExecutedThisTimeout += s_hookInterval;
                    }
                    if (machine.CheckTimeout())
                    {
                        LUA.luaL_error(state, str_timeout);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        [MonoPInvokeCallback(typeof(LUA.lua_Alloc))]
        private static IntPtr Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize)
        {
            try
            {
                var machine = s_memoryLookup[ud];
                var memory = machine.m_memoryTracker;
                var forceAlloc = (machine.m_forceAllocations > 0);
                if (nsize == IntPtr.Zero)
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                        memory.Free(osize.ToInt64());
                    }
                    return IntPtr.Zero;
                }
                else
                {
                    if (ptr != IntPtr.Zero)
                    {
                        if (nsize.ToInt64() >= osize.ToInt64())
                        {
                            if (forceAlloc)
                            {
                                memory.ForceAlloc(nsize.ToInt64() - osize.ToInt64());
                                return Marshal.ReAllocHGlobal(ptr, nsize);
                            }
                            else if (memory.Alloc(nsize.ToInt64() - osize.ToInt64(), false))
                            {
                                return Marshal.ReAllocHGlobal(ptr, nsize);
                            }
                            else
                            {
                                return IntPtr.Zero;
                            }
                        }
                        else
                        {
                            var result = Marshal.ReAllocHGlobal(ptr, nsize);
                            memory.Free(osize.ToInt64() - nsize.ToInt64());
                            return result;
                        }
                    }
                    else
                    {
                        if (forceAlloc)
                        {
                            memory.ForceAlloc(nsize.ToInt64());
                            return Marshal.AllocHGlobal(nsize);
                        }
                        else if (memory.Alloc(nsize.ToInt64(), false))
                        {
                            return Marshal.AllocHGlobal(nsize);
                        }
                        else
                        {
                            return IntPtr.Zero;
                        }
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                return IntPtr.Zero;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        [MonoPInvokeCallback(typeof(LUA.lua_Writer))]
        private static int Dump(IntPtr state, byte* p, IntPtr sz, IntPtr ud)
        {
            try
            {
                var machine = LookupMachine(state);
                var builder = machine.m_dumpStringBuilder;
                for (int i = 0; i < (int)sz; ++i)
                {
                    var b = *(p + i);
                    builder.Append(b);
                }
                return 0;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private void PreventOOM()
        {
            m_forceAllocations++;
        }

        private void AllowOOM()
        {
            m_forceAllocations--;
        }

        private void ResetTimeoutTimer()
        {
            m_instructionsExecutedThisTimeout = 0;
            m_firstTimeoutEmitted = false;
        }

        private bool CheckTimeout()
        {
            if (m_forceAllocations == 0)
            {
                if (!m_firstTimeoutEmitted)
                {
                    if (m_instructionsExecutedThisTimeout >= s_firstTimeLimit)
                    {
                        m_instructionsExecutedThisTimeout = 0;
                        m_firstTimeoutEmitted = true;
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (m_instructionsExecutedThisTimeout >= s_secondTimeLimit)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        private static void PrintStack(IntPtr state)
        {
            int top = LUA.lua_gettop(state);
            var builder = new System.Text.StringBuilder("Stack (top=" + top + "): ");
            for (int i = 1; i <= top; ++i)
            {
                IntPtr len;
                var ptr = LUA.luaL_tolstring(state, i, out len); // top + 1
                var str = new ByteString(ptr, (int)len);
                LUA.lua_pop(state, 1); // top

                builder.Append(str.ToString());
                if (i < top)
                {
                    builder.Append(", ");
                }
                else
                {
                    builder.Append(".");
                }
            }
            System.Diagnostics.Debug.Print(builder.ToString());
        }
    }
}

