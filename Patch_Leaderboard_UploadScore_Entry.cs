using System;
using Assets.Scripts.Managers;
using Assets.Scripts.Steam;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(Leaderboards), "UploadScore")]
internal static class Patch_Leaderboard_UploadScore_Entry
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.LbUpload");

	private static int _lastScore;

	private static float _lastTime = -999f;

	[HarmonyPrefix]
	private unsafe static bool Prefix(int score)
	{
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			if (score == _lastScore && Time.realtimeSinceStartup - _lastTime < 5f)
			{
				return false;
			}
			_lastScore = score;
			_lastTime = Time.realtimeSinceStartup;
			int num = 0;
			PlayerMovement instance = PlayerMovement.Instance;
			if ((UnityEngine.Object)(object)instance != (UnityEngine.Object)null)
			{
				num = *(int*)(void*)(((Il2CppObjectBase)instance).Pointer + 440);
			}
			int mapIndex = 0;
			MapData currentMap = MapController.currentMap;
			if (currentMap != null)
			{
				mapIndex = (int)currentMap.eMap;
			}
			if (PersonalTab.PersonalBests.TryGetValue(num, out var value) && score <= value)
			{
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(73, 3, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Leaderboard] Score ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(score);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" not a new record for char ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" (best=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(value);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("), skipping upload.");
				}
				Log.LogInfo(val);
				return false;
			}
			LeaderboardRelay.SendBothBoards(score, num, mapIndex);
			PersonalTab.PersonalBests[num] = score;
		}
		catch (Exception ex)
		{
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(16, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Capture failed: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val2);
		}
		return false;
	}
}
