using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SaveManager), "SaveConfig")]
internal static class Patch_SaveManager_SaveConfig_LockUploadOff
{
	[HarmonyPrefix]
	private static void Prefix(SaveManager __instance)
	{
		SteamUploadLocker.ForceOff(__instance);
	}
}
