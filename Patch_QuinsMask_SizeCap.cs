using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemQuinsMask), "OnInitOrAmountChanged")]
internal static class Patch_QuinsMask_SizeCap
{
	[HarmonyPostfix]
	private unsafe static void Postfix(ItemQuinsMask __instance)
	{
		float num = *(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 64);
		float num2 = *(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 60);
		if (num >= num2)
		{
			SizeCapHelper.RemoveFromPool((EItem)80);
		}
	}
}
