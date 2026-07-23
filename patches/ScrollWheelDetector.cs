using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal class ScrollWheelDetector : MonoBehaviour
{
	public ScrollWheelDetector(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		float y = Input.mouseScrollDelta.y;
		if (y != 0f)
		{
			int delta = ((!(y > 0f)) ? 1 : (-1));
			if (AllTimeTab.IsActive)
			{
				AllTimeTab.ScrollBy(delta);
			}
			else if (PersonalTab.IsActive)
			{
				PersonalTab.ScrollBy(delta);
			}
			else
			{
				LeaderboardInjector.ScrollBy(delta);
			}
		}
	}
}
