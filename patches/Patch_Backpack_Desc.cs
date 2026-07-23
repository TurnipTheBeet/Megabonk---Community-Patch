using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemBase), "GetDescription")]
internal static class Patch_Backpack_Desc
{
	[HarmonyPostfix]
	private static void Postfix(ItemBase __instance, ref string __result)
	{
		if (__result != null && __instance != null && ((Il2CppObjectBase)__instance).TryCast<ItemBackpack>() != null)
		{
			__result = __result.Replace("+1 Projectile Count", "+2 Projectile Count").Replace("+1 projectile count", "+2 projectile count");
		}
	}
}
