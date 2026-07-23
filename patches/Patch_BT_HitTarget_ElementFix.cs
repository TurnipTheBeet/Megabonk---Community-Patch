using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ProjectileBluetooth), "HitTarget")]
internal static class Patch_BT_HitTarget_ElementFix
{
	internal static bool Active;

	[HarmonyPrefix]
	private static void Prefix()
	{
		HotPatches.SetBTFix(on: true);
		Active = true;
	}

	[HarmonyFinalizer]
	private static void Finalizer()
	{
		Active = false;
	}
}
