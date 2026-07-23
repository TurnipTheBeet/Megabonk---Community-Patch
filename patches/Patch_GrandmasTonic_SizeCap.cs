using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemGrandmasSecretTonic), "OnInitOrAmountChanged")]
internal static class Patch_GrandmasTonic_SizeCap
{
	private const int MaxStacks = 6;

	[HarmonyPostfix]
	private unsafe static void Postfix(ItemGrandmasSecretTonic __instance)
	{
		int amount = *(int*)(void*)(((Il2CppObjectBase)__instance).Pointer + 24);
		if (amount >= MaxStacks)
		{
			SizeCapHelper.RemoveFromPool((EItem)11);
		}
	}
}