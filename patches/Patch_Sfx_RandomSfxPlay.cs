using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(RandomSfx), "Play")]
internal static class Patch_Sfx_RandomSfxPlay
{
	[HarmonyPrefix]
	private static void Prefix(RandomSfx __instance, ref float volumeMultiplier)
	{
		float num = WeaponSfxVolume.FactorFor(__instance);
		if (num != 1f)
		{
			volumeMultiplier *= num;
		}
	}
}
