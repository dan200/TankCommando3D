#if STEAM
using System;
using Steamworks;
using Dan200.Core.Render;
using Dan200.Core.Main;

namespace Dan200.Core.Network.Steamworks
{
	internal class SteamworksRemoteUser : IRemoteUser
	{
        private SteamworksNetwork m_network;
		private CSteamID m_id;

		public ulong ID
		{
			get
			{
				return m_id.m_SteamID;
			}
		}

		public CSteamID SteamID
		{
			get
			{
				return m_id;
			}
		}

		public string DisplayName
		{
			get
			{
				return SteamFriends.GetFriendPersonaName(m_id);
			}
		}

		public OnlineStatus Status
		{
			get
			{
				var personaState = SteamFriends.GetFriendPersonaState(m_id);
				if( personaState != EPersonaState.k_EPersonaStateOffline )
				{
					FriendGameInfo_t gameInfo; 
					if( SteamFriends.GetFriendGamePlayed(m_id, out gameInfo) )
					{
						if( gameInfo.m_gameID.AppID().m_AppId == App.Info.SteamAppID )
						{
							return OnlineStatus.InGame;
						}
						else
						{
							return OnlineStatus.InDifferentGame;
						}
					}

					switch(personaState)
					{
						case EPersonaState.k_EPersonaStateOnline:
						case EPersonaState.k_EPersonaStateLookingToTrade:
						case EPersonaState.k_EPersonaStateLookingToPlay:
						default:
							{
								return OnlineStatus.Online;
							}
						case EPersonaState.k_EPersonaStateBusy:
							{
								return OnlineStatus.Busy;
							}
						case EPersonaState.k_EPersonaStateAway:
						case EPersonaState.k_EPersonaStateSnooze:
							{
								return OnlineStatus.Away;
							}
					}
				}
				return OnlineStatus.Offline;
			}
		}

		public SteamworksRemoteUser(SteamworksNetwork network, CSteamID id)
		{
			m_network = network;
			m_id = id;
		}

		public Bitmap GetAvatar()
		{
			var handle = SteamFriends.GetMediumFriendAvatar(m_id);
			if( handle != 0 )
			{
				return m_network.DecodeBitmap(handle);
			}
			return null;
		}
	}
}
#endif
