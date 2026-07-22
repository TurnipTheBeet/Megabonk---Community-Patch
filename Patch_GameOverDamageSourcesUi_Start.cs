using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(GameOverDamageSourcesUi), "Start")]
internal static class Patch_GameOverDamageSourcesUi_Start
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		GameObject val = GameObject.Find("GameUI/GameUI/DeathScreen/StatsWindows/W_Damage/WindowLayers/Content/ScrollRect/ContentEntries");
		if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
		{
			Transform transform = val.transform;
			for (int num = transform.childCount - 1; num >= 3; num--)
			{
				Object.Destroy((UnityEngine.Object)(object)((Component)transform.GetChild(num)).gameObject);
			}
		}
	}
}
