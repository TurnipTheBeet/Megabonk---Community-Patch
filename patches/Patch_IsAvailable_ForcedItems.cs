using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(MyAchievements), "IsAvailable")]
internal static class Patch_IsAvailable_ForcedItems
{
	[HarmonyPrefix]
	private static bool Prefix(UnlockableBase unlockable, ref bool __result)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		DataManager instance = DataManager.Instance;
		if ((UnityEngine.Object)(object)instance == (UnityEngine.Object)null)
		{
			return true;
		}
		foreach (EItem forcedPoolItem in Plugin.ForcedPoolItems)
		{
			if ((UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem(forcedPoolItem))
			{
				__result = true;
				return false;
			}
		}
		return true;
	}
}
