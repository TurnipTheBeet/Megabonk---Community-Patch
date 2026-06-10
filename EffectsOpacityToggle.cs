#nullable disable
using UnityEngine;

namespace MegaBonkMod;

// Toggles the game's "Effects" opacity slider (Settings > Effects, backed by
// CFVisualsSettings.particle_opacity, a 0–1 alpha multiplier) between 0% and
// 100%. Writes the live setting, fires the game's settings-changed event so
// every ParticleOpacity component refreshes immediately, then persists it.
internal static class EffectsOpacityToggle
{
    const string SettingName = "particle_opacity";

    internal static void Toggle()
    {
        try
        {
            var sm = SaveManager.Instance;
            var vis = sm != null && sm.config != null ? sm.config.cfVisualsSettings : null;
            if (vis == null) { Toast.Show("Effects opacity: settings not loaded", Color.yellow); return; }

            float cur  = vis.particle_opacity;
            float next = cur > 0.5f ? 0f : 1f;
            vis.particle_opacity = next;

            // Tell the game a setting changed → ParticleOpacity components re-read
            // the value and refresh. Subscribers ignore the value args for any name
            // that isn't theirs, so null payloads are safe.
            CurrentSettings.A_SettingUpdated?.Invoke(SettingName, null, null);

            sm.SaveConfig();

            Toast.Show($"Effects opacity: {(next > 0.5f ? "100%" : "0%")}",
                       next > 0.5f ? Color.green : Color.gray);
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogWarning($"[EffectsOpacityToggle] {e.Message}");
        }
    }
}
