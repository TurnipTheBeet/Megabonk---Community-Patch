using BepInEx.Configuration;
using UnityEngine;
using Assets.Scripts.Menu.Shop;                         // EStat
using Assets.Scripts.Inventory__Items__Pickups.Upgrades; // EncounterUtility

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// CHAOS STAT MENU  (hotkey: Hotkeys.ChaosMenu, default F3)
//
// The mod blacklists 7 "junk" stats from the Chaos Tome / Gamble pool
// (Plugin.BlacklistedStats). This little window lets the player re-enable any
// of them per taste. Choices persist via the BepInEx config and re-apply each
// run; toggling mid-run updates the live pool immediately.
//
// Deliberately separate from the password-gated ModGui cheat menu and from the
// native settings tab — this is just pool curation, not a cheat.
// ─────────────────────────────────────────────────────────────────────────
internal static class ChaosMenu
{
    // The toggleable stats. `def` = default allowed-in-pool state:
    //   false → junk stat, blacklisted by default, toggle ON to re-enable
    //   true  → useful stat, in pool by default, toggle OFF to remove
    static readonly (int id, string name, bool def)[] Stats =
    {
        (1,  "Health Regen",     false),
        (2,  "Shield",           false),
        (3,  "Thorns",           false),
        (4,  "Armor",            false),
        (5,  "Evasion",          false),
        (11, "Projectile Speed", false),
        (24, "Knockback",        false),
        (10, "Duration",         true),
        (25, "Movement Speed",   true),
    };

    static ConfigEntry<bool>[] _allow;   // parallel to Stats; true = allowed in pool
    static ConfigFile _cfg;              // kept so toggles can be force-saved
    internal static bool Visible;

    // Window geometry
    const float WinW = 260f, PadX = 12f, LineH = 24f;
    static readonly GuiWindowFrame _frame = new(new Vector2(40f, 120f));  // double size by default
    static float _lastWinH;

    static float WinHeight() =>
        LineH + 8f + LineH /*hint*/ + Stats.Length * (LineH + 2f) + 8f;

    // Raw-input drag/resize, driven from ModGui.Update while the menu is open.
    internal static void HandleInput() =>
        _frame.HandleInput(WinW, _lastWinH > 0f ? _lastWinH : WinHeight(), LineH + 4f);

    internal static void Init(ConfigFile cfg)
    {
        _cfg = cfg;
        _allow = new ConfigEntry<bool>[Stats.Length];
        for (int i = 0; i < Stats.Length; i++)
        {
            _allow[i] = cfg.Bind("ChaosStats", Stats[i].name, Stats[i].def,
                $"Allow '{Stats[i].name}' to appear in the Chaos Tome / Gamble pool.");
        }
        ApplyAllToBlacklist();   // sync config → Plugin.BlacklistedStats at startup
    }

    // Rebuild Plugin.BlacklistedStats from the saved config (run-time pool is
    // re-applied per run by Patch_RunUnlockables_Init using this set).
    static void ApplyAllToBlacklist()
    {
        for (int i = 0; i < Stats.Length; i++)
        {
            if (_allow[i].Value) Plugin.BlacklistedStats.Remove(Stats[i].id);
            else                 Plugin.BlacklistedStats.Add(Stats[i].id);
        }
    }

    internal static void Toggle() => Visible = !Visible;

    static void SetAllowed(int idx, bool allowed)
    {
        if (_allow[idx].Value == allowed) return;
        _allow[idx].Value = allowed;
        try { _cfg?.Save(); } catch { }   // force-persist so toggle survives restart

        int id = Stats[idx].id;
        if (allowed) Plugin.BlacklistedStats.Remove(id);
        else         Plugin.BlacklistedStats.Add(id);

        // Update the live pool so the change takes effect this run too.
        try
        {
            var pool = EncounterUtility.upgradableStatsChaosAndGamble;
            if (pool != null)
            {
                var e = (EStat)id;
                if (allowed) { if (!pool.Contains(e)) pool.Add(e); }
                else         { if (pool.Contains(e))  pool.Remove(e); }
            }
        }
        catch { }
    }

    internal static void Draw()
    {
        if (!Visible) return;

        float winH = WinHeight();
        _lastWinH = winH;

        var saved = _frame.Begin();
        float ox = _frame.Pivot.x, oy = _frame.Pivot.y;
        UiTheme.Backdrop(new Rect(ox, oy, WinW, winH));
        GUI.Box(new Rect(ox, oy, WinW, winH), "Chaos Tome Stats");

        float cw = WinW - PadX * 2f;
        float lx = ox + PadX;
        float y  = oy + LineH + 2f;

        GUI.Label(new Rect(lx, y, cw, LineH), "Toggle stats in the Chaos/Gamble pool:");
        y += LineH;

        var toggle = GUI.skin.button;
        for (int i = 0; i < Stats.Length; i++)
        {
            bool cur = _allow[i].Value;
            bool nv = GUI.Toggle(new Rect(lx, y, cw, LineH), cur,
                (cur ? "ON   " : "OFF  ") + Stats[i].name, toggle);
            if (nv != cur) SetAllowed(i, nv);
            y += LineH + 2f;
        }

        _frame.End(saved);
        _frame.DrawGrip(WinW, winH);
    }
}
