using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegaBonkMod;

internal sealed class IconHover : MonoBehaviour
{
    private RectTransform _rt;
    private Camera _cam;
    private bool _shown;

    public IconHover(IntPtr ptr) : base(ptr) { }

    private void OnEnable()
    {
        _rt = ((Component)this).GetComponent<RectTransform>();
        Canvas componentInParent = ((Component)this).GetComponentInParent<Canvas>();
        _cam = (((Object)(object)componentInParent != (Object)null && (int)componentInParent.renderMode != 0) ? componentInParent.worldCamera : null);
    }

    private void Update()
    {
        if ((Object)(object)_rt == (Object)null)
        {
            _rt = ((Component)this).GetComponent<RectTransform>();
            if ((Object)(object)_rt == (Object)null) return;
        }

        bool flag = RectTransformUtility.RectangleContainsScreenPoint(_rt, (Vector2)Input.mousePosition, _cam);
        if (flag && !_shown)
        {
            if (UpgradeStatTooltip.Text.TryGetValue(((Object)((Component)this).gameObject).GetInstanceID(), out var value) && (Object)(object)ToolTip.Instance != (Object)null)
            {
                ToolTip.Instance.SetTip(value, _rt);
                _shown = true;
            }
        }
        else if (!flag && _shown)
        {
            Hide();
        }
    }

    private void OnDisable()
    {
        Hide();
    }

    private void Hide()
    {
        if (_shown && (Object)(object)ToolTip.Instance != (Object)null)
            ToolTip.Instance.HideTip();
        _shown = false;
    }
}
