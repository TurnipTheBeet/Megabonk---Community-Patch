using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemGoldenShield), "OnPlayerTakeDamage")]
internal static class Patch_GoldenShield_NoKevinReduction
{
	[HarmonyPrefix]
	private static void Prefix(DamageContainer dc)
	{
		if (dc != null && dc.damageSource == ItemKevin.damageSource)
		{
			dc.damageSource = "";
		}
	}
}
