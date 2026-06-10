#nullable disable
using HarmonyLib;
using UnityEngine;
using TMPro;
using Assets.Scripts.Utility;
using Il2CppInterop.Runtime;
using System.Runtime.InteropServices;

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
using Assets.Scripts.UI.Localization;
using UnityEngine.Localization;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Actors;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Objects.Particles___Effects.ParticleOpacity;
using Assets.Scripts.Game.Spawning;

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
                // Cap Grandma's (6 stacks) and Meatball (6 stacks) via pool directly
                if (item.eItem == EItem.GrandmasSecretTonic)
                    item.maxAmount = item.maxAmountPerRun = 6;
                if (item.eItem == EItem.SpicyMeatball)
                    item.maxAmount = item.maxAmountPerRun = 6;

            }
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
    static void Postfix()
    {
        Patch_RunUnlockables_Init.RemoveItemCaps();
        ZaWarudoTracker.Reset();
        ModGui.CheatsUsed      = false;
        ModGui.GodMode         = false;
        ModGui.FlyMode         = false;
        ModGui.InstaKill       = false;
        ModGui.FreezeEnemies   = false;
    }
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
// MOVEMENT REWORK — IdleJuice (Turbo Juice)
// Originally activates while standing still. We invert it to activate
// while moving by lying to the Tick() method about its stored position:
//   moving  → pos = player.pos  → dist=0 → "still" branch → activates
//   still   → pos = (999999,…)  → dist huge → "moved" branch → deactivates
// ─────────────────────────────────────────────────────────────────

// Horizontal speed (units/sec) above which the player counts as "moving".
// Read from the Rigidbody velocity so it's frame-rate independent.
static class MovementHelper
{
    public const float MoveSpeedThreshold = 1.5f;

    public static bool IsPlayerMoving(PlayerMovement pm)
    {
        try
        {
            unsafe
            {
                var rbPtr = *(System.IntPtr*)(pm.Pointer + 0x48); // rb field
                if (rbPtr == System.IntPtr.Zero) return false;
                var v = new Rigidbody(rbPtr).velocity;
                // Ignore vertical — falling/jumping shouldn't count as ground movement.
                return (v.x * v.x + v.z * v.z) > (MoveSpeedThreshold * MoveSpeedThreshold);
            }
        }
        catch { return false; }
    }
}

[HarmonyPatch(typeof(ItemIdleJuice), "Tick")]
static class Patch_ItemIdleJuice_Movement
{
    [HarmonyPrefix]
    static void Prefix(ItemIdleJuice __instance)
    {
        try
        {
            var pm = PlayerMovement.Instance;
            if (pm == null) return;
            var pos = pm.transform.position;
            unsafe
            {
                var p = __instance.Pointer;
                if (MovementHelper.IsPlayerMoving(pm))      // moving → fake "still" so effect activates
                {
                    *(float*)(p + 0x3C) = pos.x;
                    *(float*)(p + 0x40) = pos.y;
                    *(float*)(p + 0x44) = pos.z;
                }
                else                          // still → fake "moved" so effect deactivates
                {
                    *(float*)(p + 0x3C) = 999999f;
                    *(float*)(p + 0x40) = 999999f;
                    *(float*)(p + 0x44) = 999999f;
                }
            }
        }
        catch { }
    }
}

// ─────────────────────────────────────────────────────────────────
// MOVEMENT REWORK — descriptions: "standing still" → "moving"
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ItemIdleJuice), "GetDescription")]
static class Patch_ItemIdleJuice_Desc
{
    [HarmonyPostfix]
    static void Postfix(ref string __result) => __result = MovementDesc.Fix(__result);
}

static class MovementDesc
{
    public static string Fix(string s)
    {
        if (s == null) return s;
        return s
            .Replace("Standing still", "Moving")
            .Replace("standing still", "moving")
            .Replace("Not moving",     "Moving")
            .Replace("not moving",     "moving")
            .Replace("Stand still",    "Keep moving")
            .Replace("stand still",    "keep moving")
            .Replace("Stay still",     "Keep moving")
            .Replace("stay still",     "keep moving");
    }
}

// ─────────────────────────────────────────────────────────────────
// RENAME — "Idle Juice" → "Turbo Juice"
// Item names come from the localization asset; intercept the central
// lookup and swap the string wherever it returns.
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(LocalizedString), "GetLocalizedString", new System.Type[] { })]
static class Patch_IdleJuice_Rename
{
    [HarmonyPostfix]
    static void Postfix(ref string __result)
    {
        if (__result == null) return;
        if (__result.Contains("Idle Juice"))
            __result = __result.Replace("Idle Juice", "Turbo Juice");
    }
}

// ─────────────────────────────────────────────────────────────────
// SIZE CAP — Grandma's Secret Tonic
// Force baseRadius=4, radiusPerAmount=2, maxRadius=16 → max 6 stacks.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemGrandmasSecretTonic), "OnInitOrAmountChanged")]
static class Patch_GrandmasTonic_SizeCap
{
    const float BaseRadius      = 4f;
    const float RadiusPerAmount = 2f;
    const float MaxRadius       = 16f;
    const int   MaxStacks       = 6;   // (16-4)/2

    [HarmonyPrefix]
    static unsafe void Prefix(ItemGrandmasSecretTonic __instance)
    {
        // Set maxRadius before the method runs so it uses our cap
        *(float*)(__instance.Pointer + 0x3C) = MaxRadius;
    }

    [HarmonyPostfix]
    static unsafe void Postfix(ItemGrandmasSecretTonic __instance)
    {
        // Read game's computed radius (includes size stat) before we overwrite it
        float gameRadius = *(float*)(__instance.Pointer + 0x40);
        *(float*)(__instance.Pointer + 0x34) = BaseRadius;
        *(float*)(__instance.Pointer + 0x38) = RadiusPerAmount;
        *(float*)(__instance.Pointer + 0x3C) = MaxRadius;
        int amount = *(int*)(__instance.Pointer + 0x18);
        *(float*)(__instance.Pointer + 0x40) = System.Math.Min(BaseRadius + amount * RadiusPerAmount, MaxRadius);
        if (gameRadius >= MaxRadius)
            SizeCapHelper.RemoveFromPool(EItem.GrandmasSecretTonic);
    }
}

// ─────────────────────────────────────────────────────────────────
// SPICY MEATBALL — 2× Grandma's values (base=8, perAmount=4, max=32)
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemSpicyMeatball), "OnInitOrAmountChanged")]
static class Patch_SpicyMeatball_Radius
{
    const float BaseRadius      = 8f;
    const float RadiusPerAmount = 4f;
    const float MaxRadius       = 32f;

    [HarmonyPrefix]
    static unsafe void Prefix(ItemSpicyMeatball __instance)
    {
        *(float*)(__instance.Pointer + 0x38) = MaxRadius;
    }

    [HarmonyPostfix]
    static unsafe void Postfix(ItemSpicyMeatball __instance)
    {
        // Read game's computed radius (includes size stat) before we overwrite it
        float gameRadius = *(float*)(__instance.Pointer + 0x3C);
        *(float*)(__instance.Pointer + 0x30) = BaseRadius;
        *(float*)(__instance.Pointer + 0x34) = RadiusPerAmount;
        *(float*)(__instance.Pointer + 0x38) = MaxRadius;
        int amount = *(int*)(__instance.Pointer + 0x18);
        *(float*)(__instance.Pointer + 0x3C) = System.Math.Min(BaseRadius + amount * RadiusPerAmount, MaxRadius);
        if (gameRadius >= MaxRadius)
            SizeCapHelper.RemoveFromPool(EItem.SpicyMeatball);
    }
}

// ─────────────────────────────────────────────────────────────────
// SIZE CAP — Quin's Mask (same as Grandma: base=4, perAmount=2, max=16)
// Field layout identical to Grandma: base 0x34, perAmount 0x38, max 0x3C, radius 0x40
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemQuinsMask), "OnInitOrAmountChanged")]
static class Patch_QuinsMask_SizeCap
{
    const float BaseRadius      = 4f;
    const float RadiusPerAmount = 2f;
    const float MaxRadius       = 16f;
    const int   MaxStacks       = 6;   // (16-4)/2

    [HarmonyPrefix]
    static unsafe void Prefix(ItemQuinsMask __instance)
    {
        // Set maxRadius before the method runs so it uses our cap
        *(float*)(__instance.Pointer + 0x3C) = MaxRadius;
    }

    [HarmonyPostfix]
    static unsafe void Postfix(ItemQuinsMask __instance)
    {
        // Read game's computed radius (includes size stat) before we overwrite it
        float gameRadius = *(float*)(__instance.Pointer + 0x40);
        *(float*)(__instance.Pointer + 0x34) = BaseRadius;
        *(float*)(__instance.Pointer + 0x38) = RadiusPerAmount;
        *(float*)(__instance.Pointer + 0x3C) = MaxRadius;
        int amount = *(int*)(__instance.Pointer + 0x18);
        *(float*)(__instance.Pointer + 0x40) = System.Math.Min(BaseRadius + amount * RadiusPerAmount, MaxRadius);
        if (gameRadius >= MaxRadius)
            SizeCapHelper.RemoveFromPool(EItem.QuinsMask);
    }
}

static class SizeCapHelper
{
    internal static void RemoveFromPool(EItem eItem)
    {
        var avail = RunUnlockables.availableItems;
        if (avail == null) return;
        foreach (var kvp in avail)
        {
            if (kvp.Value == null) continue;
            for (int i = kvp.Value.Count - 1; i >= 0; i--)
                if (kvp.Value[i]?.eItem == eItem) { kvp.Value.RemoveAt(i); return; }
        }
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
// POWER GLOVES — buff on-hit proc damage (~22 → ~200)
// GetDamage() = amount * baseDamageMultiplier * playerDamage. Scale the
// returned value so the buff still tracks player damage scaling.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemGlovesPower), "GetDamage")]
static class Patch_GlovesPower_Damage
{
    const float DamageScale = 9f;   // ~22 base proc → ~198

    [HarmonyPostfix]
    static void Postfix(ref float __result) => __result *= DamageScale;
}

// ─────────────────────────────────────────────────────────────────
// KEY — 15% chest open chance per stack (was 10%)
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemKey), "OnInitOrAmountChanged")]
static class Patch_ItemKey_Chance
{
    [HarmonyPrefix]
    static unsafe void Prefix(ItemKey __instance)
    {
        *(float*)(__instance.Pointer + 0x30) = 0.15f; // chancePerStack  0.1 → 0.15
    }
}

