using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InventoryItemPrefabUI), "SetItem", new Type[] { typeof(UnlockableBase) })]
internal static class Patch_InventorySlot_SetItem
{
	[HarmonyPostfix]
	private static void Postfix(InventoryItemPrefabUI __instance, UnlockableBase item)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		try
		{
			ToolTipObject toolTipObject = __instance.toolTipObject;
			if ((UnityEngine.Object)(object)toolTipObject == (UnityEngine.Object)null || (UnityEngine.Object)(object)item == (UnityEngine.Object)null)
			{
				return;
			}
			WeaponData val = ((Il2CppObjectBase)item).TryCast<WeaponData>();
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				string text = UpgradeStatTooltip.BuildWeapon(val);
				if (!string.IsNullOrWhiteSpace(text))
				{
					toolTipObject.text = (string.IsNullOrWhiteSpace(toolTipObject.text) ? text : (toolTipObject.text + "\n\n" + text));
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(30, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[UpgradeStatTooltip] InvSlot: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log.LogWarning(val2);
		}
	}
}
