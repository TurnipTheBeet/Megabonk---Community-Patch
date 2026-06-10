using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// SFX VOLUME  (weapon / hit / item, independent of the game's master SFX)
//
// The game only exposes one shared "SFX" volume, so you can't quiet the spammy
// combat sounds without losing important cues. This adds three independent
// multipliers.
//
// Combat sounds funnel through ONE method — RandomSfx.Play — but from many
// callers: AttackMuzzle (muzzle/swing), AttackHit (impact), and each weapon's
// own projectile (e.g. ProjectileAxe.TryInit plays its sfx directly, NOT via a
// muzzle — which is why patching AttackMuzzle alone missed secondary weapons).
// So instead of chasing every caller we patch RandomSfx.Play itself and scale
// its volumeMultiplier, classifying each RandomSfx instance ONCE by the owner it
// hangs off:
//
//   AttackHit owner                       → Hit
//   ProjectileBase / AttackMuzzle owner   → Weapon
//   PlaySfxOnEnable owner                 → Item
//   anything else (UI, music, xp/gold, footsteps, achievements) → untouched
//
// Scaling the per-call volumeMultiplier (not the cached defaultVolume) is
// stateless: no capture, no compounding on pooled re-enable.
// ─────────────────────────────────────────────────────────────────────────
internal static class WeaponSfxVolume
{
    const int None = 0, WeaponCat = 1, HitCat = 2, ItemCat = 3;

    static ConfigEntry<float> _weapon;
    static ConfigEntry<float> _hit;
    static ConfigEntry<float> _item;
    static ConfigEntry<bool>  _debug;

    // Owner-name fragments that mark a sound as a gameplay ANNOUNCEMENT (boss
    // spawn, incoming swarm, sandstorm, event warning) rather than a combat /
    // item effect. These must stay at full volume regardless of the per-category
    // sliders, so any RandomSfx hanging off such an object is left untouched.
    static readonly string[] _announceKeywords =
        { "warning", "boss", "swarm", "horde", "event", "siren", "alarm",
          "announce", "portal", "storm", "spawner" };

    // classification cached per RandomSfx instance id (owner never changes)
    static readonly Dictionary<int, int>   _cat     = new();
    // authored baseline for the rare raw-AudioSource item path
    static readonly Dictionary<int, float> _origSrc = new();

    internal static float Weapon
    {
        get => _weapon != null ? _weapon.Value : 1f;
        set { if (_weapon != null) _weapon.Value = Mathf.Clamp01(value); }
    }
    internal static float Hit
    {
        get => _hit != null ? _hit.Value : 1f;
        set { if (_hit != null) _hit.Value = Mathf.Clamp01(value); }
    }
    internal static float Item
    {
        get => _item != null ? _item.Value : 1f;
        set { if (_item != null) _item.Value = Mathf.Clamp01(value); }
    }

    internal static void Init(ConfigFile cfg)
    {
        _weapon = cfg.Bind("Audio", "WeaponSfxVolume", 1f,
            "Volume multiplier (0..1) for weapon attack sounds (muzzle / swing / projectile) only.");
        _hit = cfg.Bind("Audio", "HitSfxVolume", 1f,
            "Volume multiplier (0..1) for hit / impact sounds only.");
        _item = cfg.Bind("Audio", "ItemSfxVolume", 1f,
            "Volume multiplier (0..1) for item / spawned-effect sounds only. " +
            "UI, music and xp/gold are unaffected by all three.");
        _debug = cfg.Bind("Audio", "SfxDebug", false,
            "Log every newly-classified combat/item sound (owner path + category) to the " +
            "BepInEx console. Use this to identify a sound that's being scaled when it " +
            "shouldn't be, then report its path.");
    }

    // Full hierarchy path of a transform, e.g. "Root/Child/Leaf".
    static string PathOf(Transform t)
    {
        try
        {
            string p = t.name;
            for (var cur = t.parent; cur != null; cur = cur.parent) p = cur.name + "/" + p;
            return p;
        }
        catch { return "?"; }
    }

