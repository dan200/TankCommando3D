#if STEAM
using Dan200.Core.Async;
using Dan200.Core.Input;
using Dan200.Core.Input.Steamworks;
using Dan200.Core.Main;
using Dan200.Core.Util;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Dan200.Core.Render;
using System.IO;
using Dan200.Core.Platform;
using Dan200.Core.Platform.SDL2;
using Dan200.Core.Assets;

namespace Dan200.Core.Network.Steamworks
{
    internal class SteamworksNetwork : INetwork
    {
		private static Dictionary<string, string> s_steamLanguageCodes = new Dictionary<string, string>() {
			{ "brazilian", "pt_BR" },
			{ "bulgarian", "bg" },
			{ "czech", "cs" },
			{ "danish", "da" },
			{ "dutch", "nl" },
			{ "english", "en" },
			{ "finnish", "fi" },
			{ "french", "fr" },
			{ "german", "de" },
			{ "greek", "el" },
			{ "hungarian", "hu" },
			{ "italian", "it" },
			{ "japanese", "ja" },
			{ "koreana", "ko" },
			{ "norwegian", "no" },
			{ "polish", "pl" },
			{ "portuguese", "pt" },
			{ "romanian", "ro" },
			{ "russian", "ru" },
			{ "schinese", "zh_CHS" },
			{ "spanish", "es" },
			{ "swedish", "sv" },
			{ "tchinese", "zh_CHT" },
			{ "thai", "th" },
			{ "turkish", "tr" },
			{ "ukrainian", "uk" },
		};

        private SteamworksLocalUser m_localUser;
        private SteamworksWorkshop m_workshop;
		private SteamRemoteStorageFileStore m_remoteSaveStore;
        private SteamworksRemoteConnectionListener m_connectionListener;

		private HashSet<CSteamID> m_interestedSenders;
		private Dictionary<CSteamID, EP2PSessionError> m_senderErrors;
		private CSteamID m_lastReadPacketSender;
		private byte[] m_lastReadPacket;
		private int m_lastReadPacketSize;

        private HashSet<Achievement> m_achievements;
		private Dictionary<Statistic, int> m_stats;
		private Dictionary<Statistic, long> m_globalStats;
		private Dictionary<Achievement, bool> m_earlyAchievementChanges;
        private Dictionary<Statistic, int> m_earlyStatAdditions;
        private Dictionary<Statistic, int> m_earlyStatSets;
        private int m_concurrentPlayers;
        private bool m_statsNeedUpload;
        private bool m_earlyUpload;
        private bool m_initialised;

        public bool SupportsAchievements
        {
            get
            {
                return true;
            }
        }

        public bool SupportsStatistics
        {
            get
            {
                return true;
            }
        }

        public bool SupportsWorkshop
        {
            get
            {
                return true;
            }
        }

        public bool SupportsLeaderboards
        {
            get
            {
                return true;
            }
        }

		public bool SupportsFriends
		{
			get
			{
				return true;
			}
		}

        public bool SupportsConnections
        {
            get
            {
                return true;
            }
        }

		public bool SupportsRemoteSave
		{
			get
			{
				return true;
			}
		}

		public ILocalUser LocalUser
        {
            get
            {
                return m_localUser;
            }
        }

        public IWorkshop Workshop
        {
            get
            {
                return m_workshop;
            }
        }

		public IWritableFileStore RemoteSaveStore
		{
			get
			{
				return m_remoteSaveStore;
			}
		}

        public IRemoteUserConnectionListener ConnectionListener
        {
            get
            {
                return m_connectionListener;
            }
        }


		public string Language
		{
			get
			{
				var steamLanguage = SteamApps.GetCurrentGameLanguage();
				if (s_steamLanguageCodes.ContainsKey(steamLanguage))
				{
					var code = s_steamLanguageCodes[steamLanguage];
					return code;
				}
				return "en";
			}
		}

