using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemBrassKnuckles), "OnInitOrAmountChanged")]
internal static class Patch_BrassKnuckles_SizeCap
{
	[HarmonyPrefix]
	private unsafe static void Prefix(ItemBrassKnuckles __instance)
	{
		*(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 56) = float.MaxValue;
	}
}
