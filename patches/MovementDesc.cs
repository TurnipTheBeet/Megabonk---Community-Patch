namespace MegabonkCommunityPatch;

internal static class MovementDesc
{
	public static string Fix(string s)
	{
		if (s == null)
		{
			return s;
		}
		return s.Replace("Standing still", "Moving").Replace("standing still", "moving").Replace("Not moving", "Moving")
			.Replace("not moving", "moving")
			.Replace("Stand still", "Keep moving")
			.Replace("stand still", "keep moving")
			.Replace("Stay still", "Keep moving")
			.Replace("stay still", "keep moving");
	}
}
