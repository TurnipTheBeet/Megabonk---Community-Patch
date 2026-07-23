using System;
using HarmonyLib;
using UnityEngine.Localization;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(LocalizedString), "GetLocalizedString", new Type[] { })]
internal static class Patch_IdleJuice_Rename
{
	[HarmonyPostfix]
	private static void Postfix(ref string __result)
	{
		if (__result != null && __result.Length >= 10 && __result.Contains("Idle Juice"))
		{
			__result = __result.Replace("Idle Juice", "Turbo Juice");
		}
	}
}
