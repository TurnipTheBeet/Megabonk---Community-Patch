using System;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemCursedDoll), "OnInitOrAmountChanged")]
internal static class Patch_CursedDoll_Buff
{
	[HarmonyPostfix]
	private unsafe static void Postfix(ItemCursedDoll __instance)
	{
		IntPtr pointer = ((Il2CppObjectBase)__instance).Pointer;
		int num = *(int*)(void*)(pointer + 24);
		*(float*)(void*)(pointer + 52) = 0.5f;
		*(int*)(void*)(pointer + 56) = 7;
		*(int*)(void*)(pointer + 60) = 7;
		*(int*)(void*)(pointer + 48) = 7 * num;
	}
}
