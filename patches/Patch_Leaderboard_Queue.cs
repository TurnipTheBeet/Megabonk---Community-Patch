using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SteamLeaderboardsManagerNew), "QueueLeaderboardUpload")]
internal static class Patch_Leaderboard_Queue
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return false;
	}
}