[HarmonyPatch(typeof(ItemKey), "GetDescription")]
static class Patch_ItemKey_Desc
{
    [HarmonyPostfix]
    static void Postfix(ref string __result)
    {
        if (__result != null)
            __result = __result.Replace("10%", "15%");
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
        *(float*)(__instance.Pointer + 0x38) = float.MaxValue;  // 0x38 = maxRadius/sizeCap
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
        // doubles amount so game computes 2x projectiles
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
// FORCE POOL AVAILABILITY — IsAvailable is the actual gate that
// RunUnlockables uses to build the item pool. Items deactivated in
// save (e.g. Sucky Magnet toggled off before we made it non-toggleable)
// return false from IsAvailable via the deactivated-set check.
// Prefix to force true for all ForcedPoolItems.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(MyAchievements), "IsAvailable")]
static class Patch_IsAvailable_ForcedItems
{
    [HarmonyPrefix]
    static bool Prefix(UnlockableBase unlockable, ref bool __result)
    {
        var dm = DataManager.Instance;
        if (dm == null) return true;
        foreach (var eItem in Plugin.ForcedPoolItems)
        {
            if (unlockable == dm.GetItem(eItem))
            {
                __result = true;
                return false;
            }
        }
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
                AddUpgradeStat(ud.upgradeModifiers, (EStat)18, 0.10f);
                AddUpgradeStat(ud.upgradeModifiers, (EStat)19, 0.20f);
                RemoveStat(ud.upgradeModifiers, 24);
            }

        var borgar = __instance.GetItem(EItem.Borgar);
        if (borgar != null) borgar.canAlwaysToggle = true;

        // Snapshot every item's ORIGINAL rarity before we change any, so the icon
        // recolor pass (Patch_UnlockContainer_IconRecolor) can remap each baked
        // rarity-outline from its old color to the new one.
        IconRecolor.Original.Clear();
        var allItemsSnap = __instance.unsortedItems;
        if (allItemsSnap != null)
            for (int i = 0; i < allItemsSnap.Count; i++)
            {
                var it = allItemsSnap[i];
                if (it != null) IconRecolor.Original[it.eItem] = it.rarity;
            }

        // Bob's Lantern → Legendary; Energy Core Legendary → Rare (weak item, better fit)
        var bobsLantern = __instance.GetItem(EItem.BobsLantern);
        var energyCore  = __instance.GetItem(EItem.EnergyCore);
        if (bobsLantern != null) bobsLantern.rarity = EItemRarity.Legendary;
        if (energyCore  != null) energyCore.rarity  = EItemRarity.Rare;

        // Swap Golden Shield (rare→epic) with Slurp Gloves / GloveBlood (epic→rare)
        var goldenShield = __instance.GetItem(EItem.GoldenShield);
        var slurpGloves  = __instance.GetItem(EItem.GloveBlood);
        if (goldenShield != null) goldenShield.rarity = EItemRarity.Epic;
        if (slurpGloves  != null) slurpGloves.rarity  = EItemRarity.Rare;

        // Swap Electric Plug (rare→epic); Spiky Shield (epic→legendary, buffed armor)
        var electricPlug = __instance.GetItem(EItem.ElectricPlug);
        var spikyShield  = __instance.GetItem(EItem.SpikyShield);
        if (electricPlug != null) electricPlug.rarity = EItemRarity.Epic;
        if (spikyShield  != null) { spikyShield.rarity = EItemRarity.Legendary; spikyShield.canAlwaysToggle = true; }

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

        // All rarity changes are done — recolor each changed item's baked rim at
        // the source (swaps ItemData.icon) so every UI surface shows the new
        // rarity color. Idempotent; skips items whose rarity we didn't change.
        if (allItemsSnap != null)
            for (int i = 0; i < allItemsSnap.Count; i++)
                IconRecolor.RecolorSource(allItemsSnap[i]);

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

        // ── Bow fire-rate buff ──
        // Fire interval = endCooldown / (baseAttackSpeed * globalAttackSpeed)  (burstTime=0).
        // Stock endCooldown 0.92 (~1.1 shots/s) felt too slow; 0.45 ≈ 2.2 shots/s.
        try
        {
            var bow = __instance.GetWeapon(EWeapon.Bow);
            if (bow != null) bow.endCooldown = 0.45f;
        }
        catch (System.Exception e) { Plugin.Log.LogError($"[BowBuff] {e.Message}"); }

        // ── Sniper tweak ──
        // Keep stock burstTime 1.0 (the burst/audio stays in sync). Halve the post-burst
        // recovery: endCooldown 1.0 → 0.5, so cycle = 0.5 + 1.0 = ~1.5s/shot (was 2.0s).
        // Lower minBurstInterval 0.3 → 0.1 so heavy projectile stacking fires a tighter burst.
        try
        {
            var snp = __instance.GetWeapon(EWeapon.Sniper);
            if (snp != null)
            {
                snp.minBurstInterval = 0.1f;
                snp.endCooldown      = 0.5f;
            }
        }
        catch (System.Exception e) { Plugin.Log.LogError($"[SniperBuff] {e.Message}"); }

        // ── Fire-rate buffs ──
        // GetCooldown = endCooldown/atk + numProj × max( minBurstInterval/atk,
        //               (burstTime/numProj)/atk, floor ); the burst term is
        //               SKIPPED entirely when burstTime <= 0.
        // These weapons are single-shot / burstTime-dominated, so the old
        // minBurstInterval-only tweak did nothing. Scale ALL THREE timing fields
        // by the divisor so every term shrinks → reliable ~div× faster firing.
        foreach (var pair in new[]
        {
            (ew: EWeapon.BlackHole,   div: 3f),
            (ew: EWeapon.Mine,        div: 2f),
            (ew: EWeapon.HeroSword,   div: 2f),
            (ew: EWeapon.PoisonFlask, div: 2f),
            (ew: EWeapon.Sword,       div: 2f),
            (ew: EWeapon.CorruptSword,div: 2f),
        })
        {
            try
            {
                var w = __instance.GetWeapon(pair.ew);
                if (w != null)
                {
                    w.endCooldown      /= pair.div;
                    if (w.burstTime > 0f) w.burstTime /= pair.div;
                    w.minBurstInterval /= pair.div;
                }
            }
            catch (System.Exception e) { Plugin.Log.LogError($"[FireRateBuff:{pair.ew}] {e.Message}"); }
        }

        // ── Scythe attack rate ~doubled ──
        // GetCooldown ≈ endCooldown + numProj×max(minBurstInterval, burstTime/numProj, floor).
        // burstTime(1.5) dominates over endCooldown(0.85), so halve BOTH:
        //   was 0.85 + 1.5 = 2.35s  →  0.425 + 0.75 = 1.175s (≈ half).
        try
        {
            var scythe = __instance.GetWeapon(EWeapon.Scythe);
            if (scythe != null)
            {
                scythe.endCooldown = 0.425f;
                scythe.burstTime   = 0.75f;
            }
        }
        catch (System.Exception e) { Plugin.Log.LogError($"[ScytheCooldown] {e.Message}"); }

        // cache Noelle (Enduring) serialized size values for Hoarder size scaling
        try
        {
            var noelle = __instance.GetCharacterData(ECharacter.Noelle);
            var pd = noelle?.passive;
            if (pd != null)
            {
                pd.Init(); // populates dummyPassive (PassiveData+0x38) if null
                unsafe
                {
                    System.IntPtr dummy = *(System.IntPtr*)(pd.Pointer + 0x38);
                    if (dummy != System.IntPtr.Zero)
                    {
                        Patch_Hoarder_EliteScaling.NoelleSizePerLevel = *(float*)(dummy + 0x18);
                        Patch_Hoarder_EliteScaling.NoelleMaxSize      = *(float*)(dummy + 0x1C);
                    }
                }
            }
        }
        catch { }


        // ── knockbackResistancePerMinute → 0 ──────────────────────────
        // Decompile of GetKnockbackResistanceMultiplierAddition confirms:
        //   static storage = *(longlong*)(classPtr + 0xB8)
        //   knockbackResistancePerMinute at offset 0xC within storage
        unsafe
        {
            try
            {
                var dmClass  = Il2CppInterop.Runtime.IL2CPP.il2cpp_object_get_class(__instance.Pointer);
                var image    = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_image(dmClass);
                var csClass  = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_from_name(image, "Assets.Scripts.Game.Combat", "CombatScaling");
                if (csClass != System.IntPtr.Zero)
                {
                    var statics = *(System.IntPtr*)(csClass + 0xB8);
                    if (statics != System.IntPtr.Zero)
                        *(float*)(statics + 0xC) = 0f;
                }
            }
            catch { }
        }

        // Force non-toggleable items into the loot pool permanently.
        // isEnabled must be true — FUN_180405d10 checks it before inItemPool.
        // Some items (e.g. SuckyMagnet) ship with isEnabled=false in asset data.
        foreach (var eItem in Plugin.ForcedPoolItems)
        {
            var data = __instance.GetItem(eItem);
            if (data != null) { data.inItemPool = true; data.isEnabled = true; }
        }
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
// CLOCK POWERUP — stage timer ticks at half rate during TimeFreeze
// stageTimer advances normally during time stops; subtract half each frame.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(MyTime), "Update")]
static class Patch_MyTime_Update_SlowStageTimer
{
    static System.IntPtr _statics  = System.IntPtr.Zero;
    static float         _preTick;
    static bool          _active;

    static unsafe System.IntPtr GetStatics()
    {
        if (_statics != System.IntPtr.Zero) return _statics;
        var dm = DataManager.Instance;
        if (dm == null) return System.IntPtr.Zero;
        var image = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_image(
                        Il2CppInterop.Runtime.IL2CPP.il2cpp_object_get_class(dm.Pointer));
        var cls   = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_from_name(
                        image, "Assets.Scripts.Utility", "MyTime");
        if (cls == System.IntPtr.Zero) return System.IntPtr.Zero;
        _statics = *(System.IntPtr*)(cls + 0xB8);
        return _statics;
    }

    [HarmonyPrefix]
    static unsafe void Prefix()
    {
        _active = false;
        try
        {
            if (GameManager.Instance?.IsTimeFreeze() != true) return;
            var statics = GetStatics();
            if (statics == System.IntPtr.Zero) return;
            _preTick = *(float*)(statics + 0x1C); // snapshot stageTimer
            _active  = true;
        }
        catch { }
    }

    [HarmonyPostfix]
    static unsafe void Postfix()
    {
        if (!_active) return;
        try
        {
            var statics = GetStatics();
            if (statics == System.IntPtr.Zero) return;
            float after   = *(float*)(statics + 0x1C);
            float advance = after - _preTick;
            *(float*)(statics + 0x1C) = _preTick + advance * 0.5f; // half rate, only stageTimer
        }
        catch { }
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
        Apply(EStat.XpIncreaseMultiplier, 0.05f, EStatModifyType.Addition);
        Apply(EStat.Luck,                 0.05f, EStatModifyType.Flat);
    }

    static void Apply(EStat stat, float amount, EStatModifyType modType = EStatModifyType.Flat)
    {
        try
        {
            var mod = new StatModifier();
            mod.stat         = stat;
            mod.modifyType   = modType;
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
            var inv = GameManager.Instance?.GetPlayerInventory();
            if (inv != null) inv.banishes += count;
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ItemInventory), "AddItem", typeof(EItem))]
static class Patch_GoldenRing_Add_Single
{
    [HarmonyPostfix]
    static void Postfix(EItem eItem)
    {
        if (eItem != EItem.GoldenRing) return;
        try
        {
            var inv = GameManager.Instance?.GetPlayerInventory();
            if (inv != null) inv.banishes += 1;
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
// ZA WARUDO — track cumulative pickups; remove from pool at 25 total
// Stack cap (10) handled by ActiveUncappedItems + GetItemCap above.
// ─────────────────────────────────────────────────────────────────

static class ZaWarudoTracker
{
    internal static int Total = 0;
    internal const  int PoolCap = 25;

    internal static void Reset() { Total = 0; }

    internal static void OnReceived(int count)
    {
        Total += count;
        if (Total >= PoolCap)
            SizeCapHelper.RemoveFromPool(EItem.ZaWarudo);
    }
}

[HarmonyPatch(typeof(ItemInventory), "AddItem", typeof(EItem), typeof(int))]
static class Patch_ZaWarudo_Add
{
    [HarmonyPostfix]
    static void Postfix(EItem eItem, int count)
    {
        if (eItem != EItem.ZaWarudo) return;
        try { ZaWarudoTracker.OnReceived(count); } catch { }
    }
}

[HarmonyPatch(typeof(ItemInventory), "AddItem", typeof(EItem))]
static class Patch_ZaWarudo_Add_Single
{
    [HarmonyPostfix]
    static void Postfix(EItem eItem)
    {
        if (eItem != EItem.ZaWarudo) return;
        try { ZaWarudoTracker.OnReceived(1); } catch { }
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
// CORRUPT (BOMBUS) CHEST — guaranteed Golden Ring.
// The Corrupted-rarity pool is empty in the live game, so the original
// GetRandomChestItem THROWS (index out of range) before returning. Skip
// it entirely for Corrupt chests and hand back a Golden Ring.
//
// (The old 1/400 → 1/128 normal-chest drop-rate boost was removed — it
//  inadvertently injected extra legendary-rarity rings on sub-legendary
//  rolls. Only BOMBUS's rigged chest is special-cased now.)
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ItemUtility), nameof(ItemUtility.GetRandomChestItem))]
static class Patch_GoldenRing_CorruptChest
{
    [HarmonyPrefix]
    static bool Prefix(EChest chestType, ref ItemData __result)
    {
        if (chestType != EChest.Corrupt) return true; // normal chests: run original untouched
        __result = DataManager.Instance?.GetItem(EItem.GoldenRing);
        return false; // skip original
    }
}

// ─────────────────────────────────────────────────────────────────
// CORRUPT CHEST — routing (→ChestEvil encounter) and loot (→Corrupted
// rarity items) are fully implemented; the dev just never assigned the
// evil chest mesh/material in the ChestOpening prefab, so it renders
// magenta. Fill the missing visual fields from the normal chest at
// runtime so BOMBUS's corrupt chest drop displays + opens correctly.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(ChestOpening), "SetChest")]
static class Patch_CorruptChest_Visual
{
    [HarmonyPrefix]
    static void Prefix(ChestOpening __instance, EChest chestType)
    {
        try
        {
            if (chestType == EChest.Corrupt)
            {
                // Force normal chest art onto the evil slots (dev left them unassigned)
                __instance.meshEvil    = __instance.meshNormal;
                __instance.matEvil     = __instance.matNormal;
                if (__instance.fxCorrupted == null) __instance.fxCorrupted = __instance.fxFinal;
            }
        }
        catch (System.Exception e) { Plugin.Log.LogError($"[CorruptChest] {e.Message}"); }
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

            // Only submit if this beats the cached personal best for this character
            if (PersonalTab.PersonalBests.TryGetValue(character, out int cachedBest) && score <= cachedBest)
            {
                Log.LogInfo($"[Leaderboard] Score {score} not a new record for char {character} (best={cachedBest}), skipping upload.");
                return false;
            }

            LeaderboardRelay.SendBothBoards(score, character);

            // Update local cache immediately so back-to-back runs don't re-submit
            PersonalTab.PersonalBests[character] = score;
        }
        catch (System.Exception ex)
        {
            Log.LogWarning($"Capture failed: {ex.Message}");
        }
        return false; // block all original logic — nothing reaches Steam or game servers
    }
}

// FINAL PORTAL — winning the run by entering the final-stage portal does not
// reliably upload the score (the game's win-path UploadScore fires late, after
// the player is being torn down, so our relay never runs). Force the upload the
// instant the player commits to the portal — Interact() returns true exactly
// once (guarded by its `done` flag), and the player still exists at that point.
// Routed through Leaderboards.UploadScore so our entry prefix handles relay,
// dedup, and personal-best gating (the game's own call, if any, is deduped).
[HarmonyPatch(typeof(InteractablePortalFinal), "Interact")]
static class Patch_FinalPortal_UploadScore
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.FinalPortal");

    [HarmonyPostfix]
    static void Postfix(bool __result)
    {
        if (!__result) return; // interaction didn't start (already used)
        try
        {
            int kills = Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats
                .GetStat(Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.kills);
            Log.LogInfo($"[FinalPortal] Run completed via final portal — uploading score (kills={kills}).");
            Leaderboards.UploadScore(kills);
        }
        catch (System.Exception ex)
        {
            Log.LogError($"[FinalPortal] Score upload failed: {ex.Message}");
        }
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

// Fourth block — raw Steamworks interop layer.
// Catches anything that bypasses the three managed blocks above,
// including CheckUploadQueue calling ISteamUserStats directly.
[HarmonyPatch(typeof(Steamworks.SteamUserStats), "UploadLeaderboardScore")]
static class Patch_Leaderboard_SteamRaw
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
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.LbDownload");

    [HarmonyPrefix]
    static bool Prefix(string lbName)
    {
        if (!LeaderboardRelay.Enabled) return true;
        var lb = FindBoard(lbName);
        if (lb != null)
        {
            Log.LogInfo($"Starting fetch for board '{lb.lbName}' (requested '{lbName}')");
            LeaderboardInjector.BeginFetch(lb);
        }
        return true; // let Steam download proceed so its callback fires A_LeaderboardReady
    }

    internal static SteamLeaderboardNew FindBoard(string lbName)
    {
        var all    = SteamLeaderboardsManagerNew.leaderboardKillsAllTime;
        var weekly = SteamLeaderboardsManagerNew.leaderboardKillsWeekly;
        if (all    != null && (all.lbName    == lbName || all.lbNameFriends    == lbName)) return all;
        if (weekly != null && (weekly.lbName == lbName || weekly.lbNameFriends == lbName)) return weekly;
        return null;
    }
}

[HarmonyPatch(typeof(SteamLeaderboardNew), "OnDownloadResultsGlobal")]
static class Patch_OnDownloadResultsGlobal
{
    // PREFIX: block original entirely when Personal tab owns the slots.
    // Without this, game fires A_LeaderboardReady internally BEFORE our postfix
    // can check IsActive, overwriting Personal tab display.
    [HarmonyPrefix]
    static bool Prefix() => !PersonalTab.IsActive;

    [HarmonyPostfix]
    static void Postfix(SteamLeaderboardNew __instance)
    {
        if (!LeaderboardRelay.Enabled || PersonalTab.IsActive) return;
        LeaderboardInjector.ReplaceEntriesIfReady(__instance);
    }
}

[HarmonyPatch(typeof(SteamLeaderboardNew), "OnDownloadResultsFriends")]
static class Patch_OnDownloadResultsFriends
{
    // PREFIX: block original when Personal tab active — same timing issue.
    [HarmonyPrefix]
    static bool Prefix() => !PersonalTab.IsActive;

    [HarmonyPostfix]
    static void Postfix(SteamLeaderboardNew __instance)
    {
        if (!LeaderboardRelay.Enabled || PersonalTab.IsActive) return;
        var all    = SteamLeaderboardsManagerNew.leaderboardKillsAllTime;
        var weekly = SteamLeaderboardsManagerNew.leaderboardKillsWeekly;
        if (__instance != all && __instance != weekly) return;
        var cache = LeaderboardInjector.CachedEntries;
        // Always replace — never leave Steam friends data visible.
        // Empty list if server cache not ready; filtered server data if it is.
        __instance.friendsEntries = cache != null && cache.Length > 0
            ? LeaderboardInjector.BuildFriendsList(cache)
            : new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>();
        // Re-invoke so game re-renders friends tab with correct rank formatting.
        try { SteamLeaderboardNew.A_LeaderboardReady?.Invoke(__instance); } catch { }
    }
}

// Override Steam name lookup with our server-cached names.
// SteamFriends.GetFriendPersonaName returns "[unknown]" until Steam fetches
// the user's info async. We already have the correct name from the server.
[HarmonyPatch(typeof(SteamFriends), nameof(SteamFriends.GetFriendPersonaName))]
static class Patch_GetFriendPersonaName
{
    [HarmonyPostfix]
    static void Postfix(CSteamID steamIDFriend, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || __result == "[unknown]")
            if (LeaderboardInjector.NameCache.TryGetValue(steamIDFriend.m_SteamID, out var cached))
                __result = cached;
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
        if (ModGui.CheatsUsed) { Log.LogInfo("[Leaderboard] Score blocked — F1 menu used this run."); return; }

        // Block if any mod other than ours is loaded from BepInEx plugins folder
        if (ModGui.UnauthorizedMods.Count > 0)
        {
            Log.LogWarning($"[Leaderboard] Score blocked — unauthorized mods: {string.Join(", ", ModGui.UnauthorizedMods)}");
            return;
        }
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

    internal static async System.Threading.Tasks.Task<string> FetchPersonal(string steamId)
    {
        if (!Enabled) return "";
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(8) };
            return await client.GetStringAsync($"{Base}/personal?steamId={System.Uri.EscapeDataString(steamId)}");
        }
        catch { return ""; }
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
    internal static bool Active;

    [HarmonyPrefix]
    static void Prefix() => Active = true;

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
                typeof(Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles.ProjectileBase),
                typeof(Enemy),
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

static class LeaderboardInjector
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.LbInject");

    static volatile ServerEntry[]       _cache    = null;
    static volatile bool                _fetching = false;
    static volatile SteamLeaderboardNew _pending  = null;
    static SteamLeaderboardNew          _lastLb   = null;
    static int                          _offset   = 0;

    internal static ServerEntry[] CachedEntries => _cache;
    internal static bool IsFetching => _fetching;
    internal static SteamLeaderboardNew LastLb => _lastLb;
    internal static int CurrentOffset => _offset;

    // Leaderboard currently shown by the active LeaderboardUiNew — set from Refresh postfix.
    // More reliable than _lastLb which gets overwritten by both weekly + all-time boards.
    internal static SteamLeaderboardNew ActiveLb { get; set; } = null;

    // steamId → display name from our server; used to override Steam's "[unknown]"
    internal static readonly System.Collections.Generic.Dictionary<ulong, string> NameCache = new();

    internal static void BeginFetch(SteamLeaderboardNew lb)
    {
        if (_fetching) return;
        _fetching = true;
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var json    = await LeaderboardRelay.FetchEntries("kills", 500);
                var entries = Parse(json);
                ModGui.MainThread.Enqueue(() =>
                {
                    _cache    = entries;
                    _fetching = false;
                    Log.LogInfo($"Fetch 'kills' complete: {entries.Length} entries");
                    var p = _pending;
                    _pending = null;
                    if (p != null && entries.Length > 0) Replace(p, entries);
                });
            }
            catch (System.Exception ex)
            {
                _fetching = false;
                Log.LogError($"Fetch 'kills' error: {ex.Message}");
            }
        });
    }

