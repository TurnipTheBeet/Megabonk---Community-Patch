using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SteamLeaderboardsManagerNew), "UploadLeaderboardScore")]
internal static class Patch_Leaderboard_UploadDirect
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return false;
	}
}
