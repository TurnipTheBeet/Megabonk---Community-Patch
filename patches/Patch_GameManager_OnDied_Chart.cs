using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(GameManager), "OnDied")]
internal static class Patch_GameManager_OnDied_Chart
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		ModGui.ChartDisabled = true;
		Plugin.GuiInstance?.RestoreForDeath();
	}
}
