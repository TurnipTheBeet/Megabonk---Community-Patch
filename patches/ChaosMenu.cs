using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;
using BepInEx.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class ChaosMenu
{
	private static readonly (int id, string name, bool def)[] Stats = new(int, string, bool)[10]
	{
		(1, "Health Regen", false),
		(2, "Shield", false),
		(3, "Thorns", false),
		(4, "Armor", false),
		(5, "Evasion", false),
		(11, "Projectile Speed", false),
		(24, "Knockback", false),
		(29, "Pickup Range", false),
		(10, "Duration", true),
		(25, "Movement Speed", true)
	};

	private static ConfigEntry<bool>[] _allow;

	private static ConfigFile _cfg;

	internal static bool Visible;

	private const float WinW = 260f;

	private const float PadX = 12f;

	private const float LineH = 24f;

	private static readonly GuiWindowFrame _frame = new GuiWindowFrame(new Vector2(40f, 120f)).Persist("ChaosMenu");

	private static float _lastWinH;

	private static float WinHeight()
	{
		return 56f + (float)Stats.Length * 26f + 8f;
	}

	internal static void HandleInput()
	{
		_frame.HandleInput(260f, (_lastWinH > 0f) ? _lastWinH : WinHeight(), 28f);
	}

	internal static void Init(ConfigFile cfg)
	{
		_cfg = cfg;
		_allow = new ConfigEntry<bool>[Stats.Length];
		for (int i = 0; i < Stats.Length; i++)
		{
			_allow[i] = cfg.Bind<bool>("ChaosStats", Stats[i].name, Stats[i].def, "Allow '" + Stats[i].name + "' to appear in the Chaos Tome / Gamble / Shrine pool.");
		}
		ApplyAllToBlacklist();
	}

	private static void ApplyAllToBlacklist()
	{
		for (int i = 0; i < Stats.Length; i++)
		{
			if (_allow[i].Value)
			{
				Plugin.BlacklistedStats.Remove(Stats[i].id);
				Plugin.BlacklistedShrineStats.Remove(Stats[i].id);
			}
			else
			{
				Plugin.BlacklistedStats.Add(Stats[i].id);
				Plugin.BlacklistedShrineStats.Add(Stats[i].id);
			}
		}
	}

	internal static void Toggle()
	{
		Visible = !Visible;
	}

	private static void SetAllowed(int idx, bool allowed)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (_allow[idx].Value == allowed)
		{
			return;
		}
		_allow[idx].Value = allowed;
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
		int item = Stats[idx].id;
		if (allowed)
		{
			Plugin.BlacklistedStats.Remove(item);
			Plugin.BlacklistedShrineStats.Remove(item);
		}
		else
		{
			Plugin.BlacklistedStats.Add(item);
			Plugin.BlacklistedShrineStats.Add(item);
		}
		try
		{
			var upgradableStatsChaosAndGamble = EncounterUtility.upgradableStatsChaosAndGamble;
			if (upgradableStatsChaosAndGamble != null)
			{
				EStat val = (EStat)item;
				if (allowed)
				{
					if (!upgradableStatsChaosAndGamble.Contains(val))
					{
						upgradableStatsChaosAndGamble.Add(val);
					}
				}
				else if (upgradableStatsChaosAndGamble.Contains(val))
				{
					upgradableStatsChaosAndGamble.Remove(val);
				}
			}
		}
		catch
		{
		}
		try
		{
			var upgradableStatsShrines = EncounterUtility.upgradableStatsShrines;
			if (upgradableStatsShrines != null)
			{
				EStat val = (EStat)item;
				if (allowed)
				{
					if (!upgradableStatsShrines.Contains(val))
					{
						upgradableStatsShrines.Add(val);
					}
				}
				else if (upgradableStatsShrines.Contains(val))
				{
					upgradableStatsShrines.Remove(val);
				}
			}
		}
		catch
		{
		}
	}

	internal static void Draw()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		float num = (_lastWinH = WinHeight());
		Matrix4x4 old = _frame.Begin();
		float x = _frame.Pivot.x;
		float y = _frame.Pivot.y;
		UiTheme.Backdrop(new Rect(x, y, 260f, num), "ChaosMenu");
		GUI.Box(new Rect(x, y, 260f, num), "Chaos Tome / Shrine Stats");
		float num2 = 236f;
		float num3 = x + 12f;
		float num4 = y + 24f + 2f;
		GUI.Label(new Rect(num3, num4, num2, 24f), "Toggle stats in Chaos/Gamble/Shrines:");
		num4 += 24f;
		GUIStyle button = GUI.skin.button;
		for (int i = 0; i < Stats.Length; i++)
		{
			bool value = _allow[i].Value;
			bool flag = GUI.Toggle(new Rect(num3, num4, num2, 24f), value, (value ? "ON   " : "OFF  ") + Stats[i].name, button);
			if (flag != value)
			{
				SetAllowed(i, flag);
			}
			num4 += 26f;
		}
		_frame.End(old);
		_frame.DrawGrip(260f, num);
	}
}
