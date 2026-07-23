using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(GameManager), "StartPlaying")]
internal static class Patch_GameManager_StartPlaying_Chart
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		ModGui.ChartDisabled = false;
		Plugin.GuiInstance?.ResetChartCache();
		WeaponSfxVolume.ResetCaches();
	}
}
