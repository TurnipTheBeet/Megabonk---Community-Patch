using System;
using System.Collections.Generic;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(LaserBeamAttack), "FixedUpdate")]
internal static class Patch_SpaceNoodle_ExtraPhysics
{
	private unsafe static void Postfix(LaserBeamAttack __instance)
	{
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		if (!SpaceNoodleState.Enabled || ((ConstantAttack)__instance).weaponBase == null)
		{
			return;
		}
		int num = WeaponUtility.GetAttackQuantity(((ConstantAttack)__instance).weaponBase) - 1;
		IntPtr pointer = ((Il2CppObjectBase)__instance).Pointer;
		bool flag = *(bool*)(void*)(((Il2CppObjectBase)__instance).Pointer + 100);
		nint num2 = *(nint*)(void*)(((Il2CppObjectBase)__instance).Pointer + 80);
		if (!SpaceNoodleState.ExtraTargetPos.TryGetValue(pointer, out var value))
		{
			value = new List<Vector3>(4);
			SpaceNoodleState.ExtraTargetPos[pointer] = value;
		}
		if (!flag || num <= 0 || num2 == 0)
		{
			value.Clear();
			return;
		}
		GameManager instance = GameManager.Instance;
		if ((UnityEngine.Object)(object)((instance != null) ? instance.player : null) == (UnityEngine.Object)null)
		{
			return;
		}
		HashSet<IntPtr> usedRbs = SpaceNoodleState.UsedRbs;
		usedRbs.Clear();
		if (num2 != ((Il2CppObjectBase)__instance).Pointer)
		{
			IntPtr intPtr = *(IntPtr*)(num2 + 72);
			if (intPtr != (IntPtr)0)
			{
				usedRbs.Add(intPtr);
			}
		}
		int num3 = Physics.OverlapSphereNonAlloc(((Component)instance.player).transform.position, 30f, (Il2CppReferenceArray<Collider>)(SpaceNoodleState.Buf), (LayerMask)(instance.whatIsEnemy));
		value.Clear();
		*(IntPtr*)(void*)(((Il2CppObjectBase)__instance).Pointer + 80) = ((Il2CppObjectBase)__instance).Pointer;
		for (int i = 0; i < num3; i++)
		{
			if (value.Count >= num)
			{
				break;
			}
			Collider val = SpaceNoodleState.Buf[i];
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				continue;
			}
			Rigidbody attachedRigidbody = val.attachedRigidbody;
			if (!((UnityEngine.Object)(object)attachedRigidbody == (UnityEngine.Object)null))
			{
				IntPtr pointer2 = ((Il2CppObjectBase)attachedRigidbody).Pointer;
				if (usedRbs.Add(pointer2))
				{
					SpaceNoodleState.HitEnemy(__instance, val);
					List<Vector3> list = value;
					Bounds bounds = val.bounds;
					list.Add(bounds.center);
				}
			}
		}
		*(nint*)(void*)(((Il2CppObjectBase)__instance).Pointer + 80) = num2;
	}
}
