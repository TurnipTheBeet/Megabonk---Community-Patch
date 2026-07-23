using Assets.Scripts.Steam.LeaderboardsNew;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SteamLeaderboardNew), "OnDownloadResultsGlobal")]
internal static class Patch_OnDownloadResultsGlobal
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !PersonalTab.IsActive;
	}

	[HarmonyPostfix]
	private static void Postfix(SteamLeaderboardNew __instance)
	{
		if (LeaderboardRelay.Enabled && !PersonalTab.IsActive)
		{
			LeaderboardInjector.ReplaceEntriesIfReady(__instance);
		}
	}
}
