using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using BepInEx.Configuration;
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
	private static ConfigEntry<bool> _enabled;

	internal static bool Enabled => _enabled != null && _enabled.Value;

	internal static void Init(ConfigFile cfg)
	{
		_enabled = cfg.Bind<bool>("Combat", "RevolverManualAim", true, "When holding the aim key, Revolver shots go toward your crosshair instead of auto-targeting enemies.");
	}

	private static readonly System.Reflection.FieldInfo AimingField =
		AccessTools.Field(typeof(PlayerInput), "aiming");

	[HarmonyPrefix]
	private static bool Prefix(ProjectileBasic __instance)
	{
		if (!Enabled)
			return true;

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

		bool aiming = false;
		try
		{
			if (AimingField != null)
				aiming = (bool)AimingField.GetValue(player.playerInput);
		}
		catch
		{
		}
		if (!aiming)
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
