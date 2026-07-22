using System;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemInventory), "AddItem", new Type[] { typeof(EItem) })]
internal static class Patch_GoldenRing_Add_Single
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
			GameManager instance = GameManager.Instance;
			PlayerInventory val = ((instance != null) ? instance.GetPlayerInventory() : null);
			if (val != null)
			{
				val.banishes += 1;
			}
		}
		catch
		{
		}
	}
}
