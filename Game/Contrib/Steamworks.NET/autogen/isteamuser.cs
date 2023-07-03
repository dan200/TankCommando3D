// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// This file is automatically generated.
// Changes to this file will be reverted when you update Steamworks.NET

using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
    public static class SteamUser
    {
        /// <summary>
        /// <para> returns the HSteamUser this interface represents</para>
        /// <para> this is only used internally by the API, and by a few select interfaces that support multi-user</para>
        /// </summary>
        public static HSteamUser GetHSteamUser()
        {
            InteropHelp.TestIfAvailableClient();
            return (HSteamUser)NativeMethods.ISteamUser_GetHSteamUser();
        }

        /// <summary>
        /// <para> returns true if the Steam client current has a live connection to the Steam servers.</para>
        /// <para> If false, it means there is no active connection due to either a networking issue on the local machine, or the Steam server is down/busy.</para>
        /// <para> The Steam client will automatically be trying to recreate the connection as often as possible.</para>
        /// </summary>
        public static bool BLoggedOn()
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_BLoggedOn();
        }

        /// <summary>
        /// <para> returns the CSteamID of the account currently logged into the Steam client</para>
        /// <para> a CSteamID is a unique identifier for an account, and used to differentiate users in all parts of the Steamworks API</para>
        /// </summary>
        public static CSteamID GetSteamID()
        {
            InteropHelp.TestIfAvailableClient();
            return (CSteamID)NativeMethods.ISteamUser_GetSteamID();
        }

        /// <summary>
        /// <para> Multiplayer Authentication functions</para>
        /// <para> InitiateGameConnection() starts the state machine for authenticating the game client with the game server</para>
        /// <para> It is the client portion of a three-way handshake between the client, the game server, and the steam servers</para>
        /// <para> Parameters:</para>
        /// <para> void *pAuthBlob - a pointer to empty memory that will be filled in with the authentication token.</para>
        /// <para> int cbMaxAuthBlob - the number of bytes of allocated memory in pBlob. Should be at least 2048 bytes.</para>
        /// <para> CSteamID steamIDGameServer - the steamID of the game server, received from the game server by the client</para>
        /// <para> CGameID gameID - the ID of the current game. For games without mods, this is just CGameID( &lt;appID&gt; )</para>
        /// <para> uint32 unIPServer, uint16 usPortServer - the IP address of the game server</para>
        /// <para> bool bSecure - whether or not the client thinks that the game server is reporting itself as secure (i.e. VAC is running)</para>
        /// <para> return value - returns the number of bytes written to pBlob. If the return is 0, then the buffer passed in was too small, and the call has failed</para>
        /// <para> The contents of pBlob should then be sent to the game server, for it to use to complete the authentication process.</para>
        /// </summary>
        public static int InitiateGameConnection(byte[] pAuthBlob, int cbMaxAuthBlob, CSteamID steamIDGameServer, uint unIPServer, ushort usPortServer, bool bSecure)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_InitiateGameConnection(pAuthBlob, cbMaxAuthBlob, steamIDGameServer, unIPServer, usPortServer, bSecure);
        }

        /// <summary>
        /// <para> notify of disconnect</para>
        /// <para> needs to occur when the game client leaves the specified game server, needs to match with the InitiateGameConnection() call</para>
        /// </summary>
        public static void TerminateGameConnection(uint unIPServer, ushort usPortServer)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamUser_TerminateGameConnection(unIPServer, usPortServer);
        }

        /// <summary>
        /// <para> Legacy functions</para>
        /// <para> used by only a few games to track usage events</para>
        /// </summary>
        public static void TrackAppUsageEvent(CGameID gameID, int eAppUsageEvent, string pchExtraInfo = "")
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchExtraInfo2 = new InteropHelp.UTF8StringHandle(pchExtraInfo))
            {
                NativeMethods.ISteamUser_TrackAppUsageEvent(gameID, eAppUsageEvent, pchExtraInfo2);
            }
        }

        /// <summary>
        /// <para> get the local storage folder for current Steam account to write application data, e.g. save games, configs etc.</para>
        /// <para> this will usually be something like "C:\Progam Files\Steam\userdata\&lt;SteamID&gt;\&lt;AppID&gt;\local"</para>
        /// </summary>
        public static bool GetUserDataFolder(out string pchBuffer, int cubBuffer)
        {
            InteropHelp.TestIfAvailableClient();
            IntPtr pchBuffer2 = Marshal.AllocHGlobal(cubBuffer);
            bool ret = NativeMethods.ISteamUser_GetUserDataFolder(pchBuffer2, cubBuffer);
            pchBuffer = ret ? InteropHelp.PtrToStringUTF8(pchBuffer2) : null;
            Marshal.FreeHGlobal(pchBuffer2);
            return ret;
        }

        /// <summary>
        /// <para> Starts voice recording. Once started, use GetVoice() to get the data</para>
        /// </summary>
        public static void StartVoiceRecording()
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamUser_StartVoiceRecording();
        }

        /// <summary>
        /// <para> Stops voice recording. Because people often release push-to-talk keys early, the system will keep recording for</para>
        /// <para> a little bit after this function is called. GetVoice() should continue to be called until it returns</para>
        /// <para> k_eVoiceResultNotRecording</para>
        /// </summary>
        public static void StopVoiceRecording()
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamUser_StopVoiceRecording();
        }

        /// <summary>
        /// <para> Determine the amount of captured audio data that is available in bytes.</para>
        /// <para> This provides both the compressed and uncompressed data. Please note that the uncompressed</para>
        /// <para> data is not the raw feed from the microphone: data may only be available if audible</para>
        /// <para> levels of speech are detected.</para>
        /// <para> nUncompressedVoiceDesiredSampleRate is necessary to know the number of bytes to return in pcbUncompressed - can be set to 0 if you don't need uncompressed (the usual case)</para>
        /// <para> If you're upgrading from an older Steamworks API, you'll want to pass in 11025 to nUncompressedVoiceDesiredSampleRate</para>
        /// </summary>
        public static EVoiceResult GetAvailableVoice(out uint pcbCompressed, out uint pcbUncompressed, uint nUncompressedVoiceDesiredSampleRate)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_GetAvailableVoice(out pcbCompressed, out pcbUncompressed, nUncompressedVoiceDesiredSampleRate);
        }

        /// <summary>
        /// <para> Gets the latest voice data from the microphone. Compressed data is an arbitrary format, and is meant to be handed back to</para>
        /// <para> DecompressVoice() for playback later as a binary blob. Uncompressed data is 16-bit, signed integer, 11025Hz PCM format.</para>
        /// <para> Please note that the uncompressed data is not the raw feed from the microphone: data may only be available if audible</para>
        /// <para> levels of speech are detected, and may have passed through denoising filters, etc.</para>
        /// <para> This function should be called as often as possible once recording has started; once per frame at least.</para>
        /// <para> nBytesWritten is set to the number of bytes written to pDestBuffer.</para>
        /// <para> nUncompressedBytesWritten is set to the number of bytes written to pUncompressedDestBuffer.</para>
        /// <para> You must grab both compressed and uncompressed here at the same time, if you want both.</para>
        /// <para> Matching data that is not read during this call will be thrown away.</para>
        /// <para> GetAvailableVoice() can be used to determine how much data is actually available.</para>
        /// <para> If you're upgrading from an older Steamworks API, you'll want to pass in 11025 to nUncompressedVoiceDesiredSampleRate</para>
        /// </summary>
        public static EVoiceResult GetVoice(bool bWantCompressed, byte[] pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten, bool bWantUncompressed, byte[] pUncompressedDestBuffer, uint cbUncompressedDestBufferSize, out uint nUncompressBytesWritten, uint nUncompressedVoiceDesiredSampleRate)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_GetVoice(bWantCompressed, pDestBuffer, cbDestBufferSize, out nBytesWritten, bWantUncompressed, pUncompressedDestBuffer, cbUncompressedDestBufferSize, out nUncompressBytesWritten, nUncompressedVoiceDesiredSampleRate);
        }

        /// <summary>
        /// <para> Decompresses a chunk of compressed data produced by GetVoice().</para>
        /// <para> nBytesWritten is set to the number of bytes written to pDestBuffer unless the return value is k_EVoiceResultBufferTooSmall.</para>
        /// <para> In that case, nBytesWritten is set to the size of the buffer required to decompress the given</para>
        /// <para> data. The suggested buffer size for the destination buffer is 22 kilobytes.</para>
        /// <para> The output format of the data is 16-bit signed at the requested samples per second.</para>
        /// <para> If you're upgrading from an older Steamworks API, you'll want to pass in 11025 to nDesiredSampleRate</para>
        /// </summary>
        public static EVoiceResult DecompressVoice(byte[] pCompressed, uint cbCompressed, byte[] pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten, uint nDesiredSampleRate)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_DecompressVoice(pCompressed, cbCompressed, pDestBuffer, cbDestBufferSize, out nBytesWritten, nDesiredSampleRate);
        }

        /// <summary>
        /// <para> This returns the frequency of the voice data as it's stored internally; calling DecompressVoice() with this size will yield the best results</para>
        /// </summary>
        public static uint GetVoiceOptimalSampleRate()
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_GetVoiceOptimalSampleRate();
        }

        /// <summary>
        /// <para> Retrieve ticket to be sent to the entity who wishes to authenticate you.</para>
        /// <para> pcbTicket retrieves the length of the actual ticket.</para>
        /// </summary>
        public static HAuthTicket GetAuthSessionTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket)
        {
            InteropHelp.TestIfAvailableClient();
            return (HAuthTicket)NativeMethods.ISteamUser_GetAuthSessionTicket(pTicket, cbMaxTicket, out pcbTicket);
        }

        /// <summary>
        /// <para> Authenticate ticket from entity steamID to be sure it is valid and isnt reused</para>
        /// <para> Registers for callbacks if the entity goes offline or cancels the ticket ( see ValidateAuthTicketResponse_t callback and EAuthSessionResponse )</para>
        /// </summary>
        public static EBeginAuthSessionResult BeginAuthSession(byte[] pAuthTicket, int cbAuthTicket, CSteamID steamID)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_BeginAuthSession(pAuthTicket, cbAuthTicket, steamID);
        }

        /// <summary>
        /// <para> Stop tracking started by BeginAuthSession - called when no longer playing game with this entity</para>
        /// </summary>
        public static void EndAuthSession(CSteamID steamID)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamUser_EndAuthSession(steamID);
        }

        /// <summary>
        /// <para> Cancel auth ticket from GetAuthSessionTicket, called when no longer playing game with the entity you gave the ticket to</para>
        /// </summary>
        public static void CancelAuthTicket(HAuthTicket hAuthTicket)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamUser_CancelAuthTicket(hAuthTicket);
        }

        /// <summary>
        /// <para> After receiving a user's authentication data, and passing it to BeginAuthSession, use this function</para>
        /// <para> to determine if the user owns downloadable content specified by the provided AppID.</para>
        /// </summary>
        public static EUserHasLicenseForAppResult UserHasLicenseForApp(CSteamID steamID, AppId_t appID)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_UserHasLicenseForApp(steamID, appID);
        }

        /// <summary>
        /// <para> returns true if this users looks like they are behind a NAT device. Only valid once the user has connected to steam</para>
        /// <para> (i.e a SteamServersConnected_t has been issued) and may not catch all forms of NAT.</para>
        /// </summary>
        public static bool BIsBehindNAT()
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_BIsBehindNAT();
        }

        /// <summary>
        /// <para> set data to be replicated to friends so that they can join your game</para>
        /// <para> CSteamID steamIDGameServer - the steamID of the game server, received from the game server by the client</para>
        /// <para> uint32 unIPServer, uint16 usPortServer - the IP address of the game server</para>
        /// </summary>
        public static void AdvertiseGame(CSteamID steamIDGameServer, uint unIPServer, ushort usPortServer)
        {
            InteropHelp.TestIfAvailableClient();
            NativeMethods.ISteamUser_AdvertiseGame(steamIDGameServer, unIPServer, usPortServer);
        }

        /// <summary>
        /// <para> Requests a ticket encrypted with an app specific shared key</para>
        /// <para> pDataToInclude, cbDataToInclude will be encrypted into the ticket</para>
        /// <para> ( This is asynchronous, you must wait for the ticket to be completed by the server )</para>
        /// </summary>
        public static SteamAPICall_t RequestEncryptedAppTicket(byte[] pDataToInclude, int cbDataToInclude)
        {
            InteropHelp.TestIfAvailableClient();
            return (SteamAPICall_t)NativeMethods.ISteamUser_RequestEncryptedAppTicket(pDataToInclude, cbDataToInclude);
        }

        /// <summary>
        /// <para> retrieve a finished ticket</para>
        /// </summary>
        public static bool GetEncryptedAppTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_GetEncryptedAppTicket(pTicket, cbMaxTicket, out pcbTicket);
        }

        /// <summary>
        /// <para> Trading Card badges data access</para>
        /// <para> if you only have one set of cards, the series will be 1</para>
        /// <para> the user has can have two different badges for a series; the regular (max level 5) and the foil (max level 1)</para>
        /// </summary>
        public static int GetGameBadgeLevel(int nSeries, bool bFoil)
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_GetGameBadgeLevel(nSeries, bFoil);
        }

        /// <summary>
        /// <para> gets the Steam Level of the user, as shown on their profile</para>
        /// </summary>
        public static int GetPlayerSteamLevel()
        {
            InteropHelp.TestIfAvailableClient();
            return NativeMethods.ISteamUser_GetPlayerSteamLevel();
        }

        /// <summary>
        /// <para> Requests a URL which authenticates an in-game browser for store check-out,</para>
        /// <para> and then redirects to the specified URL. As long as the in-game browser</para>
        /// <para> accepts and handles session cookies, Steam microtransaction checkout pages</para>
        /// <para> will automatically recognize the user instead of presenting a login page.</para>
        /// <para> The result of this API call will be a StoreAuthURLResponse_t callback.</para>
        /// <para> NOTE: The URL has a very short lifetime to prevent history-snooping attacks,</para>
        /// <para> so you should only call this API when you are about to launch the browser,</para>
        /// <para> or else immediately navigate to the result URL using a hidden browser window.</para>
        /// <para> NOTE 2: The resulting authorization cookie has an expiration time of one day,</para>
        /// <para> so it would be a good idea to request and visit a new auth URL every 12 hours.</para>
        /// </summary>
        public static SteamAPICall_t RequestStoreAuthURL(string pchRedirectURL)
        {
            InteropHelp.TestIfAvailableClient();
            using (var pchRedirectURL2 = new InteropHelp.UTF8StringHandle(pchRedirectURL))
            {
                return (SteamAPICall_t)NativeMethods.ISteamUser_RequestStoreAuthURL(pchRedirectURL2);
            }
        }
