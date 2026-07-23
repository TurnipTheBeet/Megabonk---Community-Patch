using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InteractableMicrowave), "Start")]
internal static class Patch_Microwave_Start
{
	[HarmonyPostfix]
	private static void Postfix(InteractableMicrowave __instance)
	{
		MicrowaveIconHelper.ApplyMicrowaveColor(__instance, scaleDown: true);
	}
}