    internal static void ReplaceEntriesIfReady(SteamLeaderboardNew lb)
    {
        if (PersonalTab.IsActive) return; // Personal tab owns the slots
        var all    = SteamLeaderboardsManagerNew.leaderboardKillsAllTime;
        var weekly = SteamLeaderboardsManagerNew.leaderboardKillsWeekly;
        if (lb != all && lb != weekly) return;

        if (_cache != null && _cache.Length > 0)
            Replace(lb, _cache);
        else
        {
            // Server data not ready yet — wipe Steam entries immediately so no Steam
            // scores are ever visible. Re-invoke A_LeaderboardReady to push the empty
            // list to UI. Replace() will re-invoke once server data arrives via _pending.
            lb.globalEntries  = new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>();
            lb.friendsEntries = new Il2CppSystem.Collections.Generic.List<LeaderboardEntry>();
            _pending = lb;
            try { SteamLeaderboardNew.A_LeaderboardReady?.Invoke(lb); } catch { }
        }
    }

    // Scroll: jump to a new offset and re-display the global leaderboard.
    internal static void ScrollBy(int delta)
    {
        var lb = ActiveLb ?? _lastLb;
        if (PersonalTab.IsActive || lb == null || _cache == null || _cache.Length == 0) return;
        const int slots = 9;
        _offset = System.Math.Max(0, System.Math.Min(_offset + delta, System.Math.Max(0, _cache.Length - slots)));
        Replace(lb, _cache);
    }

