using System;
using Dan200.Core.Network;
using Dan200.Core.Util;

namespace Dan200.Core.Main
{
    internal class GameInfo
    {
        public string Title;
        public Version Version;
        public string Author;
        public uint SteamAppID;

        public GameInfo()
        {
            Title = "Untitled";
            Version = new Version(0, 0, 0);
            Author = "";
            SteamAppID = 0;
        }
    }
}

