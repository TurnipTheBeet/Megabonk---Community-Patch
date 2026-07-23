using System;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemInventory), "RemoveItem", new Type[]
{
	typeof(EItem),
	typeof(bool)
})]
internal static class Patch_GoldenRing_Remove
{
	[HarmonyPostfix]
	private static void Postfix(EItem eItem)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		if ((int)eItem != 79)
		{
			return;
		}
		try
		{
			PlayerInventory playerInventory = MapController.GetPlayerInventory((CharacterData)null);
			if (playerInventory != null)
			{
				playerInventory.banishes = Math.Max(0, playerInventory.banishes - 1);
			}
		}
		catch
		{
		}
	}
}
