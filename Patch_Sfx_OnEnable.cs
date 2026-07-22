using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(PlaySfxOnEnable), "OnEnable")]
internal static class Patch_Sfx_OnEnable
{
	[HarmonyPrefix]
	private static void Prefix(PlaySfxOnEnable __instance)
	{
		if ((UnityEngine.Object)(object)__instance.randomSfx == (UnityEngine.Object)null)
		{
			WeaponSfxVolume.ScaleItemSource(__instance.audioSource);
		}
	}
}
