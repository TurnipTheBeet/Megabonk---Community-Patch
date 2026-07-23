using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(Enemy), "EnemyDied", new Type[] { typeof(DamageContainer) })]
internal static class Patch_BOMBUS_Death
{
	[HarmonyPrefix]
	private static void Prefix(Enemy __instance)
	{
		GiantBeeState.OnBombusDied(__instance);
	}
}
