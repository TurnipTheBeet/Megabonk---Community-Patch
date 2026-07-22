using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using HarmonyLib;
using UnityEngine;

namespace MegabonkCommunityPatch;

/// <summary>
/// When the player holds the game's built-in aim key with a Revolver equipped,
/// use manual camera-aim like Sniper/Shotgun instead of auto-targeting.
/// </summary>
[HarmonyPatch(typeof(ProjectileBasic), "FindMovementDirection")]
internal static class Patch_Revolver_ManualAim
{
    [HarmonyPrefix]
    private static bool Prefix(ProjectileBasic __instance)
    {
        // Only affect revolvers (EWeapon = 3)
        WeaponBase wb = __instance.weaponBase;
        if (wb == null || wb.weaponData == null)
            return true;

        if ((int)wb.weaponData.eWeapon != 3)
            return true;

        // Check if the player is holding the built-in aim key
        MyPlayer player = MyPlayer.Instance;
        if (player == null || player.playerInput == null)
            return true;

        if (!player.playerInput.aiming)
            return true;

        // Manual aim: use camera forward direction
        Camera cam = Camera.main;
        if (cam == null)
            return true;

        Vector3 aimDir = cam.transform.forward;
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude < 0.0001f)
            return true;

        aimDir.Normalize();
        __instance.direction = aimDir;

        // Skip the original auto-targeting logic
        return false;
    }
}