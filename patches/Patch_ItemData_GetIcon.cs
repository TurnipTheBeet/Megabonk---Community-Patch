using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemData), "GetIcon")]
internal static class Patch_ItemData_GetIcon
{
	[HarmonyPostfix]
	private static void Postfix(ItemData __instance, ref Texture __result)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (!((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null))
		{
			Texture2D recolored = IconRecolor.GetRecolored(__instance.eItem);
			if ((UnityEngine.Object)(object)recolored != (UnityEngine.Object)null)
			{
				__result = (Texture)(object)recolored;
			}
		}
	}
}
