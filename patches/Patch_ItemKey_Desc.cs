using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemKey), "GetDescription")]
internal static class Patch_ItemKey_Desc
{
	[HarmonyPostfix]
	private static void Postfix(ref string __result)
	{
		if (__result != null)
		{
			__result = __result.Replace("10%", "15%");
		}
	}
}
