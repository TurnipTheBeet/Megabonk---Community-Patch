using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MegabonkCommunityPatch;

internal static class NativeSettings
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.NativeSettings");

	private const string TabButtonName = "B_CommunityPatch";

	private static GameObject _ourTab;

	private static MyButtonTabs _ourMbt;

	private static Settings _sm;

	private static List<(KeyDisplay kd, ConfigEntry<KeyCode> entry)> _rows;

	private static bool _kcHas;

	private static Texture _kcKdTex;

	private static Color _kcKdColor = Color.white;

	private static Texture _kcBtnTex;

	private static Color _kcBtnColor = Color.white;

	private static Color _kcTextColor = Color.white;

	private static bool _capturing;

	private static ConfigEntry<KeyCode> _capEntry;

	private static KeyDisplay _capKd;

	private static string _capName;

	private static int _capStartFrame;

	private static Sprite _capSprite;

	private static bool _capTried;

	private static readonly KeyCode[] AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

	internal static void TryInject(Settings sm)
	{
		//IL_063c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0643: Expected O, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected O, but got Unknown
		try
		{
			if ((UnityEngine.Object)(object)sm == (UnityEngine.Object)null)
			{
				return;
			}
			_sm = sm;
			GameObject gameObject = ((Component)sm).gameObject;
			ButtonNavigationSelectionOnly componentInChildren = gameObject.GetComponentInChildren<ButtonNavigationSelectionOnly>(true);
			if ((UnityEngine.Object)(object)componentInChildren == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] no ButtonNavigationSelectionOnly found");
				return;
			}
			Transform transform = ((Component)componentInChildren).transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				if (((Object)transform.GetChild(i)).name == "B_CommunityPatch")
				{
					return;
				}
			}
			if ((UnityEngine.Object)(object)sm.otherContent == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] otherContent null");
				return;
			}
			if ((UnityEngine.Object)(object)sm.controlPrefabNew == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] controlPrefabNew null");
				return;
			}
			CaptureKeycapFromPrefab(sm.controlPrefabNew);
			Transform parent = sm.otherContent.parent;
			Transform parent2 = parent.parent;
			GameObject val = new GameObject("CP_Holder");
			val.SetActive(false);
			Transform transform2 = val.transform;
			GameObject ourTab = Object.Instantiate<GameObject>(((Component)parent).gameObject, transform2);
			((Object)ourTab).name = "CP_Tab";
			ScrollRect component = ourTab.GetComponent<ScrollRect>();
			Transform val2 = (Transform)(((UnityEngine.Object)(object)component != (UnityEngine.Object)null && (UnityEngine.Object)(object)component.content != (UnityEngine.Object)null) ? ((object)component.content) : ((object)ourTab.transform));
			for (int num = val2.childCount - 1; num >= 0; num--)
			{
				Object.DestroyImmediate((UnityEngine.Object)(object)((Component)val2.GetChild(num)).gameObject);
			}
			_rows = new List<(KeyDisplay, ConfigEntry<KeyCode>)>();
			(string, ConfigEntry<KeyCode>)[] array = Hotkeys.All();
			for (int j = 0; j < array.Length; j++)
			{
				var (label, entry) = array[j];
				BuildRow(sm.controlPrefabNew, val2, label, entry);
			}
			// All opacity sliders together below the hotkeys for a clean look
			foreach (var (label, _) in array)
			{
				string opacityKey = label switch
				{
					"Mod Menu" => "ModMenu",
					"Chaos Menu" => "ChaosMenu",
					"Map Scanner" => "MapScanner",
					"Auto-Upgrade Log" => "AutoUpgrade",
					"Powerup Tracker" => "PowerupTracker",
					"Chest Odds Tracker" => "ChestOdds",
					_ => null
				};
				if (opacityKey != null)
				{
					BuildSliderRow(sm.sliderPrefab, val2, label + " Opacity",
						() => UiTheme.GetOpacity(opacityKey),
						delegate(float v) { UiTheme.SetOpacity(opacityKey, v); });
				}
			}
		BuildSliderRow(sm.sliderPrefab, val2, "Weapon SFX Volume", () => WeaponSfxVolume.Weapon, delegate(float v)
		{
			WeaponSfxVolume.Weapon = v;
		});
		BuildSliderRow(sm.sliderPrefab, val2, "Hit SFX Volume", () => WeaponSfxVolume.Hit, delegate(float v)
		{
			WeaponSfxVolume.Hit = v;
		});
		BuildSliderRow(sm.sliderPrefab, val2, "Item SFX Volume", () => WeaponSfxVolume.Item, delegate(float v)
		{
			WeaponSfxVolume.Item = v;
		});
		BuildEnumToggleRow(sm.enumPrefab, val2, "Enable Mod Menu (F1)", () => Hotkeys.ModMenuEnabled.Value, delegate(bool v)
		{
			Hotkeys.ModMenuEnabled.Value = v;
		});
			GameObject val3 = ((transform.childCount > 0) ? ((Component)transform.GetChild(0)).gameObject : null);
			if ((UnityEngine.Object)(object)val3 == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] no tab-button template");
				Object.Destroy((UnityEngine.Object)(object)val);
				return;
			}
			GameObject val4 = Object.Instantiate<GameObject>(val3, transform2);
			((Object)val4).name = "B_CommunityPatch";
			MyButtonTabs mbt = val4.GetComponent<MyButtonTabs>();
			TMP_Text componentInChildren2 = val4.GetComponentInChildren<TMP_Text>(true);
			if ((UnityEngine.Object)(object)componentInChildren2 != (UnityEngine.Object)null)
			{
				StripLocalizer(((Component)componentInChildren2).gameObject);
				componentInChildren2.text = "Mod";
			}
			List<MyButtonTabs> sixTabs = new List<MyButtonTabs>();
			for (int k = 0; k < transform.childCount; k++)
			{
				MyButtonTabs component2 = ((Component)transform.GetChild(k)).GetComponent<MyButtonTabs>();
				if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
				{
					sixTabs.Add(component2);
				}
			}
			Button component3 = val4.GetComponent<Button>();
			if ((UnityEngine.Object)(object)component3 != (UnityEngine.Object)null)
			{
				((UnityEventBase)component3.onClick).RemoveAllListeners();
				((UnityEvent)component3.onClick).AddListener((UnityAction)((Action)delegate
				{
					ShowTab(ourTab, mbt, sixTabs);
				}));
			}
			foreach (MyButtonTabs item in sixTabs)
			{
				Button component4 = ((Component)item).GetComponent<Button>();
				if ((UnityEngine.Object)(object)component4 != (UnityEngine.Object)null)
				{
					((UnityEvent)component4.onClick).AddListener((UnityAction)((Action)delegate
					{
						HideTab(ourTab, mbt);
					}));
				}
			}
			ourTab.transform.SetParent(parent2, false);
			ourTab.SetActive(false);
			if ((UnityEngine.Object)(object)mbt != (UnityEngine.Object)null)
			{
				mbt.associatedContent = ourTab;
			}
			val4.transform.SetParent(transform, false);
			val4.SetActive(true);
			Object.Destroy((UnityEngine.Object)(object)val);
			_ourTab = ourTab;
			_ourMbt = mbt;
			Log.LogInfo((object)"[CP] Community Patch tab injected.");
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val5 = new BepInExWarningLogInterpolatedStringHandler(20, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("[CP] inject failed: ");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<Exception>(ex);
			}
			Log.LogWarning(val5);
		}
	}

	internal static void OnSettingsClosed()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((UnityEngine.Object)(object)_ourTab != (UnityEngine.Object)null)
			{
				_ourTab.SetActive(false);
			}
			if ((UnityEngine.Object)(object)_ourMbt != (UnityEngine.Object)null && (UnityEngine.Object)(object)_ourMbt.background != (UnityEngine.Object)null)
			{
				((Graphic)_ourMbt.background).color = _ourMbt.defaultColor;
			}
			if (_capturing)
			{
				_capturing = false;
				Hotkeys.Capturing = false;
				_capEntry = null;
				_capKd = null;
				_capName = null;
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(23, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[CP] OnSettingsClosed: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val);
		}
	}

	private static void BuildRow(GameObject prefab, Transform parent, string label, ConfigEntry<KeyCode> entry)
	{
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			GameObject val = Object.Instantiate<GameObject>(prefab, parent);
			((Object)val).name = "CP_Row_" + label.Replace(" ", "");
			val.SetActive(true);
			StripComp(val, "InputSettingNew");
			StripMyButtons(val);
			Transform transform = val.transform;
			Transform val2 = transform.Find("Content/Left/Text (TMP)");
			TMP_Text val3 = (((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? ((Component)val2).GetComponent<TMP_Text>() : null);
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
			{
				StripLocalizer(((Component)val3).gameObject);
				val3.text = label;
			}
			Transform val4 = transform.Find("Content/Right/InteractSetting");
			KeyDisplay val5 = null;
			if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
			{
				Transform val6 = val4.Find("InputBtn0");
				if ((UnityEngine.Object)(object)val6 != (UnityEngine.Object)null)
				{
					((Component)val6).gameObject.SetActive(true);
					StripComp(((Component)val6).gameObject, "Button");
					RawImage component = ((Component)val6).GetComponent<RawImage>();
					if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
					{
						((Graphic)component).color = new Color(0f, 0f, 0f, 0.25f);
						((Behaviour)component).enabled = true;
					}
					HideChild(val6, "OutlineMask");
					Transform val7 = val6.Find("KeyDisplay");
					if ((UnityEngine.Object)(object)val7 != (UnityEngine.Object)null)
					{
						val5 = ((Component)val7).GetComponent<KeyDisplay>();
					}
				}
				HideChild(val4, "InputBtn1");
				HideChild(val4, "GlyphContainer1");
				HideChild(val4, "GlyphContainer1 (1)");
			}
			if ((UnityEngine.Object)(object)val5 != (UnityEngine.Object)null)
			{
				BuildKeycapVisual(val5);
				_rows?.Add((val5, entry));
			}
			Button component2 = val.GetComponent<Button>();
			if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
			{
				((UnityEventBase)component2.onClick).RemoveAllListeners();
				string capName = label;
				ConfigEntry<KeyCode> capEntry = entry;
				KeyDisplay capKd = val5;
				((UnityEvent)component2.onClick).AddListener((UnityAction)((Action)delegate
				{
					BeginCapture(capName, capEntry, capKd);
				}));
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val8 = new BepInExWarningLogInterpolatedStringHandler(18, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val8).AppendLiteral("[CP] BuildRow '");
				((BepInExLogInterpolatedStringHandler)val8).AppendFormatted<string>(label);
				((BepInExLogInterpolatedStringHandler)val8).AppendLiteral("': ");
				((BepInExLogInterpolatedStringHandler)val8).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val8);
		}
	}

	private static void BuildSliderRow(GameObject prefab, Transform parent, string label, Func<float> get, Action<float> set)
	{
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Expected O, but got Unknown
		try
		{
			if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] sliderPrefab null");
				return;
			}
			GameObject val = Object.Instantiate<GameObject>(prefab, parent);
			((Object)val).name = "CP_Slider_" + label.Replace(" ", "").Replace("/", "");
			val.SetActive(true);
			SliderSetting componentInChildren = val.GetComponentInChildren<SliderSetting>(true);
			Slider val2 = (((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null && (UnityEngine.Object)(object)componentInChildren.slider != (UnityEngine.Object)null) ? componentInChildren.slider : val.GetComponentInChildren<Slider>(true));
			TMP_InputField val3 = (((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null) ? componentInChildren.valueText : null);
			StripComp(val, "SliderSetting");
			StripMyButtons(val);
			Transform val4 = val.transform.Find("Content/Left/Text (TMP)");
			TMP_Text val5 = (((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null) ? ((Component)val4).GetComponent<TMP_Text>() : null);
			if ((UnityEngine.Object)(object)val5 == (UnityEngine.Object)null)
			{
				foreach (TMP_Text componentsInChild in val.GetComponentsInChildren<TMP_Text>(true))
				{
					if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null && (UnityEngine.Object)(object)((Component)componentsInChild).gameObject == (UnityEngine.Object)(object)((Component)val3).gameObject)
					{
						continue;
					}
					val5 = componentsInChild;
					break;
				}
			}
			if ((UnityEngine.Object)(object)val5 != (UnityEngine.Object)null)
			{
				StripLocalizer(((Component)val5).gameObject);
				val5.text = label;
			}
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
			{
				((Selectable)val3).interactable = false;
			}
			if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] slider row has no Slider");
				return;
			}
			val2.wholeNumbers = false;
			val2.minValue = 0f;
			val2.maxValue = 1f;
			val2.value = Mathf.Clamp01(get());
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
			{
				val3.text = Mathf.RoundToInt(get() * 100f) + "%";
			}
			((UnityEventBase)val2.onValueChanged).RemoveAllListeners();
			Action<float> setLocal = set;
			TMP_InputField vtLocal = val3;
			((UnityEvent<float>)(object)val2.onValueChanged).AddListener((UnityAction<float>)((Action<float>)delegate(float v)
			{
				setLocal(v);
				if ((UnityEngine.Object)(object)vtLocal != (UnityEngine.Object)null)
				{
					vtLocal.text = Mathf.RoundToInt(v * 100f) + "%";
				}
			}));
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val6 = new BepInExWarningLogInterpolatedStringHandler(24, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val6).AppendLiteral("[CP] BuildSliderRow '");
				((BepInExLogInterpolatedStringHandler)val6).AppendFormatted<string>(label);
				((BepInExLogInterpolatedStringHandler)val6).AppendLiteral("': ");
				((BepInExLogInterpolatedStringHandler)val6).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val6);
		}
	}

	private static void BuildToggleRow(GameObject prefab, Transform parent, string label, Func<bool> get, Action<bool> set)
	{
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Expected O, but got Unknown
		try
		{
			if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] sliderPrefab null (toggle)");
				return;
			}
			GameObject val = Object.Instantiate<GameObject>(prefab, parent);
			((Object)val).name = "CP_Toggle_" + label.Replace(" ", "").Replace("/", "");
			val.SetActive(true);
			SliderSetting componentInChildren = val.GetComponentInChildren<SliderSetting>(true);
			Slider val2 = (((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null && (UnityEngine.Object)(object)componentInChildren.slider != (UnityEngine.Object)null) ? componentInChildren.slider : val.GetComponentInChildren<Slider>(true));
			TMP_InputField val3 = (((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null) ? componentInChildren.valueText : null);
			StripComp(val, "SliderSetting");
			StripMyButtons(val);
			Transform val4 = val.transform.Find("Content/Left/Text (TMP)");
			TMP_Text val5 = (((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null) ? ((Component)val4).GetComponent<TMP_Text>() : null);
			if ((UnityEngine.Object)(object)val5 == (UnityEngine.Object)null)
			{
				foreach (TMP_Text componentsInChild in val.GetComponentsInChildren<TMP_Text>(true))
				{
					if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null && (UnityEngine.Object)(object)((Component)componentsInChild).gameObject == (UnityEngine.Object)(object)((Component)val3).gameObject)
					{
						continue;
					}
					val5 = componentsInChild;
					break;
				}
			}
			if ((UnityEngine.Object)(object)val5 != (UnityEngine.Object)null)
			{
				StripLocalizer(((Component)val5).gameObject);
				val5.text = label;
			}
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
			{
				((Selectable)val3).interactable = false;
			}
			if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] toggle row has no Slider");
				return;
			}
			val2.wholeNumbers = true;
			val2.minValue = 0f;
			val2.maxValue = 1f;
			val2.value = (get() ? 1f : 0f);
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
			{
				val3.text = (get() ? "On" : "Off");
			}
			((UnityEventBase)val2.onValueChanged).RemoveAllListeners();
			Action<bool> setLocal = set;
			TMP_InputField vtLocal = val3;
			((UnityEvent<float>)(object)val2.onValueChanged).AddListener((UnityAction<float>)((Action<float>)delegate(float v)
			{
				bool flag2 = v >= 0.5f;
				setLocal(flag2);
				if ((UnityEngine.Object)(object)vtLocal != (UnityEngine.Object)null)
				{
					vtLocal.text = (flag2 ? "On" : "Off");
				}
			}));
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val6 = new BepInExWarningLogInterpolatedStringHandler(24, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val6).AppendLiteral("[CP] BuildToggleRow '");
				((BepInExLogInterpolatedStringHandler)val6).AppendFormatted<string>(label);
				((BepInExLogInterpolatedStringHandler)val6).AppendLiteral("': ");
				((BepInExLogInterpolatedStringHandler)val6).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val6);
		}
	}

	private static void BuildEnumToggleRow(GameObject prefab, Transform parent, string label, Func<bool> get, Action<bool> set)
	{
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Expected O, but got Unknown
		try
		{
			if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] enumPrefab null (toggle)");
				return;
			}
			GameObject val = Object.Instantiate<GameObject>(prefab, parent);
			((Object)val).name = "CP_Enum_" + label.Replace(" ", "").Replace("/", "");
			val.SetActive(true);
			EnumSetting componentInChildren = val.GetComponentInChildren<EnumSetting>(true);
			object obj;
			if (!((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null))
			{
				obj = null;
			}
			else
			{
				TextMeshProUGUI valueText2 = componentInChildren.valueText;
				obj = ((valueText2 != null) ? ((Il2CppObjectBase)valueText2).TryCast<TMP_Text>() : null);
			}
			TMP_Text valueText = (TMP_Text)obj;
			object obj2;
			if (!((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null))
			{
				obj2 = null;
			}
			else
			{
				TextMeshProUGUI settingName = ((BetterSetting)componentInChildren).settingName;
				obj2 = ((settingName != null) ? ((Il2CppObjectBase)settingName).TryCast<TMP_Text>() : null);
			}
			TMP_Text val2 = (TMP_Text)obj2;
			List<Button> list = new List<Button>();
			foreach (Button componentsInChild in val.GetComponentsInChildren<Button>(true))
			{
				if ((UnityEngine.Object)(object)componentsInChild != (UnityEngine.Object)null && !list.Contains(componentsInChild))
				{
					list.Add(componentsInChild);
				}
			}
			foreach (MyButton componentsInChild2 in val.GetComponentsInChildren<MyButton>(true))
			{
				if (!((UnityEngine.Object)(object)componentsInChild2 == (UnityEngine.Object)null))
				{
					Button button = componentsInChild2.GetButton();
					if ((UnityEngine.Object)(object)button != (UnityEngine.Object)null && !list.Contains(button))
					{
						list.Add(button);
					}
				}
			}
			if ((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null)
			{
				Object.DestroyImmediate((UnityEngine.Object)(object)componentInChildren);
			}
			if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
			{
				Transform val3 = val.transform.Find("Content/Left/Text (TMP)");
				val2 = (((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null) ? ((Component)val3).GetComponent<TMP_Text>() : null);
			}
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
			{
				StripLocalizer(((Component)val2).gameObject);
				val2.text = label;
			}
			Render();
			Action<bool> setLocal = set;
			foreach (Button item in list)
			{
				((UnityEventBase)item.onClick).RemoveAllListeners();
			}
			foreach (Button item2 in list)
			{
				string text = ((Object)item2).name ?? "";
				if (text.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					((UnityEvent)item2.onClick).AddListener((UnityAction)((Action)delegate
					{
						setLocal(obj: false);
						Render();
					}));
				}
				else if (text.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					((UnityEvent)item2.onClick).AddListener((UnityAction)((Action)delegate
					{
						setLocal(obj: true);
						Render();
					}));
				}
			}
			void Render()
			{
				if ((UnityEngine.Object)(object)valueText != (UnityEngine.Object)null)
				{
					StripLocalizer(((Component)valueText).gameObject);
					valueText.text = (get() ? "On" : "Off");
				}
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val4 = new BepInExWarningLogInterpolatedStringHandler(28, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[CP] BuildEnumToggleRow '");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(label);
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("': ");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val4);
		}
	}

	private static void HideChild(Transform parent, string name)
	{
		Transform val = parent.Find(name);
		if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
		{
			((Component)val).gameObject.SetActive(false);
		}
	}

	private static void StripComp(GameObject go, string typeName)
	{
		foreach (Component component in go.GetComponents<Component>())
		{
			if (!((UnityEngine.Object)(object)component == (UnityEngine.Object)null))
			{
				string name;
				try
				{
				name = ((Object)component).GetIl2CppType().Name;
			}
			catch
			{
				continue;
			}
			if (name == typeName)
				{
					Object.DestroyImmediate((UnityEngine.Object)(object)component);
				}
			}
		}
	}

	private static void StripMyButtons(GameObject go)
	{
		foreach (Component component in go.GetComponents<Component>())
		{
			if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
			{
				continue;
			}
			try
			{
				if ((UnityEngine.Object)(object)((Il2CppObjectBase)component).TryCast<MyButton>() != (UnityEngine.Object)null)
				{
					Object.DestroyImmediate((UnityEngine.Object)(object)component);
				}
			}
			catch
			{
			}
		}
	}

	private static void CaptureKeycapFromPrefab(GameObject prefab)
	{
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		if (_kcHas || (UnityEngine.Object)(object)prefab == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			Transform val = prefab.transform.Find("Content/Right/InteractSetting/InputBtn0");
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] prefab InputBtn0 not found");
				return;
			}
			RawImage component = ((Component)val).GetComponent<RawImage>();
			Transform val2 = val.Find("KeyDisplay");
			KeyDisplay val3 = (((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? ((Component)val2).GetComponent<KeyDisplay>() : null);
			RawImage val4 = (((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null) ? val3.background : null);
			if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
			{
				_kcKdTex = val4.texture;
				_kcKdColor = ((Graphic)val4).color;
			}
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
			{
				_kcBtnTex = component.texture;
				_kcBtnColor = ((Graphic)component).color;
			}
			if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null && (UnityEngine.Object)(object)val3.text != (UnityEngine.Object)null)
			{
				_kcTextColor = ((Graphic)val3.text).color;
			}
			if (((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null && (UnityEngine.Object)(object)val4.texture != (UnityEngine.Object)null) || ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && (UnityEngine.Object)(object)component.texture != (UnityEngine.Object)null))
			{
				_kcHas = true;
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val5 = new BepInExWarningLogInterpolatedStringHandler(30, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val5).AppendLiteral("[CP] CaptureKeycapFromPrefab: ");
				((BepInExLogInterpolatedStringHandler)val5).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val5);
		}
	}

	internal static void PollKeycapCapture()
	{
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		if (_kcHas || (UnityEngine.Object)(object)_sm == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			Transform controlContent = _sm.controlContent;
			if ((UnityEngine.Object)(object)controlContent == (UnityEngine.Object)null || !((Component)controlContent).gameObject.activeInHierarchy)
			{
				return;
			}
			Il2CppArrayBase<KeyDisplay> componentsInChildren = ((Component)controlContent).GetComponentsInChildren<KeyDisplay>(true);
			if (componentsInChildren == null)
			{
				return;
			}
			foreach (KeyDisplay item in componentsInChildren)
			{
				if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null || !((Component)item).gameObject.activeInHierarchy)
				{
					continue;
				}
				RawImage background = item.background;
				if (!((UnityEngine.Object)(object)background != (UnityEngine.Object)null) || !((UnityEngine.Object)(object)background.texture != (UnityEngine.Object)null))
				{
					continue;
				}
				_kcKdTex = background.texture;
				_kcKdColor = ((Graphic)background).color;
				if ((UnityEngine.Object)(object)item.text != (UnityEngine.Object)null)
				{
					_kcTextColor = ((Graphic)item.text).color;
				}
				_kcHas = true;
				RepaintOurRows();
				break;
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(24, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[CP] PollKeycapCapture: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val);
		}
	}

	private static void RepaintOurRows()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (_rows == null)
		{
			return;
		}
		foreach (var row in _rows)
		{
			try
			{
				if ((UnityEngine.Object)(object)row.kd != (UnityEngine.Object)null)
				{
					row.kd.SetKey(row.entry.Value);
					ApplyKeycapVisual(row.kd);
				}
			}
			catch
			{
			}
		}
	}

	private static void CaptureKeycapTemplate()
	{
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Expected O, but got Unknown
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		if (_kcHas || (UnityEngine.Object)(object)_sm == (UnityEngine.Object)null)
		{
			return;
		}
		bool flag = default(bool);
		try
		{
			Transform controlContent = _sm.controlContent;
			if ((UnityEngine.Object)(object)controlContent == (UnityEngine.Object)null)
			{
				return;
			}
			Il2CppArrayBase<KeyDisplay> componentsInChildren = ((Component)controlContent).GetComponentsInChildren<KeyDisplay>(true);
			if (componentsInChildren == null)
			{
				return;
			}
			foreach (KeyDisplay item in componentsInChildren)
			{
				if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
				{
					continue;
				}
				Transform parent = ((Component)item).transform.parent;
				RawImage val = (((UnityEngine.Object)(object)parent != (UnityEngine.Object)null) ? ((Component)parent).GetComponent<RawImage>() : null);
				RawImage background = item.background;
				if (!_kcHas && (((UnityEngine.Object)(object)background != (UnityEngine.Object)null && (UnityEngine.Object)(object)background.texture != (UnityEngine.Object)null) || ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && (UnityEngine.Object)(object)val.texture != (UnityEngine.Object)null)))
				{
					if ((UnityEngine.Object)(object)background != (UnityEngine.Object)null)
					{
						_kcKdTex = background.texture;
						_kcKdColor = ((Graphic)background).color;
					}
					if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
					{
						_kcBtnTex = val.texture;
						_kcBtnColor = ((Graphic)val).color;
					}
					if ((UnityEngine.Object)(object)item.text != (UnityEngine.Object)null)
					{
						_kcTextColor = ((Graphic)item.text).color;
					}
					_kcHas = true;
				}
			}
			if (!_kcHas)
			{
				BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(43, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[CP] no textured keycap among ");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(componentsInChildren.Length);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" KeyDisplays.");
				}
				Log.LogWarning(val2);
			}
		}
		catch (Exception ex)
		{
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(28, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[CP] CaptureKeycapTemplate: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val2);
		}
	}

	private static Sprite CapSprite()
	{
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		if (_capTried)
		{
			return _capSprite;
		}
		_capTried = true;
		try
		{
			Il2CppArrayBase<Sprite> val = Resources.FindObjectsOfTypeAll<Sprite>();
			if (val == null)
			{
				Log.LogWarning((object)"[CP] no sprites loaded");
				return null;
			}
			Sprite val2 = null;
			foreach (Sprite item in val)
			{
				if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
				{
					continue;
				}
				Vector4 border;
				try
				{
					border = item.border;
				}
				catch
				{
					continue;
				}
				if (!(border == Vector4.zero))
				{
					string text = ((Object)item).name ?? "";
					string text2 = text.ToLowerInvariant();
					bool flag = text2.Contains("key") || text2.Contains("input") || text2.Contains("bind");
					bool flag2 = text2.Contains("round") || text2.Contains("button") || text2.Contains("btn") || text2.Contains("cell") || text2.Contains("box") || text2.Contains("frame") || text2.Contains("panel") || text2.Contains("bg") || text2.Contains("back");
					if (flag)
					{
						val2 = item;
						break;
					}
					if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null && flag2)
					{
						val2 = item;
					}
				}
			}
			_capSprite = val2;
			if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
			{
				Log.LogWarning((object)"[CP] no rounded 9-slice sprite found — keycaps stay square");
			}
		}
		catch (Exception ex)
		{
			bool flag3 = default(bool);
			BepInExWarningLogInterpolatedStringHandler val3 = new BepInExWarningLogInterpolatedStringHandler(16, 1, out flag3);
			if (flag3)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("[CP] CapSprite: ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val3);
		}
		return _capSprite;
	}

	private static Image AddImg(Transform parent, string name, Sprite sprite, Color col, float inset)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		RectTransform val2 = val.AddComponent<RectTransform>();
		((Transform)val2).SetParent(parent, false);
		val2.anchorMin = Vector2.zero;
		val2.anchorMax = Vector2.one;
		val2.offsetMin = new Vector2(inset, inset);
		val2.offsetMax = new Vector2(0f - inset, 0f - inset);
		Image val3 = val.AddComponent<Image>();
		val3.sprite = sprite;
		((Graphic)val3).color = col;
		if ((UnityEngine.Object)(object)sprite != (UnityEngine.Object)null)
		{
			val3.type = (Image.Type)1;
		}
		((Graphic)val3).raycastTarget = false;
		return val3;
	}

	private static void BuildKeycapVisual(KeyDisplay kd)
	{
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Expected O, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)kd == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			Transform transform = ((Component)kd).transform;
			if (!((UnityEngine.Object)(object)transform.Find("CP_CapDark") != (UnityEngine.Object)null))
			{
				Sprite sprite = CapSprite();
				if ((UnityEngine.Object)(object)kd.background != (UnityEngine.Object)null)
				{
					((Behaviour)kd.background).enabled = false;
				}
				AddImg(transform, "CP_BarA", sprite, new Color(0f, 0f, 0f, 0.15f), -4f);
				AddImg(transform, "CP_BarB", sprite, new Color(0f, 0f, 0f, 0.15f), -2f);
				AddImg(transform, "CP_CapWhite", sprite, Color.white, 0f);
				AddImg(transform, "CP_CapDark", sprite, new Color(0.12f, 0.13f, 0.15f, 1f), 2.5f);
				if ((UnityEngine.Object)(object)kd.text != (UnityEngine.Object)null)
				{
					((Graphic)kd.text).color = Color.white;
					((TMP_Text)kd.text).transform.SetAsLastSibling();
				}
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(24, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[CP] BuildKeycapVisual: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val);
		}
	}

	private static void ApplyKeycapVisual(KeyDisplay kd)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)kd == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			if ((UnityEngine.Object)(object)kd.background != (UnityEngine.Object)null)
			{
				((Behaviour)kd.background).enabled = false;
			}
			if ((UnityEngine.Object)(object)kd.text != (UnityEngine.Object)null)
			{
				((Graphic)kd.text).color = Color.white;
				((TMP_Text)kd.text).transform.SetAsLastSibling();
			}
		}
		catch
		{
		}
	}

	private static void ShowTab(GameObject ourTab, MyButtonTabs ourMbt, List<MyButtonTabs> others)
	{
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			foreach (MyButtonTabs other in others)
			{
				if ((UnityEngine.Object)(object)other.associatedContent != (UnityEngine.Object)null)
				{
					other.associatedContent.SetActive(false);
				}
				if ((UnityEngine.Object)(object)other.background != (UnityEngine.Object)null)
				{
					((Graphic)other.background).color = other.defaultColor;
				}
			}
			ourTab.SetActive(true);
			if ((UnityEngine.Object)(object)ourMbt != (UnityEngine.Object)null && (UnityEngine.Object)(object)ourMbt.background != (UnityEngine.Object)null)
			{
				((Graphic)ourMbt.background).color = ourMbt.selectedColor;
			}
			CaptureKeycapTemplate();
			if (_rows == null)
			{
				return;
			}
			foreach (var row in _rows)
			{
				try
				{
					if (!((UnityEngine.Object)(object)row.kd == (UnityEngine.Object)null))
					{
						row.kd.SetKey(row.entry.Value);
						ApplyKeycapVisual(row.kd);
					}
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(14, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[CP] ShowTab: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val);
		}
	}

	private static void HideTab(GameObject ourTab, MyButtonTabs ourMbt)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ourTab.SetActive(false);
			if ((UnityEngine.Object)(object)ourMbt != (UnityEngine.Object)null && (UnityEngine.Object)(object)ourMbt.background != (UnityEngine.Object)null)
			{
				((Graphic)ourMbt.background).color = ourMbt.defaultColor;
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(14, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[CP] HideTab: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val);
		}
	}

	private static void StripLocalizer(GameObject go)
	{
		try
		{
			foreach (Component component in go.GetComponents<Component>())
			{
				if (!((UnityEngine.Object)(object)component == (UnityEngine.Object)null))
				{
					string name;
					try
					{
				name = ((Object)component).GetIl2CppType().Name;
					}
					catch
					{
						continue;
					}
					if (name == "LocalizeStringEvent")
					{
						Object.Destroy((UnityEngine.Object)(object)component);
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void BeginCapture(string name, ConfigEntry<KeyCode> entry, KeyDisplay kd)
	{
		_capturing = true;
		Hotkeys.Capturing = true;
		_capName = name;
		_capEntry = entry;
		_capKd = kd;
		_capStartFrame = Time.frameCount;
		if ((UnityEngine.Object)(object)kd != (UnityEngine.Object)null && (UnityEngine.Object)(object)kd.text != (UnityEngine.Object)null)
		{
			((TMP_Text)kd.text).text = "?";
		}
	}

	internal static void TickCapture()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if (!_capturing || Time.frameCount == _capStartFrame)
		{
			return;
		}
		if (Input.GetKeyDown((KeyCode)27))
		{
			EndCapture(_capEntry.Value);
			return;
		}
		KeyCode[] allKeyCodes = AllKeyCodes;
		foreach (KeyCode val in allKeyCodes)
		{
			if ((int)val != 0 && ((int)val < 323 || (int)val > 329) && Input.GetKeyDown(val))
			{
				_capEntry.Value = val;
				EndCapture(val);
				break;
			}
		}
	}

	private static void EndCapture(KeyCode k)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_capKd != (UnityEngine.Object)null)
		{
			_capKd.SetKey(k);
			ApplyKeycapVisual(_capKd);
		}
		_capturing = false;
		Hotkeys.Capturing = false;
		Hotkeys.LastCaptureFrame = Time.frameCount;
		_capEntry = null;
		_capKd = null;
		_capName = null;
	}
}
