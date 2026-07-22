using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(MyAchievements), "CanToggleActivation")]
internal static class Patch_CanToggleActivation_NonToggleable
{
	[HarmonyPrefix]
	private static bool Prefix(UnlockableBase unlockable, ref bool __result)
	{
		DataManager instance = DataManager.Instance;
		if ((UnityEngine.Object)(object)instance == (UnityEngine.Object)null)
		{
			return true;
		}
		if ((UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)40) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)33) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)32) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)55) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)56) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)19))
		{
			__result = false;
			return false;
		}
		if ((UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)72) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)8) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)59) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)4) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)42) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)23))
		{
			__result = true;
			return false;
		}
		if ((UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)38) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)82) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)7) || (UnityEngine.Object)(object)unlockable == (UnityEngine.Object)(object)instance.GetItem((EItem)0))
		{
			__result = false;
			return false;
		}
		return true;
	}
}
