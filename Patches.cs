#nullable disable
using HarmonyLib;
using UnityEngine;
using TMPro;
using Assets.Scripts.Utility;
using Il2CppInterop.Runtime;

using Assets.Scripts.Steam;
using Assets.Scripts.Steam.LeaderboardsNew;
using Steamworks;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Menu.Shop.Leaderboards;
using Assets.Scripts.Game.MapGeneration;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.InGame.Rewards.Effects;
using Assets.Scripts.UI.HUD;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Actors;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Settings___Saves.SaveFiles;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────
// ITEM CAPS + STAT BLACKLISTS — re-applied every new run
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(RunUnlockables), nameof(RunUnlockables.Init))]
static class Patch_RunUnlockables_Init
{
    [HarmonyPostfix]
    static void Postfix()
    {
        RemoveItemCaps();
        CacheAndApplyStatBlacklist();
        CacheAndApplyShrineStatBlacklist();
    }

    internal static void RemoveItemCaps()
    {
        var avail = RunUnlockables.availableItems;
        if (avail == null) return;

        foreach (var kvp in avail)
        {
            if (kvp.Value == null) continue;
            foreach (var item in kvp.Value)
            {
                if (Plugin.ActiveUncappedItems.Contains(item.eItem))
                {
                    item.maxAmount = Plugin.GetItemCap(item.eItem);
                    item.maxAmountPerRun = 9999;
                }
            }
        }

        // Force default items into the loot pool
        var dm = DataManager.Instance;
        if (dm == null) return;
        foreach (var eItem in Plugin.ForcedPoolItems)
        {
            var data = dm.GetItem(eItem);
            if (data != null) data.inItemPool = true;
        }
    }

    static void CacheAndApplyStatBlacklist()
    {
        var pool = EncounterUtility.upgradableStatsChaosAndGamble;
        if (pool == null) return;

        if (Plugin.FullStatPool.Count == 0)
            foreach (var stat in pool)
                Plugin.FullStatPool.Add((int)stat);

        foreach (var statInt in Plugin.BlacklistedStats)
        {
            var eStat = (EStat)statInt;
            if (pool.Contains(eStat)) pool.Remove(eStat);
        }
    }

    static void CacheAndApplyShrineStatBlacklist()
    {
        var pool = EncounterUtility.upgradableStatsShrines;
        if (pool == null) return;

        if (Plugin.FullShrineStatPool.Count == 0)
            foreach (var stat in pool)
                Plugin.FullShrineStatPool.Add((int)stat);

        foreach (var statInt in Plugin.BlacklistedShrineStats)
        {
            var eStat = (EStat)statInt;
            if (pool.Contains(eStat)) pool.Remove(eStat);
        }
    }
}

[HarmonyPatch(typeof(RunUnlockables), "OnNewRunStarted")]
static class Patch_RunUnlockables_OnNewRunStarted
{
    [HarmonyPostfix]
    static void Postfix() => Patch_RunUnlockables_Init.RemoveItemCaps();
}

// ─────────────────────────────────────────────────────────────────
// MICROWAVE — raise cap by 1 when player clicks to duplicate an already-capped item
// MicrowaveItemButton.SelectUpgrade checks: if (maxAmount > 0 && count >= maxAmount) → error
// Bumping maxAmount by 1 before that check lets the cook proceed.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(MicrowaveItemButton), "SelectUpgrade")]
static class Patch_MicrowaveItemButton_SelectUpgrade
{
    [HarmonyPrefix]
    static unsafe void Prefix(MicrowaveItemButton __instance)
    {
        long dataPtr = *(long*)(__instance.Pointer + 0x68);
        if (dataPtr == 0) return;

        var eItem = (EItem)(*(int*)(dataPtr + 0x54));
        if (!Plugin.ActiveUncappedItems.Contains(eItem)) return;

        int maxAmount = *(int*)(dataPtr + 0x70);
        if (maxAmount <= 0) return;

        // Bump cap by 1 — SelectUpgrade only fires for items the player already has,
        // so if maxAmount is reached this lets the cook proceed.
        *(int*)(dataPtr + 0x70) = maxAmount + 1;
    }
}

// ─────────────────────────────────────────────────────────────────
// SIZE CAPS — Grandma's Secret Tonic (16 units) & Spicy Meatball (32 units)
// Stack cap computed dynamically from baseRadius/radiusPerAmount so the max
// stack count lands exactly at the size cap.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemGrandmasSecretTonic), "OnInitOrAmountChanged")]
static class Patch_GrandmasTonic_SizeCap
{
    const float TargetMax = 16f;

    [HarmonyPrefix]
    static unsafe void Prefix(ItemGrandmasSecretTonic __instance)
    {
        *(float*)(__instance.Pointer + 0x3C) = TargetMax;
    }

    [HarmonyPostfix]
    static unsafe void Postfix(ItemGrandmasSecretTonic __instance)
    {
        float baseRadius      = *(float*)(__instance.Pointer + 0x34);
        float radiusPerAmount = *(float*)(__instance.Pointer + 0x38);
        if (radiusPerAmount <= 0f) return;
        int maxStacks = (int)System.Math.Ceiling((TargetMax - baseRadius) / radiusPerAmount);
        var data = DataManager.Instance?.GetItem(EItem.GrandmasSecretTonic);
        if (data == null) return;
        data.maxAmount = data.maxAmountPerRun = maxStacks;
    }
}

[HarmonyPatch(typeof(ItemSpicyMeatball), "OnInitOrAmountChanged")]
static class Patch_SpicyMeatball_SizeCap
{
    const float TargetMax = 16f;

    [HarmonyPrefix]
    static unsafe void Prefix(ItemSpicyMeatball __instance)
    {
        *(float*)(__instance.Pointer + 0x38) = TargetMax;
    }

    [HarmonyPostfix]
    static unsafe void Postfix(ItemSpicyMeatball __instance)
    {
        float baseRadius      = *(float*)(__instance.Pointer + 0x30);
        float radiusPerAmount = *(float*)(__instance.Pointer + 0x34);
        if (radiusPerAmount <= 0f) return;
        int maxStacks = (int)System.Math.Ceiling((TargetMax - baseRadius) / radiusPerAmount);
        var data = DataManager.Instance?.GetItem(EItem.SpicyMeatball);
        if (data == null) return;
        data.maxAmount = data.maxAmountPerRun = maxStacks;
    }
}

// ─────────────────────────────────────────────────────────────────
// BOB'S LANTERN — double fire rate by halving the computed cooldown
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemBobLantern), "OnInitOrAmountChanged")]
static class Patch_BobLantern_FireRate
{
    [HarmonyPostfix]
    static unsafe void Postfix(ItemBobLantern __instance)
    {
        *(float*)(__instance.Pointer + 0x3C) /= 2f;
    }
}

// ─────────────────────────────────────────────────────────────────
// BRASS KNUCKLES — remove size cap (PreAttack only fires when size <= radius)
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemBrassKnuckles), "OnInitOrAmountChanged")]
static class Patch_BrassKnuckles_SizeCap
{
    [HarmonyPrefix]
    static unsafe void Prefix(ItemBrassKnuckles __instance)
    {
        *(float*)(__instance.Pointer + 0x38) = float.MaxValue;
    }
}

