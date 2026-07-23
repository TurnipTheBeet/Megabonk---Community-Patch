using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InteractableMicrowave), "set_rarity")]
internal static class Patch_Microwave_SetRarity
{
	[HarmonyPostfix]
	private static void Postfix(InteractableMicrowave __instance)
	{
		MicrowaveIconHelper.ApplyMicrowaveColor(__instance);
	}
}
