using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class Toast
{
	private static string _msg;

	private static Color _col = Color.white;

	private static float _until;

	private static GUIStyle _style;

	private static int _styleFontSize;

	internal static void Show(string msg, Color col, float seconds = 2.5f)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		_msg = msg;
		_col = col;
		_until = Time.unscaledTime + seconds;
	}

	internal static void Draw()
	{
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		if (_msg != null && !(Time.unscaledTime > _until))
		{
			float num = Mathf.Max(1f, (float)Screen.height / 1080f);
			float num2 = 320f * num;
			float num3 = 46f * num;
			float num4 = ((float)Screen.width - num2) / 2f;
			float num5 = (float)Screen.height * 0.16f;
			int num6 = Mathf.RoundToInt(20f * num);
			if (_style == null || _styleFontSize != num6)
			{
				_style = new GUIStyle(GUI.skin.box)
				{
					fontSize = num6
				};
				_styleFontSize = num6;
			}
			Color color = GUI.color;
			GUI.color = _col;
			GUI.Box(new Rect(num4, num5, num2, num3), _msg, _style);
			GUI.color = color;
		}
	}
}
