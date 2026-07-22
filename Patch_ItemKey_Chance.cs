using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemKey), "OnInitOrAmountChanged")]
internal static class Patch_ItemKey_Chance
{
	[HarmonyPrefix]
	private unsafe static void Prefix(ItemKey __instance)
	{
		*(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 48) = 0.15f;
	}
}
