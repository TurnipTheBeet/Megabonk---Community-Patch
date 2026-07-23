using Assets.Scripts.Utility;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(MyTime), "OnNewStageStarted")]
internal static class Patch_BOMBUS_ResetOnNewStage
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		GiantBeeState.Reset();
	}
}
