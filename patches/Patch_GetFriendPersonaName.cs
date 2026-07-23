using HarmonyLib;
using Steamworks;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(SteamFriends), "GetFriendPersonaName")]
internal static class Patch_GetFriendPersonaName
{
	[HarmonyPostfix]
	private static void Postfix(CSteamID steamIDFriend, ref string __result)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if ((string.IsNullOrEmpty(__result) || __result == "[unknown]") && LeaderboardInjector.NameCache.TryGetValue(steamIDFriend.m_SteamID, out var value))
		{
			__result = value;
		}
	}
}
