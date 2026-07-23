using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(LaserBeamAttack), "OnDestroy")]
internal static class Patch_SpaceNoodle_Cleanup
{
	private static void Postfix(LaserBeamAttack __instance)
	{
		IntPtr pointer = ((Il2CppObjectBase)__instance).Pointer;
		SpaceNoodleState.ExtraTargetPos.Remove(pointer);
		if (!SpaceNoodleState.ExtraRenderers.TryGetValue(pointer, out var value))
		{
			return;
		}
		foreach (LineRenderer item in value)
		{
			if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null && (UnityEngine.Object)(object)((Component)item).gameObject != (UnityEngine.Object)null)
			{
				Object.Destroy((UnityEngine.Object)(object)((Component)item).gameObject);
			}
		}
		SpaceNoodleState.ExtraRenderers.Remove(pointer);
	}
}
