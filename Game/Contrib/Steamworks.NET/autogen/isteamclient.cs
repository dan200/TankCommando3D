// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// This file is automatically generated.
// Changes to this file will be reverted when you update Steamworks.NET

using System;

namespace Steamworks
{
    public static class SteamClient
    {
        /// <summary>
        /// <para> Creates a communication pipe to the Steam client.</para>
        /// <para> NOT THREADSAFE - ensure that no other threads are accessing Steamworks API when calling</para>
        /// </summary>
        public static HSteamPipe CreateSteamPipe()
        {
            InteropHelp.TestIfAvailableClient();
            return (HSteamPipe)NativeMethods.ISteamClient_CreateSteamPipe();
        }

        /// <summary>
        /// <para> Releases a previously created communications pipe</para>
        /// <para> NOT THREADSAFE - ensure that no other threads are accessing Steamworks API when calling</para>
        /// </summary>
        public static bool BReleaseSteamPipe(HSteamPipe hSteamPipe)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamClient_BReleaseSteamPipe(hSteamPipe);
        }

        /// <summary>
        /// <para> connects to an existing global user, failing if none exists</para>
        /// <para> used by the game to coordinate with the steamUI</para>
        /// <para> NOT THREADSAFE - ensure that no other threads are accessing Steamworks API when calling</para>
        /// </summary>
        public static HSteamUser ConnectToGlobalUser(HSteamPipe hSteamPipe)
        {
            InteropHelp.TestIfAvailableClient();
            return (HSteamUser)NativeMethods.ISteamClient_ConnectToGlobalUser(hSteamPipe);
        }

        /// <summary>
        /// <para> used by game servers, create a steam user that won't be shared with anyone else</para>
        /// <para> NOT THREADSAFE - ensure that no other threads are accessing Steamworks API when calling</para>
        /// </summary>
        public static HSteamUser CreateLocalUser(out HSteamPipe phSteamPipe, EAccountType eAccountType)
        {
            InteropHelp.TestIfAvailableClient();
            return (HSteamUser)NativeMethods.ISteamClient_CreateLocalUser(out phSteamPipe, eAccountType);
        }

        /// <summary>
        /// <para> removes an allocated user</para>
        /// <para> NOT THREADSAFE - ensure that no other threads are accessing Steamworks API when calling</para>
        /// </summary>
        public static void ReleaseUser(HSteamPipe hSteamPipe, HSteamUser hUser)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamClient_ReleaseUser(hSteamPipe, hUser);
        }

        /// <summary>
        /// <para> retrieves the ISteamUser interface associated with the handle</para>
        /// </summary>
        public static IntPtr GetISteamUser(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamUser(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> retrieves the ISteamGameServer interface associated with the handle</para>
        /// </summary>
        public static IntPtr GetISteamGameServer(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamGameServer(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> set the local IP and Port to bind to</para>
        /// <para> this must be set before CreateLocalUser()</para>
        /// </summary>
        public static void SetLocalIPBinding(uint unIP, ushort usPort)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamClient_SetLocalIPBinding(unIP, usPort);
        }

        /// <summary>
        /// <para> returns the ISteamFriends interface</para>
        /// </summary>
        public static IntPtr GetISteamFriends(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamFriends(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns the ISteamUtils interface</para>
        /// </summary>
        public static IntPtr GetISteamUtils(HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamUtils(hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns the ISteamMatchmaking interface</para>
        /// </summary>
        public static IntPtr GetISteamMatchmaking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamMatchmaking(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns the ISteamMatchmakingServers interface</para>
        /// </summary>
        public static IntPtr GetISteamMatchmakingServers(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamMatchmakingServers(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns the a generic interface</para>
        /// </summary>
        public static IntPtr GetISteamGenericInterface(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamGenericInterface(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns the ISteamUserStats interface</para>
        /// </summary>
        public static IntPtr GetISteamUserStats(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamUserStats(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns the ISteamGameServerStats interface</para>
        /// </summary>
        public static IntPtr GetISteamGameServerStats(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamGameServerStats(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns apps interface</para>
        /// </summary>
        public static IntPtr GetISteamApps(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamApps(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> networking</para>
        /// </summary>
        public static IntPtr GetISteamNetworking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamNetworking(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> remote storage</para>
        /// </summary>
        public static IntPtr GetISteamRemoteStorage(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamRemoteStorage(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> user screenshots</para>
        /// </summary>
        public static IntPtr GetISteamScreenshots(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamScreenshots(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns the number of IPC calls made since the last time this function was called</para>
        /// <para> Used for perf debugging so you can understand how many IPC calls your game makes per frame</para>
        /// <para> Every IPC call is at minimum a thread context switch if not a process one so you want to rate</para>
        /// <para> control how often you do them.</para>
        /// </summary>
        public static uint GetIPCCallCount()
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamClient_GetIPCCallCount();
        }

        /// <summary>
        /// <para> API warning handling</para>
        /// <para> 'int' is the severity; 0 for msg, 1 for warning</para>
        /// <para> 'const char *' is the text of the message</para>
        /// <para> callbacks will occur directly after the API function is called that generated the warning or message.</para>
        /// </summary>
        public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t pFunction)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamClient_SetWarningMessageHook(pFunction);
        }

        /// <summary>
        /// <para> Trigger global shutdown for the DLL</para>
        /// </summary>
        public static bool BShutdownIfAllPipesClosed()
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamClient_BShutdownIfAllPipesClosed();
        }

        /// <summary>
        /// <para> Expose HTTP interface</para>
        /// </summary>
        public static IntPtr GetISteamHTTP(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamHTTP(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> Exposes the ISteamUnifiedMessages interface</para>
        /// </summary>
        public static IntPtr GetISteamUnifiedMessages(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamUnifiedMessages(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> Exposes the ISteamController interface</para>
        /// </summary>
        public static IntPtr GetISteamController(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamController(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> Exposes the ISteamUGC interface</para>
        /// </summary>
        public static IntPtr GetISteamUGC(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamUGC(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> returns app list interface, only available on specially registered apps</para>
        /// </summary>
        public static IntPtr GetISteamAppList(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamAppList(hSteamUser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> Music Player</para>
        /// </summary>
        public static IntPtr GetISteamMusic(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamMusic(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> Music Player Remote</para>
        /// </summary>
        public static IntPtr GetISteamMusicRemote(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamMusicRemote(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> html page display</para>
        /// </summary>
        public static IntPtr GetISteamHTMLSurface(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamHTMLSurface(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> inventory</para>
        /// </summary>
        public static IntPtr GetISteamInventory(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamInventory(hSteamuser, hSteamPipe, pchVersion2);
            }
        }

        /// <summary>
        /// <para> Video</para>
        /// </summary>
        public static IntPtr GetISteamVideo(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchVersion2 = new InteropHelp.UTF8StringHandle(pchVersion))
            {
                return NativeMethods.ISteamClient_GetISteamVideo(hSteamuser, hSteamPipe, pchVersion2);
            }
        }
    }
}