using Assets.Scripts.Utility;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(MyTime), "Update")]
internal static class Patch_MyTime_Update_GiantBee
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		GiantBeeState.Tick();
	}
}