		public SteamworksNetwork()
        {
            m_localUser = new SteamworksLocalUser(this);
            m_workshop = new SteamworksWorkshop(this);
			m_remoteSaveStore = new SteamRemoteStorageFileStore();
            m_connectionListener = new SteamworksRemoteConnectionListener(this);

			m_interestedSenders = new HashSet<CSteamID>();
			m_senderErrors = new Dictionary<CSteamID, EP2PSessionError>();
			m_lastReadPacket = new byte[4096];
			m_lastReadPacketSize = 0;
			m_lastReadPacketSender = CSteamID.Nil;

			m_achievements = new HashSet<Achievement>();
            m_stats = new Dictionary<Statistic, int>();
			m_globalStats = new Dictionary<Statistic, long>();
            m_earlyAchievementChanges = new Dictionary<Achievement, bool>();
            m_earlyStatAdditions = new Dictionary<Statistic, int>();
            m_earlyStatSets = new Dictionary<Statistic, int>();
			m_concurrentPlayers = 1;
            m_statsNeedUpload = false;
            m_earlyUpload = false;
            m_initialised = false;

            RegisterCallback<UserStatsReceived_t>(OnUserStatsReceived);
            RegisterCallback<GlobalStatsReceived_t>(OnGlobalStatsReceived);
            RegisterCallback<UserStatsStored_t>(OnUserStatsStored);
			RegisterCallback<UserAchievementStored_t>(OnUserAchievementStored);
			RegisterCallback<P2PSessionConnectFail_t>(OnP2PSessionConnectFail);

            RequestStats();
        }

		public IRemoteUserConnection ConnectTo(IRemoteUser user)
        {
			var steamUser = (SteamworksRemoteUser)user;
			if (!AddInterestedPacketSender(steamUser.SteamID))
			{
				throw new IOException("A connection to this user is already open");
			}
			return new SteamworksRemoteConnection(this, steamUser);
        }

		public bool AddInterestedPacketSender(CSteamID id)
		{
			return m_interestedSenders.Add(id);
		}

		public bool RemoveInterestedPacketSender(CSteamID id)
		{
			if (m_interestedSenders.Remove(id))
			{
				m_senderErrors.Remove(id);
				if (m_lastReadPacketSender == id)
				{
					m_lastReadPacketSender = CSteamID.Nil;
				}
				return true;
			}
			return false;
		}

		public bool ReadErrorFrom(CSteamID id, out EP2PSessionError error)
		{
			if (m_senderErrors.TryGetValue(id, out error))
			{
				m_senderErrors.Remove(id);
				return true;
			}
			return false;
		}

		public bool PeekPacketFrom(CSteamID id)
		{
			byte[] unusedData;
			int unusedSize, unusedOffset;
			return ReadPacketFrom(id, out unusedData, out unusedOffset, out unusedSize, true);
		}

		public bool ReadPacketFrom(CSteamID id, out byte[] o_data, out int o_offset, out int o_size)
		{
			return ReadPacketFrom(id, out o_data, out o_offset, out o_size, false);
		}

		private bool ReadPacketFrom(CSteamID id, out byte[] o_data, out int o_offset, out int o_size, bool peek)
		{
			// See if the last packet we read matches
			if (m_lastReadPacketSender == id)
			{
				o_data = m_lastReadPacket;
				o_offset = 0;
				o_size = m_lastReadPacketSize;
				if (!peek)
				{
					m_lastReadPacketSender = CSteamID.Nil;
				}
				return true;
			}

			// Otherwise, read a new packet
			if (m_lastReadPacketSender == CSteamID.Nil)
			{
				uint size;
				if (SteamNetworking.IsP2PPacketAvailable(out size, 0))
				{
					CSteamID sender;
					if (m_lastReadPacket.Length < size)
					{
						Array.Resize(ref m_lastReadPacket, (int)size);
					}
					if (SteamNetworking.ReadP2PPacket(m_lastReadPacket, (uint)m_lastReadPacket.Length, out size, out sender))
					{
						if (m_interestedSenders.Contains(sender))
						{
							m_lastReadPacketSender = sender;
							m_lastReadPacketSize = (int)size;
							if (sender == id)
							{
								// If the data matches, return it
								o_data = m_lastReadPacket;
								o_data = m_lastReadPacket;
								o_offset = 0;
								o_size = m_lastReadPacketSize;
								if (!peek)
								{
									m_lastReadPacketSender = CSteamID.Nil;
								}
								return true;
							}
						}
					}
				}
			}

			// No matching packet found
			o_data = null;
			o_offset = 0;
			o_size = 0;
			return false;
		}