// ─────────────────────────────────────────────────────────────────
// BACKPACK — give 2 projectiles per stack instead of 1
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemBackpack), "OnInitOrAmountChanged")]
static class Patch_ItemBackpack_Projectiles
{
    [HarmonyPrefix]
    static unsafe void Prefix(ItemBackpack __instance)
    {
        // The method reads amount at 0x18 and uses it directly as the projectile modifier.
        // Double it so each stack gives +2 instead of +1.
        *(int*)(__instance.Pointer + 0x18) *= 2;
    }

    [HarmonyPostfix]
    static unsafe void Postfix(ItemBackpack __instance)
    {
        *(int*)(__instance.Pointer + 0x18) /= 2;
    }
}

// ─────────────────────────────────────────────────────────────────
// SUCKY MAGNET — sort first within its rarity group in the Unlocks screen
// ItemData.CompareTo sorts by rarity then a secondary virtual (not sortingPriority)
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemData), nameof(ItemData.CompareTo), typeof(UnlockableBase))]
static class Patch_ItemData_CompareTo_SuckyMagnet
{
    // Originally toggleable, we made non-toggleable → sort first
    static readonly System.Collections.Generic.HashSet<EItem> SortsFirst = new()
    {
        EItem.SuckyMagnet, EItem.Scarf, EItem.EchoShard, EItem.BrassKnuckles,
        EItem.IdleJuice, EItem.DemonicBlood,
        EItem.Skuleg, EItem.OldMask, EItem.Battery, EItem.Key,
    };
    // Originally non-toggleable, we made toggleable → sort last
    static readonly System.Collections.Generic.HashSet<EItem> SortsLast = new()
    {
        EItem.Borgar, EItem.Beer, EItem.SpikyShield, EItem.CursedDoll,
        EItem.GloveLightning, EItem.PhantomShroud,
        EItem.Medkit, EItem.SlipperyRing, EItem.Oats, EItem.GoldenGlove,
    };

    [HarmonyPrefix]
    static bool Prefix(ItemData __instance, UnlockableBase other, ref int __result)
    {
        if (!(other is ItemData od)) return true;

        if (__instance.rarity != od.rarity) return true;
        bool thisFirst  = SortsFirst.Contains(__instance.eItem);
        bool otherFirst = SortsFirst.Contains(od.eItem);
        bool thisLast   = SortsLast.Contains(__instance.eItem);
        bool otherLast  = SortsLast.Contains(od.eItem);
        if (thisFirst && !otherFirst) { __result = -1; return false; }
        if (otherFirst && !thisFirst) { __result =  1; return false; }
        if (thisLast  && !otherLast)  { __result =  1; return false; }
        if (otherLast && !thisLast)   { __result = -1; return false; }
        if (thisLast  && otherLast)   { __result =  0; return false; }
        return true;
    }
}

// ─────────────────────────────────────────────────────────────────
// CURSED DOLL — block CanToggleActivation so it is never toggleable
// canAlwaysToggle=false bypasses the early-return but the type-based
// fallthrough still grants toggle for ItemBase types; patch the gate.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(MyAchievements), "CanToggleActivation")]
static class Patch_CanToggleActivation_NonToggleable
{
    [HarmonyPrefix]
    static bool Prefix(UnlockableBase unlockable, ref bool __result)
    {
        var dm = DataManager.Instance;
        if (dm == null) return true;
        if (unlockable == dm.GetItem(EItem.Scarf)        ||
            unlockable == dm.GetItem(EItem.SuckyMagnet)  ||
            unlockable == dm.GetItem(EItem.EchoShard)    ||
            unlockable == dm.GetItem(EItem.BrassKnuckles)||
            unlockable == dm.GetItem(EItem.IdleJuice)    ||
            unlockable == dm.GetItem(EItem.DemonicBlood))
        { __result = false; return false; }
        if (unlockable == dm.GetItem(EItem.GloveLightning) ||
            unlockable == dm.GetItem(EItem.PhantomShroud)  ||
            unlockable == dm.GetItem(EItem.Medkit)         ||
            unlockable == dm.GetItem(EItem.SlipperyRing)   ||
            unlockable == dm.GetItem(EItem.Oats)           ||
            unlockable == dm.GetItem(EItem.GoldenGlove))
        { __result = true; return false; }
        if (unlockable == dm.GetItem(EItem.Skuleg)  ||
            unlockable == dm.GetItem(EItem.OldMask) ||
            unlockable == dm.GetItem(EItem.Battery) ||
            unlockable == dm.GetItem(EItem.Key))
        { __result = false; return false; }
        return true;
    }
}

