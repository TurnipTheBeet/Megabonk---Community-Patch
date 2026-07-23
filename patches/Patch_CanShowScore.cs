using Assets.Scripts.Steam;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(Leaderboards), "CanShowScore")]
internal static class Patch_CanShowScore
{
	[HarmonyPrefix]
	private static bool Prefix(int score, ref string s, ref bool __result)
	{
		s = score.ToString("N0");
		__result = true;
		return false;
	}
}
