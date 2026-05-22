#nullable disable
using HarmonyLib;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;

namespace MegaBonkMod;

internal static class FreeLaserHelper
{
    // Use the same camera + layermask the shotgun/sniper use for their aim raycast
    internal static Vector3 GetAimPoint(Vector3 fallback)
    {
        var cam = PlayerCamera.Instance?.camera;
        if (cam == null) return fallback;

        var ray  = cam.ScreenPointToRay(Input.mousePosition);
        int mask = GameManager.Instance?.whatIsProjectileRaycast ?? -1;

        return Physics.Raycast(ray, out RaycastHit hit, 1000f, mask)
            ? hit.point
            : fallback;
    }

    // GetBeamStart() = player world pos. Add chest offset.
    internal static Vector3 GetMuzzlePos(LaserBeamAttack instance)
    {
        var player = GameManager.Instance?.player;
        var origin = player != null
            ? player.transform.position
            : instance.transform.position;
        return origin + Vector3.up * 1.2f;
    }
}

// ── Block auto-targeting ──────────────────────────────────────────────────────
[HarmonyPatch(typeof(LaserBeamAttack), "FindTarget")]
static class Patch_FreeLaser_FindTarget
{
    static bool Prefix() => false;
}

// ── Drive beam endpoint to aim point (used by FixedUpdate hit detection) ──────
[HarmonyPatch(typeof(LaserBeamAttack), "GetBeamEnd")]
static class Patch_FreeLaser_GetBeamEnd
{
    static bool Prefix(LaserBeamAttack __instance, ref Vector3 __result)
    {
        __result = FreeLaserHelper.GetAimPoint(
            FreeLaserHelper.GetMuzzlePos(__instance));
        return false;
    }
}

// ── Replace Update: manage isShooting + render beam toward aim point ──────────
[HarmonyPatch(typeof(LaserBeamAttack), "Update")]
static class Patch_FreeLaser_Update
{
    static bool Prefix(LaserBeamAttack __instance)
    {
        if (__instance.weaponBase == null || !__instance.enabled) return false;
        if (Time.timeScale == 0f) return false; // paused

        Vector3 startPos = FreeLaserHelper.GetMuzzlePos(__instance);
        Vector3 aimPoint = FreeLaserHelper.GetAimPoint(startPos);
        Vector3 dir      = (aimPoint - startPos).normalized;

        unsafe
        {
            bool isShooting = *(bool*)(__instance.Pointer + 0x64);
            if (!isShooting)
            {
                *(bool*) (__instance.Pointer + 0x64) = true;
                *(float*)(__instance.Pointer + 0x58) = Time.time + 999f;
                *(float*)(__instance.Pointer + 0x5C) = Time.time;

                *(float*)(__instance.Pointer + 0x68) = startPos.x;
                *(float*)(__instance.Pointer + 0x6C) = startPos.y;
                *(float*)(__instance.Pointer + 0x70) = startPos.z;
                *(float*)(__instance.Pointer + 0x74) = aimPoint.x;
                *(float*)(__instance.Pointer + 0x78) = aimPoint.y;
                *(float*)(__instance.Pointer + 0x7C) = aimPoint.z;

                if (__instance.linerenderer != null) __instance.linerenderer.enabled = true;
                if (__instance.laserStart  != null) __instance.laserStart.SetActive(true);
                if (__instance.laserEnd    != null) __instance.laserEnd.SetActive(true);
            }

            *(float*)(__instance.Pointer + 0x44) = dir.x;
            *(float*)(__instance.Pointer + 0x48) = dir.y;
            *(float*)(__instance.Pointer + 0x4C) = dir.z;
        }

        if (__instance.laserStart != null)
            __instance.laserStart.transform.position = startPos;
        if (__instance.laserEnd != null)
            __instance.laserEnd.transform.position = aimPoint;

        if (__instance.linerenderer != null)
        {
            __instance.linerenderer.useWorldSpace = true;
            __instance.linerenderer.positionCount = 2;
            __instance.linerenderer.SetPosition(0, startPos);
            __instance.linerenderer.SetPosition(1, aimPoint);
        }

        return false;
    }
}

// ── FixedUpdate: keep stopTime ahead + reset prevEnd to muzzle every tick ──────
// Resetting prevEnd forces the native box cast to sweep the FULL beam (muzzle →
// aim point) every physics tick, so enemies anywhere along the beam get hit.
[HarmonyPatch(typeof(LaserBeamAttack), "FixedUpdate")]
static class Patch_FreeLaser_FixedUpdate
{
    static void Prefix(LaserBeamAttack __instance)
    {
        if (__instance.weaponBase == null) return;
        unsafe
        {
            if (!*(bool*)(__instance.Pointer + 0x64)) return; // not shooting

            *(float*)(__instance.Pointer + 0x58) = Time.time + 999f; // keep stopTime ahead

            // HitEnemy throws if target==null. Point target at the instance itself so:
            // - target != 0 (no throw)
            // - *(target+0x50) = 0 (null rigidbody) → rigidbody check fails → no enemy is skipped
            if (*(nint*)(__instance.Pointer + 0x50) == 0)
                *(nint*)(__instance.Pointer + 0x50) = __instance.Pointer;

            // Reset prevEnd to muzzle so native sweep covers muzzle → aim point
            Vector3 muzzle = FreeLaserHelper.GetMuzzlePos(__instance);
            *(float*)(__instance.Pointer + 0x74) = muzzle.x;
            *(float*)(__instance.Pointer + 0x78) = muzzle.y;
            *(float*)(__instance.Pointer + 0x7C) = muzzle.z;
        }
    }
}
