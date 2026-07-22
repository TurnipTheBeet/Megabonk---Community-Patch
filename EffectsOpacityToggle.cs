using System;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class EffectsOpacityToggle
{
	private const string SettingName = "particle_opacity";

	internal static void Toggle()
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			SaveManager instance = SaveManager.Instance;
			CFVisualsSettings val = (((UnityEngine.Object)(object)instance != (UnityEngine.Object)null && instance.config != null) ? instance.config.cfVisualsSettings : null);
			if (val == null)
			{
				Toast.Show("Effects opacity: settings not loaded", Color.yellow);
				return;
			}
			float particle_opacity = val.particle_opacity;
			float num2 = (val.particle_opacity = ((particle_opacity > 0.5f) ? 0f : 1f));
			CurrentSettings.A_SettingUpdated?.Invoke("particle_opacity", (UnityEngine.Object)null, (UnityEngine.Object)null);
			instance.SaveConfig();
			Toast.Show("Effects opacity: " + ((num2 > 0.5f) ? "100%" : "0%"), (num2 > 0.5f) ? Color.green : Color.gray);
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(23, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[EffectsOpacityToggle] ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log.LogWarning(val2);
		}
	}
}
