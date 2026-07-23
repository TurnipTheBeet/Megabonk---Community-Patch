using System;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

namespace MegabonkCommunityPatch;

internal static class ChestOddsTracker
{
	private static readonly GuiWindowFrame _frame = new GuiWindowFrame(new Vector2(300f, 200f)).Persist("ChestOddsTracker");

	internal static bool Visible = false;

	private static float _lastLuck = -999f;
	private static float _cachedCommon;
	private static float _cachedRare;
	private static float _cachedEpic;
	private static float _cachedLegendary;

	internal static void HandleInput()
	{
		_frame.HandleInput(82f, 90f, 20f);
	}

	internal static void Toggle()
	{
		Visible = !Visible;
	}

	internal static void Draw()
	{
		if (!Visible) return;
		if (!PlayerStats.HasStats()) return;

		float luck = PlayerStats.GetStat((EStat)30);

		if (Math.Abs(luck - _lastLuck) > 0.001f)
		{
			CalculateOdds(luck);
			_lastLuck = luck;
		}

		float winW = 82f;
		float lineH = 20f;
		float padY = 2f;
		float winH = padY + 4 * lineH + 4f;

		var old = _frame.Begin();
		float x = _frame.Pivot.x;
		float y = _frame.Pivot.y;

		float cx = x + 6f;
		float cy = y + padY;

		UiTheme.Backdrop(new Rect(x, y, winW, winH), "ChestOdds");

		float total = _cachedCommon + _cachedRare + _cachedEpic + _cachedLegendary;
		if (total > 0f)
		{
			DrawPct(ref cy, cx, lineH, _cachedCommon / total * 100f,    "#8080FF");
			DrawPct(ref cy, cx, lineH, _cachedRare / total * 100f,      "#C050FF");
			DrawPct(ref cy, cx, lineH, _cachedEpic / total * 100f,      "#FF50C0");
			DrawPct(ref cy, cx, lineH, _cachedLegendary / total * 100f, "#FFD700");
		}

		_frame.End(old);
		_frame.DrawGrip(winW, winH);
	}

	private static void DrawPct(ref float cy, float cx, float lineH, float pct, string color)
	{
		GUI.contentColor = ParseColor(color);
		GUI.Label(new Rect(cx, cy, 70f, lineH), $"{pct:0.0}%");
		GUI.contentColor = Color.white;
		cy += lineH;
	}

	private static void CalculateOdds(float luck)
	{
		float common = 70f;
		float rare = 15f + luck * 2f;
		float epic = 3f + luck * 1.5f;
		float legendary = 1.5f + luck * 1f;

		float total = common + rare + epic + legendary;
		if (total > 0f)
		{
			_cachedCommon    = common / total;
			_cachedRare      = rare / total;
			_cachedEpic      = epic / total;
			_cachedLegendary = legendary / total;
		}
	}

	private static Color ParseColor(string hex)
	{
		if (hex.Length == 7 && hex[0] == '#')
			return new Color(
				Convert.ToByte(hex.Substring(1, 2), 16) / 255f,
				Convert.ToByte(hex.Substring(3, 2), 16) / 255f,
				Convert.ToByte(hex.Substring(5, 2), 16) / 255f);
		return new Color(0.8f, 0.8f, 0.9f);
	}
}