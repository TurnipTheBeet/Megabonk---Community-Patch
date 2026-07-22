using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

/// <summary>
/// Space Noodle buffs:
/// 1. AoE burst explosion radius scales with weapon size + player size
/// 2. Reduced latch-to-burst time  
/// 3. Reduced cooldown
/// 4. Full burst damage preserved despite faster latch
/// </summary>
[HarmonyPatch]
internal static class Patch_SpaceNoodle_Buffs
{
    internal static bool Enabled = false;

    // ── Timing overrides ──────────────────────────────────────────────
    // Override the weapon data fields that control latch/burst timing
    // by intercepting the methods that read them.

    /// <summary>Faster burst: reduce latch time to 40% of normal</summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LaserBeamAttack), "StartLaser")]
    private static void StartLaser_Prefix(LaserBeamAttack __instance)
    {
        if (!Enabled) return;
        // laserReadyTime and laserStopTime are set from weapon config durs
        // ing StartLaser. We compress the timing by overriding the fields
        // after they're set, via postfix.
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LaserBeamAttack), "StartLaser")]
    private static void StartLaser_Postfix(LaserBeamAttack __instance)
    {
        if (!Enabled) return;
        // Speed up burst timer (laserStopTime = Time + latchDuration)
        // Keep laserStartedAtTime the same for damage calc
        float now = Time.time;
        float elapsed = now - __instance.laserStartedAtTime;
        // Aim for ~1/3 of the original latch duration
        float newDuration = elapsed * 0.35f;
        __instance.laserReadyTime = now + newDuration * 0.7f;
        __instance.laserStopTime = now + newDuration;
    }

    /// <summary>Reduce cooldown to ~40%</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LaserBeamAttack), "GetCooldown")]
    private static void GetCooldown_Postfix(ref float __result)
    {
        if (!Enabled) return;
        __result *= 0.35f;
    }

    // ── AoE burst on StopLaser ───────────────────────────────────────
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LaserBeamAttack), "StopLaser")]
    private static void StopLaser_Prefix(LaserBeamAttack __instance)
    {
        if (!Enabled) return;

        // Get the target position for the explosion
        Enemy target = __instance.target;
        if ((Object)(object)target == (Object)null) return;

        // Calculate burst radius from weapon size
        WeaponBase wb = ((ConstantAttack)__instance).weaponBase;
        if (wb == null) return;

        // GetAttackSizeMultiplier returns playerSizeStat * weaponSizeStat
        float sizeMult = WeaponUtility.GetAttackSizeMultiplier(wb);
        // Base radius 1.5m × weapon size multiplier × player size multiplier
        float radius = 1.5f * sizeMult;

        // Deal AoE damage around the target
        Collider[] hits = SpaceNoodleState.Buf;
        int count = Physics.OverlapSphereNonAlloc(
            target.GetCenterPosition(), radius,
            (Il2CppReferenceArray<Collider>)hits,
            (LayerMask)(GameManager.Instance.whatIsEnemy));

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i];
            if ((Object)(object)col == (Object)null) continue;
            if ((Object)(object)col.attachedRigidbody == (Object)null) continue;
            
            // Skip the main target - already handled by StopLaser
            if ((Object)(object)col.attachedRigidbody == 
                (Object)(object)((Component)target).GetComponent<Rigidbody>())
                continue;
            
            __instance.HitEnemy(col);
        }
    }

    // ── Preserve full burst damage ───────────────────────────────────
    // The burst damage scales with (Time - laserStartedAtTime).
    // Since we're shortening the latch, we pre-date laserStartedAtTime
    // so the elapsed duration appears full.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LaserBeamAttack), "Init")]
    private static void Init_Prefix(LaserBeamAttack __instance)
    {
        if (!Enabled) return;
        // No-op; the actual override happens in FixedUpdate
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LaserBeamAttack), "FixedUpdate")]
    private static void FixedUpdate_Postfix(LaserBeamAttack __instance)
    {
        if (!Enabled) return;

        // When the laser first starts on a new target, backdate
        // laserStartedAtTime to make the latch appear full-length
        // for damage purposes.
        bool isShooting = __instance.isShooting;
        if (!isShooting) return;

        float now = Time.time;
        float stopTime = __instance.laserStopTime;
        if (stopTime <= now) return; // about to burst anyway

        float remaining = stopTime - now;
        float targetRemaining = 0.4f; // we want ~0.4s remaining

        if (remaining > targetRemaining * 2f)
        {
            // Shift laserStartedAtTime so elapsed time appears larger
            float shift = remaining - targetRemaining;
            __instance.laserStartedAtTime -= shift;
            __instance.laserReadyTime -= shift;
            __instance.laserStopTime -= shift;
        }
    }
}
