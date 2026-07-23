using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(CurrentSettings), "BetterUpdateCfSettings")]
internal static class Patch_CurrentSettings_BetterUpdateCfSettings_BlockUpload
{
	[HarmonyPrefix]
	private static bool Prefix(string settingName)
	{
		return settingName != "upload_score_to_leaderboard";
	}
}
