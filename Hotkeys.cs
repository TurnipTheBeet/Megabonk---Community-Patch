using BepInEx.Configuration;
using UnityEngine;

namespace MegaBonkMod;

// Central, rebindable hotkeys. Backed by the BepInEx config file so binds persist,
// and surfaced in-game through the native "Community Patch" settings tab
// (see NativeSettings.cs).
internal static class Hotkeys
{
    internal static ConfigEntry<KeyCode> ModMenu;
    internal static ConfigEntry<KeyCode> DamageChart;
    internal static ConfigEntry<KeyCode> FastFall;
    internal static ConfigEntry<KeyCode> ChaosMenu;
    internal static ConfigEntry<KeyCode> MapScanner;
    internal static ConfigEntry<KeyCode> MapScanToggle;
    internal static ConfigEntry<KeyCode> SkipChest;
    internal static ConfigEntry<KeyCode> EffectsOpacity;
    internal static ConfigEntry<KeyCode> GameSpeed;
    internal static ConfigEntry<KeyCode> PowerupTracker;
    internal static ConfigEntry<KeyCode> ChestOddsTracker;

    // True while the rebind UI is waiting for the next key — ModGui suppresses
    // hotkey actions for that frame so the captured key doesn't also fire.
    internal static bool Capturing = false;

    // Frame on which a rebind was just committed. The capture finishes (and clears
    // Capturing) earlier in the same Update than the hotkey-action checks, so the
    // freshly-bound key's GetKeyDown would otherwise fire the action on that very
    // frame. Suppress for the rest of the capture frame too.
    internal static int LastCaptureFrame = -1;

    // Hotkey actions should be ignored while capturing, and on the frame a capture
    // was just committed.
    internal static bool Suppressed => Capturing || UnityEngine.Time.frameCount == LastCaptureFrame;

    internal static void Init(ConfigFile cfg)
    {
        // Defaults laid out in display order. F-keys run F1..F9 top-to-bottom,
        // skipping F10 (it opens the in-game console). Fast Fall = Space.
        ModMenu        = cfg.Bind("Hotkeys", "ModMenu",           KeyCode.F1,    "Toggle the mod menu.");
        DamageChart    = cfg.Bind("Hotkeys", "DamageChart",      KeyCode.F2,    "Toggle the damage chart.");
        FastFall       = cfg.Bind("Hotkeys", "FastFall",         KeyCode.LeftControl, "Hold while airborne to fast-fall.");
        ChaosMenu      = cfg.Bind("Hotkeys", "ChaosMenu",        KeyCode.F3,    "Toggle the chaos stat-toggle menu.");
        MapScanner     = cfg.Bind("Hotkeys", "MapScanner",       KeyCode.F4,    "Toggle the map scanner window.");
        MapScanToggle  = cfg.Bind("Hotkeys", "MapScanToggle",    KeyCode.F5,    "Start / stop the map scan (auto-reroll until the map matches your criteria).");
        SkipChest      = cfg.Bind("Hotkeys", "SkipChest",        KeyCode.F6,    "Toggle Smart Skip Chest Animation (auto-skips when banishes are 0).");
        EffectsOpacity = cfg.Bind("Hotkeys", "EffectsOpacity",   KeyCode.F11,   "Toggle the game's Effects opacity (Settings > Effects) between 0% and 100%.");
        GameSpeed      = cfg.Bind("Hotkeys", "GameSpeed",        KeyCode.T,     "Toggle game speed between 1x and 2x.");
        PowerupTracker = cfg.Bind("Hotkeys", "PowerupTracker",   KeyCode.Period,    "Toggle the active powerup tracker overlay.");
        ChestOddsTracker = cfg.Bind("Hotkeys", "ChestOddsTracker", KeyCode.Slash,  "Toggle the chest rarity odds tracker overlay.");
    }

    // The rebindable entries in display order — drives the settings-tab rows.
    internal static (string label, ConfigEntry<KeyCode> entry)[] All() => new[]
    {
        ("Mod Menu",     ModMenu),
        ("Damage Chart", DamageChart),
        ("Fast Fall",    FastFall),
        ("Chaos Menu",   ChaosMenu),
        ("Map Scanner",      MapScanner),
        ("Map Scan Start/Stop", MapScanToggle),
        ("Smart Skip Chest", SkipChest),
        ("Effects Opacity 0/100", EffectsOpacity),
        ("Game Speed 1x/2x",      GameSpeed),
        ("Powerup Tracker",       PowerupTracker),
        ("Chest Odds Tracker",    ChestOddsTracker),
    };

    internal static string Pretty(KeyCode k) => k switch
    {
        KeyCode.LeftControl  => "Left Ctrl",
        KeyCode.RightControl => "Right Ctrl",
        KeyCode.LeftShift    => "Left Shift",
        KeyCode.RightShift   => "Right Shift",
        KeyCode.LeftAlt      => "Left Alt",
        KeyCode.RightAlt     => "Right Alt",
        KeyCode.None         => "None",
        _                    => k.ToString(),
    };
}
