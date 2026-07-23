using System;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.HUD;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.InGame.Rewards.Effects;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InteractableShrineGreed), "Interact")]
internal static class Patch_ShrineGreed_Stats
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.ShrineGreed");

	private static bool _wasNotDone;

	[HarmonyPrefix]
	private static void Prefix(InteractableShrineGreed __instance)
	{
		_wasNotDone = !__instance.done;
	}

	[HarmonyPostfix]
	private static void Postfix(InteractableShrineGreed __instance)
	{
		if (_wasNotDone && __instance.done)
		{
			Apply((EStat)32, 0.05f, (EStatModifyType)0);
			Apply((EStat)30, 0.05f, (EStatModifyType)2);
		}
	}

	private static void Apply(EStat stat, float amount, EStatModifyType modType = (EStatModifyType)2)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			StatModifier val = new StatModifier();
			val.stat = stat;
			val.modifyType = modType;
			val.modification = amount;
			EffectStat val2 = new EffectStat();
			val2.effectType = (EEncounterEffect)0;
			val2.statModifier = val;
			val2.permanent = true;
			val2.ApplyEffect();
			try
			{
				UiManager instance = UiManager.Instance;
				if (instance != null)
				{
					ScoreUi scoreUi = instance.scoreUi;
					if (scoreUi != null)
					{
						scoreUi.AddScore(val, true, true, 1f);
					}
				}
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val3 = new BepInExErrorLogInterpolatedStringHandler(14, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Apply ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<EStat>(stat);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" threw: ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<Exception>(ex);
			}
			Log.LogError(val3);
		}
	}
}
