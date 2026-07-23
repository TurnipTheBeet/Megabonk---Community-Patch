using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SaveManager), "Init")]
internal static class Patch_SaveManager_Init_LockUploadOff
{
	[HarmonyPostfix]
	private static void Postfix(SaveManager __instance)
	{
		SteamUploadLocker.ForceOff(__instance);
	}
}
