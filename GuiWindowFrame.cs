using UnityEngine;

namespace MegaBonkMod;

// Shared draggable + uniform-resizeable frame for our IMGUI popup menus.
//   • Default Scale = 2.0  → menus open at double their old size.
//   • Drag the title bar to move; drag the bottom-right grip to resize.
// All hit-testing runs in raw screen space (call HandleInput from Update), so it
// stays correct no matter what GUI.matrix the draw pass uses. Begin()/End() wrap
// the draw in a ScaleAroundPivot so every Rect inside scales uniformly.
internal class GuiWindowFrame
{
    public Vector2 Pivot;            // screen-space top-left of the window
    public float   Scale = 2f;       // double size by default
    public float   MinScale = 1f;
    public float   MaxScale = 4f;

    public const float Grip = 18f;   // screen-px resize grip square

    bool _dragging, _resizing;
    Vector2 _dragOff;

    public GuiWindowFrame(Vector2 pivot) { Pivot = pivot; }

    // Raw-input drag/resize. winW/winH are the LOGICAL (unscaled) window size.
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
        if (Input.GetMouseButtonUp(0)) { _dragging = false; _resizing = false; }

        if (_dragging && Input.GetMouseButton(0))
            Pivot = mp - _dragOff;
        if (_resizing && Input.GetMouseButton(0))
            Scale = Mathf.Clamp((mx - Pivot.x) / Mathf.Max(1f, winW), MinScale, MaxScale);
    }

    public bool Busy => _dragging || _resizing;

    // Wrap drawing: returns the previous matrix; pass it back to End().
    public Matrix4x4 Begin()
    {
        var old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(Scale, Scale), Pivot);
        return old;
    }

    public void End(Matrix4x4 old) => GUI.matrix = old;

    // Draw the grip in raw screen space — call AFTER End() so it lines up exactly
    // with the screen-space hit area used by HandleInput.
    public void DrawGrip(float winW, float winH)
    {
        float sw = winW * Scale, sh = winH * Scale;
        GUI.Box(new Rect(Pivot.x + sw - Grip, Pivot.y + sh - Grip, Grip, Grip), "↘");
    }
}
