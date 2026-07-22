using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Assets.Scripts.Steam.LeaderboardsNew;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Steamworks;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MegabonkCommunityPatch;

internal static class AllTimeTab
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.AllTimeTab");

	private static LeaderboardUiNew _ui;

	private static GameObject _btnGo = null;

	private static IntPtr _wiredUi = IntPtr.Zero;

	private static IntPtr _weeklyWiredUi = IntPtr.Zero;

	private static LeaderboardInjector.ServerEntry[] _cache = null;

	private static LeaderboardInjector.ServerEntry[] _raw = null;

	private static bool _fetching = false;

	private static int _offset = 0;

	private static int _charFilter = -1;

	private static int _cacheFilter = -1;

	private static List<int> _charOrder = null;

	private static GameObject _cycleGo = null;

	private static RawImage _cycleIcon = null;

	private static TextMeshProUGUI _cycleLabel = null;

	private static IntPtr _cycleBuiltUi = IntPtr.Zero;

	private static int _mapFilter = 1;

	private static GameObject _mapCycleGo = null;

	private static RawImage _mapCycleIcon = null;

	private static TextMeshProUGUI _mapCycleLabel = null;

	private static IntPtr _mapCycleBuiltUi = IntPtr.Zero;

	private static readonly int[] MapOrder = { 1, 2, 8, 0 };

	private static GameObject _resetGo = null;

	private const int TabIndex = 1;

	private const int FetchCount = 100;

	internal static bool IsActive { get; set; } = false;


	internal static bool HasData => _raw != null;

	internal static void Init(LeaderboardUiNew ui)
	{
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Expected O, but got Unknown
		_ui = ui;
		try
		{
			ButtonNavigationSelectionOnly leaderboardTypeButtons = ui.leaderboardTypeButtons;
			if (((leaderboardTypeButtons != null) ? leaderboardTypeButtons.buttons : null) == null || ((Il2CppArrayBase<MyButtonTabs>)(object)leaderboardTypeButtons.buttons).Length <= 1)
			{
				return;
			}
			MyButtonTabs val = ((Il2CppArrayBase<MyButtonTabs>)(object)leaderboardTypeButtons.buttons)[1];
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return;
			}
			_btnGo = ((Component)val).gameObject;
			Button component = _btnGo.GetComponent<Button>();
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && _wiredUi != ((Il2CppObjectBase)ui).Pointer)
			{
				((UnityEventBase)component.onClick).RemoveAllListeners();
				((UnityEvent)component.onClick).AddListener((UnityAction)((Action)OnClick));
				_wiredUi = ((Il2CppObjectBase)ui).Pointer;
				Log.LogInfo((object)"All-Time tab wired (repurposed Friends button).");
			}
			TextMeshProUGUI componentInChildren = _btnGo.GetComponentInChildren<TextMeshProUGUI>();
			if ((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null)
			{
				((TMP_Text)componentInChildren).text = "Lifetime";
				((TMP_Text)componentInChildren).m_text = "Lifetime";
			}
			MyButtonTabs val2 = ((Il2CppArrayBase<MyButtonTabs>)(object)leaderboardTypeButtons.buttons)[0];
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
			{
				TextMeshProUGUI componentInChildren2 = ((Component)val2).gameObject.GetComponentInChildren<TextMeshProUGUI>();
				if ((UnityEngine.Object)(object)componentInChildren2 != (UnityEngine.Object)null)
				{
					((TMP_Text)componentInChildren2).text = "Weekly";
					((TMP_Text)componentInChildren2).m_text = "Weekly";
				}
				if (_weeklyWiredUi != ((Il2CppObjectBase)ui).Pointer)
				{
					Button component2 = ((Component)val2).gameObject.GetComponent<Button>();
					if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
					{
						((UnityEvent)component2.onClick).AddListener((UnityAction)((Action)OnWeeklyClick));
						_weeklyWiredUi = ((Il2CppObjectBase)ui).Pointer;
					}
				}
			}
			BuildCycleButton(ui);
			BuildMapCycleButton(ui);
			BuildResetButton(ui);
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val3 = new BepInExWarningLogInterpolatedStringHandler(6, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Init: ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val3);
		}
	}

	internal static void BeginFetch()
	{
		if (_fetching || _raw != null)
		{
			return;
		}
		_fetching = true;
		Task.Run<Task>((Func<Task>)async delegate
		{
			LeaderboardInjector.ServerEntry[] entries = Parse(await LeaderboardRelay.FetchEntries("alltime", 100, -1, "char"));
			ModGui.MainThread.Enqueue(delegate
			{
				//IL_0030: Unknown result type (might be due to invalid IL or missing references)
				//IL_0036: Expected O, but got Unknown
				_raw = entries;
				_cache = BuildView(_charFilter);
				_cacheFilter = _charFilter;
				_fetching = false;
				bool flag = default(bool);
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(29, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("All-Time pre-fetch: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(entries.Length);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" raw rows");
				}
				Log.LogInfo(val);
				if (IsActive)
				{
					Display(_cache);
				}
			});
		});
	}

	private static LeaderboardInjector.ServerEntry[] BuildView(int filter)
	{
		if (_raw == null)
		{
			return Array.Empty<LeaderboardInjector.ServerEntry>();
		}
		IEnumerable<LeaderboardInjector.ServerEntry> source = _raw;
		if (_mapFilter > 0)
		{
			source = source.Where((LeaderboardInjector.ServerEntry e) => e.MapIndex == _mapFilter || e.MapIndex == 0);
		}
		if (filter >= 0)
		{
			source = source.Where((LeaderboardInjector.ServerEntry e) => e.CharacterIndex == filter);
		}
		else
		{
			Dictionary<string, LeaderboardInjector.ServerEntry> dictionary = new Dictionary<string, LeaderboardInjector.ServerEntry>();
			foreach (LeaderboardInjector.ServerEntry serverEntry in source)
			{
				string key = ((!string.IsNullOrEmpty(serverEntry.SteamId)) ? serverEntry.SteamId : ("name:" + serverEntry.Name));
				if (!dictionary.TryGetValue(key, out var value) || serverEntry.Score > value.Score)
				{
					dictionary[key] = serverEntry;
				}
			}
			source = dictionary.Values;
		}
		return source.OrderByDescending((LeaderboardInjector.ServerEntry e) => e.Score).ToArray();
	}

	internal static void Refresh()
	{
		_raw = null;
		_cache = null;
		BeginFetch();
	}

	private static void OnClick()
	{
		IsActive = true;
		PersonalTab.IsActive = false;
		LeaderboardInjector.ResetScroll();
		_offset = 0;
		_charFilter = -1;
		_mapFilter = 1;
		ShowCycle();
		UpdateCycleVisual();
		ShowMapCycle();
		UpdateMapCycleVisual();
		if ((UnityEngine.Object)(object)_ui == (UnityEngine.Object)null)
		{
			return;
		}
		if (_raw != null)
		{
			_cache = BuildView(_charFilter);
			_cacheFilter = _charFilter;
			Display(_cache);
		}
		else
		{
			BeginFetch();
		}
		try
		{
			_ui.leaderboardTypeButtons.ButtonPressed(1, false);
		}
		catch
		{
		}
	}

	private static void OnWeeklyClick()
	{
		IsActive = false;
		PersonalTab.IsActive = false;
		HideCycle();
		HideMapCycle();
		LeaderboardInjector.ResetScroll();
		SteamLeaderboardNew lastLb = LeaderboardInjector.LastLb;
		if (lastLb != null)
		{
			try
			{
				SteamLeaderboardNew.A_LeaderboardReady?.Invoke(lastLb);
			}
			catch
			{
			}
		}
	}

	internal static void Redisplay()
	{
		if (_cache != null)
		{
			Display(_cache);
		}
	}

	internal static void ScrollBy(int delta)
	{
		if (_cache != null && _cache.Length != 0)
		{
			LeaderboardUiNew ui = _ui;
			if (((ui != null) ? ui.leaderboardEntries : null) != null)
			{
				int count = _ui.leaderboardEntries.Count;
				_offset = Math.Max(0, Math.Min(_offset + delta, Math.Max(0, _cache.Length - count)));
				Display(_cache);
			}
		}
	}

	internal static void HideCycle()
	{
		try
		{
			_charFilter = -1;
			UpdateCycleVisual();
			if ((UnityEngine.Object)(object)_cycleGo != (UnityEngine.Object)null)
			{
				_cycleGo.SetActive(false);
			}
			if ((UnityEngine.Object)(object)_mapCycleGo != (UnityEngine.Object)null)
			{
				_mapCycleGo.SetActive(false);
			}
			if ((UnityEngine.Object)(object)_resetGo != (UnityEngine.Object)null)
			{
				_resetGo.SetActive(false);
			}
		}
		catch
		{
		}
	}

	private static void ShowCycle()
	{
		try
		{
			if ((UnityEngine.Object)(object)_cycleGo != (UnityEngine.Object)null)
			{
				_cycleGo.SetActive(true);
			}
			if ((UnityEngine.Object)(object)_mapCycleGo != (UnityEngine.Object)null)
			{
				_mapCycleGo.SetActive(true);
			}
			if ((UnityEngine.Object)(object)_resetGo != (UnityEngine.Object)null)
			{
				_resetGo.SetActive(true);
			}
		}
		catch
		{
		}
	}

	private static void HideMapCycle()
	{
		try
		{
			if ((UnityEngine.Object)(object)_mapCycleGo != (UnityEngine.Object)null)
			{
				_mapCycleGo.SetActive(false);
			}
		}
		catch
		{
		}
	}

	private static void ShowMapCycle()
	{
		try
		{
			if ((UnityEngine.Object)(object)_mapCycleGo != (UnityEngine.Object)null)
			{
				_mapCycleGo.SetActive(true);
			}
		}
		catch
		{
		}
	}

	private static void BuildCycleButton(LeaderboardUiNew ui)
	{
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Expected O, but got Unknown
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Expected O, but got Unknown
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((UnityEngine.Object)(object)_cycleGo != (UnityEngine.Object)null && _cycleBuiltUi == ((Il2CppObjectBase)ui).Pointer)
			{
				return;
			}
			_cycleBuiltUi = ((Il2CppObjectBase)ui).Pointer;
			_cycleGo = null;
			RectTransform component = ((Component)ui).GetComponent<RectTransform>();
			if (!((UnityEngine.Object)(object)component == (UnityEngine.Object)null))
			{
				TMP_FontAsset val = null;
				TextMeshProUGUI val2 = (((UnityEngine.Object)(object)_btnGo != (UnityEngine.Object)null) ? _btnGo.GetComponentInChildren<TextMeshProUGUI>() : null);
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					val = ((TMP_Text)val2).font;
				}
				GameObject val3 = new GameObject("AllTimeCharCycle");
				RectTransform val4 = val3.AddComponent<RectTransform>();
				((Transform)val4).SetParent((Transform)(object)component, false);
				val4.anchorMin = new Vector2(1f, 1f);
				val4.anchorMax = new Vector2(1f, 1f);
				val4.pivot = new Vector2(1f, 1f);
				val4.sizeDelta = new Vector2(54f, 54f);
				val4.anchoredPosition = new Vector2(-22f, -12f);
				Image val5 = val3.AddComponent<Image>();
				((Graphic)val5).color = Color.white;
				Button val6 = val3.AddComponent<Button>();
				((Selectable)val6).targetGraphic = (Graphic)(object)val5;
				((UnityEvent)val6.onClick).AddListener((UnityAction)((Action)OnCycle));
				GameObject val7 = new GameObject("Face");
				RectTransform val8 = val7.AddComponent<RectTransform>();
				((Transform)val8).SetParent((Transform)(object)val4, false);
				val8.anchorMin = Vector2.zero;
				val8.anchorMax = Vector2.one;
				val8.offsetMin = new Vector2(2.5f, 2.5f);
				val8.offsetMax = new Vector2(-2.5f, -2.5f);
				Image val9 = val7.AddComponent<Image>();
				((Graphic)val9).color = new Color(0.086f, 0.086f, 0.165f, 1f);
				((Graphic)val9).raycastTarget = false;
				GameObject val10 = new GameObject("Icon");
				RectTransform val11 = val10.AddComponent<RectTransform>();
				((Transform)val11).SetParent((Transform)(object)val4, false);
				val11.anchorMin = new Vector2(0f, 0f);
				val11.anchorMax = new Vector2(1f, 1f);
				val11.offsetMin = new Vector2(5f, 5f);
				val11.offsetMax = new Vector2(-5f, -5f);
				_cycleIcon = val10.AddComponent<RawImage>();
				((Graphic)_cycleIcon).raycastTarget = false;
				((Component)_cycleIcon).gameObject.SetActive(false);
				GameObject val12 = new GameObject("Label");
				RectTransform val13 = val12.AddComponent<RectTransform>();
				((Transform)val13).SetParent((Transform)(object)val4, false);
				val13.anchorMin = Vector2.zero;
				val13.anchorMax = Vector2.one;
				val13.offsetMin = Vector2.zero;
				val13.offsetMax = Vector2.zero;
				_cycleLabel = val12.AddComponent<TextMeshProUGUI>();
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					((TMP_Text)_cycleLabel).font = val;
				}
				((TMP_Text)_cycleLabel).text = "A";
				((TMP_Text)_cycleLabel).alignment = (TextAlignmentOptions)514;
				((TMP_Text)_cycleLabel).fontSize = 28f;
				((Graphic)_cycleLabel).color = Color.white;
				((Graphic)_cycleLabel).raycastTarget = false;
				_cycleGo = val3;
				BuildCharOrder();
				UpdateCycleVisual();
				_cycleGo.SetActive(IsActive);
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val14 = new BepInExWarningLogInterpolatedStringHandler(18, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val14).AppendLiteral("BuildCycleButton: ");
				((BepInExLogInterpolatedStringHandler)val14).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val14);
		}
	}

	private static void BuildMapCycleButton(LeaderboardUiNew ui)
	{
		try
		{
			if ((UnityEngine.Object)(object)_mapCycleGo != (UnityEngine.Object)null && _mapCycleBuiltUi == ((Il2CppObjectBase)ui).Pointer)
			{
				return;
			}
			_mapCycleBuiltUi = ((Il2CppObjectBase)ui).Pointer;
			_mapCycleGo = null;
			RectTransform component = ((Component)ui).GetComponent<RectTransform>();
			if (!((UnityEngine.Object)(object)component == (UnityEngine.Object)null))
			{
				TMP_FontAsset val = null;
				TextMeshProUGUI val2 = (((UnityEngine.Object)(object)_btnGo != (UnityEngine.Object)null) ? _btnGo.GetComponentInChildren<TextMeshProUGUI>() : null);
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					val = ((TMP_Text)val2).font;
				}
				GameObject val3 = new GameObject("AllTimeMapCycle");
				RectTransform val4 = val3.AddComponent<RectTransform>();
				((Transform)val4).SetParent((Transform)(object)component, false);
				val4.anchorMin = new Vector2(1f, 1f);
				val4.anchorMax = new Vector2(1f, 1f);
				val4.pivot = new Vector2(1f, 1f);
				val4.sizeDelta = new Vector2(54f, 54f);
				val4.anchoredPosition = new Vector2(-82f, -12f);
				Image val5 = val3.AddComponent<Image>();
				((Graphic)val5).color = Color.white;
				Button val6 = val3.AddComponent<Button>();
				((Selectable)val6).targetGraphic = (Graphic)(object)val5;
				((UnityEvent)val6.onClick).AddListener((UnityAction)((Action)OnMapCycle));
				GameObject val7 = new GameObject("Face");
				RectTransform val8 = val7.AddComponent<RectTransform>();
				((Transform)val8).SetParent((Transform)(object)val4, false);
				val8.anchorMin = Vector2.zero;
				val8.anchorMax = Vector2.one;
				val8.offsetMin = new Vector2(2.5f, 2.5f);
				val8.offsetMax = new Vector2(-2.5f, -2.5f);
				Image val9 = val7.AddComponent<Image>();
				((Graphic)val9).color = new Color(0.086f, 0.086f, 0.165f, 1f);
				((Graphic)val9).raycastTarget = false;
				GameObject val10 = new GameObject("Icon");
				RectTransform val11 = val10.AddComponent<RectTransform>();
				((Transform)val11).SetParent((Transform)(object)val4, false);
				val11.anchorMin = new Vector2(0f, 0f);
				val11.anchorMax = new Vector2(1f, 1f);
				val11.offsetMin = new Vector2(5f, 5f);
				val11.offsetMax = new Vector2(-5f, -5f);
				_mapCycleIcon = val10.AddComponent<RawImage>();
				((Graphic)_mapCycleIcon).raycastTarget = false;
				((Component)_mapCycleIcon).gameObject.SetActive(false);
				GameObject val12 = new GameObject("Label");
				RectTransform val13 = val12.AddComponent<RectTransform>();
				((Transform)val13).SetParent((Transform)(object)val4, false);
				val13.anchorMin = Vector2.zero;
				val13.anchorMax = Vector2.one;
				val13.offsetMin = Vector2.zero;
				val13.offsetMax = Vector2.zero;
				_mapCycleLabel = val12.AddComponent<TextMeshProUGUI>();
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					((TMP_Text)_mapCycleLabel).font = val;
				}
				((TMP_Text)_mapCycleLabel).text = "1";
				((TMP_Text)_mapCycleLabel).alignment = (TextAlignmentOptions)514;
				((TMP_Text)_mapCycleLabel).fontSize = 28f;
				((Graphic)_mapCycleLabel).color = Color.white;
				((Graphic)_mapCycleLabel).raycastTarget = false;
				_mapCycleGo = val3;
				UpdateMapCycleVisual();
				_mapCycleGo.SetActive(IsActive);
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val14 = new BepInExWarningLogInterpolatedStringHandler(19, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val14).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val14);
		}
	}

	private static void BuildResetButton(LeaderboardUiNew ui)
	{
		try
		{
			if ((UnityEngine.Object)(object)_resetGo != (UnityEngine.Object)null && _cycleBuiltUi == ((Il2CppObjectBase)ui).Pointer)
			{
				return;
			}
			_resetGo = null;
			RectTransform component = ((Component)ui).GetComponent<RectTransform>();
			if (!((UnityEngine.Object)(object)component == (UnityEngine.Object)null))
			{
				TMP_FontAsset val = null;
				TextMeshProUGUI val2 = (((UnityEngine.Object)(object)_btnGo != (UnityEngine.Object)null) ? _btnGo.GetComponentInChildren<TextMeshProUGUI>() : null);
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					val = ((TMP_Text)val2).font;
				}
				GameObject val3 = new GameObject("AllTimeReset");
				RectTransform val4 = val3.AddComponent<RectTransform>();
				((Transform)val4).SetParent((Transform)(object)component, false);
				val4.anchorMin = new Vector2(1f, 1f);
				val4.anchorMax = new Vector2(1f, 1f);
				val4.pivot = new Vector2(1f, 1f);
				val4.sizeDelta = new Vector2(36f, 36f);
				val4.anchoredPosition = new Vector2(-142f, -12f);
				Image val5 = val3.AddComponent<Image>();
				((Graphic)val5).color = Color.white;
				Button val6 = val3.AddComponent<Button>();
				((Selectable)val6).targetGraphic = (Graphic)(object)val5;
				((UnityEvent)val6.onClick).AddListener((UnityAction)((Action)OnReset));
				GameObject val7 = new GameObject("Face");
				RectTransform val8 = val7.AddComponent<RectTransform>();
				((Transform)val8).SetParent((Transform)(object)val4, false);
				val8.anchorMin = Vector2.zero;
				val8.anchorMax = Vector2.one;
				val8.offsetMin = new Vector2(2f, 2f);
				val8.offsetMax = new Vector2(-2f, -2f);
				Image val9 = val7.AddComponent<Image>();
				((Graphic)val9).color = new Color(0.086f, 0.086f, 0.165f, 1f);
				((Graphic)val9).raycastTarget = false;
				GameObject val10 = new GameObject("Label");
				RectTransform val11 = val10.AddComponent<RectTransform>();
				((Transform)val11).SetParent((Transform)(object)val4, false);
				val11.anchorMin = Vector2.zero;
				val11.anchorMax = Vector2.one;
				val11.offsetMin = Vector2.zero;
				val11.offsetMax = Vector2.zero;
				TextMeshProUGUI label = val10.AddComponent<TextMeshProUGUI>();
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					((TMP_Text)label).font = val;
				}
				((TMP_Text)label).text = "R";
				((TMP_Text)label).alignment = (TextAlignmentOptions)514;
				((TMP_Text)label).fontSize = 24f;
				((Graphic)label).color = Color.white;
				((Graphic)label).raycastTarget = false;
				_resetGo = val3;
				_resetGo.SetActive(IsActive);
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val12 = new BepInExWarningLogInterpolatedStringHandler(17, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val12).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val12);
		}
	}

	private static void OnReset()
	{
		_charFilter = -1;
		_mapFilter = 1;
		UpdateCycleVisual();
		UpdateMapCycleVisual();
		_offset = 0;
		LeaderboardInjector.ResetScroll();
		if (_raw != null)
		{
			_cache = BuildView(_charFilter);
			_cacheFilter = _charFilter;
			if (IsActive) Display(_cache);
		}
		else
		{
			BeginFetch();
		}
	}

	private static void BuildCharOrder()
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected I4, but got Unknown
		_charOrder = new List<int>();
		DataManager instance = DataManager.Instance;
		if (((instance != null) ? instance.unsortedCharacterData : null) == null)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < instance.unsortedCharacterData.Count; i++)
		{
			CharacterData val = instance.unsortedCharacterData[i];
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				int item = (int)val.eCharacter;
				if (hashSet.Add(item))
				{
					_charOrder.Add(item);
				}
			}
		}
		_charOrder.Sort();
	}

	private static void OnCycle()
	{
		if (_charOrder == null || _charOrder.Count == 0)
		{
			BuildCharOrder();
		}
		if (_charFilter < 0)
		{
			_charFilter = ((_charOrder != null && _charOrder.Count > 0) ? _charOrder[0] : (-1));
		}
		else
		{
			int num = _charOrder.IndexOf(_charFilter);
			if (num < 0 || num + 1 >= _charOrder.Count)
			{
				_charFilter = -1;
			}
			else
			{
				_charFilter = _charOrder[num + 1];
			}
		}
		UpdateCycleVisual();
		_offset = 0;
		LeaderboardInjector.ResetScroll();
		if (_raw != null)
		{
			_cache = BuildView(_charFilter);
			_cacheFilter = _charFilter;
			if (IsActive)
			{
				Display(_cache);
			}
		}
		else
		{
			BeginFetch();
		}
	}

	private static void OnMapCycle()
	{
		int num = Array.IndexOf(MapOrder, _mapFilter);
		if (num < 0 || num + 1 >= MapOrder.Length)
		{
			_mapFilter = MapOrder[0];
		}
		else
		{
			_mapFilter = MapOrder[num + 1];
		}
		UpdateMapCycleVisual();
		_offset = 0;
		LeaderboardInjector.ResetScroll();
		if (_raw != null)
		{
			_cache = BuildView(_charFilter);
			_cacheFilter = _charFilter;
			if (IsActive)
			{
				Display(_cache);
			}
		}
		else
		{
			BeginFetch();
		}
	}

	private static string MapName(int mapIndex)
	{
		return mapIndex switch
		{
			1 => "F",
			2 => "D",
			8 => "G",
			_ => "A",
		};
	}

	private static void UpdateMapCycleVisual()
	{
		if ((UnityEngine.Object)(object)_mapCycleGo == (UnityEngine.Object)null)
		{
			return;
		}
		if (_mapFilter == 0)
		{
			if ((UnityEngine.Object)(object)_mapCycleIcon != (UnityEngine.Object)null)
			{
				((Component)_mapCycleIcon).gameObject.SetActive(false);
			}
			if ((UnityEngine.Object)(object)_mapCycleLabel != (UnityEngine.Object)null)
			{
				((Component)_mapCycleLabel).gameObject.SetActive(true);
				((TMP_Text)_mapCycleLabel).text = "A";
			}
			return;
		}
		Texture tex = null;
		DataManager instance = DataManager.Instance;
		if (((instance != null) ? instance.maps : null) != null)
		{
			foreach (MapData md in instance.maps)
			{
				if (!((UnityEngine.Object)(object)md == (UnityEngine.Object)null) && (int)md.eMap == _mapFilter)
				{
					tex = md.icon;
					break;
				}
			}
		}
		if ((UnityEngine.Object)(object)_mapCycleIcon != (UnityEngine.Object)null && (UnityEngine.Object)(object)tex != (UnityEngine.Object)null)
		{
			_mapCycleIcon.texture = tex;
			((Component)_mapCycleIcon).gameObject.SetActive(true);
		}
		if ((UnityEngine.Object)(object)_mapCycleLabel != (UnityEngine.Object)null)
		{
			((Component)_mapCycleLabel).gameObject.SetActive(false);
		}
	}

	private static void UpdateCycleVisual()
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Invalid comparison between Unknown and I4
		if ((UnityEngine.Object)(object)_cycleGo == (UnityEngine.Object)null)
		{
			return;
		}
		if (_charFilter < 0)
		{
			if ((UnityEngine.Object)(object)_cycleIcon != (UnityEngine.Object)null)
			{
				((Component)_cycleIcon).gameObject.SetActive(false);
			}
			if ((UnityEngine.Object)(object)_cycleLabel != (UnityEngine.Object)null)
			{
				((Component)_cycleLabel).gameObject.SetActive(true);
				((TMP_Text)_cycleLabel).text = "A";
			}
			return;
		}
		Texture val = null;
		DataManager instance = DataManager.Instance;
		if (((instance != null) ? instance.unsortedCharacterData : null) != null)
		{
			for (int i = 0; i < instance.unsortedCharacterData.Count; i++)
			{
				CharacterData val2 = instance.unsortedCharacterData[i];
				if (!((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null) && (int)val2.eCharacter == _charFilter)
				{
					val = val2.icon;
					break;
				}
			}
		}
		if ((UnityEngine.Object)(object)_cycleIcon != (UnityEngine.Object)null && (UnityEngine.Object)(object)val != (UnityEngine.Object)null)
		{
			_cycleIcon.texture = val;
			((Component)_cycleIcon).gameObject.SetActive(true);
		}
		if ((UnityEngine.Object)(object)_cycleLabel != (UnityEngine.Object)null)
		{
			((Component)_cycleLabel).gameObject.SetActive(false);
		}
	}

	private static void Display(LeaderboardInjector.ServerEntry[] entries)
	{
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Expected O, but got Unknown
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Invalid comparison between Unknown and I4
		if (!IsActive)
		{
			return;
		}
		LeaderboardUiNew ui = _ui;
		if (((ui != null) ? ui.leaderboardEntries : null) == null)
		{
			return;
		}
		foreach (LeaderboardInjector.ServerEntry serverEntry in entries)
		{
			if (ulong.TryParse(serverEntry.SteamId, out var result) && result != 0L && !string.IsNullOrEmpty(serverEntry.Name))
			{
				LeaderboardInjector.NameCache[result] = serverEntry.Name;
			}
		}
		DataManager instance = DataManager.Instance;
		int count = _ui.leaderboardEntries.Count;
		_offset = Math.Max(0, Math.Min(_offset, Math.Max(0, entries.Length - count)));
		LeaderboardInjector.SlotRunData = new string[count];
		string text = SteamUser.GetSteamID().m_SteamID.ToString();
		bool flag = default(bool);
		for (int j = 0; j < count; j++)
		{
			LeaderboardEntryUi val = _ui.leaderboardEntries[j];
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				continue;
			}
			int num = j + _offset;
			if (num >= entries.Length)
			{
				try
				{
					val.Clear();
				}
				catch
				{
				}
				continue;
			}
			try
			{
				LeaderboardInjector.ServerEntry serverEntry2 = entries[num];
				LeaderboardInjector.SlotRunData[j] = serverEntry2.RunData;
				if (!((Component)val).gameObject.activeSelf)
				{
					((Component)val).gameObject.SetActive(true);
				}
				((TMP_Text)val.rank).SetText($"#{num + 1}");
				((TMP_Text)val.playerName).SetText(serverEntry2.Name ?? "?");
				((TMP_Text)val.score).SetText(serverEntry2.Score.ToString("N0"));
				if (((instance != null) ? instance.unsortedCharacterData : null) != null)
				{
					for (int k = 0; k < instance.unsortedCharacterData.Count; k++)
					{
						CharacterData val2 = instance.unsortedCharacterData[k];
						if (!((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null) && (int)val2.eCharacter == serverEntry2.CharacterIndex)
						{
							if ((UnityEngine.Object)(object)val2.icon != (UnityEngine.Object)null)
							{
								val.characterIcon.texture = val2.icon;
							}
							break;
						}
					}
				}
				if ((UnityEngine.Object)(object)val.playerIcon != (UnityEngine.Object)null && ulong.TryParse(serverEntry2.SteamId, out var result2))
				{
					Texture avatar = LeaderboardInjector.GetAvatar(result2);
					if ((UnityEngine.Object)(object)avatar != (UnityEngine.Object)null)
					{
						val.playerIcon.texture = avatar;
					}
					else
					{
						LeaderboardInjector.NoteAvatarMiss(result2);
					}
				}
				bool active = serverEntry2.SteamId == text;
				if ((UnityEngine.Object)(object)val.localHighlight != (UnityEngine.Object)null)
				{
					((Component)val.localHighlight).gameObject.SetActive(active);
				}
			}
			catch (Exception ex)
			{
				BepInExWarningLogInterpolatedStringHandler val3 = new BepInExWarningLogInterpolatedStringHandler(7, 2, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Slot ");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(j);
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(": ");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
				}
				Log.LogWarning(val3);
			}
		}
		BepInExInfoLogInterpolatedStringHandler val4 = new BepInExInfoLogInterpolatedStringHandler(43, 3, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("All-Time Display: ");
			((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<int>(entries.Length);
			((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(" entries, slots=");
			((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<int>(count);
			((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(", offset=");
			((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<int>(_offset);
		}
		Log.LogInfo(val4);
		try
		{
			if ((UnityEngine.Object)(object)_ui != (UnityEngine.Object)null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(((Component)_ui).GetComponent<RectTransform>());
			}
		}
		catch
		{
		}
	}

	private static LeaderboardInjector.ServerEntry[] Parse(string json)
	{
		if (string.IsNullOrEmpty(json) || json == "[]")
		{
			return Array.Empty<LeaderboardInjector.ServerEntry>();
		}
		try
		{
			return JsonSerializer.Deserialize<LeaderboardInjector.ServerEntry[]>(json) ?? Array.Empty<LeaderboardInjector.ServerEntry>();
		}
		catch
		{
			return Array.Empty<LeaderboardInjector.ServerEntry>();
		}
	}
}
