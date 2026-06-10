using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// SMART TARGETING
//
// A real target picker, tuned for Megabonk specifically (the game's own auto-aim
// just grabs whatever it considers "targeted"). Enemies spawn in a full ring
// around the player at all ranges, so plain "closest" is nearly meaningless
// (there's always something close). Instead we score every candidate:
//
//   1. TIER (hard priority, never crossed):
//        0 = boss-class  (Boss / StageBoss / FinalBoss / SummonerMiniboss)
//        1 = elite
//        2 = normal
//      A boss always beats a swarm regardless of distance — that's the
//      dangerous / valuable kill — so a separate "boss focus" toggle is
//      redundant and folded in here.
//
//   2. Within a tier, minimise  cost = distance + HP_WEIGHT * hpRatio.
//      Prefers enemies that are BOTH near the player (the bodies actually
//      dealing contact damage in the ring) AND close to death (finish them so
//      they stop hitting you), without overkilling a full-HP target when a
//      near-dead one is just as close. HP_WEIGHT is in "metres per full HP bar",
//      so a full-HP enemy is treated as ~HP_WEIGHT m farther than an identical
//      near-dead one — distance still dominates across big gaps.
//
// Toggle with the Smart Targeting hotkey in the Community Patch settings tab.
// When OFF we return true and the game's stock targeting runs unchanged.
// ─────────────────────────────────────────────────────────────────────────
internal static class SmartTargeting
{
    const int   AnyBoss   = 54;     // EEnemyFlag Boss|StageBoss|SummonerMiniboss|FinalBoss
    const float HP_WEIGHT = 4f;     // metres of "distance" one full HP bar is worth

    static ConfigEntry<bool> _enabled;

    internal static bool Enabled
    {
        get => _enabled != null && _enabled.Value;
        set { if (_enabled != null) _enabled.Value = value; }
    }

    internal static void Init(ConfigFile cfg)
    {
        // Named "Priority Targeting" to avoid clashing with the game's own
        // built-in "Smart" aim mode (EEnemyTargetingMode.SmartAim).
        _enabled = cfg.Bind("Targeting", "PriorityTargeting", false,
            "Priority Targeting: replace auto-aim target selection with a scorer that " +
            "prioritises bosses/elites, then the nearest enemy you can kill soonest. " +
            "Toggle in-game with the Priority Targeting hotkey.");
    }

    internal static void Toggle()
    {
        Enabled = !Enabled;
        string state = Enabled ? "ON" : "OFF";
        Plugin.Log.LogInfo($"[PriorityTargeting] {state}");
        Toast.Show($"Priority Targeting: {state}",
                   Enabled ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.6f, 0.45f, 1f));
    }

    // Score the collider set and return the best target, or null if none valid
    // (in which case we let the game's own targeting take over).
    internal static Enemy Pick(Il2CppReferenceArray<Collider> colliders, int count, Vector3 pos, GameObject exceptObject)
    {
        Enemy best = null;
        int   bestTier = int.MaxValue;
        float bestCost = float.MaxValue;
        int   exceptId = exceptObject != null ? exceptObject.GetInstanceID() : 0;

        for (int i = 0; i < count; i++)
        {
            var col = colliders[i];
            if (col == null) continue;
            if (exceptId != 0 && col.gameObject.GetInstanceID() == exceptId) continue;

            var e = col.GetComponent<Enemy>();
            if (e == null) continue;
            if (e.IsDead() || e.IsDeadOrDyingNextFrame()) continue;

            int tier = ((int)e.enemyFlag & AnyBoss) != 0 ? 0 : (e.IsElite() ? 1 : 2);
            if (tier > bestTier) continue;   // can't beat a higher-priority tier

            float dist = Vector3.Distance(e.GetCenterPosition(), pos);
            float cost = dist + HP_WEIGHT * e.GetHpRatio();

            if (tier < bestTier || cost < bestCost)
            {
                bestTier = tier;
                bestCost = cost;
                best = e;
            }
        }
        return best;
    }
}

// Replace target selection when Smart Targeting is on. GetTargetedEnemy is the
// game's per-shot "which enemy does this projectile aim at" call.
[HarmonyPatch(typeof(EnemyTargeting), "GetTargetedEnemy")]
static class Patch_SmartTargeting
{
    [HarmonyPrefix]
    static bool Prefix(Il2CppReferenceArray<Collider> colliders, int count, Vector3 pos,
                       bool useVision, GameObject exceptObject, ref Enemy __result)
    {
        if (!SmartTargeting.Enabled) return true;            // stock targeting

        var pick = SmartTargeting.Pick(colliders, count, pos, exceptObject);
        if (pick == null) return true;                       // nothing valid → let game decide

        __result = pick;
        return false;                                        // skip original
    }
}
