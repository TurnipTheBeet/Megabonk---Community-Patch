using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(Settings), "OnDisable")]
internal static class Patch_CPTab_Close
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		NativeSettings.OnSettingsClosed();
	}
}
