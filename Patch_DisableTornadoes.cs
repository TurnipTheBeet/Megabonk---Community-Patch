using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(EffectManager), "SpawnTornadoes")]
internal static class Patch_DisableTornadoes
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return false;
	}
}
