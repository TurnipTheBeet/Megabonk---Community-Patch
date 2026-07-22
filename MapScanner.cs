using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class MapScanner
{
	private sealed class Row
	{
		public string Key;

		public string Label;

		public string[] Match;

		public EItemRarity? MwTier;

		public bool Combined;

		public bool MwAny;

		public bool Exact;
	}

	private static readonly Row[] Rows = new Row[12]
	{
		new Row
		{
			Key = "bosscurse",
			Label = "Boss Curses (exact)",
			Match = new string[1] { "curse" },
			Exact = true
		},
		new Row
		{
			Key = "challenge",
			Label = "Challenge Shrines (exact)",
			Match = new string[1] { "challenge" },
			Exact = true
		},
		new Row
		{
			Key = "baldhead",
			Label = "Baldhead Shrines",
			Match = new string[1] { "bald" }
		},
		new Row
		{
			Key = "magnet",
			Label = "Magnet Shrines",
			Match = new string[1] { "magnet" }
		},
		new Row
		{
			Key = "mw_any",
			Label = "Microwave: Any",
			MwAny = true
		},
		new Row
		{
			Key = "mw_common",
			Label = "Microwave: Common",
			MwTier = (EItemRarity)0
		},
		new Row
		{
			Key = "mw_rare",
			Label = "Microwave: Rare",
			MwTier = (EItemRarity)1
		},
		new Row
		{
			Key = "mw_epic",
			Label = "Microwave: Epic",
			MwTier = (EItemRarity)2
		},
		new Row
		{
			Key = "mw_legend",
			Label = "Microwave: Legendary",
			MwTier = (EItemRarity)3
		},
		new Row
		{
			Key = "moaishady",
			Label = "Moai + Shady (any)",
			Combined = true
		},
		new Row
		{
			Key = "moais",
			Label = "Moais",
			Match = new string[1] { "moai" }
		},
		new Row
		{
			Key = "shady",
			Label = "Shady Guy",
			Match = new string[1] { "shady" }
		}
	};

	private static ConfigEntry<int>[] _desired;

	private static ConfigFile _cfg;

	internal static bool Visible;

	internal static bool Active;

	internal static int Attempts;

	internal static string Status = "Idle";

	private static Dictionary<string, int> _cur = new Dictionary<string, int>();

	private static float _nextLiveRefresh;

	private static float _nextScanPoll;

	private static int _seedAtRestart;

	private static bool _awaitingNewMap;

	private const float WinW = 320f;

	private const float PadX = 12f;

	private const float LineH = 24f;

	private static readonly GuiWindowFrame _frame = new GuiWindowFrame(new Vector2(360f, 80f)).Persist("MapScanner");

	private static float _lastWinH;

	private static float WinHeight()
	{
		return 56f + (float)Rows.Length * 26f + 24f + 24f + 8f;
	}

	internal static void Init(ConfigFile cfg)
	{
		_cfg = cfg;
		_desired = new ConfigEntry<int>[Rows.Length];
		for (int i = 0; i < Rows.Length; i++)
		{
			_desired[i] = cfg.Bind<int>("MapScanner", Rows[i].Key, 0, "Desired '" + Rows[i].Label + "' the scanner waits for (0 = ignore).");
		}
	}

	internal static void Toggle()
	{
		Visible = !Visible;
	}

	private static Dictionary<string, int> ReadDict()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		try
		{
			var interactablesByName = InteractablesStatus.interactablesByName;
			if (interactablesByName == null)
			{
				return dictionary;
			}
			var enumerator = interactablesByName.GetEnumerator();
			while (enumerator.MoveNext())
			{
				var current = enumerator.Current;
				var value = current.Value;
				if (value != null)
				{
					dictionary[current.Key] = value.numTotal;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(19, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[MapScanner] dict: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			log.LogWarning(val);
		}
		return dictionary;
	}

	private static int DictCount(Dictionary<string, int> dict, string[] match)
	{
		int num = 0;
		foreach (KeyValuePair<string, int> item in dict)
		{
			string text = item.Key.ToLowerInvariant();
			foreach (string value in match)
			{
				if (text.Contains(value))
				{
					num += item.Value;
					break;
				}
			}
		}
		return num;
	}

	private static int[] CountMicrowaveTiers()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected I4, but got Unknown
		int[] array = new int[6];
		try
		{
			Il2CppArrayBase<InteractableMicrowave> val = Object.FindObjectsOfType<InteractableMicrowave>();
			if (val != null)
			{
				foreach (InteractableMicrowave item in val)
				{
					if (!((UnityEngine.Object)(object)item == (UnityEngine.Object)null))
					{
						int num = (int)item.rarity;
						if (num >= 0 && num < array.Length)
						{
							array[num]++;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(17, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[MapScanner] mw: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log.LogWarning(val2);
		}
		return array;
	}

	private static Dictionary<string, int> ComputeCounts()
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, int> dict = ReadDict();
		int[] array = CountMicrowaveTiers();
		int num = DictCount(dict, new string[1] { "moai" });
		int num2 = DictCount(dict, new string[1] { "shady" });
		int num3 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			num3 += array[i];
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Row[] rows = Rows;
		foreach (Row row in rows)
		{
			int value = ((!row.Combined) ? ((!row.MwAny) ? ((!row.MwTier.HasValue) ? DictCount(dict, row.Match) : array[(int)row.MwTier.Value]) : num3) : (num + num2));
			dictionary[row.Key] = value;
		}
		return dictionary;
	}

	private static bool AnyDesired()
	{
		for (int i = 0; i < _desired.Length; i++)
		{
			if (_desired[i].Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool Matches(Dictionary<string, int> cur)
	{
		for (int i = 0; i < Rows.Length; i++)
		{
			int value = _desired[i].Value;
			if (value <= 0)
			{
				continue;
			}
			cur.TryGetValue(Rows[i].Key, out var value2);
			if (Rows[i].Exact)
			{
				if (value2 != value)
				{
					return false;
				}
			}
			else if (value2 < value)
			{
				return false;
			}
		}
		return true;
	}

	private static bool HasAnyCount(Dictionary<string, int> cur)
	{
		foreach (int value in cur.Values)
		{
			if (value > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void PauseGame()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		try
		{
			PauseUi val = null;
			PauseHandler val2 = Object.FindObjectOfType<PauseHandler>();
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
			{
				val = val2.pauseUi;
			}
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				val = Object.FindObjectOfType<PauseUi>();
			}
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null) && !val.IsPaused() && val.CanPause())
			{
				val.Pause();
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val3 = new BepInExWarningLogInterpolatedStringHandler(20, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("[MapScanner] pause: ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
			}
			log.LogWarning(val3);
		}
	}

	internal static void StartScan()
	{
		if (MapController.IsMainMenu())
		{
			Status = "Start a run first.";
			return;
		}
		if (!AnyDesired())
		{
			Status = "Set at least one count > 0.";
			return;
		}
		Active = true;
		Attempts = 0;
		_awaitingNewMap = false;
		Status = "Scanning…";
	}

	internal static void StopScan(string msg = "Stopped")
	{
		Active = false;
		_awaitingNewMap = false;
		Status = msg;
	}

	internal static void ToggleScan()
	{
		if (Active)
		{
			StopScan("Cancelled.");
		}
		else
		{
			StartScan();
		}
	}

	internal static void Tick()
	{
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected O, but got Unknown
		if (!Active)
		{
			return;
		}
		try
		{
			if (MapController.IsMainMenu())
			{
				StopScan("Left run.");
			}
			else
			{
				if (MapGenerationController.isGenerating)
				{
					return;
				}
				if (_awaitingNewMap)
				{
					if (MapGenerationController.mapSeed == _seedAtRestart)
					{
						return;
					}
					_awaitingNewMap = false;
				}
				if (Time.unscaledTime < _nextScanPoll)
				{
					return;
				}
				_nextScanPoll = Time.unscaledTime + 0.2f;
				Dictionary<string, int> cur = ComputeCounts();
				if (HasAnyCount(cur))
				{
					_cur = cur;
					if (Matches(cur))
					{
						StopScan($"Match found after {Attempts} reroll(s).");
						PauseGame();
					}
					else
					{
						Attempts++;
						_seedAtRestart = MapGenerationController.mapSeed;
						_awaitingNewMap = true;
						Status = $"Reroll #{Attempts}…";
						MapController.RestartRun();
					}
				}
			}
		}
		catch (Exception ex)
		{
			StopScan("Error: " + ex.Message);
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(13, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[MapScanner] ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<Exception>(ex);
			}
			log.LogError(val);
		}
	}

	internal static void HandleInput()
	{
		if (Visible)
		{
			_frame.HandleInput(320f, (_lastWinH > 0f) ? _lastWinH : WinHeight(), 28f);
		}
	}

	internal static void Draw()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!Active && !MapController.IsMainMenu() && Time.unscaledTime >= _nextLiveRefresh)
		{
			_nextLiveRefresh = Time.unscaledTime + 0.5f;
			Dictionary<string, int> cur = ComputeCounts();
			if (HasAnyCount(cur))
			{
				_cur = cur;
			}
		}
		float num = (_lastWinH = WinHeight());
		Matrix4x4 old = _frame.Begin();
		float x = _frame.Pivot.x;
		float y = _frame.Pivot.y;
		UiTheme.Backdrop(new Rect(x, y, 320f, num), "MapScanner");
		GUI.Box(new Rect(x, y, 320f, num), "Map Scanner");
		float num2 = 296f;
		float num3 = x + 12f;
		float num4 = y + 24f + 2f;
		GUI.Label(new Rect(num3, num4, num2, 24f), "Want at least (0 = ignore):");
		num4 += 24f;
		for (int i = 0; i < Rows.Length; i++)
		{
			_cur.TryGetValue(Rows[i].Key, out var value);
			string text = ((Active || _cur.Count > 0) ? $"{Rows[i].Label}  ({value})" : Rows[i].Label);
			GUI.Label(new Rect(num3, num4, num2 - 92f, 24f), text);
			GUI.enabled = !Active;
			float num5 = num3 + num2 - 92f;
			if (GUI.Button(new Rect(num5, num4, 26f, 22f), "-") && _desired[i].Value > 0)
			{
				SetDesired(i, _desired[i].Value - 1);
			}
			GUI.Label(new Rect(num5 + 34f, num4, 26f, 24f), _desired[i].Value.ToString());
			if (GUI.Button(new Rect(num5 + 64f, num4, 26f, 22f), "+"))
			{
				SetDesired(i, _desired[i].Value + 1);
			}
			GUI.enabled = true;
			num4 += 26f;
		}
		string text2 = Hotkeys.Pretty(Hotkeys.MapScanToggle.Value);
		string text3 = (Active ? $"SCANNING — reroll #{Attempts}  ({text2} to stop)" : (text2 + " to start scan"));
		GUI.Label(new Rect(num3, num4, num2, 24f), text3);
		num4 += 24f;
		GUI.Label(new Rect(num3, num4, num2, 24f), "Status: " + Status);
		_frame.End(old);
		_frame.DrawGrip(320f, num);
	}

	private static void SetDesired(int idx, int value)
	{
		if (value < 0)
		{
			value = 0;
		}
		if (_desired[idx].Value == value)
		{
			return;
		}
		_desired[idx].Value = value;
		try
		{
			ConfigFile cfg = _cfg;
			if (cfg != null)
			{
				cfg.Save();
			}
		}
		catch
		{
		}
	}
}
