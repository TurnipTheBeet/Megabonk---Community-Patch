using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(PlayerMovement), "CheckInput")]
internal static class Patch_PasswordInput
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !ModGui.SuppressMovementInput;
	}
}