        public void SetAchievementCorner(AchievementCorner corner)
        {
            ENotificationPosition position;
            switch (corner)
            {
                case AchievementCorner.TopLeft:
                    {
                        position = ENotificationPosition.k_EPositionTopLeft;
                        break;
                    }
                case AchievementCorner.TopRight:
                    {
                        position = ENotificationPosition.k_EPositionTopRight;
                        break;
                    }
                case AchievementCorner.BottomLeft:
                    {
                        position = ENotificationPosition.k_EPositionBottomLeft;
                        break;
                    }
                case AchievementCorner.BottomRight:
                default:
                    {
                        position = ENotificationPosition.k_EPositionBottomRight;
                        break;
                    }
            }
            SteamUtils.SetOverlayNotificationPosition(position);
        }

        public bool OpenOverlayWebBrowser(string url)
        {
            if (SteamUtils.IsOverlayEnabled())
            {
                App.Log("Opening steam web browser to {0}", url);
                SteamFriends.ActivateGameOverlayToWebPage(url);
                return true;
            }
            return false;
        }

        public bool OpenWorkshopItem(ulong id)
        {
			return App.Platform.OpenWebBrowser(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", id), WebBrowserType.Overlay);
        }

        public bool OpenWorkshopHub()
        {
            return App.Platform.OpenWebBrowser(string.Format("http://steamcommunity.com/workshop/browse/?appid={0}", App.Info.SteamAppID), WebBrowserType.Overlay);
        }

        public bool OpenWorkshopHub(string[] filterTags)
        {
            var url = new StringBuilder();
            url.Append(string.Format("http://steamcommunity.com/workshop/browse/?appid={0}", App.Info.SteamAppID));
            for (int i = 0; i < filterTags.Length; ++i)
            {
                var tag = filterTags[i];
                url.Append(string.Format("&requiredtags[]={0}", tag.URLEncode()));
            }
            return App.Platform.OpenWebBrowser(url.ToString(), WebBrowserType.Overlay);
        }

        public bool OpenAchievementsHub()
        {
            if (SteamUtils.IsOverlayEnabled())
            {
                SteamFriends.ActivateGameOverlay("Achievements");
                return true;
            }
            else
            {
                return App.Platform.OpenWebBrowser(string.Format("http://steamcommunity.com/stats/{0}/achievements/", App.Info.SteamAppID), WebBrowserType.Overlay);
            }
        }

        public bool OpenSteamControllerConfig(ISteamController controller)
        {
            if (controller is SteamworksSteamController)
            {
                return SteamController.ShowBindingPanel(
                    ((SteamworksSteamController)controller).Handle
                );
            }
            return false;
        }

        public bool OpenLeaderboard(ulong id)
        {
            return App.Platform.OpenWebBrowser(string.Format("http://steamcommunity.com/stats/{0}/leaderboards/{1}", App.Info.SteamAppID, id), WebBrowserType.Overlay);
        }

        public Promise<ulong> GetLeaderboardID(string name, bool createIfAbsent)
        {
            var result = new Promise<ulong>();
            MakeCall(
                createIfAbsent ?
                    SteamUserStats.FindOrCreateLeaderboard(name, ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric) :
                    SteamUserStats.FindLeaderboard(name),
                delegate (LeaderboardFindResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_bLeaderboardFound == 0)
                    {
                        result.Fail(new SteamworksException("Failed to get leaderboard ID"));
                    }
                    else
                    {
                        var id = args.m_hSteamLeaderboard.m_SteamLeaderboard;
                        result.Succeed(id);
                    }
                }
            );
            return result;
        }

