using Assets.Scripts.Inventory__Items__Pickups.Chests;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InteractableChest), "Start")]
internal static class Patch_Chest_MinimapIcon
{
	[HarmonyPostfix]
	private static void Postfix(InteractableChest __instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		if ((int)__instance.chestType == 0)
		{
			color = new Color(0.55f, 0.28f, 0.08f);
		}
		else
		{
			if ((int)__instance.chestType != 2 && (int)__instance.chestType != 3)
			{
				return;
			}
			color = new Color(1f, 0.85f, 0.1f);
		}
		Transform icon = __instance.icon;
		if (!((UnityEngine.Object)(object)icon == (UnityEngine.Object)null))
		{
			IconColorHelper.ApplyColor(((Component)icon).gameObject, color);
			icon.localScale *= 0.5f;
		}
	}
}