#if _PS3
		/// <summary>
		/// <para> Initiates PS3 Logon request using just PSN ticket.</para>
		/// <para> PARAMS: bInteractive - If set tells Steam to go ahead and show the PS3 NetStart dialog if needed to</para>
		/// <para> prompt the user for network setup/PSN logon before initiating the Steam side of the logon.</para>
		/// <para> Listen for SteamServersConnected_t or SteamServerConnectFailure_t for status.  SteamServerConnectFailure_t</para>
		/// <para> may return with EResult k_EResultExternalAccountUnlinked if the PSN account is unknown to Steam.  You should</para>
		/// <para> then call LogOnAndLinkSteamAccountToPSN() after prompting the user for credentials to establish a link.</para>
		/// <para> Future calls to LogOn() after the one time link call should succeed as long as the user is connected to PSN.</para>
		/// </summary>
		public static void LogOn(bool bInteractive) {
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_LogOn(bInteractive);
		}

		/// <summary>
		/// <para> Initiates a request to logon with a specific steam username/password and create a PSN account link at</para>
		/// <para> the same time.  Should call this only if LogOn() has failed and indicated the PSN account is unlinked.</para>
		/// <para> PARAMS: bInteractive - If set tells Steam to go ahead and show the PS3 NetStart dialog if needed to</para>
		/// <para> prompt the user for network setup/PSN logon before initiating the Steam side of the logon.  pchUserName</para>
		/// <para> should be the users Steam username, and pchPassword should be the users Steam password.</para>
		/// <para> Listen for SteamServersConnected_t or SteamServerConnectFailure_t for status.  SteamServerConnectFailure_t</para>
		/// <para> may return with EResult k_EResultOtherAccountAlreadyLinked if already linked to another account.</para>
		/// </summary>
		public static void LogOnAndLinkSteamAccountToPSN(bool bInteractive, string pchUserName, string pchPassword) {
			InteropHelp.TestIfAvailableClient();
			using (var pchUserName2 = new InteropHelp.UTF8StringHandle(pchUserName))
			using (var pchPassword2 = new InteropHelp.UTF8StringHandle(pchPassword)) {
				NativeMethods.ISteamUser_LogOnAndLinkSteamAccountToPSN(bInteractive, pchUserName2, pchPassword2);
			}
		}

		/// <summary>
		/// <para> Final logon option for PS3, this logs into an existing account if already linked, but if not already linked</para>
		/// <para> creates a new account using the info in the PSN ticket to generate a unique account name.  The new account is</para>
		/// <para> then linked to the PSN ticket.  This is the faster option for new users who don't have an existing Steam account</para>
		/// <para> to get into multiplayer.</para>
		/// <para> PARAMS: bInteractive - If set tells Steam to go ahead and show the PS3 NetStart dialog if needed to</para>
		/// <para> prompt the user for network setup/PSN logon before initiating the Steam side of the logon.</para>
		/// </summary>
		public static void LogOnAndCreateNewSteamAccountIfNeeded(bool bInteractive) {
			InteropHelp.TestIfAvailableClient();
			NativeMethods.ISteamUser_LogOnAndCreateNewSteamAccountIfNeeded(bInteractive);
		}

		/// <summary>
		/// <para> Returns a special SteamID that represents the user's PSN information. Can be used to query the user's PSN avatar,</para>
		/// <para> online name, etc. through the standard Steamworks interfaces.</para>
		/// </summary>
		public static CSteamID GetConsoleSteamID() {
			InteropHelp.TestIfAvailableClient();
			return (CSteamID)NativeMethods.ISteamUser_GetConsoleSteamID();
		}
#endif
    }
}