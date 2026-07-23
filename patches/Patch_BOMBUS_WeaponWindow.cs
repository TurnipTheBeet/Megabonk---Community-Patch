using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

internal static class Patch_BOMBUS_WeaponWindow
{
	private static bool Prefix(Enemy __instance, DamageContainer dc)
	{
		if (Perf.Enabled)
		{
			Perf.Hit("DamageFromPlayerWeapon");
		}
		GiantBeeState.WeaponDamageTarget = ((Il2CppObjectBase)__instance).Pointer;
		GiantBeeState.WeaponHitExecute = dc != null && dc.isExecute;
		if (!ModGui.InstaKill)
		{
			return true;
		}
		try
		{
			__instance.Kill("instakill");
		}
		catch (Exception ex)
		{
			HotErr.Once("InstaKill.Weapon", ex);
		}
		return false;
	}

	private static void Finalizer()
	{
		GiantBeeState.WeaponDamageTarget = IntPtr.Zero;
		GiantBeeState.WeaponHitExecute = false;
	}
}
