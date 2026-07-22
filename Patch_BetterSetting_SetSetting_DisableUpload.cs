using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(BetterSetting), "SetSetting")]
internal static class Patch_BetterSetting_SetSetting_DisableUpload
{
	[HarmonyPostfix]
	private static void Postfix(BetterSetting __instance, string settingName)
	{
		if (!(settingName != "upload_score_to_leaderboard"))
		{
			TextMeshProUGUI t_disabledText = __instance.t_disabledText;
			if ((UnityEngine.Object)(object)t_disabledText != (UnityEngine.Object)null)
			{
				((TMP_Text)t_disabledText).text = "Disabled";
			}
			GameObject disabledOverlay = __instance.disabledOverlay;
			if ((UnityEngine.Object)(object)disabledOverlay != (UnityEngine.Object)null)
			{
				disabledOverlay.SetActive(true);
			}
		}
	}
}
