using Assets.Scripts.Managers;   // GameManager
using BepInEx.Configuration;
using UnityEngine;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// SMART AUTO SKIP CHEST ANIMATION
//
// The chest / level-up banish choice is pointless once you have no banishes
// left, so turn the game's Skip Chest Animation setting ON when banishes hit 0.
//
// Our twist: this mod's Golden Ring patch GRANTS banishes (vanilla only ever
// spends them), so banishes can climb back above 0 mid-run. A one-way "enable
// at 0" would leave the animation skipped even after a Golden Ring refill. So we
// MIRROR the setting to the live banish count both ways:
//     banishes <= 0  →  skip ON   (no choice to make, skip the wait)
//     banishes  > 0  →  skip OFF  (you can banish again, show the screen)
//
// Polled once per frame (ModGui.Update) so it catches every banish change path
// (level-up banish, chest banish button, upgrade-picker banish, Golden Ring
// add/remove) without hooking each one. We only write when the value actually
// changes, and we set the IN-MEMORY CFGameSettings flag only — never SaveConfig —
// so the player's on-disk preference is never permanently rewritten or leaked to
// vanilla; it resets naturally on game restart / mod removal.
// ─────────────────────────────────────────────────────────────────────────
internal static class SkipChestSync
{
    const int Unset = -999;
    static int _lastApplied = Unset;

    // Persisted on/off, toggled by the "Smart Skip Chest" hotkey in the mod tab.
    static ConfigEntry<bool> _enabled;

    internal static bool Enabled
    {
        get => _enabled != null && _enabled.Value;
        set { if (_enabled != null) _enabled.Value = value; }
    }

    internal static void Init(ConfigFile cfg)
    {
        _enabled = cfg.Bind("SkipChest", "Enabled", true,
            "Smart Skip Chest Animation: auto-skip the chest/level-up animation when " +
            "banishes are 0, and show it again when Golden Rings refill banishes. " +
            "Toggle in-game with the Smart Skip Chest hotkey.");
    }

    // Flip on/off (bound hotkey). When turning off, restore the animation so the
    // user isn't left with skip stuck on from our last write.
    internal static void Toggle()
    {
        Enabled = !Enabled;
        if (!Enabled) Restore();
        string state = Enabled ? "ON" : "OFF";
        Plugin.Log.LogInfo($"[SkipChest] Smart Skip Chest {state}");
        Toast.Show($"Smart Skip Chest: {state}",
                   Enabled ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.6f, 0.45f, 1f));
    }

    // Put skip_chest_animation back to OFF and forget our cached value.
    static void Restore()
    {
        try
        {
            var gs = SaveManager.Instance?.config?.cfGameSettings;
            if (gs != null) gs.skip_chest_animation = 0;
        }
        catch { }
        _lastApplied = Unset;
    }

    internal static void Tick()
    {
        try
        {
            if (!Enabled) { _lastApplied = Unset; return; }

            var inv = GameManager.Instance?.GetPlayerInventory();
            if (inv == null) { _lastApplied = Unset; return; }   // not in a run

            int want = inv.banishes <= 0 ? 1 : 0;
            if (want == _lastApplied) return;                    // nothing changed

            var gs = SaveManager.Instance?.config?.cfGameSettings;
            if (gs == null) return;

            gs.skip_chest_animation = want;
            _lastApplied = want;
        }
        catch { }
    }
}
