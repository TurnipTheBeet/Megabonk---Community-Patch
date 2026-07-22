using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class IconColorHelper
{
	internal static Color MicrowaveRarityColor(EItemRarity rarity)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected I4, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (1 == 0)
		{
		}
		Color result = (Color)(((int)(rarity - 1)) switch
		{
			0 => new Color(0.5f, 0.75f, 1f), 
			1 => new Color(0.8f, 0.6f, 1f), 
			2 => new Color(1f, 0.95f, 0.6f), 
			_ => Color.white, 
		});
		if (1 == 0)
		{
		}
		return result;
	}

	internal static Color ShadyGuyRarityColor(EItemRarity rarity)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected I4, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (1 == 0)
		{
		}
		Color result = (Color)(((int)(rarity - 1)) switch
		{
			0 => new Color(0.1f, 0.25f, 0.7f), 
			1 => new Color(0.35f, 0.05f, 0.6f), 
			2 => new Color(0.7f, 0.5f, 0.05f), 
			_ => new Color(0.6f, 0.6f, 0.6f), 
		});
		if (1 == 0)
		{
		}
		return result;
	}

	internal static void ApplyColor(GameObject go, Color color)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		Renderer val = go.GetComponent<Renderer>();
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			val = go.GetComponentInChildren<Renderer>();
		}
		if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
		{
			Material material = val.material;
			material.mainTexture = (Texture)(object)Texture2D.whiteTexture;
			material.color = color;
		}
	}
}