// ─────────────────────────────────────────────────────────────────
// WEAPON + TOME TOGGLES — expose canAlwaysToggle on all entries
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(DataManager), "Load")]
static class Patch_DataManager_Load
{
    [HarmonyPostfix]
    static unsafe void Postfix(DataManager __instance)
    {
        var tomes = __instance.GetAllTomes();
        if (tomes != null)
            for (int i = 0; i < tomes.Count; i++)
                tomes[i].canAlwaysToggle = true;

        var weapons = __instance.GetAllWeapons();
        if (weapons != null)
            for (int i = 0; i < weapons.Count; i++)
            {
                weapons[i].canAlwaysToggle = true;
                var ud = weapons[i].upgradeData;
                if (ud?.upgradeModifiers == null) continue;
                AddUpgradeStat(ud.upgradeModifiers, (EStat)18, 0.05f);
                AddUpgradeStat(ud.upgradeModifiers, (EStat)19, 0.25f);
                RemoveStat(ud.upgradeModifiers, 24);
            }

        var borgar = __instance.GetItem(EItem.Borgar);
        if (borgar != null) borgar.canAlwaysToggle = true;

        // Bob's Lantern → Legendary; Energy Core → Epic
        var bobsLantern = __instance.GetItem(EItem.BobsLantern);
        var energyCore  = __instance.GetItem(EItem.EnergyCore);
        if (bobsLantern != null) bobsLantern.rarity = EItemRarity.Legendary;
        if (energyCore  != null) energyCore.rarity  = EItemRarity.Epic;

        // Swap Electric Plug (rare→epic) with Spiky Shield (epic→rare)
        var electricPlug = __instance.GetItem(EItem.ElectricPlug);
        var spikyShield  = __instance.GetItem(EItem.SpikyShield);
        if (electricPlug != null) electricPlug.rarity = EItemRarity.Epic;
        if (spikyShield  != null) { spikyShield.rarity = EItemRarity.Rare; spikyShield.canAlwaysToggle = true; }

        // Swap Sucky Magnet (legendary→epic) with Scarf (legendary→rare)
        var suckyMagnet = __instance.GetItem(EItem.SuckyMagnet);
        var scarf       = __instance.GetItem(EItem.Scarf);
        if (suckyMagnet != null) { suckyMagnet.rarity = EItemRarity.Epic; suckyMagnet.canAlwaysToggle = false; suckyMagnet.sortingPriority = -1000; }
        if (scarf       != null) { scarf.rarity       = EItemRarity.Rare; scarf.canAlwaysToggle       = false; }

        // Swap Backpack (→Common) with Cursed Doll (→Legendary, toggleable)
        var backpack   = __instance.GetItem(EItem.Backpack);
        var cursedDoll = __instance.GetItem(EItem.CursedDoll);
        if (backpack   != null) { backpack.rarity   = EItemRarity.Common;    backpack.canAlwaysToggle   = false; }
        if (cursedDoll != null) { cursedDoll.rarity = EItemRarity.Legendary; cursedDoll.canAlwaysToggle = true;  }

        // Beer — make toggleable
        var beer = __instance.GetItem(EItem.Beer);
        if (beer != null) beer.canAlwaysToggle = true;

        // Echo Shard, Brass Knuckles, Idle Juice, Demonic Blood — non-toggleable
        var echoShard     = __instance.GetItem(EItem.EchoShard);
        var brassKnuckles = __instance.GetItem(EItem.BrassKnuckles);
        var idleJuice     = __instance.GetItem(EItem.IdleJuice);
        var demonicBlood  = __instance.GetItem(EItem.DemonicBlood);
        if (echoShard     != null) echoShard.canAlwaysToggle     = false;
        if (brassKnuckles != null) brassKnuckles.canAlwaysToggle = false;
        if (idleJuice     != null) idleJuice.canAlwaysToggle     = false;
        if (demonicBlood  != null) demonicBlood.canAlwaysToggle  = false;

        // Thunder Mitts, Phantom Shroud — toggleable
        var thunderMitts  = __instance.GetItem(EItem.GloveLightning);
        var phantomShroud = __instance.GetItem(EItem.PhantomShroud);
        if (thunderMitts  != null) thunderMitts.canAlwaysToggle  = true;
        if (phantomShroud != null) { phantomShroud.rarity = EItemRarity.Rare; phantomShroud.canAlwaysToggle = true; }

        // Common toggleable
        foreach (var e in new[] { EItem.Medkit, EItem.SlipperyRing, EItem.Oats, EItem.GoldenGlove })
        { var it = __instance.GetItem(e); if (it != null) it.canAlwaysToggle = true; }

        // Common non-toggleable
        foreach (var e in new[] { EItem.Skuleg, EItem.OldMask, EItem.Battery, EItem.Key })
        { var it = __instance.GetItem(e); if (it != null) it.canAlwaysToggle = false; }



        var chars = __instance.unsortedCharacterData;
        if (chars != null)
            for (int i = 0; i < chars.Count; i++)
            {
                var cd = chars[i];
                if (cd?.statModifiers == null) continue;
                var mods = cd.statModifiers;
                float sumSpeed = 0f, sumJump = 0f, sumPickup = 0f;
                for (int j = 0; j < mods.Count; j++)
                {
                    var mod = mods[j];
                    if (mod == null) continue;
                    int   rawStat = *(int*)  (mod.Pointer + 0x10);
                    float rawVal  = *(float*)(mod.Pointer + 0x18);
                    if      (rawStat == 25) sumSpeed  += rawVal;
                    else if (rawStat == 26) sumJump   += rawVal;
                    else if (rawStat == 29) sumPickup += rawVal;
                }
                // EStat(25) speed: floor at 1.2x (0.2) — only boost slow chars, never reduce fast ones
                // EStat(26)=3.0 → jump 10 (base 7); EStat(29)=5.0 → pickup 10 (base 5)
                float speedDelta  = 0.20f - sumSpeed;   // only apply if positive (never reduce)
                float jumpDelta   = 3.00f - sumJump;
                float pickupDelta = 5.00f - sumPickup;
                if (speedDelta  > 0.001f) AppendFlat(cd.statModifiers, (EStat)25, speedDelta);
                if (System.Math.Abs(jumpDelta)   > 0.001f) AppendFlat(cd.statModifiers, (EStat)26, jumpDelta);
                if (System.Math.Abs(pickupDelta) > 0.001f) AppendFlat(cd.statModifiers, (EStat)29, pickupDelta);

                // Add 10 starting shield to characters who have none
                float sumShield = 0f;
                for (int j = 0; j < mods.Count; j++)
                {
                    var mod = mods[j];
                    if (mod == null) continue;
                    if (*(int*)(mod.Pointer + 0x10) == 2) sumShield += *(float*)(mod.Pointer + 0x18);
                }
                if (sumShield < 1f) AppendFlat(cd.statModifiers, (EStat)2, 10f);
            }

        // ── Bow + Revolver — +2 projectiles per level instead of +1 ──
        foreach (var eWeapon in new[] { EWeapon.Bow, EWeapon.Revolver })
        {
            var wd = __instance.GetWeapon(eWeapon);
            if (wd?.upgradeData?.upgradeModifiers == null) continue;
            bool found = false;
            for (int i = 0; i < wd.upgradeData.upgradeModifiers.Count; i++)
            {
                var mod = wd.upgradeData.upgradeModifiers[i];
                if (mod == null) continue;
                if ((int)mod.stat == 16) // EStat.Projectiles
                {
                    mod.modification = 2f;
                    found = true;
                    break;
                }
            }
            if (!found)
                AddUpgradeStat(wd.upgradeData.upgradeModifiers, (EStat)16, 2f);
        }

        // ── Combat scaling ramp ────────────────────────────────────────
        // hpMultiplicationPerMinute   0.1  → 0.2   (2×)
        // damageMultiplicationPerMinute 0.028 → 0.056 (2×)
        // knockbackResistancePerMinute  0.028 → 0.0
        AccessTools.Field(typeof(CombatScaling), "hpMultiplicationPerMinute")
                   ?.SetValue(null, 0.2f);
        AccessTools.Field(typeof(CombatScaling), "damageMultiplicationPerMinute")
                   ?.SetValue(null, 0.056f);
        AccessTools.Field(typeof(CombatScaling), "knockbackResistancePerMinute")
                   ?.SetValue(null, 0.0f);
    }

    static unsafe void RemoveStat(Il2CppSystem.Collections.Generic.List<StatModifier> mods, int statInt)
    {
        for (int i = mods.Count - 1; i >= 0; i--)
        {
            var mod = mods[i];
            if (mod == null) continue;
            if (*(int*)(mod.Pointer + 0x10) == statInt) mods.RemoveAt(i);
        }
    }

    static void AppendFlat(Il2CppSystem.Collections.Generic.List<StatModifier> mods, EStat stat, float value)
    {
        var m = new StatModifier();
        m.stat         = stat;
        m.modifyType   = EStatModifyType.Flat;
        m.modification = value;
        mods.Add(m);
    }

    static void AddUpgradeStat(Il2CppSystem.Collections.Generic.List<StatModifier> mods, EStat stat, float modification)
    {
        for (int i = 0; i < mods.Count; i++)
            if ((int)mods[i].stat == (int)stat) return;
        var m = new StatModifier();
        m.stat         = stat;
        m.modifyType   = EStatModifyType.Flat;
        m.modification = modification;
        mods.Add(m);
    }
}

