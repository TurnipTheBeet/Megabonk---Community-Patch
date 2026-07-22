using System;
using Assets.Scripts._Data;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(UpgradeButton), "SetUpgrade")]
internal static class Patch_UpgradeButton_SetUpgrade
{
	[HarmonyPostfix]
	private static void Postfix(UpgradeButton __instance, IUpgradable upgradable)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		try
		{
			UpgradeStatTooltip.Clear(__instance);
			if (upgradable != null)
			{
				WeaponData val = ((Il2CppObjectBase)upgradable).TryCast<WeaponData>();
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					UpgradeStatTooltip.Attach(__instance, UpgradeStatTooltip.BuildWeapon(val));
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[UpgradeStatTooltip] SetUpgrade: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log.LogWarning(val2);
		}
	}
}
