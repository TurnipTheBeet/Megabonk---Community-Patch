using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal sealed class IconHover : MonoBehaviour
{
	private RectTransform _rt;

	private Camera _cam;

	private bool _shown;

	public IconHover(IntPtr ptr)
		: base(ptr)
	{
	}

	private void OnEnable()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		_rt = ((Component)this).GetComponent<RectTransform>();
		Canvas componentInParent = ((Component)this).GetComponentInParent<Canvas>();
		_cam = (((UnityEngine.Object)(object)componentInParent != (UnityEngine.Object)null && (int)componentInParent.renderMode != 0) ? componentInParent.worldCamera : null);
	}

	private void Update()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_rt == (UnityEngine.Object)null)
		{
			_rt = ((Component)this).GetComponent<RectTransform>();
			if ((UnityEngine.Object)(object)_rt == (UnityEngine.Object)null)
			{
				return;
			}
		}
		bool flag = RectTransformUtility.RectangleContainsScreenPoint(_rt, (Vector2)(Input.mousePosition), _cam);
		if (flag && !_shown)
		{
			if (UpgradeStatTooltip.Text.TryGetValue(((Object)((Component)this).gameObject).GetInstanceID(), out var value) && (UnityEngine.Object)(object)ToolTip.Instance != (UnityEngine.Object)null)
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
		if (_shown && (UnityEngine.Object)(object)ToolTip.Instance != (UnityEngine.Object)null)
		{
			ToolTip.Instance.HideTip();
		}
		_shown = false;
	}
}