    internal static void ResetScroll() => _offset = 0;

    private static void Replace(SteamLeaderboardNew lb, ServerEntry[] allEntries)
    {
        _lastLb = lb;

        // Clamp offset and build the visible window
        const int slots = 9;
        _offset = System.Math.Max(0, System.Math.Min(_offset, System.Math.Max(0, allEntries.Length - slots)));
        int count  = System.Math.Min(slots, allEntries.Length - _offset);
        var window = new ServerEntry[count];
        System.Array.Copy(allEntries, _offset, window, 0, count);

        // Cache server names and request Steam info (for avatars)
        foreach (var e in allEntries)
            if (ulong.TryParse(e.SteamId, out var sid) && sid != 0)
            {
                if (!string.IsNullOrEmpty(e.Name)) NameCache[sid] = e.Name;
                SteamFriends.RequestUserInformation(new CSteamID(sid), true);
            }

        lb.globalEntries  = BuildList(window, _offset);
        lb.friendsEntries = BuildFriendsList(allEntries);

        // Block localEntry injection: game checks *(longlong*)(leaderboard+0x40) != 0
        // (m_steamIDUser of localEntry) before pinning user's rank at the last slot.
        // Zeroing it makes the condition fail → injection skipped entirely.
        try { unsafe { *(long*)(lb.Pointer + 0x40) = 0L; } } catch { }
        Log.LogInfo($"Replaced global(window {_offset}+{count}/{allEntries.Length}) on '{lb.lbName}', invoking A_LeaderboardReady");
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

    internal static Il2CppSystem.Collections.Generic.List<LeaderboardEntry> BuildList(ServerEntry[] entries, int rankOffset = 0)
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
                m_nGlobalRank = rankOffset + i + 1,
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
// SCROLL WHEEL — lets players scroll through the global leaderboard
// ─────────────────────────────────────────────────────────────────

class ScrollWheelDetector : MonoBehaviour
{
    public ScrollWheelDetector(System.IntPtr ptr) : base(ptr) { }
    void Update()
    {
        float dy = UnityEngine.Input.mouseScrollDelta.y;
        if (dy == 0f) return;
        int delta = dy > 0f ? -1 : 1;
        if (PersonalTab.IsActive)
            PersonalTab.ScrollBy(delta);
        else
            LeaderboardInjector.ScrollBy(delta);
    }
}

[HarmonyPatch(typeof(LeaderboardUiNew), "Refresh")]
static class Patch_LeaderboardUiNew_Refresh
{
    // The game's Refresh() unconditionally accesses globalEntries[0] before its loop,
    // with no bounds check. When we temporarily set an empty globalEntries list (waiting
    // for server data), the re-invoked A_LeaderboardReady triggers Refresh → crash.
    // Skip Refresh entirely when the leaderboard's globalEntries is empty.
    [HarmonyPrefix]
    static bool Prefix(LeaderboardUiNew __instance)
    {
        try
        {
            unsafe
            {
                // leaderboard field is at offset 0x48 (from dump.cs)
                var lbPtr = *(System.IntPtr*)(__instance.Pointer + 0x48);
                if (lbPtr == System.IntPtr.Zero) return true; // no leaderboard yet — let game handle
                var lb = new SteamLeaderboardNew(lbPtr);
                if (lb.globalEntries == null || lb.globalEntries.Count == 0)
                    return false; // skip — would crash on globalEntries[0]
            }
        }
        catch { }
        return true;
    }

    [HarmonyPostfix]
    static void Postfix(LeaderboardUiNew __instance)
    {
        // Attach scroll detector once per panel instance
        try
        {
            if (__instance.GetComponent<ScrollWheelDetector>() == null)
                __instance.gameObject.AddComponent<ScrollWheelDetector>();
        }
        catch { }

        // Track which leaderboard the active UI is showing — used by ScrollBy.
        // _lastLb gets overwritten by weekly+all-time boards; this is always current.
        try
        {
            unsafe
            {
                var lbPtr = *(System.IntPtr*)(__instance.Pointer + 0x48);
                if (lbPtr != System.IntPtr.Zero)
                    LeaderboardInjector.ActiveLb = new SteamLeaderboardNew(lbPtr);
            }
        }
        catch { }

        try { PersonalTab.Init(__instance); } catch { }
        try { PersonalTab.BeginFetch(); }    catch { }

        try
        {
        // If Personal tab is active, Refresh just wiped our slots — restore immediately.
        if (PersonalTab.IsActive)
        {
            PersonalTab.Redisplay();
        }
        else if (Patch_LBTypeSelected.CurrentTab == 0)
        {
            var slots = __instance.leaderboardEntries;
            var cache = LeaderboardInjector.CachedEntries;
            if (slots != null && slots.Count >= 10 && cache != null)
            {
                int offset = LeaderboardInjector.CurrentOffset;
                var dm     = DataManager.Instance;

                // Force-write slots 0-8 with server data.
                // Game pins localEntry at slot 0 (top) when user's rank exits the window,
                // shifting all other entries down — we override this entirely.
                var mySteamId = Steamworks.SteamUser.GetSteamID().m_SteamID.ToString();
                for (int i = 0; i < 9 && i < slots.Count; i++)
                {
                    var row = slots[i];
                    if (row == null) continue;
                    int idx = offset + i;
                    if (idx >= cache.Length) { try { row.Clear(); } catch { } continue; }
                    var e = cache[idx];
                    try
                    {
                        row.gameObject.SetActive(true);
                        ((TMPro.TMP_Text)row.rank).SetText($"#{idx + 1}");
                        ((TMPro.TMP_Text)row.playerName).SetText(e.Name ?? "?");
                        ((TMPro.TMP_Text)row.score).SetText(e.Score.ToString("N0"));
                        if (dm?.unsortedCharacterData != null)
                            for (int j = 0; j < dm.unsortedCharacterData.Count; j++)
                            {
                                var cd = dm.unsortedCharacterData[j];
                                if (cd == null || (int)cd.eCharacter != e.CharacterIndex) continue;
                                if (cd.icon != null) { row.characterIcon.texture = cd.icon; }
                                break;
                            }
                        // Clear localHighlight + playerIcon unless this is the user's entry.
                        // Game activates highlight + sets Steam avatar but never clears them
                        // when the entry scrolls off — both persist on the wrong row.
                        bool isMe = e.SteamId == mySteamId;
                        if (row.localHighlight != null)
                            row.localHighlight.gameObject.SetActive(isMe);
                        if (!isMe && row.playerIcon != null
                            && ulong.TryParse(e.SteamId, out var sid))
                        {
                            var avatar = Assets.Scripts.Steam.SteamUtility.LoadAvatar(sid, 0);
                            if (avatar != null) row.playerIcon.texture = avatar;
                        }
                    }
                    catch { }
                }

                // Slot 9 is the game's localEntry slot — localEntry is zeroed in Replace
                // so the game won't inject anything there. Leave it as-is.
                try { slots[9]?.Clear(); } catch { }
            }
        }
        } catch { }
    }
}

[HarmonyPatch(typeof(LeaderboardUiNew), "OnLeaderboardTypeSelected")]
static class Patch_LBTypeSelected
{
    internal static int CurrentTab { get; private set; } = 0;

    [HarmonyPrefix]
    static void Prefix(int index)
    {
        // Set CurrentTab BEFORE original runs so any internal Refresh() sees correct tab.
        if (index < 2) CurrentTab = index;
    }

    [HarmonyPostfix]
    static void Postfix(int index)
    {
        // index 0 = Global, 1 = Friends — real tabs, deactivate personal
        // index 2+ = Personal (we injected it), leave IsActive alone
        if (index >= 2) return;
        PersonalTab.IsActive = false;
        if (index == 0)
        {
            LeaderboardInjector.ResetScroll();
            var lb = LeaderboardInjector.LastLb;
            if (lb != null)
                try { SteamLeaderboardNew.A_LeaderboardReady?.Invoke(lb); } catch { }
        }
    }
}

// ─────────────────────────────────────────────────────────────────
// PERSONAL LEADERBOARD TAB
// Repurposes the hidden B_Effects button in leaderboardTypeButtons.
// On click: fetches /personal?steamId=X, writes directly to slots.
// ─────────────────────────────────────────────────────────────────

static class PersonalTab
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.PersonalTab");

    internal static bool IsActive { get; set; } = false;
    static LeaderboardUiNew                   _ui;
    static MyButtonTabs                       _tabButton;
    static GameObject                         _tabGo       = null;
    static LeaderboardInjector.ServerEntry[]  _lastEntries = null;
    static bool                               _fetching    = false;
    static int                                _offset      = 0;

    // characterIndex → personal best score, populated from /personal fetch
    internal static readonly System.Collections.Generic.Dictionary<int, int> PersonalBests = new();

    internal static void Init(LeaderboardUiNew ui)
    {
        _ui = ui; // always update — reference does leaderboardDisplay = __instance every Refresh

        try
        {
            // Find and wire the button once; cache _tabGo so we skip FindObjectsOfType after
            if (_tabGo == null)
            {
                var allGos = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
                foreach (var go in allGos)
                {
                    if (go.name != "B_Effects") continue;
                    if (!go.transform.IsChildOf(ui.leaderboardTypeButtons.transform)) continue;

                    var btn = go.GetComponent<MyButtonTabs>();
                    if (btn == null) continue;

                    var uiBtn = go.GetComponent<UnityEngine.UI.Button>();
                    if (uiBtn != null)
                    {
                        uiBtn.onClick.RemoveAllListeners();
                        uiBtn.onClick.AddListener(new System.Action(OnClick));
                    }

                    go.SetActive(true);
                    _tabButton = btn;
                    _tabGo     = go;
                    Log.LogInfo("Personal tab wired.");
                    break;
                }
            }

            // Always re-set label — game resets it to "Effects" between Refreshes
            if (_tabGo != null)
            {
                var tmp = _tabGo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text   = "Personal";
                    tmp.m_text = "Personal"; // IL2CPP TMPro needs both
                }
            }
        }
        catch (System.Exception ex) { Log.LogWarning($"Init: {ex.Message}"); }
    }

