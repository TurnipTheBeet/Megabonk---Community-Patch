using Assets.Scripts.Actors.Enemies;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

internal static class Patch_BOMBUS_DamageGate
{
	private static bool Prefix(Enemy __instance)
	{
		if (!GiantBeeState.IsBombus(__instance))
		{
			return true;
		}
		return ((Il2CppObjectBase)__instance).Pointer == GiantBeeState.WeaponDamageTarget && !GiantBeeState.WeaponHitExecute;
	}
}
