using System;

namespace Lua
{
#if !IOS
	// Dummy MonoPInvokeCallbackAttribute for platforms that don't need/support it.
	// Tag all your methods that are called from Lua with this!
	public class MonoPInvokeCallbackAttribute : Attribute
	{
		public MonoPInvokeCallbackAttribute(Type type)
		{
		}
	}
#endif
}
