#if STEAM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Network.Steamworks
{
    internal class SteamworksException : Exception
    {
        public SteamworksException(string message) : base(message)
        {
        }

        public SteamworksException(string method, string message) : base(method + " failed: " + message + ".")
        {
        }
    }

    internal static class SteamworksUtils
    {
        public static void CheckResult(string functionName, bool result)
        {
            if (!result)
            {
                throw new SteamworksException(functionName + " failed.");
            }
        }
    }
}
#endif
