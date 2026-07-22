using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemCreditCardGreen), "GetDescription")]
internal static class Patch_CreditCardGreen_Desc
{
	[HarmonyPostfix]
	private static void Postfix(ref string __result)
	{
		if (__result != null)
		{
			__result = __result.Replace("10%", "2%").Replace("10 %", "2%");
		}
	}
}
