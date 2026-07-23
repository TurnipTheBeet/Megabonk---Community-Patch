using Assets.Scripts.Game.Combat;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(CombatScaling), "GetStageHpMultiplier")]
internal static class Patch_CombatScaling_HpMultiplier
{
	[HarmonyPostfix]
	private static void Postfix(ref float __result)
	{
		__result = 2f * __result - 1f;
	}
}
