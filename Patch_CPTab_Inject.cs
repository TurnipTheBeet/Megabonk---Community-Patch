using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(Settings), "OnEnable")]
internal static class Patch_CPTab_Inject
{
	[HarmonyPostfix]
	private static void Postfix(Settings __instance)
	{
		NativeSettings.TryInject(__instance);
	}
}
