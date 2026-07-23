using System;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemIdleJuice), "Tick")]
internal static class Patch_ItemIdleJuice_Movement
{
	[HarmonyPrefix]
	private unsafe static void Prefix(ItemIdleJuice __instance)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		using (Perf.Measure("IdleJuice.Tick"))
		{
			try
			{
				PlayerMovement instance = PlayerMovement.Instance;
				if ((UnityEngine.Object)(object)instance == (UnityEngine.Object)null)
				{
					return;
				}
				Vector3 position = ((Component)instance).transform.position;
				IntPtr pointer = ((Il2CppObjectBase)__instance).Pointer;
				if (MovementHelper.IsPlayerMoving(instance))
				{
					*(float*)(void*)(pointer + 60) = position.x;
					*(float*)(void*)(pointer + 64) = position.y;
					*(float*)(void*)(pointer + 68) = position.z;
					return;
				}
				float num = *(float*)(void*)(pointer + 76);
				if (num <= 0f || float.IsNaN(num))
				{
					num = 50f;
				}
				*(float*)(void*)(pointer + 60) = position.x + num + 50f;
				*(float*)(void*)(pointer + 64) = position.y;
				*(float*)(void*)(pointer + 68) = position.z;
			}
			catch (Exception ex)
			{
				HotErr.Once("IdleJuice.Tick", ex);
			}
		}
	}
}
