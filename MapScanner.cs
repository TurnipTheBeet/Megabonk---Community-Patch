using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Assets.Scripts.Managers;   // MapController
using Assets.Scripts.Inventory__Items__Pickups.Items;   // EItemRarity

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// MAP SCANNER  (hotkey: Hotkeys.MapScanner, default F4)
//
// In-process map-criteria auto-restart. You pick how many of each map feature
// you want to see, press Start, and the mod auto-rerolls the run until a
// generated map meets every minimum — then it stops (and optionally pauses).
//
// Everything here is plain managed IL2CPP interop — no native hooks, no raw
// memory pokes, no VirtualProtect:
//   • Moai / Shady Guy / Boss Curse counts come from the game's own static
//     InteractablesStatus dictionary (debugName -> container.numTotal)
//   • Microwave counts are split by tier (EItemRarity) by scanning the live
//     InteractableMicrowave objects, since the dictionary only has one total
//   • the reroll is the game's own public MapController.RestartRun()
//   • "map ready" is detected via MapGenerationController.isGenerating + mapSeed
//   • the optional pause-on-hit uses the game's own PauseUi.Pause()
// The scan loop is ticked once per frame from ModGui.Update (main thread).
// ─────────────────────────────────────────────────────────────────────────
internal static class MapScanner
{
    // A row in the scanner. `key` is the internal id used for config + count
    // lookup; `match` substrings are matched (case-insensitive) against the
    // game's interactable debugName keys (null = computed specially).
    sealed class Row
    {
        public string Key;
        public string Label;
        public string[] Match;            // dict substrings, or null for special
        public EItemRarity? MwTier;       // set for microwave-tier rows
        public bool Combined;             // Moai + Shady combined row
        public bool MwAny;                // total microwaves (all tiers)
        public bool Exact;                // require count == want (not >=)
    }

    static readonly Row[] Rows =
    {
        new() { Key = "bosscurse",  Label = "Boss Curses (exact)",   Match = new[] { "curse" }, Exact = true },
        new() { Key = "magnet",     Label = "Magnet Shrines",        Match = new[] { "magnet" } },
        new() { Key = "mw_any",     Label = "Microwave: Any",        MwAny = true },
        new() { Key = "mw_common",  Label = "Microwave: Common",     MwTier = EItemRarity.Common },
        new() { Key = "mw_rare",    Label = "Microwave: Rare",       MwTier = EItemRarity.Rare },
        new() { Key = "mw_epic",    Label = "Microwave: Epic",       MwTier = EItemRarity.Epic },
        new() { Key = "mw_legend",  Label = "Microwave: Legendary",  MwTier = EItemRarity.Legendary },
        new() { Key = "moaishady",  Label = "Moai + Shady (any)",    Combined = true },
        new() { Key = "moais",      Label = "Moais",                 Match = new[] { "moai" } },
        new() { Key = "shady",      Label = "Shady Guy",             Match = new[] { "shady" } },
    };

    static ConfigEntry<int>[] _desired;     // parallel to Rows; min wanted (0 = ignore)
    static ConfigFile _cfg;

    internal static bool   Visible;
    internal static bool   Active;
    internal static int    Attempts;
    internal static string Status = "Idle";

    // Last computed current-count per row key (for GUI display + matching).
    static Dictionary<string, int> _cur = new();
    static float _nextLiveRefresh;

    // Scan-loop bookkeeping.
    static int  _seedAtRestart;
    static bool _awaitingNewMap;

    // Window geometry
    const float WinW = 320f, PadX = 12f, LineH = 24f;
    static readonly GuiWindowFrame _frame = new(new Vector2(360f, 80f));
    static float _lastWinH;

    static float WinHeight() =>
        LineH + 8f                       // title
        + LineH                          // hint
        + Rows.Length * (LineH + 2f)     // rows
        + LineH                          // hotkey state line
        + LineH + 8f;                    // status (scan start/stop is on a hotkey)

    internal static void Init(ConfigFile cfg)
    {
        _cfg = cfg;
        _desired = new ConfigEntry<int>[Rows.Length];
        for (int i = 0; i < Rows.Length; i++)
            _desired[i] = cfg.Bind("MapScanner", Rows[i].Key, 0,
                $"Desired '{Rows[i].Label}' the scanner waits for (0 = ignore).");
    }

    internal static void Toggle() => Visible = !Visible;

