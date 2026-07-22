using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(RunUnlockables), "Init")]
internal static class Patch_RunUnlockables_Init
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		RemoveItemCaps();
		CacheAndApplyStatBlacklist();
		CacheAndApplyShrineStatBlacklist();
	}

	internal static void RemoveItemCaps()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Invalid comparison between Unknown and I4
		var availableItems = RunUnlockables.availableItems;
		if (availableItems == null)
		{
			return;
		}
		var enumerator = availableItems.GetEnumerator();
		while (enumerator.MoveNext())
		{
			var current = enumerator.Current;
			if (current.Value == null)
			{
				continue;
			}
			var enumerator2 = current.Value.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				ItemData current2 = enumerator2.Current;
				if (Plugin.ActiveUncappedItems.Contains(current2.eItem))
				{
					current2.maxAmount = Plugin.GetItemCap(current2.eItem);
					current2.maxAmountPerRun = 9999;
				}
				if ((int)current2.eItem == 11)
				{
					int maxAmount = (current2.maxAmountPerRun = 6);
					current2.maxAmount = maxAmount;
				}
				if ((int)current2.eItem == 15)
				{
					int maxAmount = (current2.maxAmountPerRun = 6);
					current2.maxAmount = maxAmount;
				}
			}
		}
	}

	private static void CacheAndApplyStatBlacklist()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected I4, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		var upgradableStatsChaosAndGamble = EncounterUtility.upgradableStatsChaosAndGamble;
		if (upgradableStatsChaosAndGamble == null)
		{
			return;
		}
		if (Plugin.FullStatPool.Count == 0)
		{
			var enumerator = upgradableStatsChaosAndGamble.GetEnumerator();
			while (enumerator.MoveNext())
			{
				EStat current = enumerator.Current;
				Plugin.FullStatPool.Add((int)current);
			}
		}
		foreach (int blacklistedStat in Plugin.BlacklistedStats)
		{
			EStat val = (EStat)blacklistedStat;
			if (upgradableStatsChaosAndGamble.Contains(val))
			{
				upgradableStatsChaosAndGamble.Remove(val);
			}
		}
	}

	private static void CacheAndApplyShrineStatBlacklist()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected I4, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		var upgradableStatsShrines = EncounterUtility.upgradableStatsShrines;
		if (upgradableStatsShrines == null)
		{
			return;
		}
		if (Plugin.FullShrineStatPool.Count == 0)
		{
			var enumerator = upgradableStatsShrines.GetEnumerator();
			while (enumerator.MoveNext())
			{
				EStat current = enumerator.Current;
				Plugin.FullShrineStatPool.Add((int)current);
			}
		}
		foreach (int blacklistedShrineStat in Plugin.BlacklistedShrineStats)
		{
			EStat val = (EStat)blacklistedShrineStat;
			if (upgradableStatsShrines.Contains(val))
			{
				upgradableStatsShrines.Remove(val);
			}
		}
	}
}
