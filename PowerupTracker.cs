using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Utility;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace MegabonkCommunityPatch;

internal static class PowerupTracker
{
    private static readonly (EStatusEffect id, string name, string color)[] Buffs =
    {
        (EStatusEffect.Haste,           "HASTE",          "#50FF50"),
        (EStatusEffect.Rage,            "RAGE",           "#FF5050"),
        (EStatusEffect.Shield,          "SHIELD",         "#50A0FF"),
        (EStatusEffect.Stonks,          "STONKS",         "#FFD700"),
        (EStatusEffect.TimeFreeze,      "TIME FREEZE",    "#D0A0FF"),
        (EStatusEffect.Invulnerability, "INVULNERABLE",    "#FFA0FF"),
    };

    private static readonly GuiWindowFrame _frame = new GuiWindowFrame(new Vector2(20f, 200f)).Persist("PowerupTracker");

    internal static bool Visible = true;

    private static float _lastH;

    internal static void HandleInput()
    {
        _frame.HandleInput(160f, _lastH > 0 ? _lastH : 40f, 20f);
    }

    internal static void Toggle()
    {
        Visible = !Visible;
    }

    internal static void Draw()
    {
        if (!Visible) return;

        var inv = GameManager.Instance?.GetPlayerInventory();
        var pse = inv?.statusEffects;
        var active = pse?.statusEffects;
        if (active == null || active.Count == 0) return;

        // Use MyTime.runTimer as the time base — the game's StatusEffect constructor
        // stores expirationTime relative to MyTime.time (offset +4 from MyTime singleton).
        // Time.time drift from MyTime.time causes the displayed remaining time to be wrong.
        var now = MyTime.time;

        var lines = new List<(string name, float remaining, string color)>();

        foreach (var kv in active)
        {
            EStatusEffect type = kv.Key;
            StatusEffect fx = kv.Value;
            if (fx == null) continue;

            var remaining = fx.expirationTime - now;
            if (remaining <= 0f) continue;

            string name = null;
            string color = "#E0E0F0";
            foreach (var b in Buffs)
            {
                if (b.id == type)
                {
                    name = b.name;
                    color = b.color;
                    break;
                }
            }
            if (name == null)
                name = type.ToString().ToUpperInvariant();

            lines.Add((name, remaining, color));
        }

        if (lines.Count == 0) return;

        float winW = 160f;
        float lineH = 20f;
        float padY = 4f;
        float winH = padY + lines.Count * lineH + 4f;
        _lastH = winH;

        var old = _frame.Begin();
        float x = _frame.Pivot.x;
        float y = _frame.Pivot.y;

        UiTheme.Backdrop(new Rect(x, y, winW, winH), "PowerupTracker");

        float cx = x + 10f;
        float cy = y + padY;

        for (int i = 0; i < lines.Count; i++)
        {
            var (name, remaining, color) = lines[i];

            int secs = Mathf.Max(0, Mathf.RoundToInt(remaining));
            string timeStr = $"{secs}s";

            var barColor = ParseColor(color);
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(barColor.r, barColor.g, barColor.b, 0.15f);
            GUI.Box(new Rect(cx - 4f, cy, winW - 12f, lineH), "");
            GUI.backgroundColor = prevBg;

            GUI.contentColor = ParseColor(color);
            GUI.Label(new Rect(cx, cy, 90f, lineH), name);
            GUI.contentColor = new Color(0.7f, 0.7f, 0.8f);
            GUI.Label(new Rect(cx + 94f, cy, 55f, lineH), timeStr);
            GUI.contentColor = Color.white;

            cy += lineH;
        }

        _frame.End(old);
        _frame.DrawGrip(winW, winH);
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