// ─────────────────────────────────────────────────────────────────
// MINIMAP ICONS — chest type colors & microwave tier colors
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(InteractableChest), "Start")]
static class Patch_Chest_MinimapIcon
{
    [HarmonyPostfix]
    static void Postfix(InteractableChest __instance)
    {
        Color color;
        if (__instance.chestType == EChest.Normal)
            color = new Color(0.55f, 0.28f, 0.08f); // brown
        else if (__instance.chestType == EChest.Free || __instance.chestType == EChest.FreeCrypt)
            color = new Color(1f, 0.85f, 0.1f);     // gold
        else
            return;

        var iconTransform = __instance.icon;
        if (iconTransform == null) return;
        IconColorHelper.ApplyColor(iconTransform.gameObject, color);
        iconTransform.localScale *= 0.5f;
    }
}

[HarmonyPatch(typeof(InteractableMicrowave), "Start")]
static class Patch_Microwave_Start
{
    [HarmonyPostfix]
    static void Postfix(InteractableMicrowave __instance) => MicrowaveIconHelper.ApplyMicrowaveColor(__instance, scaleDown: true);
}

[HarmonyPatch(typeof(InteractableMicrowave), "set_rarity")]
static class Patch_Microwave_SetRarity
{
    [HarmonyPostfix]
    static void Postfix(InteractableMicrowave __instance) => MicrowaveIconHelper.ApplyMicrowaveColor(__instance);
}

static class MicrowaveIconHelper
{
    private const int MinimapIconOffset = 0xD8;

    internal static unsafe void ApplyMicrowaveColor(InteractableMicrowave instance, bool scaleDown = false)
    {
        var iconObj = instance.minimapIcon;
        if (iconObj == null)
        {
            var ptr = *(System.IntPtr*)(instance.Pointer + MinimapIconOffset);
            if (ptr == System.IntPtr.Zero) return;
            iconObj = new GameObject(ptr);
        }

        IconColorHelper.ApplyColor(iconObj, IconColorHelper.MicrowaveRarityColor(instance.rarity));
        if (scaleDown) iconObj.transform.localScale *= 0.5f;
    }
}

static class IconColorHelper
{
    internal static Color MicrowaveRarityColor(EItemRarity rarity) => rarity switch
    {
        EItemRarity.Rare      => new Color(0.5f, 0.75f, 1f),    // light blue
        EItemRarity.Epic      => new Color(0.8f, 0.6f, 1f),     // light purple
        EItemRarity.Legendary => new Color(1f, 0.95f, 0.6f),    // light gold
        _                     => Color.white,
    };

    internal static Color ShadyGuyRarityColor(EItemRarity rarity) => rarity switch
    {
        EItemRarity.Rare      => new Color(0.1f, 0.25f, 0.7f),  // dark blue
        EItemRarity.Epic      => new Color(0.35f, 0.05f, 0.6f), // dark purple
        EItemRarity.Legendary => new Color(0.7f, 0.5f, 0.05f),  // dark gold
        _                     => new Color(0.6f, 0.6f, 0.6f),   // grey
    };

    internal static void ApplyColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;
        var mat = r.material;
        mat.mainTexture = Texture2D.whiteTexture;
        mat.color = color;
    }
}

// ─────────────────────────────────────────────────────────────────
// SHRINE / INTERACTABLE MINIMAP ICONS
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(BaseInteractable), "Start")]
static class Patch_BaseInteractable_Start
{
    [HarmonyPostfix]
    static void Postfix(BaseInteractable __instance)
    {
        var cursed = __instance.TryCast<InteractableShrineCursed>();
        if (cursed != null) { ApplyShrineColor(cursed.minimapIcon, 0x58, cursed.Pointer, new Color(1f, 0.4f, 0.4f)); return; } // light red

        var challenge = __instance.TryCast<InteractableShrineChallenge>();
        if (challenge != null) { ApplyShrineColor(challenge.minimapIcon, 0x58, challenge.Pointer, new Color(1f, 0.4f, 0.7f)); return; } // pink

        var magnet = __instance.TryCast<InteractableShrineMagnet>();
        if (magnet != null) { ApplyShrineColor(magnet.minimapIcon, 0x58, magnet.Pointer, Color.black); return; }
    }

    static unsafe void ApplyShrineColor(GameObject icon, int offset, System.IntPtr ptr, Color color)
    {
        if (icon == null)
        {
            var rawPtr = *(System.IntPtr*)(ptr + offset);
            if (rawPtr == System.IntPtr.Zero) return;
            icon = new GameObject(rawPtr);
        }
        IconColorHelper.ApplyColor(icon, color);
        icon.transform.localScale *= 0.5f;
    }
}

// ─────────────────────────────────────────────────────────────────
// SHADY GUY — no minimapIcon field; search children for MinimapIcon
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(InteractableShadyGuy), "Start")]
static class Patch_ShadyGuy_Start
{
    [HarmonyPostfix]
    static void Postfix(InteractableShadyGuy __instance)
    {
        var root = __instance.transform.parent;
        if (root == null) return;
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.gameObject.layer == 14) // minimap layer
            {
                IconColorHelper.ApplyColor(child.gameObject, IconColorHelper.ShadyGuyRarityColor(__instance.rarity));
                child.localScale *= 0.5f;
                return;
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────
// MICROWAVE SPAWN COUNT — default 2, max 3
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch]
static class Patch_MicrowaveSpawnCount
{
    static System.Reflection.MethodBase TargetMethod() =>
        HarmonyLib.AccessTools.Method(typeof(RandomObjectPlacer), "RandomObjectSpawner",
            new[] { typeof(RandomMapObject), typeof(float) });

    [HarmonyPrefix]
    static void Prefix(RandomMapObject randomObject)
    {
        if (randomObject?.prefabs == null) return;
        foreach (var prefab in randomObject.prefabs)
        {
            if (prefab == null) continue;
            if (prefab.name.IndexOf("Microwave", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                randomObject.amount    = 2;
                randomObject.maxAmount = 3;
                break;
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────
// GREED SHRINE — +5% XP, +5% luck (native difficulty unchanged)
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(InteractableShrineGreed), nameof(InteractableShrineGreed.Interact))]
static class Patch_ShrineGreed_Stats
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.ShrineGreed");

    static bool _wasNotDone;

    [HarmonyPrefix]
    static void Prefix(InteractableShrineGreed __instance)
    {
        _wasNotDone = !__instance.done;
    }

    [HarmonyPostfix]
    static void Postfix(InteractableShrineGreed __instance)
    {
        if (!_wasNotDone || !__instance.done) return;
        Apply(EStat.XpIncreaseMultiplier, 0.05f);
        Apply(EStat.Luck,                 0.05f);
    }

    static void Apply(EStat stat, float amount)
    {
        try
        {
            var mod = new StatModifier();
            mod.stat         = stat;
            mod.modifyType   = EStatModifyType.Flat;
            mod.modification = amount;
            var effect = new EffectStat();
            effect.effectType   = EEncounterEffect.StatChange;
            effect.statModifier = mod;
            effect.permanent    = true;
            effect.ApplyEffect();
            try { UiManager.Instance?.scoreUi?.AddScore(mod, true); } catch { }
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Apply {stat} threw: {ex}");
        }
    }
}

// ─────────────────────────────────────────────────────────────────
// INTERACTABLE POT — drop random powerup instead of always health
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(InteractablePot), "SpawnStuff")]
static class Patch_InteractablePot_SpawnStuff
{
    static readonly EPickup[] Powerups =
    {
        EPickup.Health, EPickup.Nuke, EPickup.Time, EPickup.Shield,
        EPickup.Rage,   EPickup.Haste, EPickup.Stonks, EPickup.Magnet,
    };

