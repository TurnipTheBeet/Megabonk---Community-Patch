using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(EnemyData), "GetName")]
internal static class Patch_EnemyData_GetName_BOMBUS
{
	[HarmonyPostfix]
	private static void Postfix(EnemyData __instance, ref string __result)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if (GiantBeeState.PhaseActive && (int)__instance.enemyName == 24)
		{
			__result = "BOMBUS";
		}
	}
}
