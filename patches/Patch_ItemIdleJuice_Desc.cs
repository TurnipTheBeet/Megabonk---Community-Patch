using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemIdleJuice), "GetDescription")]
internal static class Patch_ItemIdleJuice_Desc
{
	[HarmonyPostfix]
	private static void Postfix(ref string __result)
	{
		__result = MovementDesc.Fix(__result);
	}
}
