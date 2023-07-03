using Dan200.Core.Main;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Dan200.Core.Util;
using Dan200.Core.Lua;

namespace Lua
{
	internal static unsafe class Lua
	{
        // Constants
        public const int LUA_VERSION_MAJOR = 5;
        public const int LUA_VERSION_MINOR = 3;

#if LUA_32BITS
		public const int LUA_32BITS = 1;
        public const int LUA_MININTEGER = int.MinValue;
        public const int LUA_MAXINTEGER = int.MaxValue;
#else
        public const int LUA_32BITS = 0;
        public const long LUA_MININTEGER = long.MinValue;
        public const long LUA_MAXINTEGER = long.MaxValue;
#endif

        public const int LUAI_MAXSTACK = 1000000;
        public const int LUA_REGISTRYINDEX = (-LUAI_MAXSTACK - 1000);
        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        public const int LUA_OK = 0;
        public const int LUA_YIELD = 1;
        public const int LUA_ERRRUN = 2;
        public const int LUA_ERRSYNTAX = 3;
        public const int LUA_ERRMEM = 4;
        public const int LUA_ERRGCMM = 5;
        public const int LUA_ERRERR = 6;

        public const int LUA_HOOKCALL = 0;
        public const int LUA_HOOKRET = 1;
        public const int LUA_HOOKLINE = 2;
        public const int LUA_HOOKCOUNT = 3;
        public const int LUA_HOOKTAILCALL = 4;

        public const int LUA_MASKCALL = (1 << LUA_HOOKCALL);
        public const int LUA_MASKRET = (1 << LUA_HOOKRET);
        public const int LUA_MASKLINE = (1 << LUA_HOOKLINE);
        public const int LUA_MASKCOUNT = (1 << LUA_HOOKCOUNT);

        public const int LUA_MULTRET = -1;

        public const int LUA_TNONE = -1;
        public const int LUA_TNIL = 0;
        public const int LUA_TBOOLEAN = 1;
        public const int LUA_TLIGHTUSERDATA = 2;
        public const int LUA_TNUMBER = 3;
        public const int LUA_TSTRING = 4;
        public const int LUA_TTABLE = 5;
        public const int LUA_TFUNCTION = 6;
        public const int LUA_TUSERDATA = 7;
        public const int LUA_TTHREAD = 8;

        public const int LUA_GCSTOP = 0;
        public const int LUA_GCRESTART = 1;
        public const int LUA_GCCOLLECT = 2;
        public const int LUA_GCCOUNT = 3;
        public const int LUA_GCCOUNTB = 4;
        public const int LUA_GCSTEP = 5;
        public const int LUA_GCSETPAUSE = 6;
        public const int LUA_GCSETSTEPMUL = 7;
        public const int LUA_GCISRUNNING = 9;

        public const int LUA_IDSIZE = 60;
        
