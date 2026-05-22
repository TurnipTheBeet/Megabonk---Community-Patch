#nullable disable
using HarmonyLib;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;

namespace MegaBonkMod;

internal static class NoodleHelper
{
    internal static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MBM.Noodle");

    internal static bool IsDead(Enemy e)
    {
        if (e == null) return true;
        unsafe { return *(bool*)(e.Pointer + 0x116) || *(bool*)(e.Pointer + 0x117); }
    }
}

// ── StartLaser: 0.5s base duration, atk speed scales duration, 0.02s gap ─────
[HarmonyPatch(typeof(LaserBeamAttack), "StartLaser")]
static class Patch_Noodle_AttackSpeedDuration
{
    private const float BASE_DURATION = 0.5f;
    private const float READY_GAP     = 0.02f;

    static void Postfix(LaserBeamAttack __instance)
    {
        var wb = __instance.weaponBase;
        if (wb == null) return;

        float baseCooldown;
        unsafe { baseCooldown = *(float*)(wb.weaponData.Pointer + 0x9C); }
        float actualCooldown = WeaponUtility.GetWeaponCooldown(wb);
        float F = (actualCooldown > 0.001f) ? baseCooldown / actualCooldown : 1f;

        float duration     = Mathf.Max(0.01f, BASE_DURATION / F);
        float now          = Time.time;
        float newStopTime  = now + duration;
        float newReadyTime = newStopTime + READY_GAP;

        NoodleHelper.Log.LogInfo($"[Noodle] StartLaser: F={F:F2} dur={duration:F3}s stop={newStopTime:F3} ready={newReadyTime:F3}");

        unsafe
        {
            *(float*)(__instance.Pointer + 0x58) = newStopTime;
            *(float*)(__instance.Pointer + 0x60) = newReadyTime;
        }
    }
}

// ── StopLaser: AoE finisher scaled by Size stat ───────────────────────────────
[HarmonyPatch(typeof(LaserBeamAttack), "StopLaser")]
static class Patch_Noodle_AoEFinisher
{
    static nint _primaryPtr;

    static void Prefix(LaserBeamAttack __instance)
    {
        unsafe { _primaryPtr = *(nint*)(__instance.Pointer + 0x50); }
    }

    static void Postfix(LaserBeamAttack __instance)
    {
        if (_primaryPtr == 0) return;

        var wb = __instance.weaponBase;
        if (wb == null) return;

        float size     = wb.GetValue((EStat)9);
        float radius   = 4f * size;
        float duration = WeaponUtility.GetDuration(wb);

        var primary       = new Enemy(_primaryPtr);
        Vector3 center    = primary.transform.position;
        Vector3 weaponPos = __instance.transform.position;

        var em = EnemyManager.Instance;
        if (em?.enemies == null) return;

        int hits = 0;
        foreach (var kvp in em.enemies)
        {
            var e = kvp.Value;
            if (e == null || e.Pointer == _primaryPtr) continue;
            if (NoodleHelper.IsDead(e)) continue;
            if (Vector3.Distance(center, e.transform.position) > radius) continue;

            Vector3 dir = (e.transform.position - weaponPos).normalized;
            var dc = WeaponUtility.GetDamageContainer(wb, null, e, dir, -1f);
            if (dc == null) continue;

            dc.damage *= duration;

            unsafe
            {
                float hp    = *(float*)(e.Pointer + 0x80);
                float newHp = Mathf.Max(0f, hp - dc.damage);
                *(float*)(e.Pointer + 0x80) = newHp;
                if (newHp <= 0f) e.DiedNextFrame();
            }
            hits++;
        }

        NoodleHelper.Log.LogInfo($"[Noodle] AoE: {hits} hits radius={radius:F1}");
    }
}
