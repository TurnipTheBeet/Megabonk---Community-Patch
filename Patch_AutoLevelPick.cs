using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(EncounterWindows), "AddEncounter")]
internal static class Patch_AutoLevelPick
{
	[HarmonyPrefix]
	private static bool Prefix(EEncounter rewardWindowType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)rewardWindowType > 0)
		{
			return true;
		}
		if (!AutoLevelPick.Enabled)
		{
			return true;
		}
		return !AutoLevelPick.TryAutoPick();
	}
}
