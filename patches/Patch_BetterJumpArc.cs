using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(PlayerMovement), "Jump")]
internal static class Patch_BetterJumpArc
{
	private const float ForwardBoost = 0.2f;

	[HarmonyPostfix]
	private unsafe static void Postfix(PlayerMovement __instance)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null)
			{
				return;
			}
			Rigidbody component = ((Component)__instance).GetComponent<Rigidbody>();
			if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
			{
				return;
			}
			Transform orientation = __instance.orientation;
			if ((UnityEngine.Object)(object)orientation == (UnityEngine.Object)null)
			{
				return;
			}
			float num = *(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 280);
			float num2 = *(float*)(void*)(((Il2CppObjectBase)__instance).Pointer + 284);
			if (num != 0f || num2 != 0f)
			{
				Vector3 val = orientation.forward * num2 + orientation.right * num;
				val.y = 0f;
				if (!(val.sqrMagnitude < 0.0001f))
				{
					val.Normalize();
					Vector3 velocity = component.velocity;
					velocity.x += val.x * 0.2f;
					velocity.z += val.z * 0.2f;
					component.velocity = velocity;
				}
			}
		}
		catch
		{
		}
	}
}
