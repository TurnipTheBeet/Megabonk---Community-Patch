using BepInEx.Configuration;
using UnityEngine;

namespace MegaBonkMod;

// Shared look for the mod's IMGUI windows. Draws a solid dark backdrop behind
// each window so text stays readable over the game world. Opacity is configurable
// (UI/MenuOpacity) and adjustable live from the F1 menu.
internal static class UiTheme
{
    static ConfigEntry<float> _opacity;
    static Texture2D          _tex;

    internal static float Opacity
    {
        get => _opacity != null ? _opacity.Value : 0.85f;
        set { if (_opacity != null) _opacity.Value = Mathf.Clamp(value, 0.2f, 1f); }
    }

    internal static void Init(ConfigFile cfg)
    {
        _opacity = cfg.Bind("UI", "MenuOpacity", 0.85f,
            "Background opacity of the mod's custom windows (0.2 = see-through, 1.0 = solid). Also adjustable live in the F1 menu.");
    }

    static Texture2D Tex()
    {
        if (_tex == null)
        {
            _tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _tex.SetPixel(0, 0, Color.white);
            _tex.Apply();
        }
        return _tex;
    }

    // Fill a window rect with an opaque dark backing before the GUI.Box is drawn.
    internal static void Backdrop(Rect r)
    {
        var prev = GUI.color;
        GUI.color = new Color(0.07f, 0.07f, 0.11f, Opacity);
        GUI.DrawTexture(r, Tex());
        GUI.color = prev;
    }
}