    // ── core reads ────────────────────────────────────────────────────────

    // Game's interactable counters (debugName -> numTotal on map).
    static Dictionary<string, int> ReadDict()
    {
        var result = new Dictionary<string, int>();
        try
        {
            var dict = InteractablesStatus.interactablesByName;
            if (dict == null) return result;
            foreach (var kv in dict)
            {
                var c = kv.Value;
                if (c != null) result[kv.Key] = c.numTotal;
            }
        }
        catch (System.Exception e) { Plugin.Log.LogWarning($"[MapScanner] dict: {e.Message}"); }
        return result;
    }

    static int DictCount(Dictionary<string, int> dict, string[] match)
    {
        int total = 0;
        foreach (var kv in dict)
        {
            string k = kv.Key.ToLowerInvariant();
            foreach (var m in match)
                if (k.Contains(m)) { total += kv.Value; break; }
        }
        return total;
    }

    // Count live microwaves per rarity tier. Index by (int)EItemRarity.
    static int[] CountMicrowaveTiers()
    {
        var tiers = new int[6];
        try
        {
            var mws = Object.FindObjectsOfType<InteractableMicrowave>();
            if (mws != null)
                foreach (var mw in mws)
                {
                    if (mw == null) continue;
                    int r = (int)mw.rarity;
                    if (r >= 0 && r < tiers.Length) tiers[r]++;
                }
        }
        catch (System.Exception e) { Plugin.Log.LogWarning($"[MapScanner] mw: {e.Message}"); }
        return tiers;
    }

    // Build the current-count map for every row from the live game state.
    static Dictionary<string, int> ComputeCounts()
    {
        var dict  = ReadDict();
        var tiers = CountMicrowaveTiers();
        int moai  = DictCount(dict, new[] { "moai" });
        int shady = DictCount(dict, new[] { "shady" });

        int mwAny = 0;
        for (int t = 0; t < tiers.Length; t++) mwAny += tiers[t];

        var cur = new Dictionary<string, int>();
        foreach (var r in Rows)
        {
            int v;
            if (r.Combined)             v = moai + shady;
            else if (r.MwAny)           v = mwAny;
            else if (r.MwTier.HasValue) v = tiers[(int)r.MwTier.Value];
            else                        v = DictCount(dict, r.Match);
            cur[r.Key] = v;
        }
        return cur;
    }

    static bool AnyDesired()
    {
        for (int i = 0; i < _desired.Length; i++)
            if (_desired[i].Value > 0) return true;
        return false;
    }

    static bool Matches(Dictionary<string, int> cur)
    {
        for (int i = 0; i < Rows.Length; i++)
        {
            int want = _desired[i].Value;
            if (want <= 0) continue;
            cur.TryGetValue(Rows[i].Key, out int have);
            if (Rows[i].Exact) { if (have != want) return false; }
            else               { if (have <  want) return false; }
        }
        return true;
    }

    static bool HasAnyCount(Dictionary<string, int> cur)
    {
        foreach (var v in cur.Values) if (v > 0) return true;
        return false;
    }

    // ── pause ──────────────────────────────────────────────────────────────

    static void PauseGame()
    {
        // IMPORTANT: never fall back to Time.timeScale = 0. The game's own
        // PauseUi.Pause() throws if called in a non-pausable state; a timeScale
        // fallback there would freeze the game with no menu (soft-lock). Instead
        // gate on CanPause()/IsPaused() so the call is always safe, and if it
        // can't pause cleanly we just leave the run running.
        try
        {
            PauseUi pause = null;
            var handler = Object.FindObjectOfType<PauseHandler>();
            if (handler != null) pause = handler.pauseUi;        // works even if the menu object is inactive
            if (pause == null) pause = Object.FindObjectOfType<PauseUi>();
            if (pause == null) return;
            if (pause.IsPaused()) return;
            if (!pause.CanPause()) return;
            pause.Pause();
        }
        catch (System.Exception e) { Plugin.Log.LogWarning($"[MapScanner] pause: {e.Message}"); }
    }

    // ── scan control ─────────────────────────────────────────────────────

    internal static void StartScan()
    {
        if (MapController.IsMainMenu()) { Status = "Start a run first."; return; }
        if (!AnyDesired())             { Status = "Set at least one count > 0."; return; }
        Active          = true;
        Attempts        = 0;
        _awaitingNewMap = false;
        Status          = "Scanning…";
    }

