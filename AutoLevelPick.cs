using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Assets.Scripts._Data.Tomes;                       // ETome
using Assets.Scripts.Inventory__Items__Pickups;          // ERarity
using Assets.Scripts.Menu.Shop;                          // EStat
using Assets.Scripts.UI.InGame.Rewards;                  // EEncounter
using Assets.Scripts.Actors.Player;                      // MyPlayer
using Assets.Scripts.UI.InGame.Levelup;                  // EncounterWindows
using Assets.Scripts.Inventory.Stats;                    // PlayerStats
using Inventory__Items__Pickups.Xp_and_Levels;           // PlayerXp
using Assets.Scripts._Data;                              // IUpgradable

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// SCALING AUTO-UPGRADE  (toggle hotkey: Hotkeys.AutoUpgrade, default F5)
//
// The game ships an auto-pick (UpgradePicker.AutoSelectUpgrade) that just grabs
// the single highest-rarity option. That misplays the early game: a fresh run
// wants its snowball pieces (XP / difficulty / luck) far more than a marginally
// rarer dead-end, and it never knows a Legendary crit-damage roll is the real
// prize. This replaces that with a scaling-aware policy.
//
// Choices fall into importance BUCKETS. We take the first non-empty bucket and,
// inside it, the highest-rarity option. Item / chest offers are ignored.
//   1. New weapon/tome, or ANY Legendary roll  (build pieces + jackpots)
//   2. Scaling tomes: XP / Difficulty (Cursed) / Luck
//   3. Any other tome or weapon                 (rarity fallback)
//
// Within a bucket "New" rarity is weighted high (a fresh piece beats a small
// bump), and a real Legendary still outranks it. Because the top bucket only
// holds New + Legendary, a Legendary jackpot always wins it; otherwise the new
// piece does.
//
// Choices are classified by structure, not display text: a tome's identity comes
// from TomeData.eTome — locale-independent and stable across game updates.
//
// Selection is deferred one frame off the ShuffleUpgrades postfix (Tick, driven
// from ModGui.Update) so the level-up screen is fully built before we commit.
// ─────────────────────────────────────────────────────────────────────────
internal static class AutoLevelPick
{
    static ConfigEntry<bool> _enabled;
    internal static bool Enabled
    {
        get => _enabled != null && _enabled.Value;
        set { if (_enabled != null) _enabled.Value = value; }
    }

    internal static void Init(ConfigFile cfg)
    {
        _enabled = cfg.Bind("AutoUpgrade", "Enabled", false,
            "Scaling-aware auto-upgrade on level up. Prefers snowball picks " +
            "(XP / difficulty / luck / new weapons) early and premium Legendary " +
            "scaling rolls, instead of the game's plain highest-rarity pick. " +
            "Toggle in-game with the Auto Upgrade hotkey.");
    }

    // Screenless level-up pick — mirrors the game's own AutoSelectUpgrade, which
    // never builds the reward window (no slow-mo, no XP-bar level effect, no
    // inventory rebuild → no hitch). We don't replicate the game's rarity/offer
    // rolling by hand: UpgradePicker.ShuffleUpgrades(Levelup) rolls a fresh set
    // onto picker.buttons (it only fetches GetRandomUpgrades + SetUpgrade per
    // button — no window/pause side effects), so we call it ourselves, read the
    // fully-rolled buttons, pick, then apply via PlayerInventory.AddUpgrade. The
    // level-up screen is never queued (see Patch_AutoLevelPick).
    static bool _selecting;   // re-entrancy guard

    internal static bool TryAutoPick()
    {
        if (!Enabled || _selecting) return false;

        var picker = Picker;
        var inv    = MyPlayer.Instance?.inventory;
        if (picker == null || inv == null) return false;

        _selecting = true;
        try
        {
            picker.ShuffleUpgrades(EEncounter.Levelup);   // roll a fresh option set
            Decide(picker, inv);                          // pick + apply (no screen)
            return true;
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogError($"[AutoLevelPick] {e.Message}");
            return false;                                 // fall back to the normal screen
        }
        finally { _selecting = false; }
    }

