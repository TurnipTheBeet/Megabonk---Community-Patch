using BepInEx.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class Hotkeys
{
	internal static ConfigEntry<KeyCode> ModMenu;

	internal static ConfigEntry<KeyCode> DamageChart;

	internal static ConfigEntry<KeyCode> FastFall;

	internal static ConfigEntry<KeyCode> ChaosMenu;

	internal static ConfigEntry<KeyCode> MapScanner;

	internal static ConfigEntry<KeyCode> MapScanToggle;

	internal static ConfigEntry<KeyCode> SkipChest;

	internal static ConfigEntry<KeyCode> SmartTargeting;

	internal static ConfigEntry<KeyCode> AutoUpgrade;

	internal static ConfigEntry<KeyCode> AutoUpgradeLog;

	internal static ConfigEntry<KeyCode> EffectsOpacity;

	internal static ConfigEntry<KeyCode> GameSpeed;

	internal static ConfigEntry<KeyCode> PowerupTracker;

	internal static ConfigEntry<KeyCode> ChestOddsTracker;

	internal static ConfigEntry<bool> ModMenuEnabled;

	internal static bool Capturing = false;

	internal static int LastCaptureFrame = -1;

	internal static bool TypingPassword = false;

	internal static bool Suppressed => Capturing || TypingPassword || Time.frameCount == LastCaptureFrame;

	internal static void Init(ConfigFile cfg)
	{
		ModMenu = cfg.Bind<KeyCode>("Hotkeys", "ModMenu", (KeyCode)282, "Toggle the mod menu.");
		DamageChart = cfg.Bind<KeyCode>("Hotkeys", "DamageChart", (KeyCode)283, "Toggle the damage chart.");
		FastFall = cfg.Bind<KeyCode>("Hotkeys", "FastFall", (KeyCode)306, "Hold while airborne to fast-fall.");
		ChaosMenu = cfg.Bind<KeyCode>("Hotkeys", "ChaosMenu", (KeyCode)284, "Toggle the chaos stat-toggle menu.");
		MapScanner = cfg.Bind<KeyCode>("Hotkeys", "MapScanner", (KeyCode)285, "Toggle the map scanner window.");
		MapScanToggle = cfg.Bind<KeyCode>("Hotkeys", "MapScanToggle", (KeyCode)286, "Start / stop the map scan (auto-reroll until the map matches your criteria).");
		SkipChest = cfg.Bind<KeyCode>("Hotkeys", "SkipChest", (KeyCode)287, "Toggle Smart Skip Chest Animation (auto-skips when banishes are 0).");
		SmartTargeting = cfg.Bind<KeyCode>("Hotkeys", "PriorityTargeting", (KeyCode)288, "Toggle Priority Targeting (prioritise bosses/elites, then nearest killable).");
		AutoUpgrade = cfg.Bind<KeyCode>("Hotkeys", "AutoUpgrade", (KeyCode)289, "Toggle Scaling Auto-Upgrade (auto-pick level-up choices favouring scaling).");
		AutoUpgradeLog = cfg.Bind<KeyCode>("Hotkeys", "AutoUpgradeLog", (KeyCode)290, "Toggle the Scaling Auto-Upgrade log window (shows what it picked and over what).");
		EffectsOpacity = cfg.Bind<KeyCode>("Hotkeys", "EffectsOpacity", (KeyCode)292, "Toggle the game's Effects opacity (Settings > Effects) between 0% and 100%.");
		GameSpeed = cfg.Bind<KeyCode>("Hotkeys", "GameSpeed", (KeyCode)116, "Toggle game speed between 1x and 2x. Not a cheat — does not affect leaderboard submission.");
		PowerupTracker = cfg.Bind<KeyCode>("Hotkeys", "PowerupTracker", (KeyCode)294, "Toggle the active powerups tracker overlay.");
		ChestOddsTracker = cfg.Bind<KeyCode>("Hotkeys", "ChestOddsTracker", (KeyCode)295, "Toggle the chest odds tracker overlay.");
		ModMenuEnabled = cfg.Bind<bool>("Hotkeys", "ModMenuEnabled", false, "Enable the F1 mod menu. Off by default so it can't be opened accidentally.");
	}

	internal static (string label, ConfigEntry<KeyCode> entry)[] All()
	{
		return new(string, ConfigEntry<KeyCode>)[14]
		{
			("Mod Menu", ModMenu),
			("Damage Chart", DamageChart),
			("Fast Fall", FastFall),
			("Chaos Menu", ChaosMenu),
			("Map Scanner", MapScanner),
			("Map Scan Start/Stop", MapScanToggle),
			("Smart Skip Chest", SkipChest),
			("Priority Targeting", SmartTargeting),
			("Scaling Auto-Upgrade", AutoUpgrade),
			("Auto-Upgrade Log", AutoUpgradeLog),
			("Effects Opacity 0/100", EffectsOpacity),
			("Game Speed 1x/2x", GameSpeed),
			("Powerup Tracker", PowerupTracker),
			("Chest Odds Tracker", ChestOddsTracker)
		};
	}

	internal static string Pretty(KeyCode k)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		string result = (((int)k == 0) ? "None" : (((int)k - 303) switch
		{
			3 => "Left Ctrl", 
			2 => "Right Ctrl", 
			1 => "Left Shift", 
			0 => "Right Shift", 
			5 => "Left Alt", 
			4 => "Right Alt", 
			_ => ((KeyCode)k).ToString(), 
		}));
		if (1 == 0)
		{
		}
		return result;
	}
}