    internal static void StopScan(string msg = "Stopped")
    {
        Active          = false;
        _awaitingNewMap = false;
        Status          = msg;
    }

    // Bound to a hotkey: start scanning, or stop if already running.
    internal static void ToggleScan()
    {
        if (Active) StopScan("Cancelled.");
        else        StartScan();
    }

    // Ticked every frame from ModGui.Update.
    internal static void Tick()
    {
        if (!Active) return;
        try
        {
            if (MapController.IsMainMenu()) { StopScan("Left run."); return; }
            if (MapGenerationController.isGenerating) return;   // still building

            if (_awaitingNewMap)
            {
                if (MapGenerationController.mapSeed == _seedAtRestart) return;
                _awaitingNewMap = false;
            }

            var cur = ComputeCounts();
            if (!HasAnyCount(cur)) return;   // not populated yet this frame
            _cur = cur;

            if (Matches(cur))
            {
                StopScan($"Match found after {Attempts} reroll(s).");
                PauseGame();
                return;
            }

            Attempts++;
            _seedAtRestart  = MapGenerationController.mapSeed;
            _awaitingNewMap = true;
            Status          = $"Reroll #{Attempts}…";
            MapController.RestartRun();
        }
        catch (System.Exception e)
        {
            StopScan($"Error: {e.Message}");
            Plugin.Log.LogError($"[MapScanner] {e}");
        }
    }

    // ── GUI ───────────────────────────────────────────────────────────────

    internal static void HandleInput()
    {
        if (Visible) _frame.HandleInput(WinW, _lastWinH > 0f ? _lastWinH : WinHeight(), LineH + 4f);
    }

    internal static void Draw()
    {
        if (!Visible) return;

        // Refresh live counts for display (throttled — FindObjectsOfType is heavy).
        if (!Active && !MapController.IsMainMenu() && Time.unscaledTime >= _nextLiveRefresh)
        {
            _nextLiveRefresh = Time.unscaledTime + 0.5f;
            var c = ComputeCounts();
            if (HasAnyCount(c)) _cur = c;
        }

        float winH = WinHeight();
        _lastWinH  = winH;

        var saved = _frame.Begin();
        float ox = _frame.Pivot.x, oy = _frame.Pivot.y;
        UiTheme.Backdrop(new Rect(ox, oy, WinW, winH));
        GUI.Box(new Rect(ox, oy, WinW, winH), "Map Scanner");

        float cw = WinW - PadX * 2f;
        float lx = ox + PadX;
        float y  = oy + LineH + 2f;

        GUI.Label(new Rect(lx, y, cw, LineH), "Want at least (0 = ignore):");
        y += LineH;

        for (int i = 0; i < Rows.Length; i++)
        {
            _cur.TryGetValue(Rows[i].Key, out int have);
            string lbl = (Active || _cur.Count > 0) ? $"{Rows[i].Label}  ({have})" : Rows[i].Label;
            GUI.Label(new Rect(lx, y, cw - 92f, LineH), lbl);

            GUI.enabled = !Active;
            float bx = lx + cw - 92f;
            if (GUI.Button(new Rect(bx, y, 26f, LineH - 2f), "-") && _desired[i].Value > 0)
                SetDesired(i, _desired[i].Value - 1);
            GUI.Label(new Rect(bx + 34f, y, 26f, LineH), _desired[i].Value.ToString());
            if (GUI.Button(new Rect(bx + 64f, y, 26f, LineH - 2f), "+"))
                SetDesired(i, _desired[i].Value + 1);
            GUI.enabled = true;

            y += LineH + 2f;
        }

        string hk = Hotkeys.Pretty(Hotkeys.MapScanToggle.Value);
        string state = Active ? $"SCANNING — reroll #{Attempts}  ({hk} to stop)"
                              : $"{hk} to start scan";
        GUI.Label(new Rect(lx, y, cw, LineH), state);
        y += LineH;
        GUI.Label(new Rect(lx, y, cw, LineH), $"Status: {Status}");

        _frame.End(saved);
        _frame.DrawGrip(WinW, winH);
    }

    static void SetDesired(int idx, int value)
    {
        if (value < 0) value = 0;
        if (_desired[idx].Value == value) return;
        _desired[idx].Value = value;
        try { _cfg?.Save(); } catch { }
    }
}
