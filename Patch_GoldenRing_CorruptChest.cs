using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ItemUtility), "GetRandomChestItem")]
internal static class Patch_GoldenRing_CorruptChest
{
	[HarmonyPrefix]
	private static bool Prefix(EChest chestType, ref ItemData __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)chestType != 1)
		{
			return true;
		}
		DataManager instance = DataManager.Instance;
		__result = ((instance != null) ? instance.GetItem((EItem)79) : null);
		return false;
	}
}
