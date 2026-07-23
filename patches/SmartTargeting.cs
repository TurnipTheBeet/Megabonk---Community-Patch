using Assets.Scripts.Actors.Enemies;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class SmartTargeting
{
	private const int AnyBoss = 54;

	private const float HP_WEIGHT = 4f;

	private const int MaxPicksPerFrame = 12;

	private static int _frameOfLastPick = -1;

	private static int _picksThisFrame = 0;

	private static ConfigEntry<bool> _enabled;

	internal static bool Enabled
	{
		get
		{
			return _enabled != null && _enabled.Value;
		}
		set
		{
			if (_enabled != null)
			{
				_enabled.Value = value;
			}
			PatchModules.ReevaluateAll();
		}
	}

	internal static bool TryTakePickBudget()
	{
		int frameCount = Time.frameCount;
		if (frameCount != _frameOfLastPick)
		{
			_frameOfLastPick = frameCount;
			_picksThisFrame = 0;
		}
		if (_picksThisFrame >= 12)
		{
			return false;
		}
		_picksThisFrame++;
		return true;
	}

	internal static void Init(ConfigFile cfg)
	{
		_enabled = cfg.Bind<bool>("Targeting", "PriorityTargeting", false, "Priority Targeting: replace auto-aim target selection with a scorer that prioritises bosses/elites, then the nearest enemy you can kill soonest. Toggle in-game with the Priority Targeting hotkey.");
	}

	internal static void Toggle()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		Enabled = !Enabled;
		string text = (Enabled ? "ON" : "OFF");
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(20, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[PriorityTargeting] ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
		}
		log.LogInfo(val);
		Toast.Show("Priority Targeting: " + text, Enabled ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.6f, 0.45f, 1f));
	}

	internal static Enemy Pick(Il2CppReferenceArray<Collider> colliders, int count, Vector3 pos, GameObject exceptObject)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		Enemy result = null;
		int num = int.MaxValue;
		float num2 = float.MaxValue;
		int num3 = (((UnityEngine.Object)(object)exceptObject != (UnityEngine.Object)null) ? ((Object)exceptObject).GetInstanceID() : 0);
		for (int i = 0; i < count; i++)
		{
			Collider val = ((Il2CppArrayBase<Collider>)(object)colliders)[i];
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null || (num3 != 0 && ((Object)((Component)val).gameObject).GetInstanceID() == num3))
			{
				continue;
			}
			Enemy component = ((Component)val).GetComponent<Enemy>();
			if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null || component.IsDead() || component.IsDeadOrDyingNextFrame())
			{
				continue;
			}
			int num4 = ((((int)component.enemyFlag & 0x36) == 0) ? (component.IsElite() ? 1 : 2) : 0);
			if (num4 <= num)
			{
				float num5 = Vector3.Distance(component.GetCenterPosition(), pos);
				float num6 = num5 + 4f * component.GetHpRatio();
				if (num4 < num || num6 < num2)
				{
					num = num4;
					num2 = num6;
					result = component;
				}
			}
		}
		return result;
	}
}
