using UnityEngine;

namespace MegaBonkMod;

// Shared transient on-screen notification for hotkey toggles that have no window
// of their own (Smart Skip Chest, Smart Targeting, …). Resolution-aware so it
// stays readable on 4K / high-DPI displays instead of a tiny fixed-pixel box.
internal static class Toast
{
    static string _msg;
    static Color  _col = Color.white;
    static float  _until;

    static GUIStyle _cachedStyle;
    static int       _cachedFontSize;

    internal static void Show(string msg, Color col, float seconds = 2.5f)
    {
        _msg = msg; _col = col; _until = Time.unscaledTime + seconds;
    }

    internal static void Draw()
    {
        if (_msg == null || Time.unscaledTime > _until) return;

        // scale everything off a 1080p baseline; never shrink below 1x
        float scale = Mathf.Max(1f, Screen.height / 1080f);
        float w = 320f * scale;
        float h = 46f  * scale;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.16f;

        // Cache the GUIStyle — only recreate when font size changes.
        int fs = Mathf.RoundToInt(20f * scale);
        if (_cachedStyle == null || fs != _cachedFontSize)
        {
            _cachedStyle = new GUIStyle(GUI.skin.box) { fontSize = fs };
            _cachedFontSize = fs;
        }

        var prev = GUI.color;
        GUI.color = _col;
        GUI.Box(new Rect(x, y, w, h), _msg, _cachedStyle);
        GUI.color = prev;
    }
}
