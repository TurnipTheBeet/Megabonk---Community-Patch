using System;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Steam;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InteractablePortalFinal), "Interact")]
internal static class Patch_FinalPortal_UploadScore
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.FinalPortal");

	[HarmonyPostfix]
	private static void Postfix(bool __result)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		if (!__result)
		{
			return;
		}
		bool flag = default(bool);
		try
		{
			int stat = RunStats.GetStat((EMyStat)0);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(72, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[FinalPortal] Run completed via final portal — uploading score (kills=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(stat);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(").");
			}
			Log.LogInfo(val);
			Leaderboards.UploadScore(stat);
		}
		catch (Exception ex)
		{
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(35, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[FinalPortal] Score upload failed: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			Log.LogError(val2);
		}
	}
}
