using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(BaseInteractable), "Start")]
internal static class Patch_BaseInteractable_Start
{
	[HarmonyPostfix]
	private static void Postfix(BaseInteractable __instance)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		InteractableShrineCursed val = ((Il2CppObjectBase)__instance).TryCast<InteractableShrineCursed>();
		if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
		{
			ApplyShrineColor(val.minimapIcon, 88, ((Il2CppObjectBase)val).Pointer, new Color(1f, 0.4f, 0.4f));
			return;
		}
		InteractableShrineChallenge val2 = ((Il2CppObjectBase)__instance).TryCast<InteractableShrineChallenge>();
		if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
		{
			ApplyShrineColor(val2.minimapIcon, 88, ((Il2CppObjectBase)val2).Pointer, new Color(1f, 0.4f, 0.7f));
			return;
		}
		InteractableShrineMagnet val3 = ((Il2CppObjectBase)__instance).TryCast<InteractableShrineMagnet>();
		if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
		{
			ApplyShrineColor(val3.minimapIcon, 88, ((Il2CppObjectBase)val3).Pointer, Color.black);
		}
	}

	private unsafe static void ApplyShrineColor(GameObject icon, int offset, IntPtr ptr, Color color)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		if ((UnityEngine.Object)(object)icon == (UnityEngine.Object)null)
		{
			IntPtr intPtr = *(IntPtr*)(void*)(ptr + offset);
			if (intPtr == IntPtr.Zero)
			{
				return;
			}
			icon = new GameObject(intPtr);
		}
		IconColorHelper.ApplyColor(icon, color);
		Transform transform = icon.transform;
		transform.localScale *= 0.5f;
	}
}
