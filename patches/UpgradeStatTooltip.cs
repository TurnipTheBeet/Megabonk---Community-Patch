using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Assets.Scripts._Data;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;

namespace MegabonkCommunityPatch;

internal static class UpgradeStatTooltip
{
    internal static readonly Dictionary<int, string> Text = new();

    static readonly Dictionary<EStat, string> Names = new()
    {
        { EStat.DamageMultiplier,          "Damage" },
        { EStat.AttackSpeed,               "Attack Speed" },
        { EStat.CritChance,                "Crit Chance" },
        { EStat.CritDamage,                "Crit Damage" },
        { EStat.Projectiles,               "Projectile Count" },
        { EStat.ProjectileBounces,         "Projectile Bounces" },
        { EStat.SizeMultiplier,            "Size" },
        { EStat.ProjectileSpeedMultiplier, "Projectile Speed" },
        { EStat.DurationMultiplier,        "Duration" },
        { EStat.EffectDurationMultiplier,  "Effect Duration" },
        { EStat.KnockbackMultiplier,       "Knockback" },
        { EStat.EliteDamageMultiplier,     "Damage to Elites" },
        { EStat.MoveSpeedMultiplier,       "Movement Speed" },
        { EStat.XpIncreaseMultiplier,      "XP Gain" },
        { EStat.GoldIncreaseMultiplier,    "Gold Gain" },
        { EStat.SilverIncreaseMultiplier,  "Silver Gain" },
        { EStat.Luck,                      "Luck" },
        { EStat.Difficulty,                "Difficulty" },
        { EStat.MaxHealth,                 "Max HP" },
        { EStat.Armor,                     "Armor" },
        { EStat.Lifesteal,                 "Lifesteal" },
    };

    static string StatName(EStat s)
    {
        if (Names.TryGetValue(s, out var n)) return n;
        var raw = s.ToString();
        if (raw.EndsWith("Multiplier")) raw = raw.Substring(0, raw.Length - "Multiplier".Length);
        var sb = new StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1])) sb.Append(' ');
            sb.Append(raw[i]);
        }
        return sb.ToString();
    }

    static string ForWeapon(WeaponData wd)
    {
        var sb = new StringBuilder();
        WeaponBase wb = null;
        try
        {
            var inv = MyPlayer.Instance?.inventory?.weaponInventory;
            if (inv != null && inv.weapons != null && inv.weapons.ContainsKey(wd.eWeapon))
                wb = inv.weapons[wd.eWeapon];
        }
        catch { }

        try
        {
            foreach (var kv in wd.baseStats)
            {
                EStat stat = kv.Key;
                float val;
                try { val = wb != null ? wb.GetValue(stat) : kv.Value; }
                catch { val = kv.Value; }
                if (stat == EStat.ProjectileBounces && val >= 99f) continue;
                sb.AppendLine($"{StatName(stat)}: {StatsUi.FormatStat(stat, val)}");
            }
        }
        catch { }
        return sb.ToString().TrimEnd();
    }

    static string ForItem(ItemData id)
    {
        try { return id.GetDescription(); } catch { return ""; }
    }

    internal static void Attach(UpgradeButton btn, string body)
    {
        try
        {
            if (btn == null || btn.icon == null) return;
            if (string.IsNullOrWhiteSpace(body)) return;

            string title = null;
            try { if (btn.t_name != null) title = btn.t_name.text; } catch { }
            string full = string.IsNullOrWhiteSpace(title) ? body : $"<b>{title}</b>\n{body}";

            var go = btn.icon.gameObject;
            Text[go.GetInstanceID()] = full;
            if (go.GetComponent<IconHover>() == null) go.AddComponent<IconHover>();
        }
        catch { }
    }

    internal static string BuildWeapon(WeaponData wd) => ForWeapon(wd);
    internal static string BuildItem(ItemData id)     => ForItem(id);

    internal static void Clear(UpgradeButton btn)
    {
        try { if (btn != null && btn.icon != null) Text.Remove(btn.icon.gameObject.GetInstanceID()); }
        catch { }
    }
}
