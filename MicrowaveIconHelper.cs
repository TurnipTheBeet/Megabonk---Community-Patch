using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class MicrowaveIconHelper
{
	private const int MinimapIconOffset = 216;

	internal unsafe static void ApplyMicrowaveColor(InteractableMicrowave instance, bool scaleDown = false)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		GameObject val = instance.minimapIcon;
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			IntPtr intPtr = *(IntPtr*)(void*)(((Il2CppObjectBase)instance).Pointer + 216);
			if (intPtr == IntPtr.Zero)
			{
				return;
			}
			val = new GameObject(intPtr);
		}
		IconColorHelper.ApplyColor(val, IconColorHelper.MicrowaveRarityColor(instance.rarity));
		if (scaleDown)
		{
			Transform transform = val.transform;
			transform.localScale *= 0.5f;
		}
	}
}
