using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Assets.Scripts.Steam;
using Assets.Scripts.Steam.LeaderboardsNew;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Steamworks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class LeaderboardInjector
{
	internal class ServerEntry
	{
		[JsonPropertyName("score")]
		public int Score { get; set; }

		[JsonPropertyName("steamId")]
		public string SteamId { get; set; } = "";


		[JsonPropertyName("name")]
		public string Name { get; set; } = "";


		[JsonPropertyName("characterIndex")]
		public int CharacterIndex { get; set; }

		[JsonPropertyName("timestamp")]
		public long Timestamp { get; set; }

		[JsonPropertyName("mapIndex")]
		public int MapIndex { get; set; }

		[JsonPropertyName("runData")]
		public string RunData { get; set; } = "";
	}

	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.LbInject");

	private static volatile ServerEntry[] _cache = null;

	private static volatile bool _fetching = false;

	private static volatile SteamLeaderboardNew _pending = null;

	private static SteamLeaderboardNew _lastLb = null;

	private static int _offset = 0;

	internal static readonly Dictionary<ulong, string> NameCache = new Dictionary<ulong, string>();

	private static readonly Dictionary<ulong, Texture> _avatarCache = new Dictionary<ulong, Texture>();

	private const float RetryWindow = 12f;

	// RunData per visible slot, populated during Display() for tooltip access
	internal static string[] SlotRunData = System.Array.Empty<string>();

	private const float RetryStep = 0.75f;

	private static bool _avatarMissFlag;

	private static float _avatarRetryUntil;

	private static float _nextAvatarRetry;

	internal static ServerEntry[] CachedEntries => _cache;

	internal static ServerEntry[] WeeklyCachedEntries { get; private set; }

	internal static bool IsFetching => _fetching;

	internal static SteamLeaderboardNew LastLb => _lastLb;

	internal static int CurrentOffset => _offset;

	internal static SteamLeaderboardNew ActiveLb { get; set; } = null;


	internal static LeaderboardUiNew LbUi { get; set; } = null;


	internal static void RefreshOpenPanel()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		try
		{
			if ((UnityEngine.Object)(object)LbUi != (UnityEngine.Object)null && (UnityEngine.Object)(object)((Component)LbUi).gameObject != (UnityEngine.Object)null && ((Component)LbUi).gameObject.activeInHierarchy)
			{
				LbUi.Refresh();
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(18, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("RefreshOpenPanel: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val);
		}
	}

	internal static Texture GetAvatar(ulong steamId)
	{
		if (steamId == 0)
		{
			return null;
		}
		if (_avatarCache.TryGetValue(steamId, out var value) && (UnityEngine.Object)(object)value != (UnityEngine.Object)null)
		{
			return value;
		}
		Texture2D val = SteamUtility.LoadAvatar(steamId, 0);
		if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
		{
			_avatarCache[steamId] = (Texture)(object)val;
		}
		return (Texture)(object)val;
	}

	internal static void RequestAvatar(ulong steamId)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (steamId == 0)
		{
			return;
		}
		try
		{
			SteamFriends.RequestUserInformation(new CSteamID(steamId), false);
		}
		catch
		{
		}
	}

	internal static void NoteAvatarMiss(ulong steamId)
	{
		if (steamId != 0)
		{
			RequestAvatar(steamId);
			_avatarMissFlag = true;
			float unscaledTime = Time.unscaledTime;
			if (unscaledTime > _avatarRetryUntil)
			{
				_avatarRetryUntil = unscaledTime + 12f;
				_nextAvatarRetry = unscaledTime + 0.75f;
			}
		}
	}

	internal static void TickAvatarRetry()
	{
		float unscaledTime = Time.unscaledTime;
		if (!(unscaledTime > _avatarRetryUntil) && !(unscaledTime < _nextAvatarRetry))
		{
			_nextAvatarRetry = unscaledTime + 0.75f;
			_avatarMissFlag = false;
			RefreshOpenPanel();
			if (!_avatarMissFlag)
			{
				_avatarRetryUntil = 0f;
			}
		}
	}

	internal static void BeginFetch(SteamLeaderboardNew lb)
	{
		if (_fetching)
		{
			return;
		}
		_fetching = true;
		Task.Run<Task>((Func<Task>)async delegate
		{
			try
			{
				ServerEntry[] entries = Parse(await LeaderboardRelay.FetchEntries("alltime", 500, -1, "char"));
				ModGui.MainThread.Enqueue(delegate
				{
					//IL_001b: Unknown result type (might be due to invalid IL or missing references)
					//IL_0021: Expected O, but got Unknown
					_cache = entries;
					WeeklyCachedEntries = FilterToCurrentWeek(entries);
					_fetching = false;
					bool flag2 = default(bool);
					BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(37, 1, out flag2);
					if (flag2)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Fetch 'alltime' complete: ");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(entries.Length);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" entries");
					}
					Log.LogInfo(val2);
					SteamLeaderboardNew pending = _pending;
					_pending = null;
					if (pending != null && entries.Length != 0)
					{
						Replace(pending, entries);
					}
					RefreshOpenPanel();
				});
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				_fetching = false;
				bool flag = default(bool);
				BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(24, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Fetch 'alltime' error: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
				}
				Log.LogError(val);
			}
		});
	}

	internal static void ReplaceEntriesIfReady(SteamLeaderboardNew lb)
	{
		if (PersonalTab.IsActive || AllTimeTab.IsActive)
		{
			return;
		}
		SteamLeaderboardNew leaderboardKillsAllTime = SteamLeaderboardsManagerNew.leaderboardKillsAllTime;
		SteamLeaderboardNew leaderboardKillsWeekly = SteamLeaderboardsManagerNew.leaderboardKillsWeekly;
		if (lb != leaderboardKillsAllTime && lb != leaderboardKillsWeekly)
		{
			return;
		}
		if (_cache != null && _cache.Length != 0)
		{
			if (lb == leaderboardKillsWeekly)
			{
				WeeklyCachedEntries = FilterToCurrentWeek(_cache);
				Replace(lb, WeeklyCachedEntries);
			}
			else
			{
				Replace(lb, _cache);
			}
			return;
		}
		lb.globalEntries = new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>();
		lb.friendsEntries = new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>();
		_pending = lb;
		try
		{
			SteamLeaderboardNew.A_LeaderboardReady?.Invoke(lb);
		}
		catch
		{
		}
	}

	private static ServerEntry[] FilterToCurrentWeek(ServerEntry[] entries)
	{
		var now = DateTimeOffset.UtcNow;
		int daysSinceSunday = (int)now.DayOfWeek;
		var weekStart = new DateTimeOffset(now.Year, now.Month, now.Day, 20, 0, 0, TimeSpan.Zero).AddDays(-daysSinceSunday);
		if (now < weekStart) weekStart = weekStart.AddDays(-7);
		long weekStartUnix = weekStart.ToUnixTimeSeconds();
		var weekly = entries.Where(e => e.Timestamp >= weekStartUnix);
		var best = weekly
			.GroupBy(e => e.SteamId)
			.Select(g => g.OrderByDescending(e => e.Score).First())
			.OrderByDescending(e => e.Score)
			.ToArray();
		return best;
	}

	internal static void ScrollBy(int delta)
	{
		SteamLeaderboardNew val = ActiveLb ?? _lastLb;
		if (!PersonalTab.IsActive && !AllTimeTab.IsActive && val != null && _cache != null && _cache.Length != 0)
		{
			_offset = Math.Max(0, Math.Min(_offset + delta, Math.Max(0, _cache.Length - 9)));
			Replace(val, _cache);
		}
	}

	internal static void ResetScroll()
	{
		_offset = 0;
	}

	private unsafe static void Replace(SteamLeaderboardNew lb, ServerEntry[] allEntries)
	{
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Expected O, but got Unknown
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Expected O, but got Unknown
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Expected O, but got Unknown
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		_lastLb = lb;
		_offset = Math.Max(0, Math.Min(_offset, Math.Max(0, allEntries.Length - 9)));
		int num = Math.Min(9, allEntries.Length - _offset);
		ServerEntry[] array = new ServerEntry[num];
		Array.Copy(allEntries, _offset, array, 0, num);
		HashSet<ulong> hashSet = new HashSet<ulong>();
		ServerEntry[] array2 = array;
		foreach (ServerEntry serverEntry in array2)
		{
			if (ulong.TryParse(serverEntry.SteamId, out var result))
			{
				hashSet.Add(result);
			}
		}
		foreach (ServerEntry serverEntry2 in allEntries)
		{
			if (ulong.TryParse(serverEntry2.SteamId, out var result2) && result2 != 0)
			{
				if (!string.IsNullOrEmpty(serverEntry2.Name))
				{
					NameCache[result2] = serverEntry2.Name;
				}
				SteamFriends.RequestUserInformation(new CSteamID(result2), !hashSet.Contains(result2));
			}
		}
		lb.globalEntries = BuildList(array, _offset);
		lb.friendsEntries = BuildFriendsList(allEntries);
		try
		{
			*(long*)(void*)(((Il2CppObjectBase)lb).Pointer + 64) = 0L;
		}
		catch
		{
		}
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(61, 4, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Replaced global(window ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_offset);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("+");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(allEntries.Length);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(") on '");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(lb.lbName);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("', invoking A_LeaderboardReady");
		}
		Log.LogInfo(val);
		try
		{
			var a_LeaderboardReady = SteamLeaderboardNew.A_LeaderboardReady;
			if (a_LeaderboardReady == null)
			{
				BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("A_LeaderboardReady is null for '");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(lb.lbName);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("'");
				}
				Log.LogWarning(val2);
				return;
			}
			a_LeaderboardReady.Invoke(lb);
			val = new BepInExInfoLogInterpolatedStringHandler(36, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("A_LeaderboardReady invoked OK for '");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(lb.lbName);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("'");
			}
			Log.LogInfo(val);
		}
		catch (Exception ex)
		{
			BepInExErrorLogInterpolatedStringHandler val3 = new BepInExErrorLogInterpolatedStringHandler(28, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("A_LeaderboardReady threw ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.GetType().Name);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(": ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("\n");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.StackTrace);
			}
			Log.LogError(val3);
		}
	}

	internal static Il2CppSystem.Collections.Generic.List<LeaderboardEntry> BuildList(ServerEntry[] entries, int rankOffset = 0)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		Il2CppSystem.Collections.Generic.List<LeaderboardEntry> val = new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>();
		if (entries == null)
		{
			return val;
		}
		for (int i = 0; i < entries.Length; i++)
		{
			ServerEntry serverEntry = entries[i];
			ulong result;
			ulong num = (ulong.TryParse(serverEntry.SteamId, out result) ? result : 0);
			int[] array = new int[64];
			array[1] = serverEntry.CharacterIndex;
			LeaderboardEntry_t val2 = default(LeaderboardEntry_t);
			val2.m_steamIDUser = new CSteamID(num);
			val2.m_nGlobalRank = rankOffset + i + 1;
			val2.m_nScore = serverEntry.Score;
			val2.m_cDetails = array.Length;
			LeaderboardEntry_t val3 = val2;
			val.Add(new LeaderboardEntry(val3, (Il2CppStructArray<int>)(array)));
		}
		return val;
	}

	private static HashSet<ulong> GetSteamFriendIds()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		HashSet<ulong> hashSet = new HashSet<ulong>();
		try
		{
			hashSet.Add(SteamUser.GetSteamID().m_SteamID);
			int friendCount = SteamFriends.GetFriendCount((EFriendFlags)4);
			for (int i = 0; i < friendCount; i++)
			{
				hashSet.Add(SteamFriends.GetFriendByIndex(i, (EFriendFlags)4).m_SteamID);
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(19, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("GetSteamFriendIds: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val);
		}
		return hashSet;
	}

	internal static Il2CppSystem.Collections.Generic.List<LeaderboardEntry> BuildFriendsList(ServerEntry[] entries)
	{
		HashSet<ulong> steamFriendIds = GetSteamFriendIds();
		List<ServerEntry> list = new List<ServerEntry>();
		foreach (ServerEntry serverEntry in entries)
		{
			if (ulong.TryParse(serverEntry.SteamId, out var result) && steamFriendIds.Contains(result))
			{
				list.Add(serverEntry);
			}
		}
		return BuildList(list.ToArray());
	}

	private static ServerEntry[] Parse(string json)
	{
		if (string.IsNullOrEmpty(json) || json == "[]")
		{
			return new ServerEntry[0];
		}
		try
		{
			return JsonSerializer.Deserialize<ServerEntry[]>(json) ?? new ServerEntry[0];
		}
		catch
		{
			return new ServerEntry[0];
		}
	}
}
