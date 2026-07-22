using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class MovementHelper
{
	public const float MoveSpeedThreshold = 1.5f;

	public unsafe static bool IsPlayerMoving(PlayerMovement pm)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			IntPtr intPtr = *(IntPtr*)(void*)(((Il2CppObjectBase)pm).Pointer + 72);
			if (intPtr == IntPtr.Zero)
			{
				return false;
			}
			Vector3 velocity = new Rigidbody(intPtr).velocity;
			return velocity.x * velocity.x + velocity.z * velocity.z > 2.25f;
		}
		catch
		{
			return false;
		}
	}
}