    // Fetch personal entries in the background. Called on panel open and can be
    // called again to refresh (e.g. after submitting a new score).
    internal static void BeginFetch()
    {
        if (_fetching) return;
        _fetching    = true;
        _lastEntries = null;
        _offset      = 0;
        var steamId  = Steamworks.SteamUser.GetSteamID().m_SteamID.ToString();
        System.Threading.Tasks.Task.Run(async () =>
        {
            var json    = await LeaderboardRelay.FetchPersonal(steamId);
            var entries = ParsePersonal(json);
            ModGui.MainThread.Enqueue(() =>
            {
                _lastEntries = entries;
                _fetching    = false;
                // Rebuild personal best cache
                PersonalBests.Clear();
                foreach (var e in entries)
                    if (!PersonalBests.TryGetValue(e.CharacterIndex, out int prev) || e.Score > prev)
                        PersonalBests[e.CharacterIndex] = e.Score;
                Log.LogInfo($"Personal pre-fetch: {entries.Length} entries, {PersonalBests.Count} characters cached");
                if (IsActive) Display(entries); // update live if tab already showing
            });
        });
    }

    static void OnClick()
    {
        IsActive = true;
        if (_ui == null) return;

        // Write slots FIRST, then ButtonPressed — matches reference implementation order.
        // ButtonPressed may trigger Refresh which would wipe slots; writing first ensures
        // data is visible, and Redisplay() in the Refresh postfix re-applies if needed.
        if (_lastEntries != null)
            Display(_lastEntries);
        else
        {
            BeginFetch(); // re-trigger if pre-fetch failed or not yet complete
            try { ((TMPro.TMP_Text)_ui.leaderboardEntries[0].playerName).SetText("Loading…"); } catch { }
        }

        ActivateTabButton();
    }

    static void ActivateTabButton()
    {
        try
        {
            if (_ui?.leaderboardTypeButtons == null || _tabButton == null) return;
            var tb = _ui.leaderboardTypeButtons;
            // Expand buttons array to 3 so ButtonPressed(2, false) has a valid index.
            if (tb.buttons == null || tb.buttons.Length < 3)
            {
                var arr = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<MyButtonTabs>(3);
                if (tb.buttons != null)
                    for (int k = 0; k < System.Math.Min((int)tb.buttons.Length, 2); k++)
                        arr[k] = tb.buttons[k];
                arr[2] = _tabButton;
                tb.buttons = arr;
            }
            tb.ButtonPressed(2, false);
        }
        catch (System.Exception ex) { Log.LogWarning($"ActivateTabButton: {ex.Message}"); }
    }

    // Called from Refresh postfix — restores personal display after game wipes slots.
    internal static void Redisplay()
    {
        if (_lastEntries != null)
            Display(_lastEntries);
    }

    internal static void ScrollBy(int delta)
    {
        if (_lastEntries == null || _lastEntries.Length == 0) return;
        if (_ui?.leaderboardEntries == null) return;
        int slots = _ui.leaderboardEntries.Count;
        _offset = System.Math.Max(0, System.Math.Min(_offset + delta, System.Math.Max(0, _lastEntries.Length - slots)));
        Display(_lastEntries);
    }

    static void Display(LeaderboardInjector.ServerEntry[] entries)
    {
        if (!IsActive || _ui?.leaderboardEntries == null) return;

        // Cache server names for GetFriendPersonaName patch
        foreach (var e in entries)
            if (ulong.TryParse(e.SteamId, out var sid) && sid != 0 && !string.IsNullOrEmpty(e.Name))
                LeaderboardInjector.NameCache[sid] = e.Name;

        var dm    = DataManager.Instance;
        int slots = _ui.leaderboardEntries.Count;
        // Clamp offset
        _offset = System.Math.Max(0, System.Math.Min(_offset, System.Math.Max(0, entries.Length - slots)));
        Log.LogInfo($"Personal Display: {entries.Length} entries, {slots} slots, offset={_offset}");
        for (int i = 0; i < slots; i++)
        {
            var row = _ui.leaderboardEntries[i];
            if (row == null) { Log.LogWarning($"Slot {i}: null row"); continue; }
            int entryIdx = i + _offset;
            if (entryIdx >= entries.Length) { try { row.Clear(); } catch { } continue; }
            try
            {
                var e = entries[entryIdx];
                bool wasActive = row.gameObject.activeSelf;
                if (!wasActive) row.gameObject.SetActive(true);
                ((TMPro.TMP_Text)row.rank).SetText($"#{entryIdx + 1}");
                ((TMPro.TMP_Text)row.playerName).SetText(e.Name ?? "?");
                ((TMPro.TMP_Text)row.score).SetText(e.Score.ToString("N0"));
                if (dm?.unsortedCharacterData != null)
                    for (int j = 0; j < dm.unsortedCharacterData.Count; j++)
                    {
                        var cd = dm.unsortedCharacterData[j];
                        if (cd == null || (int)cd.eCharacter != e.CharacterIndex) continue;
                        if (cd.icon != null)
                        {
                            row.characterIcon.texture = cd.icon;
                            row.playerIcon.texture    = cd.icon;
                        }
                        break;
                    }
            }
            catch (System.Exception ex) { Log.LogWarning($"Slot {i}: EXCEPTION {ex.Message}"); }
        }
        Log.LogInfo($"Personal tab: {entries.Length} entries displayed.");
        // Force canvas rebuild so text changes render immediately
        try
        {
            if (_ui != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _ui.GetComponent<UnityEngine.RectTransform>());
        }
        catch { }
    }

    static LeaderboardInjector.ServerEntry[] ParsePersonal(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
            return System.Array.Empty<LeaderboardInjector.ServerEntry>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<LeaderboardInjector.ServerEntry[]>(json)
                   ?? System.Array.Empty<LeaderboardInjector.ServerEntry>();
        }
        catch { return System.Array.Empty<LeaderboardInjector.ServerEntry>(); }
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
    [HarmonyPostfix]
    static void Postfix(ref string __result)
    {
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
        *(int*)(p + 0x3C)   = 7;                 // maxNumCursesPerCheck   5   → 7
        *(int*)(p + 0x30)   = 7 * amount;        // maxNumCursedEnemies    recalc
    }
}

// ─────────────────────────────────────────────────────────────────
// SPIKY SHIELD — armorPerAmount 10 → 50.
// Armor stat is built in OnInitOrAmountChanged as amount × armorPerAmount(0x30)
// and fed through the game's hyperbolic armor→DR curve (never reaches 100%).
// We set the RAW per-stack value (0x30); set BEFORE the method reads it (prefix).
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(ItemSpikyShield), "OnInitOrAmountChanged")]
static class Patch_SpikyShield_Armor
{
    [HarmonyPrefix]
    static unsafe void Prefix(ItemSpikyShield __instance)
        => *(float*)(__instance.Pointer + 0x30) = 0.5f;  // armorPerAmount 0.10 (10%) → 0.50 (50%)
}

// Description ("Also gain 10% Armor") is built in GetLocalizationKeys from the
// same armorPerAmount(0x30) field. The unlock-menu preview instance never runs
// OnInitOrAmountChanged, so set the field here too → description shows 50%.
[HarmonyPatch(typeof(ItemSpikyShield), "GetLocalizationKeys")]
static class Patch_SpikyShield_Desc
{
    [HarmonyPrefix]
    static unsafe void Prefix(ItemSpikyShield __instance)
        => *(float*)(__instance.Pointer + 0x30) = 0.5f;  // armorPerAmount 0.10 (10%) → 0.50 (50%)
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

    static System.Action<int> _handler;

    [HarmonyPostfix]
    static void Postfix(PassiveAbilityZooma __instance)
    {
        if (_handler != null) PlayerXp.A_LevelUp -= _handler;
        _handler = level =>
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
        };
        PlayerXp.A_LevelUp += _handler;
    }
}

