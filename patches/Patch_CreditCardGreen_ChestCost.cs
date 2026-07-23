using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemCreditCardGreen), "OnInitOrAmountChanged")]
internal static class Patch_CreditCardGreen_ChestCost
{
	[HarmonyPostfix]
	private static void Postfix(ItemCreditCardGreen __instance)
	{
		__instance.chestPriceIncreasePerAmount = 0f;
	}
}