    [HarmonyPrefix]
    static void Prefix(ref EPickup ePickup)
    {
        if (ePickup == EPickup.Health)
            ePickup = Powerups[UnityEngine.Random.Range(0, Powerups.Length)];
    }
}

// ─────────────────────────────────────────────────────────────────
// HEART POWERUP — full heal on pickup
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(PlayerHealth), "OnPickup")]
static class Patch_PlayerHealth_OnPickup_FullHeal
{
    [HarmonyPrefix]
    static bool Prefix(PlayerHealth __instance, Pickup pickup)
    {
        if (pickup == null || pickup.ePickup != EPickup.Health) return true;
        __instance.Heal((float)__instance.maxHp, false);
        return false;
    }
}

// ─────────────────────────────────────────────────────────────────
// GOLDEN RING — grant +1 banish per ring held
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemInventory), "AddItem", typeof(EItem), typeof(int))]
static class Patch_GoldenRing_Add
{
    [HarmonyPostfix]
    static void Postfix(EItem eItem, int count)
    {
        if (eItem != EItem.GoldenRing) return;
        try
        {
            var inv = MapController.GetPlayerInventory(null);
            if (inv != null) inv.banishes += count;
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ItemInventory), "RemoveItem", typeof(EItem), typeof(bool))]
static class Patch_GoldenRing_Remove
{
    [HarmonyPostfix]
    static void Postfix(EItem eItem)
    {
        if (eItem != EItem.GoldenRing) return;
        try
        {
            var inv = MapController.GetPlayerInventory(null);
            if (inv != null) inv.banishes = System.Math.Max(0, inv.banishes - 1);
        }
        catch { }
    }
}

// ─────────────────────────────────────────────────────────────────
// ECHO SHARD — overflow % becomes extra shard chances
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemEchoShard), "GetExtraShards")]
static class Patch_EchoShard_Overflow
{
    [HarmonyPostfix]
    static void Postfix(ItemEchoShard __instance, ref int __result)
    {
        float c = __instance.chance;
        if (c <= 1f) return; // no overflow — original result is correct
        int   guaranteed = (int)c;
        float frac       = c - guaranteed;
        __result = guaranteed + (UnityEngine.Random.value < frac ? 1 : 0);
    }
}

// ─────────────────────────────────────────────────────────────────
// LEADERBOARD — block Steam submission, relay to local server
// ─────────────────────────────────────────────────────────────────

// Intercept at Leaderboards.UploadScore — the earliest entry point before any
// server submissions, score hashing, or direct Steamworks calls happen inside it.
// Return false blocks ALL original logic (including suspicious hash/server calls
// and QueueLeaderboardUpload). QueueLeaderboardUpload + UploadLeaderboardScore
// are also blocked below as a secondary safety net.
[HarmonyPatch(typeof(Leaderboards), "UploadScore")]
static class Patch_Leaderboard_UploadScore_Entry
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.LbUpload");

    static int   _lastScore;
    static float _lastTime = -999f;

    [HarmonyPrefix]
    static bool Prefix(int score)
    {
        try
        {
            if (score == _lastScore && UnityEngine.Time.realtimeSinceStartup - _lastTime < 5f)
                return false;

            _lastScore = score;
            _lastTime  = UnityEngine.Time.realtimeSinceStartup;

            int character = 0;
            var pm = PlayerMovement.Instance;
            if (pm != null)
                unsafe { character = (int)(*(int*)((System.IntPtr)pm.Pointer + 0x1B8)); }

            LeaderboardRelay.SendBothBoards(score, character);
        }
        catch (System.Exception ex)
        {
            Log.LogWarning($"Capture failed: {ex.Message}");
        }
        return false; // block all original logic — nothing reaches Steam or game servers
    }
}

// Secondary blocks — in case anything calls these directly, bypassing UploadScore.
[HarmonyPatch(typeof(SteamLeaderboardsManagerNew), "QueueLeaderboardUpload")]
static class Patch_Leaderboard_Queue
{
    [HarmonyPrefix]
    static bool Prefix() => false;
}


[HarmonyPatch(typeof(EffectManager), "SpawnTornadoes")]
static class Patch_DisableTornadoes
{
    [HarmonyPrefix]
    static bool Prefix() => false;
}

[HarmonyPatch(typeof(SteamLeaderboardsManagerNew), "UploadLeaderboardScore")]
static class Patch_Leaderboard_UploadDirect
{
    [HarmonyPrefix]
    static bool Prefix() => false;
}

// CanShowScore validates details[] — bypass entirely since all entries come from our server.
// Do not bind int[] param — IL2CPP passes Il2CppStructArray which won't match and causes faults.
[HarmonyPatch(typeof(Leaderboards), "CanShowScore")]
static class Patch_CanShowScore
{
    [HarmonyPrefix]
    static bool Prefix(int score, ref string s, ref bool __result)
    {
        s = score.ToString("N0");
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(SteamLeaderboardsManagerNew), "DownloadLeaderboardEntries")]
static class Patch_Leaderboard_Download
{
    [HarmonyPrefix]
    static bool Prefix() => false; // block Steam entirely — we inject from LeaderboardUiNew.Start
}

// Hook Start so we have the exact lb the UI is subscribed to — no string matching needed.
[HarmonyPatch(typeof(LeaderboardUiNew), "Start")]
static class Patch_LeaderboardUiNew_Start
{
    [HarmonyPostfix]
    static unsafe void Postfix(LeaderboardUiNew __instance)
    {
        if (!LeaderboardRelay.Enabled) return;
        var lbPtr = *(System.IntPtr*)(__instance.Pointer + 0x48);
        if (lbPtr == System.IntPtr.Zero) return;
        var lb = new SteamLeaderboardNew(lbPtr);
        LeaderboardInjector.ReplaceOrQueue(lb);
    }
}

static class LeaderboardRelay
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.Leaderboard");

    static string Base => Plugin.LeaderboardServer.TrimEnd('/');
    internal static bool Enabled => !string.IsNullOrEmpty(Plugin.LeaderboardServer);

    internal static void SendBothBoards(int score, int character)
    {
        Send("kills", score, character);
    }

    internal static void Send(string board, int score, int character)
    {
        if (!Enabled) return;
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                ulong steamId = Steamworks.SteamUser.GetSteamID().m_SteamID;
                string name   = Steamworks.SteamFriends.GetPersonaName();
                string payload = $"{{\"board\":\"{board}\",\"score\":{score}," +
                                 $"\"steamId\":\"{steamId}\",\"name\":{System.Text.Json.JsonSerializer.Serialize(name)}," +
                                 $"\"characterIndex\":{character}," +
                                 $"\"modVersion\":\"{Plugin.ModVersion}\"," +
                                 $"\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()}}}";

                using var client = new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(5) };
                var content = new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(Base + "/submit", content);
                Log.LogInfo($"Submitted score {score} char={character} on '{board}' as {name}.");
            }
            catch (System.Exception ex)
            {
                Log.LogWarning($"Leaderboard server unreachable: {ex.Message}");
            }
        });
    }

    internal static async System.Threading.Tasks.Task<string> FetchEntries(string board, int count = 20)
    {
        if (!Enabled) return "";
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(5) };
            return await client.GetStringAsync($"{Base}/entries?board={System.Uri.EscapeDataString(board)}&count={count}");
        }
        catch { return ""; }
    }
}

