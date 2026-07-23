using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemSpicyMeatball), "OnInitOrAmountChanged")]
internal static class Patch_SpicyMeatball_Radius
{
	private const int MaxStacks = 6;

	[HarmonyPostfix]
	private unsafe static void Postfix(ItemSpicyMeatball __instance)
	{
		int amount = *(int*)(void*)(((Il2CppObjectBase)__instance).Pointer + 24);
		if (amount >= MaxStacks)
		{
			SizeCapHelper.RemoveFromPool((EItem)15);
		}
	}
}
