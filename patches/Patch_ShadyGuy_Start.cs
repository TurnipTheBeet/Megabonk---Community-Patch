using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InteractableShadyGuy), "Start")]
internal static class Patch_ShadyGuy_Start
{
	[HarmonyPostfix]
	private static void Postfix(InteractableShadyGuy __instance)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		Transform parent = ((Component)__instance).transform.parent;
		if ((UnityEngine.Object)(object)parent == (UnityEngine.Object)null)
		{
			return;
		}
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (((Component)child).gameObject.layer == 14)
			{
				IconColorHelper.ApplyColor(((Component)child).gameObject, IconColorHelper.ShadyGuyRarityColor(__instance.rarity));
				child.localScale *= 0.5f;
				break;
			}
		}
	}
}