    // The in-scene UpgradePicker, looked up lazily and cached (one per run).
    static UpgradePicker _picker;
    static UpgradePicker Picker
    {
        get
        {
            if (_picker == null) _picker = UnityEngine.Object.FindObjectOfType<UpgradePicker>();
            return _picker;
        }
    }

    internal static void Toggle()
    {
        Enabled = !Enabled;
        string state = Enabled ? "ON" : "OFF";
        Plugin.Log.LogInfo($"[AutoLevelPick] {state}");
        Toast.Show($"Scaling Auto-Upgrade: {state}",
                   Enabled ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.6f, 0.45f, 1f));
    }

    // ── decision log (shown in the log window) ──
    internal const int LogMax = 10;
    internal static readonly List<string> Log = new();

    static void Record(string line)
    {
        Log.Add(line);
        if (Log.Count > LogMax) Log.RemoveRange(0, Log.Count - LogMax);
    }

    // ── log window (toggle hotkey: Hotkeys.AutoUpgradeLog, default F4) ──
    internal static bool Visible;
    const float WinW = 460f, PadX = 12f, LineH = 22f;
    static readonly GuiWindowFrame _frame = new(new Vector2(60f, 200f));
    static float _lastWinH;

    internal static void ToggleWindow() => Visible = !Visible;

    static float WinHeight() =>
        LineH + 8f + LineH /*status*/ + LineH /*header*/ +
        Mathf.Max(1, Log.Count) * (LineH + 1f) + 10f;

    internal static void HandleInput() =>
        _frame.HandleInput(WinW, _lastWinH > 0f ? _lastWinH : WinHeight(), LineH + 4f);

    internal static void Draw()
    {
        if (!Visible) return;

        float winH = WinHeight();
        _lastWinH = winH;

        var saved = _frame.Begin();
        float ox = _frame.Pivot.x, oy = _frame.Pivot.y;
        UiTheme.Backdrop(new Rect(ox, oy, WinW, winH));
        GUI.Box(new Rect(ox, oy, WinW, winH), "Scaling Auto-Upgrade");

        float cw = WinW - PadX * 2f;
        float lx = ox + PadX;
        float y  = oy + LineH + 2f;

        GUI.Label(new Rect(lx, y, cw, LineH),
            Enabled ? "Status: ON (auto-picking level-ups)" : "Status: OFF");
        y += LineH;
        GUI.Label(new Rect(lx, y, cw, LineH), "Recent picks (newest first):");
        y += LineH;

        if (Log.Count == 0)
            GUI.Label(new Rect(lx, y, cw, LineH), "  (none yet)");
        else
            for (int i = Log.Count - 1; i >= 0; i--)
            {
                GUI.Label(new Rect(lx, y, cw, LineH), Log[i]);
                y += LineH + 1f;
            }

        _frame.End(saved);
        _frame.DrawGrip(WinW, winH);
    }

    // ── classification helpers ──

    // Rarity weight. "New" (a brand-new, unowned option) is treated as
    // strong — between Rare and Epic — since fresh pieces usually beat a small
    // level bump on something you already have.
    static int RarityWeight(ERarity r) => r switch
    {
        ERarity.Legendary => 6,
        ERarity.Epic      => 5,
        ERarity.New       => 4,
        ERarity.Rare      => 3,
        ERarity.Uncommon  => 2,
        ERarity.Common    => 1,
        _                 => 0,
    };

    // Weapon scaling-stat preference (higher = better). 0 = not a tracked stat.
    static int WeaponStatRank(EStat s) => s switch
    {
        EStat.CritDamage        => 6,
        EStat.DamageMultiplier  => 5,
        EStat.CritChance        => 4,
        EStat.SizeMultiplier    => 3,
        EStat.Projectiles       => 2,
        EStat.ProjectileBounces => 1,
        _                       => 0,
    };

    // Best scaling-stat rank across a button's offered stat modifiers.
    static int BestStatRank(UpgradeButton b)
    {
        int best = 0;
        try
        {
            var offer = b.upgradeOffer;
            if (offer == null) return 0;
            int n = offer.Count;
            for (int i = 0; i < n; i++)
            {
                var m = offer[i];
                if (m == null) continue;
                int r = WeaponStatRank(m.stat);
                if (r > best) best = r;
            }
        }
        catch { }
        return best;
    }

    // Tie-break rank within a bucket (only consulted when rarity is equal).
    // Order: XP > Difficulty(Cursed) > Luck > weapon CritDmg > Dmg > CritChance >
    // Size > Proj > Bounce > other tomes > anything else.
    static int Importance(Choice c)
    {
        if (c.IsTome)
            return c.Tome switch
            {
                ETome.Xp     => 100,
                ETome.Cursed => 90,
                ETome.Luck   => 80,
                _            => 10,
            };
        if (c.IsWeapon) return 10 + c.StatRank;   // 16..11 for tracked stats, 10 otherwise
        return 0;
    }

    // Live XP-gain multiplier (EStat.XpIncreaseMultiplier) vs the game's hard
    // 10x cap (PlayerXp.maxXpMultiplier). Returns remaining headroom 0..1:
    // 1 = far from cap (XP fully valuable), 0 = at/over cap (XP worthless).
    static float XpHeadroom()
    {
        try
        {
            float cap = PlayerXp.maxXpMultiplier;
            if (cap <= 0f) return 1f;
            float mult = PlayerStats.GetStat(EStat.XpIncreaseMultiplier);
            return Mathf.Clamp01(1f - mult / cap);
        }
        catch { return 1f; }
    }

    // A pre-chewed view of one offered choice.
    struct Choice
    {
        public UpgradeButton Btn;
        public bool   IsTome;
        public bool   IsWeapon;
        public ETome  Tome;       // valid only when IsTome
        public ERarity Rarity;
        public bool   IsNew;
        public int    StatRank;   // best weapon scaling-stat rank (weapons only)
        public string Name;
    }

    static List<Choice> Gather(UpgradePicker picker)
    {
        var list = new List<Choice>();
        var buttons = picker.buttons;
        if (buttons == null) return list;

        foreach (var b in buttons)
        {
            if (b == null) continue;
            if (b.isItem) continue;                 // skip chest / shop offers
            var up = b.upgradable;
            if (up == null) continue;

            var c = new Choice { Btn = b, Rarity = b.rarity };
            try { c.IsNew = (b.rarity == ERarity.New) || up.GetLevel() <= 0; } catch { }
            try { c.Name  = up.GetName() ?? "?"; } catch { c.Name = "?"; }

            var tome = up.TryCast<TomeData>();
            if (tome != null) { c.IsTome = true; c.Tome = tome.eTome; }
            else if (up.TryCast<WeaponData>() != null) { c.IsWeapon = true; c.StatRank = BestStatRank(b); }

            list.Add(c);
        }
        return list;
    }

    // Difficulty (Cursed) does nothing past 600 — the game lets you climb higher
    // but the community has confirmed there's no effect. Past the cap the tome is
    // dead weight, so we treat it like a capped XP tome and sink it to bucket 4.
    const float DifficultyCap = 600f;

    // Luck used to sit in this bucket; it now drops to the bucket below (other
    // tomes/weapons) where its high Importance keeps it at the top.
    // XP and Difficulty both leave the scaling bucket once they hit their caps
    // (see IsCappedXp / IsCappedDiff) — there they fall all the way to bucket 4.
    static bool IsScalingTome(Choice c) =>
        c.IsTome && ((c.Tome == ETome.Cursed && !IsCappedDiff(c)) ||
                     (c.Tome == ETome.Xp     && !IsCappedXp(c)));

    // XP tome with no headroom left against the 10x gain cap: worthless, so it
    // sinks to the lowest bucket (only taken when nothing else is offered).
    static bool IsCappedXp(Choice c) =>
        c.IsTome && c.Tome == ETome.Xp && XpHeadroom() <= 0.15f;

    // Difficulty (Cursed) tome at/over the 600 effective cap: no longer does
    // anything, so it also sinks to the lowest bucket.
    static bool IsCappedDiff(Choice c)
    {
        if (!c.IsTome || c.Tome != ETome.Cursed) return false;
        try { return PlayerStats.GetStat(EStat.Difficulty) >= DifficultyCap; }
        catch { return false; }
    }

    static void Decide(UpgradePicker picker, PlayerInventory inv)
    {
        var choices = Gather(picker);
        if (choices.Count == 0) return;             // nothing to auto-pick

        // Bucket 1: a brand-new piece, or any Legendary roll.
        if (Best(choices, c => c.IsNew || c.Rarity == ERarity.Legendary, out var pick))
        { Commit(inv, choices, pick, "bucket 1 (new / legendary)"); return; }

        // Bucket 2: scaling tomes (XP / difficulty).
        if (Best(choices, IsScalingTome, out pick))
        { Commit(inv, choices, pick, "bucket 2 (scaling tome)"); return; }

        // Bucket 3: any other tome or weapon — but NOT a capped XP / Difficulty tome.
        if (Best(choices, c => !IsCappedXp(c) && !IsCappedDiff(c), out pick))
        { Commit(inv, choices, pick, "bucket 3 (other tome / weapon)"); return; }

        // Bucket 4: last resort — capped XP / Difficulty tome (only thing left).
        if (Best(choices, _ => true, out pick))
            Commit(inv, choices, pick, "bucket 4 (capped XP / difficulty — last resort)");
    }

    // Best choice matching `match`: highest rarity, then importance order on ties.
    // False if none match. Key packs rarity into the high digits so it always
    // dominates; Importance (0..100) only separates equal-rarity options.
    static bool Best(List<Choice> choices, System.Func<Choice, bool> match, out Choice best)
    {
        best = default; int bestKey = -1;
        foreach (var c in choices)
        {
            if (!match(c)) continue;
            int key = RarityWeight(c.Rarity) * 1000 + Importance(c);
            if (key > bestKey) { bestKey = key; best = c; }
        }
        return bestKey >= 0;
    }

    static void Commit(PlayerInventory inv, List<Choice> choices, Choice c, string reason)
    {
        var b = c.Btn;
        if (b == null) return;

        var alts = new List<string>();
        foreach (var o in choices)
            if (o.Btn != b) alts.Add($"{o.Name} ({o.Rarity})");

        string line = $"{c.Name} ({c.Rarity}) — {reason}";
        if (alts.Count > 0) line += "  | over: " + string.Join(", ", alts);
        Record(line);
        Plugin.Log.LogInfo($"[AutoLevelPick] picked {line}");

        // Apply directly to the inventory — the same primitive the game's own
        // AutoSelectUpgrade uses. No reward window, no encounter queue: the level
        // has already been granted, we just add the chosen upgrade.
        inv.AddUpgrade(b.upgradable, b.upgradeOffer, b.rarity);

        // Mirror the stock auto-upgrade's HUD popup (ServerFeed.SetFeed) so the
        // player still sees what was picked — skipping the screen otherwise ate
        // that feedback.
        ShowFeed(b.upgradable, b.rarity);
    }

    static ServerFeed _feed;
    static ServerFeed Feed
    {
        get { if (_feed == null) _feed = UnityEngine.Object.FindObjectOfType<ServerFeed>(); return _feed; }
    }

    static void ShowFeed(IUpgradable up, ERarity rarity)
    {
        try
        {
            var feed = Feed;
            if (feed == null || up == null) return;
            string name = up.GetName() ?? "?";
            string text = $"{name} ({rarity})";
            feed.SetFeed(text, 4f, up.GetIcon());
        }
        catch (System.Exception e) { Plugin.Log.LogError($"[AutoLevelPick] feed {e.Message}"); }
    }
}

// Intercept the level-up reward window before it's ever queued. When our auto-pick
// is on, do the pick screenlessly and skip opening the screen entirely (this is
// what the game's stock auto-upgrade accessibility option does). Any other
// encounter type — or a failed pick — falls through to the normal window.
[HarmonyPatch(typeof(EncounterWindows), "AddEncounter")]
static class Patch_AutoLevelPick
{
    [HarmonyPrefix]
    static bool Prefix(EEncounter rewardWindowType)
    {
        if (rewardWindowType != EEncounter.Levelup) return true;   // not a level-up
        if (!AutoLevelPick.Enabled) return true;                   // auto off → normal screen
        return !AutoLevelPick.TryAutoPick();                       // picked → skip the window
    }
}
