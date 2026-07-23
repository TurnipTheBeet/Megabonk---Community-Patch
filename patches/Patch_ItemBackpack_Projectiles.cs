using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemBackpack), "OnInitOrAmountChanged")]
internal static class Patch_ItemBackpack_Projectiles
{
	[HarmonyPrefix]
	private unsafe static void Prefix(ItemBackpack __instance)
	{
		*(int*)(void*)(((Il2CppObjectBase)__instance).Pointer + 24) *= 2;
	}

	[HarmonyPostfix]
	private unsafe static void Postfix(ItemBackpack __instance)
	{
		*(int*)(void*)(((Il2CppObjectBase)__instance).Pointer + 24) /= 2;
	}
}
