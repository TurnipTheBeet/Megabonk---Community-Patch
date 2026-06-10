using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// CURSED SWORD AUTO-AIM
//
// The Cursed Sword (EWeapon.CorruptSword) swings along the PLAYER'S FACING, so
// you must physically steer the character to land hits — it feels bad.
//
// ProjectileCringeSword.TryInit builds the ENTIRE swing — hitbox start pos,
// sweep direction, rotation — from MyPlayer.Instance.playerRenderer's forward
// (the player's aim/facing transform). HeroSword.TryInit instead points that
// same direction at an enemy via GetEnemy. An earlier attempt that only patched
// two cached fields (transform.rotation + movingProjectileDir) in a POSTFIX
// looked backwards: the visual blade turned, but the damage hitbox had already
// been placed/baked along the old facing inside TryInit.
//
// Correct fix mirrors manual steering, which is known-good: in a PREFIX, rotate
// the player's renderer transform to face the nearest enemy, let TryInit run so
// the whole swing is built consistently toward the target, then restore the
// renderer's rotation in the POSTFIX. The swap is fully synchronous (no frame
// renders between prefix/original/postfix) so the model never visibly flicks,
// and the game re-aims the renderer on its own next update. Gated to
// CorruptSword so other users of this projectile type are untouched.
// ─────────────────────────────────────────────────────────────────────────
internal static class CursedSwordAim
{
    internal const float AimRange = 30f;   // metres to look for a target before giving up

    static ConfigEntry<bool> _enabled;

    internal static bool Enabled
    {
        get => _enabled != null && _enabled.Value;
        set { if (_enabled != null) _enabled.Value = value; }
    }

    internal static void Init(ConfigFile cfg)
    {
        _enabled = cfg.Bind("Weapons", "CursedSwordAutoAim", true,
            "Cursed Sword auto-aim: the Cursed Sword fires at the nearest enemy " +
            "instead of along your facing direction. Always on; set false here to disable.");
    }
}

[HarmonyPatch(typeof(ProjectileCringeSword), "TryInit")]
static class Patch_CursedSwordAim
{
    // Carries the renderer + its pre-swap rotation from prefix to postfix so we
    // restore exactly what we changed (and nothing when we didn't touch it).
    internal struct AimState
    {
        public bool      Changed;
        public Transform Renderer;
        public Quaternion Saved;
    }

    [HarmonyPrefix]
    static void Prefix(ProjectileCringeSword __instance, out AimState __state)
    {
        __state = default;
        try
        {
            if (!CursedSwordAim.Enabled) return;

            // gate to the Cursed Sword specifically (projectile may be shared)
            var wb = __instance.weaponBase;
            if (wb == null) return;
            var wd = wb.weaponData;
            if (wd == null || wd.eWeapon != EWeapon.CorruptSword) return;

            var player = MyPlayer.Instance;
            if (player == null) return;
            var renderer = player.playerRenderer;
            if (renderer == null) return;
            var rt = renderer.transform;

            // TryInit derives the whole swing from this transform's forward.
            Vector3 origin = rt.position;
            Enemy target = EnemyTargeting.GetEnemy(origin, CursedSwordAim.AimRange, 0, false, null);
            if (target == null) return;                          // nothing in range → leave facing

            Vector3 dir = target.GetCenterPosition() - origin;
            dir.y = 0f;                                          // keep the swing level
            if (dir.sqrMagnitude < 0.0001f) return;              // enemy on top of us
            dir.Normalize();

            __state.Renderer = rt;
            __state.Saved    = rt.rotation;
            __state.Changed  = true;
            // The cursed sword's hitbox is asymmetric — the BACK side reaches
            // farther than the front. Point the player's back at the enemy
            // (face away) so that longer rear arc sweeps the target.
            rt.rotation = Quaternion.LookRotation(-dir);
        }
        catch { __state = default; }
    }

    [HarmonyPostfix]
    static void Postfix(AimState __state)
    {
        try
        {
            if (__state.Changed && __state.Renderer != null)
                __state.Renderer.rotation = __state.Saved;       // restore player facing
        }
        catch { }
    }
}
