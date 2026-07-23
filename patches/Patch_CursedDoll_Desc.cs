using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemCursedDoll), "GetDescription")]
internal static class Patch_CursedDoll_Desc
{
	[HarmonyPostfix]
	private static void Postfix(ref string __result)
	{
		if (__result != null)
		{
			__result = __result.Replace("30%", "50%").Replace("30 %", "50%");
		}
	}
}