        public Promise<Leaderboard> DownloadLeaderboard(ulong id, LeaderboardType type, int maxEntries)
        {
            var result = new Promise<Leaderboard>();
            if (maxEntries > 0)
            {
                ELeaderboardDataRequest requestType;
                int rangeStart, rangeEnd;
                switch (type)
                {
                    case LeaderboardType.Global:
                    default:
                        {
                            requestType = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal;
                            rangeStart = 1;
                            rangeEnd = maxEntries;
                            break;
                        }
                    case LeaderboardType.Local:
                        {
                            requestType = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser;
                            rangeStart = -(maxEntries / 2);
                            rangeEnd = rangeStart + maxEntries - 1;
                            break;
                        }
                    case LeaderboardType.Friends:
                        {
                            requestType = ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends;
                            rangeStart = 1;
                            rangeEnd = maxEntries;
                            break;
                        }
                }
                MakeCall(
                    SteamUserStats.DownloadLeaderboardEntries(new SteamLeaderboard_t(id), requestType, rangeStart, rangeEnd),
                    delegate (LeaderboardScoresDownloaded_t args2, bool ioFailure2)
                    {
                        if (ioFailure2)
                        {
                            result.Fail(new SteamworksException("Failed to download leaderboard"));
                        }
                        else
                        {
                            var leaderboard = new Leaderboard(id, type);
                            for (int i = 0; i < System.Math.Min(args2.m_cEntryCount, maxEntries); ++i)
                            {
                                LeaderboardEntry_t entry;
                                if (SteamUserStats.GetDownloadedLeaderboardEntry(args2.m_hSteamLeaderboardEntries, i, out entry, null, 0))
                                {
                                    var rank = entry.m_nGlobalRank;
                                    var username = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
                                    var score = entry.m_nScore;
                                    leaderboard.Entries.Add(new LeaderboardEntry(rank, username, score));
                                }
                            }
                            result.Succeed(leaderboard);
                        }
                    }
                );
            }
            else
            {
                result.Succeed(new Leaderboard(id, type));
            }
            return result;
        }

        public Promise SubmitLocalUserLeaderboardScore(ulong id, int score)
        {
            var result = new Promise();
            MakeCall(
                SteamUserStats.UploadLeaderboardScore(new SteamLeaderboard_t(id), ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, null, 0),
                delegate (LeaderboardScoreUploaded_t args2, bool ioFailure2)
                {
                    if (ioFailure2 || args2.m_bSuccess == 0)
                    {
                        result.Fail(new SteamworksException("Failed to submit leaderboard score"));
                    }
                    else
                    {
                        result.Succeed();
                    }
                }
            );
            return result;
        }

		public void UnlockLocalUserAchievement(Achievement achivement)
        {
			App.Assert(App.Info.Achievements.Contains(achivement));
            if (m_initialised)
            {
				if (!m_achievements.Contains(achivement))
                {
					if (SteamUserStats.SetAchievement(achivement.ID) && SteamUserStats.StoreStats())
                    {
						m_achievements.Add(achivement);
                        m_statsNeedUpload = false;
                    }
                }
            }
            else
            {
				m_earlyAchievementChanges[achivement] = true;
            }
        }

		public void RemoveLocalUserAchievement(Achievement achievement)
        {
			App.Assert(App.Info.Achievements.Contains(achievement));
            if (m_initialised)
            {
				if (m_achievements.Contains(achievement))
                {
					if (SteamUserStats.ClearAchievement(achievement.ID) && SteamUserStats.StoreStats())
                    {
						m_achievements.Remove(achievement);
                        m_statsNeedUpload = false;
                    }
                }
            }
            else
            {
				m_earlyAchievementChanges[achievement] = false;
            }
        }

		public void IndicateLocalUserAchievementProgress(Achievement achievement, int currentValue, int unlockValue)
        {
			App.Assert(App.Info.Achievements.Contains(achievement));
			App.Assert(currentValue >= 0);
			App.Assert(unlockValue >= currentValue);
            if (m_initialised)
            {
				if (!m_achievements.Contains(achievement))
                {
					SteamUserStats.IndicateAchievementProgress(achievement.ID, (uint)currentValue, (uint)unlockValue);
                }
            }
        }

