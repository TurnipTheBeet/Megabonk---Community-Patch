using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(PlayerHealth), "DamagePlayer", new Type[]
{
	typeof(Enemy),
	typeof(Vector3),
	typeof(DcFlags)
})]
internal static class Patch_GodMode
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !ModGui.GodMode;
	}
}