// ─────────────────────────────────────────────────────────────────
// ROBERTO (Hoarder passive) — +0.5% elite damage per level
// Mirrors the Tony/Zooma pattern: subscribe to PlayerXp.A_LevelUp and push
// player StatModifiers via PassiveAbility.SetStat (auto-aggregated, replaced each level).
//   EliteDamageMultiplier = 23. 0.5% = 0.005f (Greed convention).
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(PassiveAbilityHoarder), "Init")]
static class Patch_Hoarder_EliteScaling
{
    static readonly System.Reflection.MethodInfo _setStat =
        HarmonyLib.AccessTools.Method(typeof(PassiveAbility), "SetStat");

    // Noelle (Enduring) serialized size values, cached at DataManager.Load.
    public static float NoelleSizePerLevel = 0f;
    public static float NoelleMaxSize      = 0f;

    static System.Action<int> _handler;

    [HarmonyPostfix]
    static void Postfix(PassiveAbilityHoarder __instance)
    {
        if (_handler != null) PlayerXp.A_LevelUp -= _handler;
        _handler = level =>
        {
            try
            {
                float val = 0.005f * level;

                var dmg = new StatModifier();
                dmg.stat         = (EStat)23; // EliteDamageMultiplier
                dmg.modifyType   = EStatModifyType.Addition;
                dmg.modification = val;
                _setStat.Invoke(__instance, new object[] { dmg });

                // 1/3 of Noelle's size-per-level, capped at 1/3 of her max (same Flat model as Enduring)
                if (NoelleSizePerLevel > 0f)
                {
                    float sizeVal = System.Math.Min(level * (NoelleSizePerLevel / 3f), NoelleMaxSize / 3f);
                    var size = new StatModifier();
                    size.stat         = (EStat)9; // SizeMultiplier
                    size.modifyType   = EStatModifyType.Flat;
                    size.modification = sizeVal;
                    _setStat.Invoke(__instance, new object[] { size });
                }
            }
            catch { }
        };
        PlayerXp.A_LevelUp += _handler;
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
        CheckVersionAsync();
        PersonalTab.BeginFetch();
        CheckUnauthorizedMods();
    }

    static void CheckUnauthorizedMods()
    {
        ModGui.UnauthorizedMods.Clear();
        try
        {
            var chainloader = BepInEx.Unity.IL2CPP.IL2CPPChainloader.Instance;

            var prop = chainloader.GetType().GetProperty("Plugins",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (prop == null) { Log.LogError("[ModCheck] Plugins property not found."); return; }

            if (prop.GetValue(chainloader) is System.Collections.IDictionary plugins)
            {
                Log.LogInfo($"[ModCheck] {plugins.Count} plugin(s) loaded.");
                foreach (var key in plugins.Keys)
                {
                    if (key.ToString() == "com.megabonk.mod") continue;
                    Log.LogWarning($"[ModCheck] Unauthorized plugin: {key}");
                    ModGui.UnauthorizedMods.Add(key.ToString());
                }
            }
            else { Log.LogError("[ModCheck] Plugins cast failed."); }
        }
        catch (System.Exception ex)
        {
            Log.LogError($"[ModCheck] Failed: {ex.Message}");
        }
        if (ModGui.UnauthorizedMods.Count == 0)
            Log.LogInfo("[ModCheck] No unauthorized mods detected.");
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
                    if (!string.IsNullOrEmpty(required) &&
                        System.Version.TryParse(required, out var reqVer) &&
                        System.Version.TryParse(Plugin.ModVersion, out var localVer) &&
                        reqVer > localVer)
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
        // If the chart was toggled this run it left the shared StatsWindows
        // object mutated (hidden Confirm window, moved transform, custom canvas,
        // stale rows). Revert so the real death screen works.
        var gui = UnityEngine.Object.FindObjectOfType<ModGui>();
        gui?.RestoreForDeath();
    }
}

// ─────────────────────────────────────────────────────────────────
// COMBAT SCALING — 2× HP/damage ramp, knockback resistance → 0
// IL2CPP static fields can't be set via reflection, so we patch the
// public getter methods instead. Formula: scale = 1 + rate * minutes,
// so doubling the rate = 2 * result - 1.
// ─────────────────────────────────────────────────────────────────


[HarmonyPatch(typeof(CombatScaling), nameof(CombatScaling.GetStageHpMultiplier))]
static class Patch_CombatScaling_HpMultiplier
{
    [HarmonyPostfix]
    static void Postfix(ref float __result) => __result = 2f * __result - 1f;
}

[HarmonyPatch(typeof(CombatScaling), nameof(CombatScaling.GetStageDamageMultiplier))]
static class Patch_CombatScaling_DamageMultiplier
{
    [HarmonyPostfix]
    static void Postfix(ref float __result) => __result = 2f * __result - 1f;
}

// ─────────────────────────────────────────────────────────────────
// GOD MODE — block all incoming player damage when enabled
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(PlayerHealth), "DamagePlayer",
    new System.Type[] { typeof(Enemy), typeof(Vector3), typeof(DcFlags) })]
static class Patch_GodMode
{
    [HarmonyPrefix]
    static bool Prefix() => !ModGui.GodMode; // return false = skip original
}

// ─────────────────────────────────────────────────────────────────
// BOMBUS — speed scales with overtime (1%/sec of base) AND inversely
// with size (smaller = up to 2x faster). Per-instance.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(Enemy), "GetSpeed")]
static class Patch_BOMBUS_Speed
{
    [HarmonyPostfix]
    static void Postfix(Enemy __instance, ref float __result)
    {
        if (GiantBeeState.IsBombus(__instance))
            __result = GiantBeeState.GetBombusSpeed(__instance);
    }
}

// ─────────────────────────────────────────────────────────────────
// BOMBUS — immune to all debuffs (kept even though no longer invulnerable)
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(Enemy), "AddDebuff")]
static class Patch_BOMBUS_DebuffImmune
{
    [HarmonyPrefix]
    static bool Prefix(Enemy __instance) => !GiantBeeState.IsBombus(__instance); // false = skip debuff
}

// ─────────────────────────────────────────────────────────────────
// BOMBUS — immune to executes. Both execute sources (Dexecutioner weapon,
// Joe's Dagger) deliver the instakill as a DamageContainer with isExecute=true.
// Skip those for BOMBUS; normal (isExecute==false) hits still apply full damage.
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(Enemy), "Damage")]
static class Patch_BOMBUS_ExecuteImmune
{
    [HarmonyPrefix]
    static bool Prefix(Enemy __instance, DamageContainer damageContainer)
        => !(damageContainer != null && damageContainer.isExecute && GiantBeeState.IsBombus(__instance)); // false = skip execute hit
}

// ─────────────────────────────────────────────────────────────────
// INSTAKILL — any hit from player weapon/other kills enemy instantly
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(Enemy), "DamageFromPlayerWeapon")]
static class Patch_InstaKill_Weapon
{
    [HarmonyPrefix]
    static bool Prefix(Enemy __instance)
    {
        if (!ModGui.InstaKill) return true;
        try { __instance.Kill("instakill"); } catch { }
        return false;
    }
}

[HarmonyPatch(typeof(Enemy), "DamageFromPlayerOther")]
static class Patch_InstaKill_Other
{
    [HarmonyPrefix]
    static bool Prefix(Enemy __instance)
    {
        if (!ModGui.InstaKill) return true;
        try { __instance.Kill("instakill"); } catch { }
        return false;
    }
}

// ─────────────────────────────────────────────────────────────────
// FREEZE ENEMIES — return 0 speed so movement system idles
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(Enemy), "GetSpeed")]
static class Patch_FreezeEnemies
{
    [HarmonyPostfix]
    static void Postfix(ref float __result)
    {
        if (ModGui.FreezeEnemies) __result = 0f;
    }
}

// ─────────────────────────────────────────────────────────────────
// STAGE TIMER HELPER — advance stageTimer by N seconds (F1 menu button)
// Uses same static pointer pattern as Patch_MyTime_Update_SlowStageTimer.
// ─────────────────────────────────────────────────────────────────

static class StageTimerHelper
{
    static System.IntPtr _statics = System.IntPtr.Zero;

    static unsafe System.IntPtr GetStatics()
    {
        if (_statics != System.IntPtr.Zero) return _statics;
        var dm = DataManager.Instance;
        if (dm == null) return System.IntPtr.Zero;
        var image = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_image(
                        Il2CppInterop.Runtime.IL2CPP.il2cpp_object_get_class(dm.Pointer));
        var cls   = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_from_name(
                        image, "Assets.Scripts.Utility", "MyTime");
        if (cls == System.IntPtr.Zero) return System.IntPtr.Zero;
        _statics = *(System.IntPtr*)(cls + 0xB8);
        return _statics;
    }

    internal static unsafe float GetFinalSwarmTimer()
    {
        try
        {
            var statics = GetStatics();
            if (statics == System.IntPtr.Zero) return 0f;
            return *(float*)(statics + 0x24);
        }
        catch { return 0f; }
    }

    internal static unsafe void Advance(float seconds)
    {
        try
        {
            var statics = GetStatics();
            if (statics == System.IntPtr.Zero) return;
            float stageTimer = *(float*)(statics + 0x1C);
            if (stageTimer < 600f)
                *(float*)(statics + 0x1C) = System.Math.Max(0f, System.Math.Min(stageTimer + seconds, 600f));
            else
            {
                float swarm = *(float*)(statics + 0x24);
                float newSwarm = swarm + seconds;
                if (newSwarm < 0f) // went back before overtime — drop back into normal stage
                {
                    *(float*)(statics + 0x1C) = System.Math.Max(0f, 600f + newSwarm);
                    *(float*)(statics + 0x24) = 0f;
                }
                else
                    *(float*)(statics + 0x24) = newSwarm;
            }
        }
        catch { }
    }
}

// ─────────────────────────────────────────────────────────────────
// CACTUS — thorn range scales with player SizeMultiplier
// Cactus hit detection uses Physics.SphereCastNonAlloc internally.
// We scale maxDistance by SizeMultiplier, guarded by an OnTakeDamage
// counter so we only affect sphere casts that happen during cactus damage.
// Visual: scale the spawned projectile's localScale from the pool.
// ─────────────────────────────────────────────────────────────────

static class CactusState
{
    internal static int Depth = 0; // incremented while inside ItemCactus.OnTakeDamage
}

// 4 projectiles per cactus stack (base = 4, +4 per additional stack)
[HarmonyPatch(typeof(ItemCactus), "OnInitOrAmountChanged")]
static class Patch_Cactus_OnInitOrAmountChanged
{
    [HarmonyPrefix]
    static unsafe void Prefix(ItemCactus __instance)
    {
        *(int*)(__instance.Pointer + 0x34) = 3; // numProjectilesPerAmount → results in 4 per stack (game adds 1)
    }
}