    // Any object in the parent chain whose name marks this as an announcement.
    static bool IsAnnouncement(Transform t)
    {
        try
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                string n = cur.name;
                if (string.IsNullOrEmpty(n)) continue;
                n = n.ToLowerInvariant();
                foreach (var k in _announceKeywords)
                    if (n.Contains(k)) return true;
            }
        }
        catch { }
        return false;
    }

    // Multiplier to apply to a given RandomSfx play (1 = leave alone).
    internal static float FactorFor(RandomSfx r)
    {
        if (r == null) return 1f;
        int id = r.GetInstanceID();
        if (!_cat.TryGetValue(id, out int c)) { c = Classify(r); _cat[id] = c; }
        switch (c)
        {
            case WeaponCat: return Weapon;
            case HitCat:    return Hit;
            case ItemCat:   return Item;
            default:        return 1f;
        }
    }

    static int Classify(RandomSfx r)
    {
        int cat = None;
        try
        {
            var t = r.transform;

            // Gameplay announcements (boss/swarm/event warnings) must never be
            // scaled by the combat/item sliders — leave them at full volume.
            if (IsAnnouncement(t)) cat = None;
            // AudioSpamFilter throttles crit / grandma-crit / explosion / fireball-muzzle
            // sounds and re-plays this RandomSfx; treat them as impact (Hit).
            else if (t.GetComponentInParent<AudioSpamFilter>(true) != null) cat = HitCat;
            else if (t.GetComponentInParent<AttackHit>(true)      != null) cat = HitCat;
            else if (t.GetComponentInParent<ProjectileBase>(true) != null) cat = WeaponCat;
            else if (t.GetComponentInParent<AttackMuzzle>(true)   != null) cat = WeaponCat;
            else if (t.GetComponentInParent<PlaySfxOnEnable>(true) != null) cat = ItemCat;

            if (_debug != null && _debug.Value)
                Plugin.Log.LogInfo($"[SfxDebug] {CatName(cat)} <- {PathOf(t)}");
        }
        catch { }
        return cat;
    }

    static string CatName(int c) => c switch
    {
        WeaponCat => "Weapon", HitCat => "Hit", ItemCat => "Item", _ => "None(untouched)"
    };

    // The PlaySfxOnEnable path that plays a raw AudioSource (no RandomSfx) can't be
    // caught by the RandomSfx.Play hook, so scale that source directly (cached
    // baseline so pooled re-enables don't compound).
    internal static void ScaleItemSource(AudioSource src)
    {
        try
        {
            if (src == null) return;
            if (IsAnnouncement(src.transform)) return;   // leave announcement audio alone
            int id = src.GetInstanceID();
            if (!_origSrc.TryGetValue(id, out float orig)) { orig = src.volume; _origSrc[id] = orig; }
            src.volume = orig * Item;
        }
        catch { }
    }
}

// Single chokepoint: every RandomSfx play scales by its owner-category factor.
[HarmonyPatch(typeof(RandomSfx), "Play")]
static class Patch_Sfx_RandomSfxPlay
{
    [HarmonyPrefix]
    static void Prefix(RandomSfx __instance, ref float volumeMultiplier)
    {
        float f = WeaponSfxVolume.FactorFor(__instance);
        if (f != 1f) volumeMultiplier *= f;
    }
}

// Raw-AudioSource item effects (PlaySfxOnEnable with no RandomSfx) don't route
// through RandomSfx.Play, so handle that one source here.
[HarmonyPatch(typeof(PlaySfxOnEnable), "OnEnable")]
static class Patch_Sfx_OnEnable
{
    [HarmonyPrefix]
    static void Prefix(PlaySfxOnEnable __instance)
    {
        if (__instance.randomSfx == null)
            WeaponSfxVolume.ScaleItemSource(__instance.audioSource);
    }
}
