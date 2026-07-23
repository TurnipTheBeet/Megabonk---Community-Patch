using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemCactus), "OnTakeDamage")]
internal static class Patch_Cactus_OnTakeDamage
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		CactusOwnership.Depth++;
	}

	[HarmonyPostfix]
	private static void Postfix()
	{
		CactusOwnership.Depth--;
	}
}
