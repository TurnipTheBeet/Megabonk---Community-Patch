namespace MegabonkCommunityPatch;

internal static class IdleJuiceOwnership
{
	internal static int Count;

	internal static bool Owned => Count > 0;

	internal static void Add()
	{
		Count++;
		if (Count == 1)
		{
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
			PatchModules.ReevaluateAll();
		}
	}

	internal static void Reset()
	{
		Count = 0;
		PatchModules.ReevaluateAll();
	}
}
