using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class UiTheme
{
	private static Dictionary<string, ConfigEntry<float>> _opacities = new();
	private static Texture2D _tex;

	internal static void Init(ConfigFile cfg)
	{
		_opacities["ModMenu"] = cfg.Bind<float>("UI", "ModMenuOpacity", 0.85f, "Background opacity of the F1 mod menu window.");
		_opacities["ChaosMenu"] = cfg.Bind<float>("UI", "ChaosMenuOpacity", 0.85f, "Background opacity of the Chaos/Shrine stat menu.");
		_opacities["MapScanner"] = cfg.Bind<float>("UI", "MapScannerOpacity", 0.85f, "Background opacity of the Map Scanner window.");
		_opacities["AutoUpgrade"] = cfg.Bind<float>("UI", "AutoUpgradeOpacity", 0.85f, "Background opacity of the Auto-Upgrade log window.");
		_opacities["PowerupTracker"] = cfg.Bind<float>("UI", "PowerupTrackerOpacity", 0.5f, "Background opacity of the Powerup Tracker overlay.");
		_opacities["ChestOdds"] = cfg.Bind<float>("UI", "ChestOddsOpacity", 0.5f, "Background opacity of the Chest Odds overlay.");
		_opacities["Notices"] = cfg.Bind<float>("UI", "NoticesOpacity", 0.85f, "Background opacity of the notices banner.");
		_opacities["Tooltip"] = cfg.Bind<float>("UI", "TooltipOpacity", 0.94f, "Background opacity of the leaderboard tooltip.");
	}

	internal static float GetOpacity(string key) =>
		_opacities.TryGetValue(key, out var entry) ? entry.Value : 0.85f;

	internal static void SetOpacity(string key, float value)
	{
		if (_opacities.TryGetValue(key, out var entry))
			entry.Value = Mathf.Clamp(value, 0f, 1f);
	}

	private static Texture2D Tex()
	{
		if ((Object)_tex == null)
		{
			_tex = new Texture2D(1, 1) { hideFlags = (HideFlags)61 };
			_tex.SetPixel(0, 0, Color.white);
			_tex.Apply();
		}
		return _tex;
	}

	internal static void Backdrop(Rect r, string key)
	{
		Color color = GUI.color;
		GUI.color = new Color(0.07f, 0.07f, 0.11f, GetOpacity(key));
		GUI.DrawTexture(r, (Texture)Tex());
		GUI.color = color;
	}
}