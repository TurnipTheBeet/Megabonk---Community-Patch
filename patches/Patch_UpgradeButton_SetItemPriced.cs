using System;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(UpgradeButton), "SetItemPriced")]
internal static class Patch_UpgradeButton_SetItemPriced
{
	[HarmonyPostfix]
	private static void Postfix(UpgradeButton __instance, ItemData itemData)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		try
		{
			UpgradeStatTooltip.Clear(__instance);
			if (!((UnityEngine.Object)(object)itemData == (UnityEngine.Object)null))
			{
				UpgradeStatTooltip.Attach(__instance, UpgradeStatTooltip.BuildItem(itemData));
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(36, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[UpgradeStatTooltip] SetItemPriced: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			log.LogWarning(val);
		}
	}
}
