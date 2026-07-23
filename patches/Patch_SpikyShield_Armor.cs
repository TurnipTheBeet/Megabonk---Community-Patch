using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemSpikyShield), "OnInitOrAmountChanged")]
internal static class Patch_SpikyShield_Armor
{
	[HarmonyPrefix]
	private unsafe static void Prefix(ItemSpikyShield __instance)
	{
		*(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 48) = 0.5f;
	}
}
