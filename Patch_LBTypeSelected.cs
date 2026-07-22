using Assets.Scripts.Steam.LeaderboardsNew;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(LeaderboardUiNew), "OnLeaderboardTypeSelected")]
internal static class Patch_LBTypeSelected
{
	internal static int CurrentTab { get; private set; }

	[HarmonyPrefix]
	private static void Prefix(int index)
	{
		if (index < 2)
		{
			CurrentTab = index;
		}
	}

	[HarmonyPostfix]
	private static void Postfix(int index)
	{
		if (index >= 2 || index != 0)
		{
			return;
		}
		PersonalTab.IsActive = false;
		AllTimeTab.IsActive = false;
		AllTimeTab.HideCycle();
		LeaderboardInjector.ResetScroll();
		SteamLeaderboardNew lastLb = LeaderboardInjector.LastLb;
		if (lastLb == null)
		{
			return;
		}
		try
		{
			SteamLeaderboardNew.A_LeaderboardReady?.Invoke(lastLb);
		}
		catch
		{
		}
	}
}
