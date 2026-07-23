using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(MicrowaveItemButton), "SelectUpgrade")]
internal static class Patch_MicrowaveItemButton_SelectUpgrade
{
	[HarmonyPrefix]
	private unsafe static void Prefix(MicrowaveItemButton __instance)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		long num = *(long*)(void*)(((Il2CppObjectBase)__instance).Pointer + 104);
		if (num == 0)
		{
			return;
		}
		EItem item = (EItem)(*(int*)(num + 84));
		if (Plugin.ActiveUncappedItems.Contains(item))
		{
			int num2 = *(int*)(num + 112);
			if (num2 > 0)
			{
				*(int*)(num + 112) = num2 + 1;
			}
		}
	}
}
