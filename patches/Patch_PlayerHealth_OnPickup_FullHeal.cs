using Assets.Scripts.Inventory__Items__Pickups;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(PlayerHealth), "OnPickup")]
internal static class Patch_PlayerHealth_OnPickup_FullHeal
{
	[HarmonyPrefix]
	private static bool Prefix(PlayerHealth __instance, Pickup pickup)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		if ((UnityEngine.Object)(object)pickup == (UnityEngine.Object)null || (int)pickup.ePickup != 2)
		{
			return true;
		}
		__instance.Heal((float)__instance.maxHp, false);
		return false;
	}
}
