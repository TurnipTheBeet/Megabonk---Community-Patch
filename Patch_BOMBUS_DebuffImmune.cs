using Assets.Scripts.Actors.Enemies;

namespace MegabonkCommunityPatch;

internal static class Patch_BOMBUS_DebuffImmune
{
	private static bool Prefix(Enemy __instance)
	{
		return !GiantBeeState.IsBombus(__instance);
	}
}
