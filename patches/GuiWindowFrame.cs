using BepInEx.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

// Shared draggable + uniform-resizeable frame for our IMGUI popup menus.
//   • Default Scale = 2.0  → menus open at double their old size.
//   • Drag the title bar to move; drag the bottom-right grip to resize.
//   • Call .Persist("WindowId") to save/restore position + scale in config.
// All hit-testing runs in raw screen space (call HandleInput from Update), so it
// stays correct no matter what GUI.matrix the draw pass uses. Begin()/End() wrap
// the draw in a ScaleAroundPivot so every Rect inside scales uniformly.
internal class GuiWindowFrame
{
    public Vector2 Pivot;
    public float   Scale = 2f;
    public float   MinScale = 1f;
    public float   MaxScale = 4f;

    public const float Grip = 24f;

    bool _dragging, _resizing;
    Vector2 _dragOff;

    static ConfigFile _cfg;
    ConfigEntry<float> _cX, _cY, _cScale;

    internal static void SetConfig(ConfigFile cfg) => _cfg = cfg;

    public GuiWindowFrame(Vector2 pivot) { Pivot = pivot; }

    public GuiWindowFrame Persist(string id)
    {
        if (_cfg == null || string.IsNullOrEmpty(id)) return this;
        _cX = _cfg.Bind("WindowLayout", id + ".X", Pivot.x, "Saved window X (screen px).");
        _cY = _cfg.Bind("WindowLayout", id + ".Y", Pivot.y, "Saved window Y (screen px).");
        _cScale = _cfg.Bind("WindowLayout", id + ".Scale", Scale, "Saved window scale.");
        Pivot = new Vector2(_cX.Value, _cY.Value);
        Scale = Mathf.Clamp(_cScale.Value, MinScale, MaxScale);
        ClampOnScreen();
        return this;
    }

    void ClampOnScreen()
    {
        if (Screen.width > 0)
        {
            Pivot.x = Mathf.Clamp(Pivot.x, 0f, Mathf.Max(0f, Screen.width - 60f));
            Pivot.y = Mathf.Clamp(Pivot.y, 0f, Mathf.Max(0f, Screen.height - 40f));
        }
    }

    void Save()
    {
        if (_cX != null)
        {
            _cX.Value = Pivot.x;
            _cY.Value = Pivot.y;
            _cScale.Value = Scale;
        }
    }

    public bool Busy => _dragging || _resizing;

    public void HandleInput(float winW, float winH, float titleH)
    {
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        var mp = new Vector2(mx, my);
        float sw = winW * Scale, sh = winH * Scale;

        if (Input.GetMouseButtonDown(0))
        {
            if (new Rect(Pivot.x + sw - Grip, Pivot.y + sh - Grip, Grip, Grip).Contains(mp))
                _resizing = true;
            else if (new Rect(Pivot.x, Pivot.y, sw, titleH * Scale).Contains(mp))
            {
                _dragging = true;
                _dragOff  = mp - Pivot;
            }
        }
        if (Input.GetMouseButtonUp(0) && (_dragging || _resizing))
        {
            _dragging = false; _resizing = false;
            Save();
        }

        if (_dragging && Input.GetMouseButton(0))
            Pivot = mp - _dragOff;
        if (_resizing && Input.GetMouseButton(0))
        {
            float newW = mx - Pivot.x;
            Scale = Mathf.Clamp(newW / Mathf.Max(1f, winW), MinScale, MaxScale);
        }
    }

    public Matrix4x4 Begin()
    {
        var old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(Scale, Scale), Pivot);
        return old;
    }

    public void End(Matrix4x4 old) => GUI.matrix = old;

    public void DrawGrip(float winW, float winH)
    {
        float sw = winW * Scale, sh = winH * Scale;
        float gx = Pivot.x + sw;
        float gy = Pivot.y + sh;
        
        Color oldColor = GUI.color;
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        
        // Draw three crisp diagonal 45-degree lines cutting the bottom-right corner
        for (int i = 0; i <= 16; i++)
        {
            GUI.DrawTexture(new Rect(gx - 16 + i, gy - i, 1.5f, 1.5f), Texture2D.whiteTexture);
        }
        for (int i = 0; i <= 11; i++)
        {
            GUI.DrawTexture(new Rect(gx - 11 + i, gy - i, 1.5f, 1.5f), Texture2D.whiteTexture);
        }
        for (int i = 0; i <= 6; i++)
        {
            GUI.DrawTexture(new Rect(gx - 6 + i, gy - i, 1.5f, 1.5f), Texture2D.whiteTexture);
        }
        
        GUI.color = oldColor;
    }
}