		public void AddLocalUserStat(Statistic statistic, int count)
        {
			App.Assert(App.Info.Statistics.Contains(statistic));
			App.Assert(count >= 0);
            if (m_initialised)
            {
                int currentValue;
				if (SteamUserStats.GetStat(statistic.ID, out currentValue))
                {
					int newValue = System.Math.Max(currentValue + count, currentValue);
					if (newValue != currentValue && SteamUserStats.SetStat(statistic.ID, newValue))
                    {
                        m_statsNeedUpload = true;
						m_stats[statistic] = newValue;
						if (m_globalStats.ContainsKey(statistic))
                        {
							m_globalStats[statistic] += newValue - currentValue;
                        }
                    }
                }
            }
            else
            {
				if (m_earlyStatSets.ContainsKey(statistic))
				{
					m_earlyStatSets[statistic] += count;
				}
				else if (m_earlyStatAdditions.ContainsKey(statistic))
                {
                    m_earlyStatAdditions[statistic] += count;
                }
                else
                {
                    m_earlyStatAdditions[statistic] = count;
                }
            }
        }

        public void SetLocalUserStat(Statistic statistic, int value)
        {
			App.Assert(App.Info.Statistics.Contains(statistic));
			App.Assert(value >= 0);
            if (m_initialised)
            {
                int currentValue;
				if (SteamUserStats.GetStat(statistic.ID, out currentValue))
                {
					int newValue = System.Math.Max(currentValue, value);
					if (newValue != currentValue && SteamUserStats.SetStat(statistic.ID, newValue))
                    {
                        m_statsNeedUpload = true;
						m_stats[statistic] = newValue;
						if (m_globalStats.ContainsKey(statistic))
                        {
                            m_globalStats[statistic] += newValue - currentValue;
                        }
                    }
                }
            }
            else
            {
				m_earlyStatAdditions.Remove(statistic);
				m_earlyStatSets[statistic] = value;
            }
        }

		public int GetLocalUserStat(Statistic statistic)
        {
			App.Assert(App.Info.Statistics.Contains(statistic));
            if (m_initialised)
            {
                if (m_stats.ContainsKey(statistic))
                {
                    return m_stats[statistic];
                }
            }
            return 0;
        }

        public long GetGlobalStatistic(Statistic statistic)
        {
			App.Assert(App.Info.Statistics.Contains(statistic));
			long globalStat = 0;
			if (m_globalStats.ContainsKey(statistic))
            {
				globalStat = m_globalStats[statistic];
            }
			long localStat = GetLocalUserStat(statistic);
            return System.Math.Max(localStat, globalStat);
        }

        public void UploadStatistics()
        {
            if (m_initialised)
            {
                if (m_statsNeedUpload && SteamUserStats.StoreStats())
                {
                    m_statsNeedUpload = false;
                }
            }
            else
            {
                m_earlyUpload = true;
            }
        }

        public int GetConcurrentPlayers()
        {
            return m_concurrentPlayers;
        }

        private void RequestStats()
        {
            SteamUserStats.RequestCurrentStats();
            MakeCall(SteamUserStats.RequestGlobalStats(0), delegate (GlobalStatsReceived_t param, bool bIOFailure)
            {
                if (!bIOFailure)
                {
                    OnGlobalStatsReceived(param);
                }
            });
            MakeCall(SteamUserStats.GetNumberOfCurrentPlayers(), delegate (NumberOfCurrentPlayers_t param, bool bIOFailure)
           	{
				if (!bIOFailure)
				{
					OnNumberOfCurrentPlayersReceived(param);
				}
           	});
        }

        private List<object> m_allCallbacks = new List<object>();

        public void RegisterCallback<TCallbackType>(Callback<TCallbackType>.DispatchDelegate callbackDelegate)
        {
            var callback = new Callback<TCallbackType>(callbackDelegate);
            m_allCallbacks.Add(callback);
        }

        private HashSet<object> m_pendingCallResults = new HashSet<object>();

        public void MakeCall<TCallResultType>(SteamAPICall_t apiCall, CallResult<TCallResultType>.APIDispatchDelegate callResultDelegate)
        {
            CallResult<TCallResultType> callResult = null;
            callResult = new CallResult<TCallResultType>(delegate (TCallResultType param, bool ioFailure)
            {
               callResultDelegate.Invoke(param, ioFailure);
               m_pendingCallResults.Remove(callResult);
            });
            m_pendingCallResults.Add(callResult);
            callResult.Set(apiCall);
        }

