using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Actors;

namespace MegabonkCommunityPatch;

internal static class WeaponSfxVolume
{
    const int None = 0, WeaponCat = 1, HitCat = 2, ItemCat = 3;

    static ConfigEntry<float> _weapon;
    static ConfigEntry<float> _hit;
    static ConfigEntry<float> _item;
    static ConfigEntry<bool>  _debug;

    static readonly string[] _announceKeywords =
        { "warning", "boss", "swarm", "horde", "event", "siren", "alarm",
          "announce", "portal", "storm", "spawner" };

    static readonly Dictionary<int, int>   _cat     = new();
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
            if (IsAnnouncement(t)) cat = None;
            else if (t.GetComponentInParent<PlayerMovement>(true) != null) cat = None;
            else if (t.GetComponentInParent<InteractableChest>(true) != null) cat = None;
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

    internal static void ScaleItemSource(AudioSource src)
    {
        try
        {
            if (src == null) return;
            if (IsAnnouncement(src.transform)) return;
            int id = src.GetInstanceID();
            if (!_origSrc.TryGetValue(id, out float orig)) { orig = src.volume; _origSrc[id] = orig; }
            src.volume = orig * Item;
        }
        catch { }
    }

    internal static void ResetCaches()
    {
        _cat.Clear();
        _origSrc.Clear();
    }
}