// ─────────────────────────────────────────────────────────────────
// LEADERBOARD INJECTOR — replace Steam entries with server data
// ─────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────
// DEBUG — LightningOrb × BluetoothDagger / LightningStaff stun bug
//
// Theory A: ProjectileBluetooth.HitTarget never sets dc.element = Lightning.
//   LOrb.ProcOnHitEffects gates the stun path on (dc.element == Lightning),
//   so BT-Dagger hits always fail the check and never stun.
//   LightningBolt.TryInit explicitly writes *(dc+0x34) = 1, so Staff hits pass.
//
// Theory B: Both stun callsites in ProcOnHitEffects pass a hardcoded global
//   float as duration with no GetStat(DurationMultiplier) lookup anywhere.
//   Duration Multiplier stat may be fully ignored for LOrb stuns.
//
// Log output lands in BepInEx/LogOutput.log under source "MBM.LOrb".
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemLightningOrb), "ProcOnHitEffects")]
static class Patch_LOrb_ProcOnHitEffects_Debug
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MBM.LOrb");

    [HarmonyPrefix]
    static void Prefix(DamageContainer dc)
    {
        if (dc == null) return;
        bool lightningCheck = (int)dc.element == 1; // EElement.Lightning = 1
        Log.LogInfo(
            $"[LOrb.ProcOnHitEffects] " +
            $"element={dc.element}({(int)dc.element}) " +
            $"procCoeff={dc.procCoefficient:F3} " +
            $"src=\"{dc.damageSource}\" " +
            $"→ lightningGate={lightningCheck}");
    }
}

// TryProcStun is only reached from ProcOnHitEffects after the element gate passes.
// If this never logs while BT-Dagger fires, Bug A is confirmed.
[HarmonyPatch]
static class Patch_LOrb_TryProcStun_Debug
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MBM.LOrb");

    static System.Reflection.MethodBase TargetMethod() =>
        HarmonyLib.AccessTools.Method(typeof(ItemLightningOrb), "TryProcStun");

    [HarmonyPrefix]
    static void Prefix(DamageContainer dc, float overrideProcCoefficient)
    {
        string coeff = dc != null ? dc.procCoefficient.ToString("F3") : "null";
        Log.LogInfo(
            $"[LOrb.TryProcStun] " +
            $"element={dc?.element}({(int)(dc?.element ?? 0)}) " +
            $"procCoeff={coeff} " +
            $"overrideCoeff={overrideProcCoefficient:F3}");
    }
}

// ── Bluetooth Dagger element fix ─────────────────────────────────
// HitTarget creates a DamageContainer locally and never sets dc.element = Lightning.
// LightningBolt.TryInit does set it explicitly. This is a bug — BT Dagger should
// be treated as an electric-element weapon.
//
// Fix: flag when HitTarget is running, then stamp element=Lightning on whatever
// GetDamageContainer returns during that window. Single-player so no thread concern.
[HarmonyPatch(typeof(ProjectileBluetooth), "HitTarget")]
static class Patch_BT_HitTarget_ElementFix
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MBM.LOrb");

    internal static bool Active;

    [HarmonyPrefix]
    static void Prefix()
    {
        Active = true;
        Log.LogInfo("[BT.HitTarget] fired — element fix active");
    }

    [HarmonyPostfix]
    static void Postfix() => Active = false;
}

[HarmonyPatch]
static class Patch_GetDamageContainer_BluetoothFix
{
    static System.Reflection.MethodBase TargetMethod() =>
        HarmonyLib.AccessTools.Method(typeof(WeaponUtility), "GetDamageContainer",
            new[]
            {
                typeof(WeaponBase),
                HarmonyLib.AccessTools.TypeByName("ProjectileBase"),
                HarmonyLib.AccessTools.TypeByName("Enemy"),
                typeof(Vector3),
                typeof(float),
            });

    [HarmonyPostfix]
    static void Postfix(DamageContainer __result)
    {
        if (!Patch_BT_HitTarget_ElementFix.Active) return;
        if (__result == null) return;
        __result.element = (EElement)1; // EElement.Lightning
    }
}

// Mirror for Lightning Staff — confirms it fires and element IS set inside TryInit.
[HarmonyPatch(typeof(ProjectileLightningBolt), "TryInit")]
static class Patch_LightningBolt_TryInit_Debug
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MBM.LOrb");

    // Postfix fires after TryInit runs (element already written to DC and damage applied).
    [HarmonyPostfix]
    static void Postfix(bool __result)
    {
        Log.LogInfo($"[LightningBolt.TryInit] completed — hit={__result} (DC element was set to Lightning=1 inside TryInit)");
    }
}

