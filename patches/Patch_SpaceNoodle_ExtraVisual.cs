using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(LaserBeamAttack), "Update")]
internal static class Patch_SpaceNoodle_ExtraVisual
{
	private static void Postfix(LaserBeamAttack __instance)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		bool enabled = SpaceNoodleState.Enabled;
		if (!enabled && SpaceNoodleState.ExtraRenderers.Count == 0)
		{
			return;
		}
		IntPtr pointer = ((Il2CppObjectBase)__instance).Pointer;
		SpaceNoodleState.ExtraTargetPos.TryGetValue(pointer, out var value);
		int num = (enabled ? (value?.Count ?? 0) : 0);
		if (!SpaceNoodleState.ExtraRenderers.TryGetValue(pointer, out var value2))
		{
			if (num == 0)
			{
				return;
			}
			value2 = new List<LineRenderer>();
			SpaceNoodleState.ExtraRenderers[pointer] = value2;
		}
		while (value2.Count < num)
		{
			GameObject val = new GameObject("SpaceNoodleExtraBeam");
			LineRenderer val2 = val.AddComponent<LineRenderer>();
			if ((UnityEngine.Object)(object)__instance.linerenderer != (UnityEngine.Object)null)
			{
				((Renderer)val2).sharedMaterial = ((Renderer)__instance.linerenderer).sharedMaterial;
				val2.widthMultiplier = __instance.linerenderer.widthMultiplier;
				val2.startColor = __instance.linerenderer.startColor;
				val2.endColor = __instance.linerenderer.endColor;
			}
			val2.positionCount = 2;
			val2.useWorldSpace = true;
			value2.Add(val2);
		}
		Vector3 val3;
		if ((UnityEngine.Object)(object)__instance.laserStart != (UnityEngine.Object)null)
		{
			val3 = __instance.laserStart.transform.position;
		}
		else
		{
			GameManager instance = GameManager.Instance;
			MyPlayer val4 = ((instance != null) ? instance.player : null);
			val3 = (((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null) ? (((Component)val4).transform.position + Vector3.up * 1.2f) : ((Component)__instance).transform.position);
		}
		for (int i = 0; i < value2.Count; i++)
		{
			LineRenderer val5 = value2[i];
			if (!((UnityEngine.Object)(object)val5 == (UnityEngine.Object)null))
			{
				if (i < num)
				{
					((Renderer)val5).enabled = true;
					val5.SetPosition(0, val3);
					val5.SetPosition(1, value[i]);
				}
				else
				{
					((Renderer)val5).enabled = false;
				}
			}
		}
	}
}
