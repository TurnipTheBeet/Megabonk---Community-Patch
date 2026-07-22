using System;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class SkipChestSync
{
	private const int Unset = -999;

	private static int _lastApplied = -999;

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
		}
	}

	internal static void Init(ConfigFile cfg)
	{
		_enabled = cfg.Bind<bool>("SkipChest", "Enabled", true, "Smart Skip Chest Animation: auto-skip the chest/level-up animation when banishes are 0, and show it again when Golden Rings refill banishes. Toggle in-game with the Smart Skip Chest hotkey.");
	}

	internal static void Toggle()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		Enabled = !Enabled;
		if (!Enabled)
		{
			Restore();
		}
		string text = (Enabled ? "ON" : "OFF");
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(29, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[SkipChest] Smart Skip Chest ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
		}
		log.LogInfo(val);
		Toast.Show("Smart Skip Chest: " + text, Enabled ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.6f, 0.45f, 1f));
	}

	private static void Restore()
	{
		try
		{
			SaveManager instance = SaveManager.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				ConfigSaveFile config = instance.config;
				obj = ((config != null) ? config.cfGameSettings : null);
			}
			CFGameSettings val = (CFGameSettings)obj;
			if (val != null)
			{
				val.skip_chest_animation = 0;
			}
		}
		catch
		{
		}
		_lastApplied = -999;
	}

	private static int _tickFrame;

	internal static void Tick()
	{
		// Only check banishes every 15 frames — SaveManager/GameManager/PlayerInventory
		// access every frame is expensive during dense swarms.
		if (++_tickFrame % 15 != 0) return;
		try
		{
			if (!Enabled)
			{
				_lastApplied = -999;
				return;
			}
			GameManager instance = GameManager.Instance;
			PlayerInventory val = ((instance != null) ? instance.GetPlayerInventory() : null);
			if (val == null)
			{
				_lastApplied = -999;
				return;
			}
			int num = ((val.banishes <= 0) ? 1 : 0);
			if (num != _lastApplied)
			{
				SaveManager instance2 = SaveManager.Instance;
				object obj;
				if (instance2 == null)
				{
					obj = null;
				}
				else
				{
					ConfigSaveFile config = instance2.config;
					obj = ((config != null) ? config.cfGameSettings : null);
				}
				CFGameSettings val2 = (CFGameSettings)obj;
				if (val2 != null)
				{
					val2.skip_chest_animation = num;
					_lastApplied = num;
				}
			}
		}
		catch (Exception ex)
		{
			HotErr.Once("SkipChestSync.Tick", ex);
		}
	}
}
