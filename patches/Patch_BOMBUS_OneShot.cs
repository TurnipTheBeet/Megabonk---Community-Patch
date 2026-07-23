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
internal static class Patch_BOMBUS_OneShot
{
	private static int _origDamage;

	private static bool _boosted;

	[HarmonyPrefix]
	private static void Prefix(PlayerHealth __instance, Enemy enemy)
	{
		if (__instance != null)
		{
			GiantBeeState.PhInstance = __instance;
		}
		_boosted = false;
		if (GiantBeeState.IsBombus(enemy))
		{
			EnemyData enemyData = enemy.enemyData;
			if (!((UnityEngine.Object)(object)enemyData == (UnityEngine.Object)null))
			{
				_origDamage = enemyData.damage;
				enemyData.damage = 9999;
				_boosted = true;
			}
		}
	}

	[HarmonyFinalizer]
	private static void Finalizer(Enemy enemy)
	{
		if (_boosted)
		{
			EnemyData val = ((enemy != null) ? enemy.enemyData : null);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				val.damage = _origDamage;
			}
			_boosted = false;
			GiantBeeState.OnHitPlayer(enemy);
		}
	}
}
