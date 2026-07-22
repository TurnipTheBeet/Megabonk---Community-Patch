using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(InteractablePot), "SpawnStuff")]
internal static class Patch_InteractablePot_SpawnStuff
{
	private static readonly EPickup[] Powerups;

	[HarmonyPrefix]
	private static void Prefix(ref EPickup ePickup)
	{
		if ((int)ePickup == 2)
		{
			ePickup = Powerups[UnityEngine.Random.Range(0, Powerups.Length)];
		}
	}

	static Patch_InteractablePot_SpawnStuff()
	{
		Powerups = new EPickup[8]
		{
			EPickup.Health, EPickup.Nuke, EPickup.Time, EPickup.Shield,
			EPickup.Rage, EPickup.Haste, EPickup.Stonks, EPickup.Magnet
		};
	}
}