		public Bitmap DecodeBitmap(int handle)
		{
			uint width, height;
			if (SteamUtils.GetImageSize(handle, out width, out height))
			{
				var buffer = new byte[width * height * 4];
				if (SteamUtils.GetImageRGBA(handle, buffer, buffer.Length))
				{
					return new Bitmap((int)width, (int)height, 4, (int)width * 4, ColourSpace.SRGB, buffer);
				}
			}
			return null;
		}

        private void OnUserStatsReceived(UserStatsReceived_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_eResult == EResult.k_EResultOK)
                {
                    // Get currently unlocked achivements and stats
                    m_initialised = true;
                    m_achievements.Clear();
					foreach(var achievement in App.Info.Achievements)
                    {
                        bool achieved = false;
						if (SteamUserStats.GetAchievement(achievement.ID, out achieved) && achieved)
                        {
							m_achievements.Add(achievement);
                        }
                    }
                    m_stats.Clear();
					foreach(var statistic in App.Info.Statistics)
                    {
                        int value = 0;
						if (SteamUserStats.GetStat(statistic.ID, out value))
                        {
							m_stats[statistic] = value;
                        }
                    }

                    // Ping achivements and stats unlocked before we initialised
                    if (m_earlyAchievementChanges.Count > 0)
                    {
                        foreach (var entry in m_earlyAchievementChanges)
                        {
                            if (entry.Value)
                            {
                                UnlockLocalUserAchievement(entry.Key);
                            }
                            else
                            {
                                RemoveLocalUserAchievement(entry.Key);
                            }
                        }
                        m_earlyAchievementChanges.Clear();
                    }
                    if (m_earlyStatAdditions.Count > 0)
                    {
                        foreach (var entry in m_earlyStatAdditions)
                        {
                            AddLocalUserStat(entry.Key, entry.Value);
                        }
                        m_earlyStatAdditions.Clear();
                    }
                    if (m_earlyStatSets.Count > 0)
                    {
                        foreach (var entry in m_earlyStatSets)
                        {
                            SetLocalUserStat(entry.Key, entry.Value);
                        }
                        m_earlyStatSets.Clear();
                    }
                    if (m_earlyUpload)
                    {
                        UploadStatistics();
                        m_earlyUpload = false;
                    }
                }
            }
        }

        private void OnGlobalStatsReceived(GlobalStatsReceived_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_eResult == EResult.k_EResultOK)
                {
                    // Collect global stats
                    m_globalStats.Clear();
					foreach(var statistic in App.Info.Statistics)
                    {
                        long globalValue = 0;
						if (SteamUserStats.GetGlobalStat(statistic.ID, out globalValue))
                        {
							m_globalStats[statistic] = globalValue;
                        }
                    }
                }
            }
        }

        private void OnNumberOfCurrentPlayersReceived(NumberOfCurrentPlayers_t args)
        {
            if (args.m_bSuccess == 1)
            {
                m_concurrentPlayers = System.Math.Max(args.m_cPlayers, 1);
            }
        }

        private void OnUserStatsStored(UserStatsStored_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_eResult == EResult.k_EResultOK)
                {
                    // Steamworks stats and achievements stored
                    //App.LogDebug( "Steamworks user stats stored." );
                }
            }
        }

        private void OnUserAchievementStored(UserAchievementStored_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_nCurProgress == 0 && args.m_nMaxProgress == 0)
                {
					// Steamworks achievement completed
					foreach (var achievement in App.Info.Achievements)
					{
						if (achievement.ID == args.m_rgchAchievementName)
						{
							m_achievements.Add(achievement);
							break;
						}
					}
                }
                else
                {
                    // Steamworks achievement progressed
                }
            }
        }

		private void OnP2PSessionConnectFail(P2PSessionConnectFail_t args)
		{
			var steamID = args.m_steamIDRemote;
			if (m_interestedSenders.Contains(steamID))
			{
				var error = (EP2PSessionError)args.m_eP2PSessionError;
				m_senderErrors[steamID] = error;
			}
		}
    }
}
#endif
