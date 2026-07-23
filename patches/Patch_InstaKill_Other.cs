using System;
using Assets.Scripts.Actors.Enemies;

namespace MegabonkCommunityPatch;

internal static class Patch_InstaKill_Other
{
	private static bool Prefix(Enemy __instance)
	{
		if (Perf.Enabled)
		{
			Perf.Hit("DamageFromPlayerOther prefix");
		}
		if (!ModGui.InstaKill)
		{
			return true;
		}
		try
		{
			__instance.Kill("instakill");
		}
		catch (Exception ex)
		{
			HotErr.Once("InstaKill.Other", ex);
		}
		return false;
	}
}