static class LeaderboardInjector
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.LbInject");

    static volatile ServerEntry[]       _cache    = null;
    static volatile bool                _fetching = false;
    static volatile SteamLeaderboardNew _pending  = null;

    internal static bool IsFetching => _fetching;

    // Start (or restart) a fetch. Clears cache so stale data is never shown.
    internal static void BeginFetch()
    {
        if (_fetching) return;
        _fetching = true;
        _cache    = null;
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var json    = await LeaderboardRelay.FetchEntries("kills", 50);
                var entries = Parse(json);
                ModGui.MainThread.Enqueue(() =>
                {
                    _cache    = entries;
                    _fetching = false;
                    Log.LogInfo($"Fetch complete: {entries.Length} entries");
                    var p = _pending;
                    _pending = null;
                    if (p != null) Replace(p, entries);
                });
            }
            catch (System.Exception ex)
            {
                _fetching = false;
                Log.LogError($"Fetch error: {ex.Message}");
            }
        });
    }

    // Called when the leaderboard UI requests data.
    // If already cached: delay one frame so LeaderboardUiNew.Start can subscribe to A_LeaderboardReady first.
    // If still fetching: queue the board; fetch completion fires Replace via MainThread already.
    internal static void ReplaceOrQueue(SteamLeaderboardNew lb)
    {
        if (_cache != null)
        {
            var captured = _cache;
            ModGui.MainThread.Enqueue(() => Replace(lb, captured));
        }
        else
        {
            _pending = lb;
            BeginFetch(); // no-op if already in progress
        }
    }

    private static void Replace(SteamLeaderboardNew lb, ServerEntry[] entries)
    {
        // Request Steam to fetch persona names for all injected entries
        foreach (var e in entries)
            if (ulong.TryParse(e.SteamId, out var sid) && sid != 0)
                SteamFriends.RequestUserInformation(new CSteamID(sid), true);

        lb.globalEntries  = BuildList(entries);
        lb.friendsEntries = BuildFriendsList(entries);
        Log.LogInfo($"Replaced global({entries.Length}) + friends({lb.friendsEntries.Count}) on '{lb.lbName}', invoking A_LeaderboardReady");
        // We are already on the main thread (called from Harmony postfix on Unity callback).
        // Call directly — no need to enqueue; enqueue adds a frame delay and silently swallows Il2CppExceptions.
        try
        {
            var ev = SteamLeaderboardNew.A_LeaderboardReady;
            if (ev == null) { Log.LogWarning($"A_LeaderboardReady is null for '{lb.lbName}'"); return; }
            ev.Invoke(lb);
            Log.LogInfo($"A_LeaderboardReady invoked OK for '{lb.lbName}'");
        }
        catch (System.Exception ex)
        {
            Log.LogError($"A_LeaderboardReady threw {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    internal static Il2CppSystem.Collections.Generic.List<LeaderboardEntry> BuildList(ServerEntry[] entries)
    {
        var list = new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>();
        if (entries == null) return list;
        for (int i = 0; i < entries.Length; i++)
        {
            var se = entries[i];
            ulong sid = ulong.TryParse(se.SteamId, out var s) ? s : 0ul;
            // Rebuild a minimal details array the game expects:
            // details[1] = character (used by Leaderboards.GetCharacter).
            int[] details = new int[64];
            details[1] = se.CharacterIndex;
            var t = new LeaderboardEntry_t
            {
                m_steamIDUser = new CSteamID(sid),
                m_nGlobalRank = i + 1,
                m_nScore      = se.Score,
                m_cDetails    = details.Length,
            };
            list.Add(new LeaderboardEntry(t, details));
        }
        return list;
    }

    private static System.Collections.Generic.HashSet<ulong> GetSteamFriendIds()
    {
        var set = new System.Collections.Generic.HashSet<ulong>();
        try
        {
            // include self
            set.Add(SteamUser.GetSteamID().m_SteamID);
            int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < count; i++)
                set.Add(SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate).m_SteamID);
        }
        catch (System.Exception ex) { Log.LogWarning($"GetSteamFriendIds: {ex.Message}"); }
        return set;
    }

    internal static Il2CppSystem.Collections.Generic.List<LeaderboardEntry> BuildFriendsList(ServerEntry[] entries)
    {
        var friendIds = GetSteamFriendIds();
        var filtered  = new System.Collections.Generic.List<ServerEntry>();
        foreach (var e in entries)
            if (ulong.TryParse(e.SteamId, out var sid) && friendIds.Contains(sid))
                filtered.Add(e);
        return BuildList(filtered.ToArray());
    }

    private static ServerEntry[] Parse(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]") return new ServerEntry[0];
        try { return System.Text.Json.JsonSerializer.Deserialize<ServerEntry[]>(json) ?? new ServerEntry[0]; }
        catch { return new ServerEntry[0]; }
    }

    internal class ServerEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("score")]          public int    Score          { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("steamId")]        public string SteamId        { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("name")]           public string Name           { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("characterIndex")] public int    CharacterIndex { get; set; }
    }
}

// ─────────────────────────────────────────────────────────────────
// GREEN CREDIT CARD — chest price increase per card: 10% → 2%
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ItemCreditCardGreen), "OnInitOrAmountChanged")]
static class Patch_CreditCardGreen_ChestCost
{
    [HarmonyPostfix]
    static void Postfix(ItemCreditCardGreen __instance)
    {
        __instance.chestPriceIncreasePerAmount = 0.02f;
    }
}

[HarmonyPatch(typeof(ItemCreditCardGreen), "GetDescription")]
static class Patch_CreditCardGreen_Desc
{
    [HarmonyPostfix]
    static void Postfix(ref string __result)
    {
        if (__result != null)
            __result = __result.Replace("10%", "2%").Replace("10 %", "2%");
    }
}

// ─────────────────────────────────────────────────────────────────
// BACKPACK — description: +1 projectile → +2 projectiles
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ItemBase), "GetDescription")]
static class Patch_Backpack_Desc
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MBM.BackpackDesc");

    [HarmonyPostfix]
    static void Postfix(ItemBase __instance, ref string __result)
    {
        Log.LogInfo($"[GetDesc] type={__instance?.GetType().Name ?? "null"} result='{__result ?? "<null>"}'");
        if (__result == null) return;
        __result = __result
            .Replace("+1 Projectile Count", "+2 Projectile Count")
            .Replace("+1 projectile count", "+2 projectile count");
    }
}

// ─────────────────────────────────────────────────────────────────
// CURSED DOLL — 50% max HP damage, 7 cursed enemies per doll
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ItemCursedDoll), "OnInitOrAmountChanged")]
static class Patch_CursedDoll_Buff
{
    [HarmonyPostfix]
    static unsafe void Postfix(ItemCursedDoll __instance)
    {
        var p = __instance.Pointer;
        int amount = *(int*)(p + 0x18);          // ItemBase.amount
        *(float*)(p + 0x34) = 0.5f;              // damageMaxHpPercentage  0.3 → 0.5
        *(int*)(p + 0x38)   = 7;                 // enemiesCursedPerDoll   2   → 7
        *(int*)(p + 0x30)   = 7 * amount;        // maxNumCursedEnemies    recalc
    }
}

[HarmonyPatch(typeof(ItemCursedDoll), "GetDescription")]
static class Patch_CursedDoll_Desc
{
    [HarmonyPostfix]
    static void Postfix(ref string __result)
    {
        if (__result == null) return;
        __result = __result.Replace("30%", "50%").Replace("30 %", "50%");
    }
}

// ─────────────────────────────────────────────────────────────────
// STEAM UPLOAD BLOCK — lock upload_score_to_leaderboard = 0
// Forces the game's own setting to off so it never tries to submit
// to Steam. Our LeaderboardRelay prefix intercepts Leaderboards.UploadScore
// BEFORE this check runs, uploads to our server, and returns false —
// so it is completely independent of this setting.
//
// Pointer chain (all instance offsets from IL2CPP dump):
//   SaveManager instance
//     + 0x20 → ConfigSaveFile* config
//       + 0x18 → CFGameSettings* cfGameSettings
//         + 0x44 → int upload_score_to_leaderboard  (0 = off)
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(SaveManager), "Init")]
static class Patch_SaveManager_Init_LockUploadOff
{
    [HarmonyPostfix]
    static void Postfix(SaveManager __instance) => SteamUploadLocker.ForceOff(__instance);
}

[HarmonyPatch(typeof(SaveManager), "SaveConfig")]
static class Patch_SaveManager_SaveConfig_LockUploadOff
{
    // Zero the field BEFORE the save writes to disk so it persists as 0.
    [HarmonyPrefix]
    static void Prefix(SaveManager __instance) => SteamUploadLocker.ForceOff(__instance);
}

static class SteamUploadLocker
{
    internal const string FieldName = "upload_score_to_leaderboard";

    internal static unsafe void ForceOff(SaveManager sm)
    {
        if (sm == null) return;
        var smPtr = sm.Pointer;
        if (smPtr == System.IntPtr.Zero) return;
        var configPtr = *(System.IntPtr*)(smPtr + 0x20);    // SaveManager.config
        if (configPtr == System.IntPtr.Zero) return;
        var cfgPtr    = *(System.IntPtr*)(configPtr + 0x18); // ConfigSaveFile.cfGameSettings
        if (cfgPtr == System.IntPtr.Zero) return;
        *(int*)(cfgPtr + 0x44) = 0;                          // upload_score_to_leaderboard = 0
    }
}

