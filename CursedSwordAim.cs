using BepInEx.Configuration;

namespace MegabonkCommunityPatch;

internal static class CursedSwordAim
{
	internal const float AimRange = 30f;

	private static ConfigEntry<bool> _enabled;

	internal static bool Enabled
	{
		get
		{
			return _enabled != null && _enabled.Value;
		}
		set
		{
			if (_enabled != null)
			{
				_enabled.Value = value;
			}
		}
	}

	internal static void Init(ConfigFile cfg)
	{
		_enabled = cfg.Bind<bool>("Weapons", "CursedSwordAutoAim", true, "Cursed Sword auto-aim: the Cursed Sword fires at the nearest enemy instead of along your facing direction. Always on; set false here to disable.");
	}
}
