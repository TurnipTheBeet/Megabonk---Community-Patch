using System;
using System.Reflection;
using Assets.Scripts.Game.MapGeneration;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch]
internal static class Patch_MicrowaveSpawnCount
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(RandomObjectPlacer), "RandomObjectSpawner", new Type[2]
		{
			typeof(RandomMapObject),
			typeof(float)
		}, (Type[])null);
	}

	[HarmonyPrefix]
	private static void Prefix(RandomMapObject randomObject)
	{
		if (((randomObject != null) ? randomObject.prefabs : null) == null)
		{
			return;
		}
		foreach (GameObject item in (Il2CppArrayBase<GameObject>)(object)randomObject.prefabs)
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null || ((Object)item).name.IndexOf("Microwave", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			randomObject.amount = 2;
			randomObject.maxAmount = 3;
			break;
		}
	}
}