[HarmonyPatch(typeof(ItemCactus), "OnTakeDamage")]
static class Patch_Cactus_OnTakeDamage
{
    [HarmonyPrefix]  static void Prefix()  => CactusState.Depth++;
    [HarmonyPostfix] static void Postfix() => CactusState.Depth--;
}

// Patch all SphereCastNonAlloc overloads — scale maxDistance when inside cactus damage
[HarmonyPatch(typeof(UnityEngine.Physics), "SphereCastNonAlloc",
    new System.Type[] { typeof(UnityEngine.Vector3), typeof(float), typeof(UnityEngine.Vector3),
        typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.RaycastHit>),
        typeof(float), typeof(int), typeof(UnityEngine.QueryTriggerInteraction) })]
static class Patch_SphereCast1 { [HarmonyPrefix] static void Prefix(ref float maxDistance) => CactusScaleHelper.Scale(ref maxDistance); }

[HarmonyPatch(typeof(UnityEngine.Physics), "SphereCastNonAlloc",
    new System.Type[] { typeof(UnityEngine.Vector3), typeof(float), typeof(UnityEngine.Vector3),
        typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.RaycastHit>),
        typeof(float), typeof(int) })]
static class Patch_SphereCast2 { [HarmonyPrefix] static void Prefix(ref float maxDistance) => CactusScaleHelper.Scale(ref maxDistance); }

[HarmonyPatch(typeof(UnityEngine.Physics), "SphereCastNonAlloc",
    new System.Type[] { typeof(UnityEngine.Vector3), typeof(float), typeof(UnityEngine.Vector3),
        typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.RaycastHit>),
        typeof(float) })]
static class Patch_SphereCast3 { [HarmonyPrefix] static void Prefix(ref float maxDistance) => CactusScaleHelper.Scale(ref maxDistance); }

[HarmonyPatch(typeof(UnityEngine.Physics), "SphereCastNonAlloc",
    new System.Type[] { typeof(UnityEngine.Ray), typeof(float),
        typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.RaycastHit>),
        typeof(float), typeof(int), typeof(UnityEngine.QueryTriggerInteraction) })]
static class Patch_SphereCast4 { [HarmonyPrefix] static void Prefix(ref float maxDistance) => CactusScaleHelper.Scale(ref maxDistance); }

[HarmonyPatch(typeof(UnityEngine.Physics), "SphereCastNonAlloc",
    new System.Type[] { typeof(UnityEngine.Ray), typeof(float),
        typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.RaycastHit>),
        typeof(float), typeof(int) })]
static class Patch_SphereCast5 { [HarmonyPrefix] static void Prefix(ref float maxDistance) => CactusScaleHelper.Scale(ref maxDistance); }

[HarmonyPatch(typeof(UnityEngine.Physics), "SphereCastNonAlloc",
    new System.Type[] { typeof(UnityEngine.Ray), typeof(float),
        typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<UnityEngine.RaycastHit>),
        typeof(float) })]
static class Patch_SphereCast6 { [HarmonyPrefix] static void Prefix(ref float maxDistance) => CactusScaleHelper.Scale(ref maxDistance); }

static class CactusScaleHelper
{
    internal static void Scale(ref float maxDistance)
    {
        if (CactusState.Depth <= 0) return;
        try
        {
            float size = PlayerStats.GetStat(EStat.SizeMultiplier);
            if (size > 0f) maxDistance *= size;
        }
        catch { }
    }
}
// ─────────────────────────────────────────────────────────────────
// BOMBUS — spawns at 15 min overtime, then every 5 min (STACKING).
// 15× scale, KILLABLE (no invulnerability), debuff-immune.
// HP scales with overtime. Speed scales with overtime + inversely with size.
// Drops a Corrupt chest on death. Multiple tracked per-instance.
// ─────────────────────────────────────────────────────────────────

static class GiantBeeState
{
    internal const float Scale    = 15f;
    internal const float ScaleMin = 1.5f;  // smallest BOMBUS shrinks to (world units → fraction MinFrac); low enough to fit tight gaps players hid in
    internal const float IconScale = 10f;  // boss map icon size relative to a normal bee dot (1/Scale was too tiny)

    // ── tuning (flag for playtest) ──
    const float FirstSpawnSwarm  = 15f * 60f;  // first spawn at 15 min overtime (finalSwarmTimer) on stage 1
    const float StageArrivalStep = 5f  * 60f;  // each later stage delays BOMBUS's first arrival by +5 min
    const float SpawnInterval    = 5f  * 60f;  // then every 5 min
    const float BaseSpeed        = 32f;
    const float SpeedTimeRate    = 0.005f;     // +0.5% of base per second past first spawn (was 1%; reduced so 3rd-spawn stays kiteable)
    const float MaxSpeed         = 140f;        // hard speed cap (was 250; lowered so a move-speed player can still outrun)
    const float BossHpMult       = 20f;        // multiplier on bee's natural overtime-scaled HP
    const float AoeRange         = 40f;        // ~15× bee radius at full size
    const float AoeInterval      = 0.5f;

    internal class Bombus
    {
        public Enemy   enemy;
        public Vector3 spawnScale    = Vector3.one;
        public float   curScale      = 1f;
        public float   scaleVelocity = 0f;
        public float   lastHitTime   = -999f;
        public Transform mapIcon     = null;          // minimap icon child (layer 14)
        public Vector3   mapIconBase = Vector3.one;    // its original localScale
    }

    static readonly System.Collections.Generic.Dictionary<System.IntPtr, Bombus> _active
        = new System.Collections.Generic.Dictionary<System.IntPtr, Bombus>();
    static readonly System.Collections.Generic.List<System.IntPtr> _deadScratch
        = new System.Collections.Generic.List<System.IntPtr>();

    internal static bool         PhaseActive     = false; // true once overtime BOMBUS phase begins
    internal static bool         SpawningNow     = false; // bypass flag during our own SpawnEnemy
    internal static float        RunTimerAtSpawn = -1f;
    internal static PlayerHealth PhInstance      = null;

    static float _nextSpawnSwarm = FirstSpawnSwarm;
    static float _aoeTimer       = 0f;

    static float MinFrac => ScaleMin / Scale;

    internal static bool IsBombus(Enemy e)
        => e != null && _active.ContainsKey(e.Pointer);

    // Ghost family — players wanted the overtime ghost swarm to keep spawning during BOMBUS.
    static readonly System.Collections.Generic.HashSet<EEnemy> GhostKinds = new()
    {
        EEnemy.Ghost, EEnemy.GreaterGhost, EEnemy.GhostRed, EEnemy.GhostPurple,
        EEnemy.GhostKing, EEnemy.GhostGrave1, EEnemy.GhostGrave2, EEnemy.GhostGrave3,
        EEnemy.GhostGrave4, EEnemy.GhostInvincible, EEnemy.Ghostham,
    };

    internal static bool IsGhost(EnemyData d)
        => d != null && GhostKinds.Contains(d.enemyName);

    internal static float GetBombusSpeed(Enemy e)
    {
        float cur = 1f;
        if (e != null && _active.TryGetValue(e.Pointer, out var b)) cur = b.curScale;
        float swarm    = StageTimerHelper.GetFinalSwarmTimer();
        float t        = System.Math.Max(0f, swarm - FirstSpawnSwarm);
        float timeMult = 1f + SpeedTimeRate * t;                            // overtime ramp
        float sizeMult = Mathf.Lerp(1f, 2f, (1f - cur) / (1f - MinFrac));   // smaller = up to 2x
        return Mathf.Clamp(BaseSpeed * timeMult * sizeMult, BaseSpeed, MaxSpeed);
    }

    internal static void OnHitPlayer(Enemy e)
    {
        if (e != null && _active.TryGetValue(e.Pointer, out var b))
            b.lastHitTime = Time.time; // grow back smoothly after hit
    }

    internal static void Reset()
    {
        _active.Clear();
        PhaseActive     = false;
        RunTimerAtSpawn = -1f;
        _nextSpawnSwarm = FirstSpawnSwarm + FirstArrivalOffset();
        _aoeTimer       = 0f;
    }

    // Later stages push BOMBUS's first arrival back by StageArrivalStep each.
    // Stage 1 (index 0) = no offset; stage 2 = +5 min; stage 3 = +10 min; ...
    static float FirstArrivalOffset()
    {
        try
        {
            int idx = Assets.Scripts.Managers.MapController.GetStageIndex();
            if (idx > 0) return idx * StageArrivalStep;
        }
        catch { }
        return 0f;
    }

    // runTimer went backwards = true new run/stage
    internal static void CheckNewRun()
    {
        if (!PhaseActive || RunTimerAtSpawn < 0f) return;
        if (MyTime.runTimer < RunTimerAtSpawn - 30f) Reset();
    }

    internal static void OnBombusDied(Enemy enemy)
    {
        if (enemy == null || !_active.ContainsKey(enemy.Pointer)) return;
        Vector3 pos = Vector3.zero;
        try { pos = enemy.transform.position; } catch { }
        _active.Remove(enemy.Pointer);
        // Disable the corpse's colliders so the ground raycast doesn't hit the bee
        try { foreach (var c in enemy.gameObject.GetComponentsInChildren<Collider>()) c.enabled = false; }
        catch { }
        DropCorruptChest(pos);
        Plugin.Log.LogInfo($"[GiantBee] BOMBUS died, corrupt chest dropped (active={_active.Count})");
    }

    static void DropCorruptChest(Vector3 pos)
    {
        try
        {
            var em = EffectManager.Instance;
            if (em == null || em.openChestNormal == null) return;
            pos = GroundChestPos(pos); // 15× bee center sits high; snap to ground
            var go = UnityEngine.Object.Instantiate(em.openChestNormal, pos, Quaternion.identity);
            var oc = go.GetComponent<OpenChest>();
            if (oc != null) oc.chestType = EChest.Corrupt;  // no corrupt prefab exists; reuse normal + override
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[GiantBee] chest drop failed: {ex.Message}");
        }
    }

    // Raycast down to find the ground so the chest sits on the floor instead
    // of floating where the giant bee's (high) center died.
    static Vector3 GroundChestPos(Vector3 pos)
    {
        try
        {
            var origin = new Vector3(pos.x, pos.y + 50f, pos.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 500f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                pos.y = hit.point.y;
                return pos;
            }
        }
        catch { }
        var pm = PlayerMovement.Instance;
        if (pm != null) pos.y = pm.transform.position.y; // fallback
        return pos;
    }

