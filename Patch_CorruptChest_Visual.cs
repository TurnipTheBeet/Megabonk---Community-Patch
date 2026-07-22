using System;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ChestOpening), "SetChest")]
internal static class Patch_CorruptChest_Visual
{
	[HarmonyPrefix]
	private static void Prefix(ChestOpening __instance, EChest chestType)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		try
		{
			if ((int)chestType == 1)
			{
				__instance.meshEvil = __instance.meshNormal;
				__instance.matEvil = __instance.matNormal;
				if ((UnityEngine.Object)(object)__instance.fxCorrupted == (UnityEngine.Object)null)
				{
					__instance.fxCorrupted = __instance.fxFinal;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(15, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[CorruptChest] ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val);
		}
	}
}
