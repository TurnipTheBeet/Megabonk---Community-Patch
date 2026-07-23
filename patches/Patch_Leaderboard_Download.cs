using Assets.Scripts.Steam.LeaderboardsNew;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SteamLeaderboardsManagerNew), "DownloadLeaderboardEntries")]
internal static class Patch_Leaderboard_Download
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.LbDownload");

	[HarmonyPrefix]
	private static bool Prefix(string lbName)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		if (!LeaderboardRelay.Enabled)
		{
			return true;
		}
		SteamLeaderboardNew val = FindBoard(lbName);
		if (val != null)
		{
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(42, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Starting fetch for board '");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(val.lbName);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("' (requested '");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(lbName);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("')");
			}
			Log.LogInfo(val2);
			LeaderboardInjector.BeginFetch(val);
		}
		return true;
	}

	internal static SteamLeaderboardNew FindBoard(string lbName)
	{
		SteamLeaderboardNew leaderboardKillsAllTime = SteamLeaderboardsManagerNew.leaderboardKillsAllTime;
		SteamLeaderboardNew leaderboardKillsWeekly = SteamLeaderboardsManagerNew.leaderboardKillsWeekly;
		if (leaderboardKillsAllTime != null && (leaderboardKillsAllTime.lbName == lbName || leaderboardKillsAllTime.lbNameFriends == lbName))
		{
			return leaderboardKillsAllTime;
		}
		if (leaderboardKillsWeekly != null && (leaderboardKillsWeekly.lbName == lbName || leaderboardKillsWeekly.lbNameFriends == lbName))
		{
			return leaderboardKillsWeekly;
		}
		return null;
	}
}
