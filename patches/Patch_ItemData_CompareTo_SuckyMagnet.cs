using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemData), "CompareTo", new Type[] { typeof(UnlockableBase) })]
internal static class Patch_ItemData_CompareTo_SuckyMagnet
{
	private static readonly HashSet<EItem> SortsFirst = new HashSet<EItem>
	{
		(EItem)33,
		(EItem)40,
		(EItem)32,
		(EItem)55,
		(EItem)56,
		(EItem)19,
		(EItem)38,
		(EItem)82,
		(EItem)7,
		(EItem)0
	};

	private static readonly HashSet<EItem> SortsLast = new HashSet<EItem>
	{
		(EItem)58,
		(EItem)1,
		(EItem)2,
		(EItem)43,
		(EItem)72,
		(EItem)8,
		(EItem)59,
		(EItem)4,
		(EItem)42,
		(EItem)23,
		(EItem)74
	};

	[HarmonyPrefix]
	private static bool Prefix(ItemData __instance, UnlockableBase other, ref int __result)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		ItemData val = (ItemData)(object)((other is ItemData) ? other : null);
		if (val == null)
		{
			return true;
		}
		if (__instance.rarity != val.rarity)
		{
			return true;
		}
		bool flag = SortsFirst.Contains(__instance.eItem);
		bool flag2 = SortsFirst.Contains(val.eItem);
		bool flag3 = SortsLast.Contains(__instance.eItem);
		bool flag4 = SortsLast.Contains(val.eItem);
		if (flag && !flag2)
		{
			__result = -1;
			return false;
		}
		if (flag2 && !flag)
		{
			__result = 1;
			return false;
		}
		if (flag3 && !flag4)
		{
			__result = 1;
			return false;
		}
		if (flag4 && !flag3)
		{
			__result = -1;
			return false;
		}
		if (flag3 && flag4)
		{
			__result = 0;
			return false;
		}
		return true;
	}
}
