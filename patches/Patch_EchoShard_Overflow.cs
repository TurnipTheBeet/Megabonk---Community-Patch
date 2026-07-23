using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemEchoShard), "GetExtraShards")]
internal static class Patch_EchoShard_Overflow
{
	[HarmonyPostfix]
	private static void Postfix(ItemEchoShard __instance, ref int __result)
	{
		float chance = __instance.chance;
		if (!(chance <= 1f))
		{
			int num = (int)chance;
			float num2 = chance - (float)num;
			__result = num + ((Random.value < num2) ? 1 : 0);
		}
	}
}