        // Types
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_CFunction(IntPtr L);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_KFunction(IntPtr L, int status, IntPtr ctx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr lua_Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void lua_Hook(IntPtr L, ref lua_Debug ar);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int lua_Writer(IntPtr L, byte* p, IntPtr sz, IntPtr ud);

        [StructLayout(LayoutKind.Sequential)]
        public struct lua_Debug
        {
            // Public part
            public readonly int eventCode;
            public readonly byte* name;
            public readonly byte* namewhat;
            public readonly byte* what;
            public readonly byte* source;
            public readonly int currentline;
            public readonly int linedefined;
            public readonly int lastlinedefined;
            public readonly byte nups;
            public readonly byte nparams;
            public readonly sbyte isvararg;
            public readonly sbyte istailcall;
			public fixed byte shortsrc[LUA_IDSIZE];

            // Private part
            private readonly IntPtr i_ci;
        }

        // Functions
		public static int lua_gc (IntPtr L, int what, int data)
		{
			return NativeMethods.lua_gc (L, what, data);
		}

		public static byte* lua_typename (IntPtr L, int type)
		{
			return NativeMethods.lua_typename (L, type);
		}

		public static int luaL_error(IntPtr L, ByteString message, int level = 1)
        {
			using (var bytes = message.Lock())
			{
				if (level != 0)
				{
					NativeMethods.luaL_where(L, level); // 1
					NativeMethods.lua_pushlstring(L, bytes.Data, new IntPtr(bytes.Length)); // 2
					NativeMethods.lua_concat(L, 2); // 1
				}
				else
				{
					NativeMethods.lua_pushlstring(L, bytes.Data, new IntPtr(bytes.Length)); // 1
				}
			}
            return NativeMethods.lua_error(L); // 0
        }

        public static int lua_error (IntPtr L)
		{
			return NativeMethods.lua_error (L);
		}

		public static IntPtr luaL_newstate ()
		{
			return NativeMethods.luaL_newstate ();
		}
			
		public static void lua_close (IntPtr L)
		{
			NativeMethods.lua_close (L);
		}

		public static void luaL_openlibs (IntPtr L)
		{
			NativeMethods.luaL_openlibs (L);
		}

		public static int luaL_loadstring (IntPtr L, byte[] str, int strStart)
		{
            App.Assert(str.IndexOf((byte)'\0', strStart) >= 0);
            fixed ( byte* pChunk = str )
            {
                return NativeMethods.luaL_loadstring(L, pChunk + strStart);
            }
		}

		public static int luaL_loadbufferx(IntPtr L, ByteString chunk, ByteString name, ByteString mode)
		{
			App.Assert(name.IsNullTerminated());
			App.Assert(mode.IsNullTerminated());
			using(var chunkBytes = chunk.Lock())
			using(var nameBytes = name.Lock())
			using(var modeBytes = mode.Lock())
			{
				return NativeMethods.luaL_loadbufferx(L, chunkBytes.Data, new IntPtr(chunkBytes.Length), nameBytes.Data, modeBytes.Data);
			}
		}

		public static void lua_createtable (IntPtr L, int narr, int nrec)
		{
			NativeMethods.lua_createtable (L, narr, nrec);
		}

		public static int lua_gettable (IntPtr L, int index)
		{
			return NativeMethods.lua_gettable (L, index);
		}

		public static int lua_rawget(IntPtr L, int index)
		{
			return NativeMethods.lua_rawget(L, index);
		}

#if LUA_32BITS
        public static int lua_rawgeti (IntPtr L, int index, int integer)
        {
            return NativeMethods.lua_rawgeti (L, index, integer);
        }

        public static void lua_rawseti (IntPtr L, int index, int integer)
        {
            NativeMethods.lua_rawseti (L, index, integer);
        }
#else
        public static int lua_rawgeti (IntPtr L, int index, long integer)
		{
			return NativeMethods.lua_rawgeti (L, index, integer);
		}

        public static void lua_rawseti(IntPtr L, int index, long integer)
        {
            NativeMethods.lua_rawseti(L, index, integer);
        }
#endif

        public static void lua_settop (IntPtr L, int newTop)
		{
			NativeMethods.lua_settop (L, newTop);
		}

		public static void lua_pop (IntPtr L, int n)
		{
			NativeMethods.lua_settop (L, -(n)-1);
		}			

		public static void lua_rotate (IntPtr L, int index, int n)
		{
			NativeMethods.lua_rotate (L, index, n);
		}

		public static void lua_copy (IntPtr L, int fromidx, int toidx)
		{
			NativeMethods.lua_copy( L, fromidx, toidx );
		}

		public static void lua_insert (IntPtr L, int index)
		{
			NativeMethods.lua_rotate (L, index, 1);
		}

		public static void lua_remove (IntPtr L, int index)
		{
			NativeMethods.lua_rotate (L, index, -1);
			NativeMethods.lua_settop(L, -2);
		}
			
		public static void lua_settable (IntPtr L, int index)
		{
			NativeMethods.lua_settable (L, index);
		}

		public static void lua_rawset(IntPtr L, int index)
		{
			NativeMethods.lua_rawset(L, index);
		}

		public static void lua_setmetatable (IntPtr L, int objIndex)
		{
			NativeMethods.lua_setmetatable (L, objIndex);
		}

		public static int lua_getmetatable (IntPtr L, int objIndex)
		{
			return NativeMethods.lua_getmetatable (L, objIndex);
		}

		public static void lua_pushvalue (IntPtr L, int index)
		{
			NativeMethods.lua_pushvalue (L, index);
		}

		public static void lua_replace (IntPtr L, int index)
		{
			NativeMethods.lua_copy (L, -1, index);
			NativeMethods.lua_settop(L, -2);
		}

		public static int lua_gettop (IntPtr L)
		{
			return NativeMethods.lua_gettop (L);
		}

		public static int lua_type (IntPtr L, int index)
		{
			return NativeMethods.lua_type (L, index);
		}

		public static IntPtr lua_newuserdata (IntPtr L, IntPtr size)
		{
			return NativeMethods.lua_newuserdata (L, size);
		}
		
		public static IntPtr lua_touserdata (IntPtr L, int index)
		{
			return NativeMethods.lua_touserdata (L, index);
		}

        public static IntPtr lua_tothread (IntPtr L, int index)
        {
            return NativeMethods.lua_tothread (L, index);
        }

		public static bool lua_isinteger (IntPtr L, int index)
		{
			return NativeMethods.lua_isinteger (L, index) != 0;
		}

		public static void lua_pushnil (IntPtr L)
		{
			NativeMethods.lua_pushnil (L);
		}

		public static lua_CFunction lua_tocfunction (IntPtr L, int index)
		{
			IntPtr ptr = NativeMethods.lua_tocfunction (L, index);
            if (ptr != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer(ptr, typeof(lua_CFunction)) as lua_CFunction;
            }
			return null;
		}

#if LUA_32BITS
		public static double lua_tonumber (IntPtr L, int index)
		{
			return NativeMethods.lua_tonumberx (L, index, IntPtr.Zero);
		}

		public static int lua_tointeger (IntPtr L, int index)
		{
			return NativeMethods.lua_tointegerx (L, index, IntPtr.Zero);
		}
#else
        public static double lua_tonumber (IntPtr L, int index)
		{
			return NativeMethods.lua_tonumberx (L, index, IntPtr.Zero);
		}

		public static long lua_tointeger (IntPtr L, int index)
		{
			return NativeMethods.lua_tointegerx (L, index, IntPtr.Zero);
		}
#endif

		public static bool lua_toboolean (IntPtr L, int index)
		{ 
			return NativeMethods.lua_toboolean (L, index) != 0;
		}

		public static byte* lua_tolstring (IntPtr L, int index, out IntPtr len)
		{
			return NativeMethods.lua_tolstring (L, index, out len);
		}

		public static byte* luaL_tolstring(IntPtr L, int index, out IntPtr len)
		{
			return NativeMethods.luaL_tolstring(L, index, out len);
		}

#if LUA_32BITS
		public static void lua_pushnumber (IntPtr L, float number)
		{
			NativeMethods.lua_pushnumber (L, number);
		}

		public static void lua_pushinteger (IntPtr L, int integer)
		{
			NativeMethods.lua_pushinteger (L, integer);
		}
#else
        public static void lua_pushnumber (IntPtr L, double number)
		{
			NativeMethods.lua_pushnumber (L, number);
		}

		public static void lua_pushinteger (IntPtr L, long integer)
		{
			NativeMethods.lua_pushinteger (L, integer);
		}
#endif

		public static void lua_pushboolean (IntPtr L, bool value)
		{
			NativeMethods.lua_pushboolean (L, value ? 1 : 0);
		}

        public static byte* lua_pushstring(IntPtr L, byte* str)
        {
            return NativeMethods.lua_pushstring(L, str);
        }

		public static byte* lua_pushlstring (IntPtr L, ByteString str)
		{
			using(var bytes = str.Lock())
            {
				return NativeMethods.lua_pushlstring(L, bytes.Data, new IntPtr(bytes.Length));
            }
		}
		
		public static int lua_getfield (IntPtr L, int stackPos, byte[] meta, int metaStart)
		{
            App.Assert(meta.IndexOf((byte)'\0', metaStart) >= 0);
            fixed (byte* pMeta = meta)
            {
                return NativeMethods.lua_getfield(L, stackPos, pMeta + metaStart);
            }
		}

		public static int luaL_getmetafield (IntPtr L, int stackPos, byte[] field, int fieldStart)
		{
            App.Assert(field.IndexOf((byte)'\0', fieldStart) >= 0);
            fixed (byte* pField = field)
            {
                return NativeMethods.luaL_getmetafield(L, stackPos, pField + fieldStart);
            }
		}

		public static int lua_checkstack (IntPtr L, int extra)
		{
			return NativeMethods.lua_checkstack (L, extra);
		}

		public static int lua_next (IntPtr L, int index)
		{
			return NativeMethods.lua_next (L, index);
		}

		public static void lua_pushlightuserdata (IntPtr L, IntPtr udata)
		{
			NativeMethods.lua_pushlightuserdata (L, udata);
		}

    	public static int lua_pcall (IntPtr L, int nArgs, int nResults, int msgh)
		{
            return NativeMethods.lua_pcallk (L, nArgs, nResults, msgh, IntPtr.Zero, IntPtr.Zero);
        }

        public static void lua_call (IntPtr L, int nArgs, int nResults)
		{
			NativeMethods.lua_callk (L, nArgs, nResults, IntPtr.Zero, IntPtr.Zero);
		}

        public static int lua_pcallk(IntPtr L, int nArgs, int nResults, int msgh, IntPtr ctx, lua_KFunction k)
        {
            IntPtr funcK = (k == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(k);
            return NativeMethods.lua_pcallk(L, nArgs, nResults, msgh, ctx, funcK);
        }

        public static void lua_callk(IntPtr L, int nArgs, int nResults, IntPtr ctx, lua_KFunction k)
        {
            IntPtr funcK = (k == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(k);
            NativeMethods.lua_callk(L, nArgs, nResults, ctx, funcK);
        }

        public static int lua_yieldk(IntPtr L, int nResults, IntPtr ctx, lua_KFunction k)
        {
            IntPtr funcK = (k == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(k);
            return NativeMethods.lua_yieldk(L, nResults, ctx, funcK);
        }

		public static bool lua_isyieldable(IntPtr L)
		{
			return NativeMethods.lua_isyieldable(L) != 0;
		}

        public static void lua_setglobal (IntPtr L, byte[] name, int nameStart)
		{
            App.Assert(name.IndexOf((byte)'\0', nameStart) >= 0);
            fixed (byte* pName = name)
            {
                NativeMethods.lua_setglobal(L, pName + nameStart);
            }
		}

        public static void lua_setfield(IntPtr L, int index, byte[] name, int nameStart)
        {
            App.Assert(name.IndexOf((byte)'\0', nameStart) >= 0);
            fixed (byte* pName = name)
            {
                NativeMethods.lua_setfield(L, index, pName + nameStart);
            }
        }

        public static int lua_getglobal (IntPtr L, byte[] name, int nameStart)
		{
            App.Assert(name.IndexOf((byte)'\0', nameStart) >= 0);
            fixed (byte* pName = name)
            {
                return NativeMethods.lua_getglobal(L, pName + nameStart);
            }
		}

		public static void lua_pushglobaltable(IntPtr L)
		{
			NativeMethods.lua_rawgeti(L, LUA_REGISTRYINDEX, LUA_RIDX_GLOBALS);
		}

		public static IntPtr lua_newstate ( lua_Alloc f, IntPtr ud )
		{
			IntPtr funcAlloc = f == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate (f);
			return NativeMethods.lua_newstate( funcAlloc, ud );
		}

		public static void lua_pushcfunction (IntPtr L, lua_CFunction f)
		{
			lua_pushcclosure (L, f, 0);
		}

		public static void lua_pushcclosure (IntPtr L, lua_CFunction f, int count)
		{
			IntPtr pfunc = (f == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate (f);
			NativeMethods.lua_pushcclosure (L, pfunc, count);
		}

		public static int lua_upvalueindex(int i)
		{
			return LUA_REGISTRYINDEX - i;
		}

		public static void lua_sethook (IntPtr L, lua_Hook func, int mask, int count)
		{
			IntPtr funcHook = func == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate (func);
			NativeMethods.lua_sethook (L, funcHook, mask, count);
		}

		public static int lua_getstack (IntPtr L, int level, ref lua_Debug ar)
		{
			return NativeMethods.lua_getstack (L, level, ref ar);
		}

		public static int lua_getinfo (IntPtr L, byte[] what, int whatStart, ref lua_Debug ar)
		{
            App.Assert(what.IndexOf((byte)'\0', whatStart) >= 0);
            fixed (byte* pWhat = what)
            {
                return NativeMethods.lua_getinfo(L, pWhat + whatStart, ref ar);
            }
		}

		public static void luaL_where(IntPtr L, int level)
		{
			NativeMethods.luaL_where(L, level);
		}

		public static void lua_concat(IntPtr L, int n)
		{
			NativeMethods.lua_concat(L, n);
		}

		public static int lua_absindex(IntPtr L, int index)
		{
			return NativeMethods.lua_absindex(L, index);
		}

		public static IntPtr lua_newthread(IntPtr L)
		{
			return NativeMethods.lua_newthread(L);
		}

		public static int lua_status(IntPtr L)
		{
			return NativeMethods.lua_status(L);
		}

		public static int lua_resume(IntPtr L, IntPtr from, int nargs)
		{
			return NativeMethods.lua_resume(L, from, nargs);
		}

		public static void lua_xmove(IntPtr from, IntPtr to, int n)
		{
			NativeMethods.lua_xmove(from, to, n);
		}

        public static lua_CFunction lua_atpanic(IntPtr L, lua_CFunction panicf)
        {
            IntPtr pfunc = (panicf == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(panicf);
            IntPtr poldfunc = NativeMethods.lua_atpanic(L, pfunc);
            if (poldfunc != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer(poldfunc, typeof(lua_CFunction)) as lua_CFunction;
            }
            return null;
        }

		public static int lua_dump(IntPtr L, lua_Writer writer, IntPtr data, int strip)
		{
			IntPtr pWriter = (writer == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(writer);
			return NativeMethods.lua_dump(L, pWriter, data, strip);
		}
    }
}

