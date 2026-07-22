using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemGlovesPower), "GetDamage")]
internal static class Patch_GlovesPower_Damage
{
	private const float DamageScale = 9f;

	[HarmonyPostfix]
	private static void Postfix(ref float __result)
	{
		__result *= 9f;
	}
}
