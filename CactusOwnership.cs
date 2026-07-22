namespace MegabonkCommunityPatch;

internal static class CactusOwnership
{
	internal static int Count;
	internal static int Depth;

	internal static bool Owned => Count > 0;

	internal static void Add()
	{
		Count++;
		if (Count == 1)
		{
			NativeHooks.CactusPickupRangeActive = true;
			PatchModules.ReevaluateAll();
		}
	}

	internal static void Remove()
	{
		if (Count > 0)
		{
			Count--;
		}
		if (Count == 0)
		{
			NativeHooks.CactusPickupRangeActive = false;
			PatchModules.ReevaluateAll();
		}
	}

	internal static void Reset()
	{
		Count = 0;
		NativeHooks.CactusPickupRangeActive = false;
		PatchModules.ReevaluateAll();
	}
}
