#nullable disable
using HarmonyLib;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using System.Collections.Generic;
using System.Reflection;

namespace MegaBonkMod;

// ── Shared state ──────────────────────────────────────────────────────────────
static class SpaceNoodleState
{
    // FixedUpdate writes target positions; Update reads them for visuals.
    internal static readonly Dictionary<System.IntPtr, Vector3[]> ExtraTargetPos = new();
    internal static readonly Dictionary<System.IntPtr, List<LineRenderer>> ExtraRenderers = new();
    internal static readonly Collider[] Buf = new Collider[64];
    internal static MethodInfo HitEnemyMi;
}

// ── FixedUpdate Postfix: deal damage to extra targets ─────────────────────────
[HarmonyPatch(typeof(LaserBeamAttack), "FixedUpdate")]
static class Patch_SpaceNoodle_ExtraPhysics
{
    static void Postfix(LaserBeamAttack __instance)
    {
        if (__instance.weaponBase == null) return;

        int extras = WeaponUtility.GetAttackQuantity(__instance.weaponBase) - 1;
        var ptr     = __instance.Pointer;

        bool  isShooting;
        nint  mainTarget;
        unsafe
        {
            isShooting = *(bool*)(__instance.Pointer + 0x64);
            mainTarget = *(nint*)(__instance.Pointer + 0x50);
        }

        if (!isShooting || extras <= 0 || mainTarget == 0)
        {
            SpaceNoodleState.ExtraTargetPos[ptr] = System.Array.Empty<Vector3>();
            return;
        }

        var gm = GameManager.Instance;
        if (gm?.player == null) return;

        // Exclude the main target's rigidbody so extra beams go to different enemies.
        var usedRbs = new HashSet<System.IntPtr>();
        unsafe
        {
            if (mainTarget != __instance.Pointer)   // guard against our own self-ptr trick
            {
                nint rb = *(nint*)(mainTarget + 0x50);
                if (rb != 0) usedRbs.Add((System.IntPtr)rb);
            }
        }

        int found = Physics.OverlapSphereNonAlloc(
            gm.player.transform.position, 30f,
            SpaceNoodleState.Buf, gm.whatIsEnemy);

        SpaceNoodleState.HitEnemyMi ??=
            AccessTools.Method(typeof(LaserBeamAttack), "HitEnemy");

        var positions = new List<Vector3>(extras);

        // Temporarily point 'target' at the instance itself.
        // target+0x50 = 0 (no rigidbody on LaserBeamAttack) so the rigidbody
        // check inside HitEnemy fails for every collider → all enemies pass through.
        unsafe { *(nint*)(__instance.Pointer + 0x50) = __instance.Pointer; }

        var args = new object[1];
        for (int i = 0; i < found && positions.Count < extras; i++)
        {
            var col = SpaceNoodleState.Buf[i];
            if (col == null) continue;
            var rb = col.attachedRigidbody;
            if (rb == null) continue;
            var rbPtr = (System.IntPtr)rb.Pointer;
            if (!usedRbs.Add(rbPtr)) continue;  // already hitting this enemy

            args[0] = col;
            SpaceNoodleState.HitEnemyMi.Invoke(__instance, args);
            positions.Add(col.bounds.center);
        }

        unsafe { *(nint*)(__instance.Pointer + 0x50) = mainTarget; }
        SpaceNoodleState.ExtraTargetPos[ptr] = positions.ToArray();
    }
}

// ── Update Postfix: draw extra beam visuals ───────────────────────────────────
[HarmonyPatch(typeof(LaserBeamAttack), "Update")]
static class Patch_SpaceNoodle_ExtraVisual
{
    static void Postfix(LaserBeamAttack __instance)
    {
        var ptr = __instance.Pointer;

        SpaceNoodleState.ExtraTargetPos.TryGetValue(ptr, out var targets);
        int beamCount = targets?.Length ?? 0;

        if (!SpaceNoodleState.ExtraRenderers.TryGetValue(ptr, out var renderers))
        {
            renderers = new List<LineRenderer>();
            SpaceNoodleState.ExtraRenderers[ptr] = renderers;
        }

        // Lazily create LineRenderers, copying style from the main beam.
        while (renderers.Count < beamCount)
        {
            var go = new GameObject("SpaceNoodleExtraBeam");
            var lr = go.AddComponent<LineRenderer>();
            if (__instance.linerenderer != null)
            {
                lr.sharedMaterial  = __instance.linerenderer.sharedMaterial;
                lr.widthMultiplier = __instance.linerenderer.widthMultiplier;
                lr.startColor      = __instance.linerenderer.startColor;
                lr.endColor        = __instance.linerenderer.endColor;
            }
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            renderers.Add(lr);
        }

        // Muzzle from the main beam's laserStart, else fall back to player pos.
        Vector3 muzzle;
        if (__instance.laserStart != null)
            muzzle = __instance.laserStart.transform.position;
        else
        {
            var player = GameManager.Instance?.player;
            muzzle = player != null
                ? player.transform.position + Vector3.up * 1.2f
                : __instance.transform.position;
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            var lr = renderers[i];
            if (lr == null) continue;
            if (i < beamCount)
            {
                lr.enabled = true;
                lr.SetPosition(0, muzzle);
                lr.SetPosition(1, targets![i]);
            }
            else
            {
                lr.enabled = false;
            }
        }
    }
}

// ── OnDestroy Postfix: tear down extra GameObjects ────────────────────────────
[HarmonyPatch(typeof(LaserBeamAttack), "OnDestroy")]
static class Patch_SpaceNoodle_Cleanup
{
    static void Postfix(LaserBeamAttack __instance)
    {
        var ptr = __instance.Pointer;
        SpaceNoodleState.ExtraTargetPos.Remove(ptr);

        if (SpaceNoodleState.ExtraRenderers.TryGetValue(ptr, out var renderers))
        {
            foreach (var lr in renderers)
                if (lr != null && lr.gameObject != null)
                    UnityEngine.Object.Destroy(lr.gameObject);
            SpaceNoodleState.ExtraRenderers.Remove(ptr);
        }
    }
}
