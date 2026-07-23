using System;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemInventory), "AddItem", new Type[] { typeof(EItem) })]
internal static class Patch_IdleJuice_Ownership_Add
{
	[HarmonyPostfix]
	private static void Postfix(EItem eItem)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		if ((int)eItem == 56)
		{
			IdleJuiceOwnership.Add();
		}
	}
}
