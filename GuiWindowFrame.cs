using BepInEx.Configuration;
using UnityEngine;

namespace MegaBonkMod;

// Shared draggable + free-resizeable frame for our IMGUI popup menus.
//   • Default Scale = 2.0  → menus open at double their old size.
//   • Drag the title bar to move; drag the bottom-right corner to resize.
//     The grip adjusts ScaleX/ScaleY independently so the user can make
//     the window any aspect ratio they want.
//   • Position and size persist across sessions via BepInEx config
//     (call Init() with a ConfigFile + section name).
// All hit-testing runs in raw screen space (call HandleInput from Update), so it
// stays correct no matter what GUI.matrix the draw pass uses. Begin()/End() wrap
// the draw in a ScaleAroundPivot so every Rect inside scales uniformly.
internal class GuiWindowFrame
{
    public Vector2 Pivot;            // screen-space top-left of the window
    public float   Scale  = 2f;      // uniform scale (kept for compatibility)
    public float   ScaleX = 2f;      // horizontal scale (independent)
    public float   ScaleY = 2f;      // vertical scale (independent)
    public float   MinScale = 1f;
    public float   MaxScale = 4f;

    public const float Grip = 18f;   // screen-px resize grip square

    bool _dragging, _resizing;
    Vector2 _dragOff;

    // persistence
    ConfigEntry<float> _cfgPX;
    ConfigEntry<float> _cfgPY;
    ConfigEntry<float> _cfgSX;
    ConfigEntry<float> _cfgSY;
    bool _loaded;

    public GuiWindowFrame(Vector2 pivot) { Pivot = pivot; }

    public void Init(ConfigFile cfg, string section)
    {
        _cfgPX = cfg.Bind(section, "PositionX", Pivot.x, "Window X position (pixels from top-left).");
        _cfgPY = cfg.Bind(section, "PositionY", Pivot.y, "Window Y position (pixels from top-left).");
        _cfgSX = cfg.Bind(section, "ScaleX",    ScaleX,  "Window horizontal scale.");
        _cfgSY = cfg.Bind(section, "ScaleY",    ScaleY,  "Window vertical scale.");
        Pivot  = new Vector2(_cfgPX.Value, _cfgPY.Value);
        ScaleX = Mathf.Clamp(_cfgSX.Value, MinScale, MaxScale);
        ScaleY = Mathf.Clamp(_cfgSY.Value, MinScale, MaxScale);
        Scale  = (ScaleX + ScaleY) * 0.5f;
        _loaded = true;
    }

    void Save()
    {
        if (!_loaded) return;
        _cfgPX.Value = Pivot.x;
        _cfgPY.Value = Pivot.y;
        _cfgSX.Value = ScaleX;
        _cfgSY.Value = ScaleY;
    }

    // Raw-input drag/resize. winW/winH are the LOGICAL (unscaled) window size.
    public void HandleInput(float winW, float winH, float titleH)
    {
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        var mp = new Vector2(mx, my);
        float sw = winW * ScaleX, sh = winH * ScaleY;

        if (Input.GetMouseButtonDown(0))
        {
            if (new Rect(Pivot.x + sw - Grip, Pivot.y + sh - Grip, Grip, Grip).Contains(mp))
                _resizing = true;
            else if (new Rect(Pivot.x, Pivot.y, sw, titleH * ScaleY).Contains(mp))
            {
                _dragging = true;
                _dragOff  = mp - Pivot;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (_dragging || _resizing) Save();
            _dragging = false;
            _resizing = false;
        }

        if (_dragging && Input.GetMouseButton(0))
            Pivot = mp - _dragOff;
        if (_resizing && Input.GetMouseButton(0))
        {
            float newSX = Mathf.Clamp((mx - Pivot.x) / Mathf.Max(1f, winW), MinScale, MaxScale);
            float newSY = Mathf.Clamp((my - Pivot.y) / Mathf.Max(1f, winH), MinScale, MaxScale);
            ScaleX = newSX;
            ScaleY = newSY;
            Scale  = (newSX + newSY) * 0.5f;
        }
    }

    public Rect GetGripRect(float winW, float winH)
    {
        float sw = winW * ScaleX, sh = winH * ScaleY;
        return new Rect(Pivot.x + sw - Grip, Pivot.y + sh - Grip, Grip, Grip);
    }

    public bool Busy => _dragging || _resizing;

    public Matrix4x4 Begin()
    {
        var old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(ScaleX, ScaleY), Pivot);
        return old;
    }

    public void End(Matrix4x4 old) => GUI.matrix = old;

    static Texture2D _gripTex;

    public void DrawGrip(float winW, float winH)
    {
        var gripRect = GetGripRect(winW, winH);
        var oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.5f);

        if (_gripTex == null)
        {
            int sz = 32;
            _gripTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            _gripTex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[sz * sz];
            // SetPixels row 0 = bottom of texture, but DrawTexture maps it to top of rect.
            // So we iterate top-down for screen, writing to rows bottom-up.
            for (int sy = 0; sy < sz; sy++)
            {
                int row = sz - 1 - sy;  // screen row 0 → texture row 31 (bottom)
                for (int col = 0; col < sz; col++)
                {
                    // Bottom-right triangle: fill where col + screenY >= sz-1
                    pixels[row * sz + col] = (col + sy >= sz - 1)
                        ? Color.white
                        : new Color(0, 0, 0, 0);
                }
            }
            _gripTex.SetPixels(pixels);
            _gripTex.Apply();
        }
        GUI.DrawTexture(gripRect, _gripTex);
        GUI.color = oldColor;
    }
}
