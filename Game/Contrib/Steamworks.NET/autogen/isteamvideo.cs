// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// This file is automatically generated.
// Changes to this file will be reverted when you update Steamworks.NET


namespace Steamworks
{
    public static class SteamVideo
    {
        /// <summary>
        /// <para> Get a URL suitable for streaming the given Video app ID's video</para>
        /// </summary>
        public static void GetVideoURL(AppId_t unVideoAppID)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamVideo_GetVideoURL(unVideoAppID);
        }

        /// <summary>
        /// <para> returns true if user is uploading a live broadcast</para>
        /// </summary>
        public static bool IsBroadcasting(out int pnNumViewers)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamVideo_IsBroadcasting(out pnNumViewers);
        }
    }
}