using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class StageTimerHelper
{
	private static IntPtr _statics = IntPtr.Zero;

	private unsafe static IntPtr GetStatics()
	{
		if (_statics != IntPtr.Zero)
		{
			return _statics;
		}
		DataManager instance = DataManager.Instance;
		if ((UnityEngine.Object)(object)instance == (UnityEngine.Object)null)
		{
			return IntPtr.Zero;
		}
		IntPtr intPtr = IL2CPP.il2cpp_class_get_image(IL2CPP.il2cpp_object_get_class(((Il2CppObjectBase)instance).Pointer));
		IntPtr intPtr2 = IL2CPP.il2cpp_class_from_name(intPtr, "Assets.Scripts.Utility", "MyTime");
		if (intPtr2 == IntPtr.Zero)
		{
			return IntPtr.Zero;
		}
		_statics = *(IntPtr*)(void*)(intPtr2 + 184);
		return _statics;
	}

	internal unsafe static float GetFinalSwarmTimer()
	{
		try
		{
			IntPtr statics = GetStatics();
			if (statics == IntPtr.Zero)
			{
				return 0f;
			}
			return *(float*)(void*)(statics + 36);
		}
		catch
		{
			return 0f;
		}
	}

	internal unsafe static void Advance(float seconds)
	{
		try
		{
			IntPtr statics = GetStatics();
			if (statics == IntPtr.Zero)
			{
				return;
			}
			float num = *(float*)(void*)(statics + 28);
			if (num < 600f)
			{
				*(float*)(void*)(statics + 28) = Math.Max(0f, Math.Min(num + seconds, 600f));
				return;
			}
			float num2 = *(float*)(void*)(statics + 36);
			float num3 = num2 + seconds;
			if (num3 < 0f)
			{
				*(float*)(void*)(statics + 28) = Math.Max(0f, 600f + num3);
				*(float*)(void*)(statics + 36) = 0f;
			}
			else
			{
				*(float*)(void*)(statics + 36) = num3;
			}
		}
		catch
		{
		}
	}
}
