using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class SteamUploadLocker
{
	internal const string FieldName = "upload_score_to_leaderboard";

	internal unsafe static void ForceOff(SaveManager sm)
	{
		if ((UnityEngine.Object)(object)sm == (UnityEngine.Object)null)
		{
			return;
		}
		IntPtr pointer = ((Il2CppObjectBase)sm).Pointer;
		if (pointer == IntPtr.Zero)
		{
			return;
		}
		IntPtr intPtr = *(IntPtr*)(void*)(pointer + 32);
		if (!(intPtr == IntPtr.Zero))
		{
			IntPtr intPtr2 = *(IntPtr*)(void*)(intPtr + 24);
			if (!(intPtr2 == IntPtr.Zero))
			{
				*(int*)(void*)(intPtr2 + 68) = 0;
			}
		}
	}
}
