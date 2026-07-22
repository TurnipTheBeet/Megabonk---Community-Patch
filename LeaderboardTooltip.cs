using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace MegabonkCommunityPatch;

internal static class LeaderboardTooltip
{
    private static GUIStyle _boxStyle;
    private static GUIStyle _headerStyle;
    private static GUIStyle _labelStyle;
    private static GUIStyle _subLabelStyle;
    private static bool _init;

    private static readonly Color BgColor   = new(0.08f, 0.08f, 0.14f, 0.94f);
    private static readonly Color HeaderCol = new(0.85f, 0.75f, 1f, 1f);
    private static readonly Color ValueCol  = new(0.85f, 0.95f, 0.7f, 1f);
    private static readonly Color SubCol    = new(0.65f, 0.65f, 0.8f, 1f);
    private static readonly Color DimCol    = new(0.55f, 0.55f, 0.7f, 1f);

    private static int _hoveredSlot = -1;

    private static readonly Dictionary<string, List<string>> _cache = new();

    private static List<string> ParseList(string json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        if (_cache.TryGetValue(json, out var cached)) return cached;
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new();
            _cache[json] = list;
            return list;
        }
        catch { return new(); }
    }

    internal static void CheckHover(LeaderboardUiNew ui, int tab)
    {
        _hoveredSlot = -1;
        if (ui == null || ui.leaderboardEntries == null) return;
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) return;

        var rectT = ((Component)ui).GetComponent<RectTransform>();
        if (rectT == null) return;

        var mp = (Vector2)Input.mousePosition;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectT, mp)) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectT, mp, null, out local);
        Vector2 size = rectT.rect.size;
        float entryH = size.y / ui.leaderboardEntries.Count;
        float top = size.y * 0.5f;
        int slot = Mathf.FloorToInt((top - local.y) / entryH);

        if (slot >= 0 && slot < LeaderboardInjector.SlotRunData.Length)
        {
            string rd = LeaderboardInjector.SlotRunData[slot];
            if (!string.IsNullOrEmpty(rd))
                _hoveredSlot = slot;
        }
    }

    internal static void OnGUI()
    {
        if (_hoveredSlot < 0) return;

        string runDataJson = LeaderboardInjector.SlotRunData[_hoveredSlot];
        if (string.IsNullOrEmpty(runDataJson)) return;

        InitStyles();

        var run = ParseRunData(runDataJson);
        if (run == null) return;

        float w = 310f, lineH = 18f;
        float y = 12f;

        float h = 12f; // title
        if (run.Weapons.Count > 0) h += lineH + run.Weapons.Count * lineH;
        if (run.Tomes.Count > 0)   h += lineH + run.Tomes.Count * lineH;
        if (run.Items.Count > 0)   h += lineH + Mathf.Min(run.Items.Count, 12) * lineH + (run.Items.Count > 12 ? lineH : 0);
        h += lineH * 5;  // header + time + kills + level + gap
        h += lineH * Mathf.Min(run.StatsList.Count, 10);

        float x = Mathf.Min(Input.mousePosition.x + 16f, Screen.width - w - 8f);
        y = Mathf.Min(Input.mousePosition.y - 10f, Screen.height - h - 8f);
        if (y < 8f) y = 8f;

        var rect = new Rect(x, y, w, h);
        UiTheme.Backdrop(rect, "Tooltip");
        GUI.Box(rect, GUIContent.none, _boxStyle);

        // Title
        GUI.Label(new Rect(x + 8f, y + 4f, w, lineH), "<b>Run Details</b>", _headerStyle);
        y += lineH + 2f;

        // Time
        string timeStr = FormatTime(run.RunTime);
        GUI.Label(new Rect(x + 8f, y, w, lineH), $"<color=#{Col(SubCol)}>Time:</color>  {timeStr}", _labelStyle);
        y += lineH;

        // Kills
        string killsStr = run.Kills.ToString("N0");
        GUI.Label(new Rect(x + 8f, y, w, lineH), $"<color=#{Col(SubCol)}>Kills:</color>  {killsStr}   <color=#{Col(DimCol)}>Elites:</color> {run.EliteKills}   <color=#{Col(DimCol)}>Bosses:</color> {run.BossKills}", _labelStyle);
        y += lineH;

        // Level
        GUI.Label(new Rect(x + 8f, y, w, lineH), $"<color=#{Col(SubCol)}>Level:</color>  {run.Level}", _labelStyle);
        y += lineH + 2f;

        // Weapons
        if (run.Weapons.Count > 0)
        {
            GUI.Label(new Rect(x + 8f, y, w, lineH), "<color=#c8a0ff><b>Weapons</b></color>", _subLabelStyle);
            y += lineH;
            foreach (var w2 in run.Weapons)
            {
                GUI.Label(new Rect(x + 16f, y, w - 16f, lineH), $"<color=#{Col(ValueCol)}>{w2}</color>", _labelStyle);
                y += lineH;
            }
        }

        // Tomes
        if (run.Tomes.Count > 0)
        {
            GUI.Label(new Rect(x + 8f, y, w, lineH), "<color=#c8a0ff><b>Tomes</b></color>", _subLabelStyle);
            y += lineH;
            foreach (var t in run.Tomes)
            {
                GUI.Label(new Rect(x + 16f, y, w - 16f, lineH), $"<color=#{Col(ValueCol)}>{t}</color>", _labelStyle);
                y += lineH;
            }
        }

        // Items
        if (run.Items.Count > 0)
        {
            GUI.Label(new Rect(x + 8f, y, w, lineH), $"<color=#c8a0ff><b>Items ({run.Items.Count})</b></color>", _subLabelStyle);
            y += lineH;
            int shown = Mathf.Min(run.Items.Count, 12);
            for (int i = 0; i < shown; i++)
            {
                GUI.Label(new Rect(x + 16f, y, w - 16f, lineH), $"<color=#{Col(DimCol)}>{run.Items[i]}</color>", _labelStyle);
                y += lineH;
            }
            if (run.Items.Count > 12)
            {
                GUI.Label(new Rect(x + 16f, y, w - 16f, lineH), $"<color=#{Col(DimCol)}>...and {run.Items.Count - 12} more</color>", _labelStyle);
                y += lineH;
            }
        }

        // Stats
        if (run.StatsList.Count > 0)
        {
            GUI.Label(new Rect(x + 8f, y, w, lineH), "<color=#c8a0ff><b>Stats</b></color>", _subLabelStyle);
            y += lineH;
            foreach (var s in run.StatsList)
            {
                GUI.Label(new Rect(x + 16f, y, w - 16f, lineH), $"<color=#{Col(DimCol)}>{s.Key}:</color>  {s.Value}", _labelStyle);
                y += lineH;
            }
        }
    }

    private static RunInfo ParseRunData(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var run = new RunInfo();

            if (root.TryGetProperty("weapons", out var wp))
                foreach (var v in wp.EnumerateArray()) run.Weapons.Add(v.GetString() ?? "");
            if (root.TryGetProperty("tomes", out var tp))
                foreach (var v in tp.EnumerateArray()) run.Tomes.Add(v.GetString() ?? "");
            if (root.TryGetProperty("items", out var ip))
                foreach (var v in ip.EnumerateArray()) run.Items.Add(v.GetString() ?? "");

            if (root.TryGetProperty("runTime", out var rt))   run.RunTime = rt.GetSingle();
            if (root.TryGetProperty("kills", out var k))      run.Kills = k.GetInt32();
            if (root.TryGetProperty("eliteKills", out var ek)) run.EliteKills = ek.GetInt32();
            if (root.TryGetProperty("bossKills", out var bk)) run.BossKills = bk.GetInt32();
            if (root.TryGetProperty("level", out var lv))     run.Level = lv.GetInt32();

            if (root.TryGetProperty("stats", out var st))
            {
                AddStat(run, st, "HP",          "hp");
                AddStat(run, st, "Damage",      "damage");
                AddStat(run, st, "Armor",       "armor");
                AddStat(run, st, "Speed",       "speed");
                AddStat(run, st, "Crit Chance", "critChance");
                AddStat(run, st, "Lifesteal",   "lifesteal");
                AddStat(run, st, "Evasion",     "evasion");
                AddStat(run, st, "Luck",        "luck");
                AddStat(run, st, "Proj Speed",  "projSpeed");
                AddStat(run, st, "Size",        "size");
            }

            return run;
        }
        catch { return null; }
    }

    private static void AddStat(RunInfo run, JsonElement stats, string label, string key)
    {
        if (stats.TryGetProperty(key, out var val))
        {
            float f = val.GetSingle();
            string display = key.Contains("Chance") || key.Contains("lifesteal") || key.Contains("evasion")
                ? $"{f:P0}" : f.ToString("F1");
            run.StatsList.Add(new(label, display));
        }
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)seconds % 60;
        return $"{m}:{s:D2}";
    }

    private static string Col(Color c) => $"{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";

    private static void InitStyles()
    {
        if (_init) return;
        _init = true;

        _boxStyle = new GUIStyle(GUI.skin.box);

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
        };
        _headerStyle.normal.textColor = Color.white;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
        };
        _labelStyle.normal.textColor = Color.white;

        _subLabelStyle = new GUIStyle(_labelStyle)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
        };
        _subLabelStyle.normal.textColor = Color.white;
    }

    private class RunInfo
    {
        public List<string> Weapons = new();
        public List<string> Tomes   = new();
        public List<string> Items   = new();
        public float RunTime;
        public int Kills, EliteKills, BossKills, Level;
        public List<KeyValuePair<string, string>> StatsList = new();
    }
}
