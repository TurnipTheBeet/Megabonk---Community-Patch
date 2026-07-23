using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;

namespace MegabonkCommunityPatch;

internal static class SizeCapHelper
{
	internal static void RemoveFromPool(EItem eItem)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		var availableItems = RunUnlockables.availableItems;
		if (availableItems == null)
		{
			return;
		}
		var enumerator = availableItems.GetEnumerator();
		while (enumerator.MoveNext())
		{
			var current = enumerator.Current;
			if (current.Value == null)
			{
				continue;
			}
			for (int num = current.Value.Count - 1; num >= 0; num--)
			{
				ItemData obj = current.Value[num];
				if (obj != null && obj.eItem == eItem)
				{
					current.Value.RemoveAt(num);
					return;
				}
			}
		}
	}
}