// Disable the toggle in the settings UI — visible but not interactable.
// BetterSetting.disabledOverlay is a built-in overlay the game uses for locked settings.
// EnumSetting (which renders int settings like this one) does not override SetSetting,
// so patching the base class covers it.
[HarmonyPatch(typeof(BetterSetting), "SetSetting")]
static class Patch_BetterSetting_SetSetting_DisableUpload
{
    [HarmonyPostfix]
    static void Postfix(BetterSetting __instance, string settingName)
    {
        if (settingName != SteamUploadLocker.FieldName) return;
        var label = __instance.t_disabledText;
        if (label != null) label.text = "Disabled";
        var overlay = __instance.disabledOverlay;
        if (overlay != null) overlay.SetActive(true);
    }
}

// Belt-and-suspenders: block any write through the UI save path too.
[HarmonyPatch(typeof(CurrentSettings), "BetterUpdateCfSettings")]
static class Patch_CurrentSettings_BetterUpdateCfSettings_BlockUpload
{
    [HarmonyPrefix]
    static bool Prefix(string settingName) =>
        settingName != SteamUploadLocker.FieldName;
}

// ─────────────────────────────────────────────────────────────────
// TONY MCZOOM (Zooma passive) — +0.5 projectiles per level
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(PassiveAbilityZooma), "Init")]
static class Patch_Zooma_Init_Projectiles
{
    static readonly System.Reflection.MethodInfo _setStat =
        HarmonyLib.AccessTools.Method(typeof(PassiveAbility), "SetStat");

    [HarmonyPostfix]
    static void Postfix(PassiveAbilityZooma __instance)
    {
        PlayerXp.A_LevelUp += new System.Action<int>(level =>
        {
            try
            {
                var mod = new StatModifier();
                mod.stat         = (EStat)16;
                mod.modifyType   = EStatModifyType.Flat;
                mod.modification = 0.25f * level;
                _setStat.Invoke(__instance, new object[] { mod });
            }
            catch { }
        });
    }
}

// ─────────────────────────────────────────────────────────────────
// GOLDEN SHIELD — remove reduced gold from Kevin self-damage
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemGoldenShield), "OnPlayerTakeDamage")]
static class Patch_GoldenShield_NoKevinReduction
{
    [HarmonyPrefix]
    static void Prefix(DamageContainer dc)
    {
        if (dc != null && dc.damageSource == ItemKevin.damageSource)
            dc.damageSource = "";
    }
}

// ─────────────────────────────────────────────────────────────────
// CLOCK POWERUP — freeze run timer while TimeFreeze is active
// Only runTimer is frozen; stageTimer/difficultyTimer/etc. keep ticking
// so mob spawning and difficulty scaling are unaffected.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(MyTime), "Update")]
static class Patch_MyTime_Update_ClockTimer
{
    static float _savedStageTimer;

    [HarmonyPrefix]
    static void Prefix()
    {
        _savedStageTimer = MyTime.stageTimer;
    }

    [HarmonyPostfix]
    static void Postfix()
    {
        var inv = GameManager.Instance?.GetPlayerInventory();
        if (inv?.statusEffects?.HasStatusEffect(EStatusEffect.TimeFreeze) ?? false)
            MyTime.stageTimer = _savedStageTimer;
    }
}

// ─────────────────────────────────────────────────────────────────
// LEADERBOARD UI — suppress Steam data while our server fetch is in progress
// LeaderboardUiNew.OnLeaderboardReady hides the buffering spinner and renders
// entries. Returning false here keeps the spinner visible until IsFetching=false,
// at which point our Replace() fires A_LeaderboardReady again and it goes through.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(LeaderboardUiNew), "OnLeaderboardReady")]
static class Patch_LeaderboardUiNew_OnLeaderboardReady
{
    [HarmonyPrefix]
    static bool Prefix() => !LeaderboardInjector.IsFetching;
}

// ─────────────────────────────────────────────────────────────────
// MAIN MENU — append mod version to game version text + check for updates
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(MainMenu), "Start")]
static class Patch_MainMenu_Start_Version
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.Version");

    [HarmonyPostfix]
    static void Postfix()
    {
        ModGui.MainThread.Enqueue(() => ModGui.NeedVersionPatch = true);
        LeaderboardInjector.BeginFetch(); // pre-cache so leaderboard is instant when opened
        CheckVersionAsync();
    }

    static void CheckVersionAsync()
    {
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(5) };
                var json = await client.GetStringAsync(Plugin.LeaderboardServer.TrimEnd('/') + "/version");
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("required", out var req))
                {
                    string required = req.GetString() ?? "";
                    if (!string.IsNullOrEmpty(required) && required != Plugin.ModVersion)
                    {
                        Log.LogWarning($"Mod outdated: local={Plugin.ModVersion} required={required}");
                        ModGui.MainThread.Enqueue(() => ModGui.UpdateAvailable = true);
                    }
                    else
                    {
                        Log.LogInfo($"Mod version OK ({Plugin.ModVersion})");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.LogWarning($"Version check failed: {ex.Message}");
            }
        });
    }
}

// ─────────────────────────────────────────────────────────────────
// DAMAGE CHART — F2 toggle: clear stale rows before Start() rebuilds them
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(GameOverDamageSourcesUi), "Start")]
static class Patch_GameOverDamageSourcesUi_Start
{
    [HarmonyPrefix]
    static void Prefix()
    {
        var entries = GameObject.Find(
            "GameUI/GameUI/DeathScreen/StatsWindows/W_Damage/WindowLayers/Content/ScrollRect/ContentEntries");
        if (entries == null) return;
        var t = entries.transform;
        for (int i = t.childCount - 1; i >= 3; i--)
            UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }
}

[HarmonyPatch(typeof(GameManager), "StartPlaying")]
static class Patch_GameManager_StartPlaying_Chart
{
    [HarmonyPostfix]
    static void Postfix()
    {
        ModGui.ChartDisabled = false;
        // scene reloaded — force re-find of StatsWindows next F2 press
        var gui = UnityEngine.Object.FindObjectOfType<ModGui>();
        gui?.ResetChartCache();
    }
}

[HarmonyPatch(typeof(GameManager), "OnDied")]
static class Patch_GameManager_OnDied_Chart
{
    [HarmonyPostfix]
    static void Postfix()
    {
        ModGui.ChartDisabled = true;
        LeaderboardInjector.BeginFetch(); // refresh server data after each run
    }
}

// ─────────────────────────────────────────────────────────────────
// COMBAT SCALING — 2× HP/damage ramp, knockback resistance → 0
// Static field layout (from cctor):
//   +0x0  speedMultiplicationPerMinute    0.025  (unchanged)
//   +0x4  hpMultiplicationPerMinute       0.1  → 0.2
//   +0x8  damageMultiplicationPerMinute   0.028 → 0.056
//   +0xC  knockbackResistancePerMinute    0.028 → 0.0
// ─────────────────────────────────────────────────────────────────

