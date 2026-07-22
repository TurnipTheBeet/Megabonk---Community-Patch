using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemCactus), "OnInitOrAmountChanged")]
internal static class Patch_Cactus_OnInitOrAmountChanged
{
	[HarmonyPrefix]
	private unsafe static void Prefix(ItemCactus __instance)
	{
		*(int*)(void*)(((Il2CppObjectBase)__instance).Pointer + 52) = 3;
	}
}
