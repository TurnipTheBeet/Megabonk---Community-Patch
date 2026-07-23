using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using HarmonyLib;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(Enemy), "DamageFromPlayerOther")]
internal static class Patch_DamageSourceDiag
{
	[HarmonyPrefix]
	private static void Prefix(DamageContainer dc)
	{
		if (Perf.Enabled)
		{
			string text = ((dc != null) ? dc.damageSource : null);
			Perf.Hit("src: " + (string.IsNullOrEmpty(text) ? "(none)" : text));
		}
	}
}
