using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class PerfBisect
{
	private struct Phase
	{
		internal PatchModules.Module Mod;

		internal bool AllOff;

		internal float Fps;

		internal long Throws;

		internal bool IsBaseline => Mod == null && !AllOff;
	}

	private const float SettleSec = 0.5f;

	private const float WindowSec = 1.5f;

	internal static string Status = "";

	internal static string Result = "";

	private static readonly List<Phase> _plan = new List<Phase>();

	private static readonly Dictionary<PatchModules.Module, bool> _origMaster = new Dictionary<PatchModules.Module, bool>();

	private static readonly List<PatchModules.Module> _hot = new List<PatchModules.Module>();

	private static int _idx;

	private static float _phaseStart;

	private static float _accumTime;

	private static int _accumFrames;

	private static long _throwsAtStart;

	private static bool _accumStarted;

	internal static bool Running { get; private set; }

	private static float Now => Time.realtimeSinceStartup;

	internal static void StartStop()
	{
		if (Running)
		{
			Abort("cancelled");
			return;
		}
		_hot.Clear();
		_origMaster.Clear();
		foreach (PatchModules.Module item in PatchModules.All)
		{
			if (item.MasterEnabled)
			{
				_hot.Add(item);
				_origMaster[item] = item.MasterEnabled;
			}
		}
		if (_hot.Count == 0)
		{
			Result = "Auto-bisect: no modules are ON to test.";
			return;
		}
		_plan.Clear();
		_plan.Add(default(Phase));
		foreach (PatchModules.Module item2 in _hot)
		{
			_plan.Add(new Phase
			{
				Mod = item2
			});
			_plan.Add(default(Phase));
		}
		_plan.Add(new Phase
		{
			AllOff = true
		});
		_plan.Add(default(Phase));
		Result = "";
		_idx = 0;
		Running = true;
		BeginPhase();
	}

	private static void BeginPhase()
	{
		Phase phase = _plan[_idx];
		foreach (PatchModules.Module item in _hot)
		{
			if (!item.MasterEnabled)
			{
				PatchModules.SetMaster(item, on: true);
			}
		}
		if (phase.AllOff)
		{
			foreach (PatchModules.Module item2 in _hot)
			{
				PatchModules.SetMaster(item2, on: false);
			}
		}
		else if (phase.Mod != null)
		{
			PatchModules.SetMaster(phase.Mod, on: false);
		}
		_phaseStart = Now;
		_accumStarted = false;
		int value = _hot.Count + 1;
		int value2 = (_idx + 1) / 2;
		int value3 = _idx / 2;
		float num = (float)(_plan.Count - _idx) * 2f;
		string value4 = ((num >= 60f) ? $"~{num / 60f:0.0}m left" : $"~{num:0}s left");
		Status = (phase.AllOff ? $"[{value}/{value}] ALL modules off… ({value4})" : (phase.IsBaseline ? $"[{value3}/{value}] baseline… ({value4})" : $"[{value2}/{value}] {phase.Mod.Name} off… ({value4})"));
	}

	internal static void Tick()
	{
		if (!Running)
		{
			return;
		}
		if (!_accumStarted)
		{
			if (!(Now - _phaseStart < 0.5f))
			{
				_accumStarted = true;
				_accumTime = 0f;
				_accumFrames = 0;
				_throwsAtStart = HotErr.Total;
			}
			return;
		}
		_accumTime += Time.unscaledDeltaTime;
		_accumFrames++;
		if (!(_accumTime < 1.5f))
		{
			Phase value = _plan[_idx];
			value.Fps = (float)_accumFrames / Mathf.Max(_accumTime, 0.0001f);
			value.Throws = (long)((float)(HotErr.Total - _throwsAtStart) / Mathf.Max(_accumTime, 0.0001f));
			_plan[_idx] = value;
			_idx++;
			if (_idx >= _plan.Count)
			{
				Finish();
			}
			else
			{
				BeginPhase();
			}
		}
	}

	private static float LocalBaseline(int i)
	{
		float num = -1f;
		float num2 = -1f;
		for (int num3 = i - 1; num3 >= 0; num3--)
		{
			if (_plan[num3].IsBaseline)
			{
				num = _plan[num3].Fps;
				break;
			}
		}
		for (int j = i + 1; j < _plan.Count; j++)
		{
			if (_plan[j].IsBaseline)
			{
				num2 = _plan[j].Fps;
				break;
			}
		}
		if (num < 0f)
		{
			return num2;
		}
		if (num2 < 0f)
		{
			return num;
		}
		return (num + num2) * 0.5f;
	}

	private static void Finish()
	{
		//IL_066c: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<PatchModules.Module, bool> item2 in _origMaster)
		{
			PatchModules.SetMaster(item2.Key, item2.Value);
		}
		Running = false;
		Status = "";
		float num = -1f;
		float num2 = -1f;
		for (int i = 0; i < _plan.Count; i++)
		{
			if (_plan[i].IsBaseline)
			{
				if (num < 0f)
				{
					num = _plan[i].Fps;
				}
				num2 = _plan[i].Fps;
			}
		}
		List<(string, float, float, long, bool)> list = new List<(string, float, float, long, bool)>();
		float num3 = 0f;
		float value = 0f;
		bool flag = false;
		long num4 = 0L;
		for (int j = 0; j < _plan.Count; j++)
		{
			Phase phase = _plan[j];
			if (!phase.IsBaseline)
			{
				float num5 = LocalBaseline(j);
				float num6 = phase.Fps - num5;
				string item = (phase.AllOff ? "ALL modules OFF" : phase.Mod.Name);
				list.Add((item, phase.Fps, num6, phase.Throws, phase.AllOff));
				if (phase.Throws > num4)
				{
					num4 = phase.Throws;
				}
				if (phase.AllOff)
				{
					num3 = num6;
					value = phase.Fps;
					flag = true;
				}
			}
		}
		List<(string, float, float, long, bool)> list2 = list.FindAll(((string label, float fps, float gain, long throws, bool allOff) r) => !r.allOff);
		list2.Sort(((string label, float fps, float gain, long throws, bool allOff) x, (string label, float fps, float gain, long throws, bool allOff) y) => y.gain.CompareTo(x.gain));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("=== AUTO-BISECT (drift-cancelled) ===");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 3, stringBuilder2);
		handler.AppendLiteral("baseline drift: ");
		handler.AppendFormatted(num, "F1");
		handler.AppendLiteral(" -> ");
		handler.AppendFormatted(num2, "F1");
		handler.AppendLiteral(" FPS (avg ");
		handler.AppendFormatted((num + num2) * 0.5f, "F1");
		handler.AppendLiteral(")");
		stringBuilder3.AppendLine(ref handler);
		foreach (var item3 in list2)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(32, 5, stringBuilder2);
			handler.AppendLiteral("  ");
			handler.AppendFormatted(item3.Item1);
			handler.AppendLiteral(" off: ");
			handler.AppendFormatted(item3.Item2, "F1");
			handler.AppendLiteral(" FPS  (gain ");
			handler.AppendFormatted((item3.Item3 >= 0f) ? "+" : "");
			handler.AppendFormatted(item3.Item3, "F1");
			handler.AppendLiteral(")  ");
			handler.AppendFormatted(item3.Item4);
			handler.AppendLiteral("/s thrown");
			stringBuilder4.AppendLine(ref handler);
		}
		if (flag)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(32, 3, stringBuilder2);
			handler.AppendLiteral("  ALL modules OFF: ");
			handler.AppendFormatted(value, "F1");
			handler.AppendLiteral(" FPS  (gain ");
			handler.AppendFormatted((num3 >= 0f) ? "+" : "");
			handler.AppendFormatted(num3, "F1");
			handler.AppendLiteral(")");
			stringBuilder5.AppendLine(ref handler);
		}
		(string, float, float, long, bool) tuple = ((list2.Count > 0) ? list2[0] : default((string, float, float, long, bool)));
		float num7 = (num + num2) * 0.5f;
		bool flag2 = Mathf.Abs(num - num2) > num7 * 0.5f;
		string text = (flag2 ? $"UNRELIABLE — baseline drifted {num:F0}→{num2:F0} FPS; the run was too long/unstable to trust per-module gains. Use the per-module toggle in the menu (esp. 'BOMBUS Damage Gate [dyn]') for a direct A/B instead." : ((num4 > 5000) ? $"EXCEPTION STORM: ~{num4}/s swallowed throws — see [MegaBonkMod.HotErr] log lines." : ((list2.Count > 0 && tuple.Item3 > 8f && tuple.Item3 >= num7 * 0.4f) ? $"CULPRIT: '{tuple.Item1}' — removing it recovered {tuple.Item3:F0} FPS." : ((flag && num3 > 8f) ? $"SPREAD: no single module dominates, but ALL hot off recovered {num3:F0} FPS — cost is split across modules." : ((!flag || !(num3 < 8f)) ? "Inconclusive — re-run from a STEADY swarm (baseline drift should be small)." : "ALWAYS-ON: turning every toggleable module off did NOT recover FPS — the culprit is an un-gated always-on patch (or game-side). Bisect can't toggle it; I'll instrument the always-on hooks next.")))));
		stringBuilder.AppendLine(text);
		if (flag2)
		{
			stringBuilder.AppendLine("(!) Large baseline drift — wave changed a lot mid-test (likely because 90+ modules make the run long). Re-run from a steady swarm, or just toggle suspects one at a time.");
		}
		Result = stringBuilder.ToString();
		Plugin.Log.LogWarning((object)Result);
		Toast.Show(text, new Color(1f, 0.85f, 0.4f, 1f), 8f);
	}

	private static void Abort(string why)
	{
		foreach (KeyValuePair<PatchModules.Module, bool> item in _origMaster)
		{
			PatchModules.SetMaster(item.Key, item.Value);
		}
		Running = false;
		Status = "";
		Result = "Auto-bisect " + why + ".";
	}
}
