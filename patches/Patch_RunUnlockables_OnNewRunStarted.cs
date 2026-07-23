using Assets.Scripts.Inventory__Items__Pickups;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(RunUnlockables), "OnNewRunStarted")]
internal static class Patch_RunUnlockables_OnNewRunStarted
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		Patch_RunUnlockables_Init.RemoveItemCaps();
		ZaWarudoTracker.Reset();
		CactusOwnership.Reset();
		IdleJuiceOwnership.Reset();
		UpgradeStatTooltip.Text.Clear();
		SpaceNoodleState.Reset();
		ModGui.CheatsUsed = false;
		ModGui.GodMode = false;
		ModGui.FlyMode = false;
		ModGui.InstaKill = false;
		HotPatches.SetInstaKill(on: false);
		ModGui.FreezeEnemies = false;
		if (PerfBisect.Running)
		{
			PerfBisect.StartStop();
		}
		PatchModules.RestoreDefaults();
	}
}