    // Called each frame from MyTime.Update postfix
    internal static void Tick()
    {
        CheckNewRun();

        // Spawn scheduling — 15 min, then every 5 min (catch up if ticks skipped)
        if (MyTime.stageTimer >= 600f)
        {
            float swarm = StageTimerHelper.GetFinalSwarmTimer();
            int guard = 0;
            while (swarm >= _nextSpawnSwarm && guard++ < 8)
            {
                Spawn();
                _nextSpawnSwarm += SpawnInterval;
            }
        }

        if (_active.Count == 0) return;

        _aoeTimer += Time.deltaTime;
        bool doAoe = _aoeTimer >= AoeInterval;
        if (doAoe) _aoeTimer = 0f;

        var pm = PlayerMovement.Instance;
        _deadScratch.Clear();

        foreach (var kv in _active)
        {
            var b    = kv.Value;
            var inst = b.enemy;
            if (inst == null || inst.Pointer == System.IntPtr.Zero) { _deadScratch.Add(kv.Key); continue; }
            bool alive; try { alive = inst.hp > 0f; } catch { alive = false; }
            if (!alive) { _deadScratch.Add(kv.Key); continue; }
            if (pm == null) continue;

            try
            {
                Vector3 ppos = pm.transform.position;
                float dist        = (inst.transform.position - ppos).magnitude;
                float aoeNow      = AoeRange * b.curScale;
                bool  playerNear  = dist <= aoeNow * 4f;
                bool  hitRecently = (Time.time - b.lastHitTime) < 5f;

                float targetScale = (playerNear && !hitRecently) ? MinFrac : 1f;
                float smoothTime  = targetScale < b.curScale ? 4f : 1.5f; // shrink slower than grow
                b.curScale = Mathf.SmoothDamp(b.curScale, targetScale, ref b.scaleVelocity, smoothTime);
                inst.transform.localScale = b.spawnScale * b.curScale;

                // Negate body scale (Scale × curScale) on the minimap icon so it
                // stays a normal bee-sized dot regardless of the boss's animation.
                if (b.mapIcon != null)
                {
                    float div = Scale * b.curScale;
                    if (div > 0.0001f) b.mapIcon.localScale = b.mapIconBase * (IconScale / div);
                }

                // Range-based one-shot — catches corners BOMBUS can't physically reach
                if (doAoe && PhInstance != null && dist <= aoeNow)
                    PhInstance.DamagePlayer(inst, ppos, DcFlags.None);
            }
            catch { }
        }

        for (int i = 0; i < _deadScratch.Count; i++) _active.Remove(_deadScratch[i]);
    }

    internal static void Spawn(bool manualOverride = false)
    {
        try
        {
            var em = EnemyManager.Instance;
            var dm = DataManager.Instance;
            if (em == null || dm == null) return;

            var beeData = dm.GetEnemyData(EEnemy.Bee);
            if (beeData == null) return;

            var pos = SpawnPositions.GetEnemySpawnPosition(beeData);
            if (pos == SpawnPositions.INVALID_POS || pos == Vector3.zero)
            {
                var pmv = PlayerMovement.Instance;
                pos = pmv != null
                    ? pmv.transform.position + new Vector3(5f, 0f, 5f)
                    : Vector3.zero;
            }

            SpawningNow = true;
            var enemy = em.SpawnEnemy(beeData, pos, 0, forceSpawn: true,
                            flag: EEnemyFlag.Boss, canBeElite: false,
                            extraSizeMultiplier: Scale);
            SpawningNow = false;

            if (enemy != null)
            {
                // NO MakeInvulnerable — BOMBUS killable now. Debuff immunity kept via AddDebuff patch.

                // Tanky boss — multiply bee's natural (already overtime-scaled) HP
                try
                {
                    float nat = enemy.maxHp;
                    if (nat <= 0f) nat = enemy.hp;
                    float hp = nat * BossHpMult;
                    enemy.maxHp = hp; enemy.hp = hp;
                }
                catch { }

                _active[enemy.Pointer] = new Bombus
                {
                    enemy       = enemy,
                    spawnScale  = enemy.transform.localScale,
                    curScale    = 1f,
                    lastHitTime = -999f
                };

                // Fix collider shape — bee base collider is flat; force upright capsule
                try
                {
                    foreach (var col in enemy.gameObject.GetComponentsInChildren<CapsuleCollider>())
                    {
                        col.direction = 1;               // Y-axis (upright)
                        col.height    = col.radius * 2f; // roughly spherical
                    }
                }
                catch { }

                // Minimap icon is a child of the enemy, so it inherits the 15× body
                // scale → giant blob on the map. Grab it and counter-scale (in Tick)
                // so it renders at a normal bee's icon size.
                try
                {
                    if (_active.TryGetValue(enemy.Pointer, out var bb))
                    {
                        foreach (var tr in enemy.gameObject.GetComponentsInChildren<Transform>(true))
                        {
                            if (tr.gameObject.layer == 14) // minimap layer
                            {
                                bb.mapIcon     = tr;
                                bb.mapIconBase = tr.localScale;
                                tr.localScale  = bb.mapIconBase * (IconScale / Scale); // curScale==1 at spawn
                                break;
                            }
                        }
                    }
                }
                catch { }
            }

            PhaseActive     = true;
            RunTimerAtSpawn = MyTime.runTimer;
            Plugin.Log.LogInfo($"[GiantBee] BOMBUS spawned (active={_active.Count}) swarm={StageTimerHelper.GetFinalSwarmTimer():F0}s pos={pos}");

            // Boss alert with custom text
            try
            {
                var alertUi = UnityEngine.Object.FindObjectOfType<AlertUi>();
                if (alertUi != null)
                {
                    alertUi.SetAlertBoss();
                    if (alertUi.t_alert != null)
                        alertUi.t_alert.text = "BOMBUS IS APPROACHING...";
                }
            }
            catch { }
        }
        catch (System.Exception ex)
        {
            SpawningNow = false;
            Plugin.Log.LogError($"[GiantBee] spawn failed: {ex.Message}");
        }
    }
}

// Give BOMBUS her name — patch GetName so the boss HP bar shows it.
// Only applies after BOMBUS has been spawned; regular bees are unaffected
// because they never show a name bar.
[HarmonyPatch(typeof(EnemyData), "GetName")]
static class Patch_EnemyData_GetName_BOMBUS
{
    [HarmonyPostfix]
    static void Postfix(EnemyData __instance, ref string __result)
    {
        if (GiantBeeState.PhaseActive && __instance.enemyName == EEnemy.Bee)
            __result = "BOMBUS";
    }
}


// BOMBUS one-shot: temporarily set beeData.damage = 9999 only while DamagePlayer
// is executing for the BOMBUS instance. Restored in postfix — no other mob affected.
[HarmonyPatch(typeof(PlayerHealth), "DamagePlayer",
    new System.Type[] { typeof(Enemy), typeof(Vector3), typeof(DcFlags) })]
static class Patch_BOMBUS_OneShot
{
    static int  _origDamage;
    static bool _boosted;

    [HarmonyPrefix]
    static void Prefix(PlayerHealth __instance, Enemy enemy)
    {
        if (__instance != null) GiantBeeState.PhInstance = __instance;
        _boosted = false;
        if (!GiantBeeState.IsBombus(enemy)) return;

        var data = enemy.enemyData;
        if (data == null) return;
        _origDamage  = data.damage;
        data.damage  = 9999;
        _boosted     = true;
    }

    [HarmonyPostfix]
    static void Postfix(Enemy enemy)
    {
        if (!_boosted) return;
        var data = enemy?.enemyData;
        if (data != null) data.damage = _origDamage;
        _boosted = false;
        GiantBeeState.OnHitPlayer(enemy); // reset scale timer on successful hit
    }
}

[HarmonyPatch(typeof(MyTime), "Update")]
static class Patch_MyTime_Update_GiantBee
{
    [HarmonyPostfix]
    static void Postfix() => GiantBeeState.Tick();
}



// (removed Patch_BOMBUS_BlockSpawns — enemies now keep spawning during/after the
//  BOMBUS phase; players wanted the overtime swarm/ghosts to continue.)

// ─────────────────────────────────────────────────────────────────
// BOMBUS — reset state on new stage so BOMBUS + warning re-arm
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(MyTime), "OnNewStageStarted")]
static class Patch_BOMBUS_ResetOnNewStage
{
    [HarmonyPostfix]
    static void Postfix() => GiantBeeState.Reset();
}

// ─────────────────────────────────────────────────────────────────
// BOMBUS — drop a Corrupt chest when a BOMBUS instance dies
// ─────────────────────────────────────────────────────────────────

[HarmonyPatch(typeof(Enemy), "EnemyDied", new System.Type[] { typeof(DamageContainer) })]
static class Patch_BOMBUS_Death
{
    [HarmonyPrefix]
    static void Prefix(Enemy __instance) => GiantBeeState.OnBombusDied(__instance);
}

// ─────────────────────────────────────────────────────────────────
// BETTER JUMP ARC — bake the Flappy Feathers' forward jump boost into
// the BASE jump (without the item's extra mid-air jumps).
//
// The feather's OnJumped does exactly: rb.velocity += speedBoost * moveDir
// (decompiled at 0x180456C70). That forward arc is what made movement /
// landings feel good — diagonal bunny-hopping stacks the boost per jump
// to build speed, which players want. We replicate just that part here.
//
// Only applies when the player is actually inputting a direction, so a
// standing jump still goes straight up.
// ─────────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(PlayerMovement), "Jump")]
static class Patch_BetterJumpArc
{
    const float ForwardBoost = 0.2f;   // per-jump forward velocity added in move direction

    [HarmonyPostfix]
    static void Postfix(PlayerMovement __instance)
    {
        try
        {
            var pm = __instance;
            if (pm == null) return;
            var rb = pm.GetComponent<Rigidbody>();
            if (rb == null) return;
            var ori = pm.orientation;
            if (ori == null) return;

            unsafe
            {
                float inputX = *(float*)(pm.Pointer + 0x118);
                float inputY = *(float*)(pm.Pointer + 0x11C);
                if (inputX == 0f && inputY == 0f) return;     // standing jump → straight up

                Vector3 wish = ori.forward * inputY + ori.right * inputX;
                wish.y = 0f;
                if (wish.sqrMagnitude < 0.0001f) return;
                wish.Normalize();

                var v = rb.velocity;
                v.x += wish.x * ForwardBoost;
                v.z += wish.z * ForwardBoost;
                rb.velocity = v;
            }
        }
        catch { }
    }
}
