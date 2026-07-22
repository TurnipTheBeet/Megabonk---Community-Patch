using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemBobLantern), "OnInitOrAmountChanged")]
internal static class Patch_BobLantern_FireRate
{
	[HarmonyPostfix]
	private unsafe static void Postfix(ItemBobLantern __instance)
	{
		*(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 60) /= 1.5f;
	}
}
