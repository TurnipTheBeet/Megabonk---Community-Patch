using Assets.Scripts.Inventory__Items__Pickups.Items;

namespace MegabonkCommunityPatch;

internal static class ZaWarudoTracker
{
	internal static int Total;

	internal const int PoolCap = 25;

	internal static void Reset()
	{
		Total = 0;
	}

	internal static void OnReceived(int count)
	{
		Total += count;
		if (Total >= 25)
		{
			SizeCapHelper.RemoveFromPool((EItem)25);
		}
	}
}
