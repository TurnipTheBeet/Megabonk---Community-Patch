using System.Collections.Generic;
using Assets.Scripts.Steam;
using Assets.Scripts.Steam.LeaderboardsNew;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SteamLeaderboardNew), "OnDownloadResultsFriends")]
internal static class Patch_OnDownloadResultsFriends
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !PersonalTab.IsActive;
	}

	[HarmonyPostfix]
	private static void Postfix(SteamLeaderboardNew __instance)
	{
		if (!LeaderboardRelay.Enabled || PersonalTab.IsActive || AllTimeTab.IsActive)
		{
			return;
		}
		SteamLeaderboardNew leaderboardKillsAllTime = SteamLeaderboardsManagerNew.leaderboardKillsAllTime;
		SteamLeaderboardNew leaderboardKillsWeekly = SteamLeaderboardsManagerNew.leaderboardKillsWeekly;
		if (__instance != leaderboardKillsAllTime && __instance != leaderboardKillsWeekly)
		{
			return;
		}
		LeaderboardInjector.ServerEntry[] cachedEntries = LeaderboardInjector.CachedEntries;
		__instance.friendsEntries = ((cachedEntries != null && cachedEntries.Length != 0) ? LeaderboardInjector.BuildFriendsList(cachedEntries) : new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>());
		try
		{
			SteamLeaderboardNew.A_LeaderboardReady?.Invoke(__instance);
		}
		catch
		{
		}
	}
}
