using HarmonyLib;
using Steamworks;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SteamUserStats), "UploadLeaderboardScore")]
internal static class Patch_Leaderboard_SteamRaw
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return false;
	}
}
