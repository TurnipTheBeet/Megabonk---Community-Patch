using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(DataManager), "Load")]
internal static class Patch_DataManager_Load
{
	[HarmonyPostfix]
	private unsafe static void Postfix(DataManager __instance)
	{
		//IL_0862: Unknown result type (might be due to invalid IL or missing references)
		//IL_0869: Expected O, but got Unknown
		//IL_08de: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e5: Expected O, but got Unknown
		//IL_0a28: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a2f: Expected O, but got Unknown
		//IL_0ad2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad9: Expected O, but got Unknown
		//IL_0a44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c32: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_048d: Unknown result type (might be due to invalid IL or missing references)
		//IL_074e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0752: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b7: Invalid comparison between Unknown and I4
		var allTomes = __instance.GetAllTomes();
		if (allTomes != null)
		{
			for (int i = 0; i < allTomes.Count; i++)
			{
				((UnlockableBase)allTomes[i]).canAlwaysToggle = true;
			}
		}
		var allWeapons = __instance.GetAllWeapons();
		if (allWeapons != null)
		{
			for (int j = 0; j < allWeapons.Count; j++)
			{
				((UnlockableBase)allWeapons[j]).canAlwaysToggle = true;
				UpgradeData upgradeData = allWeapons[j].upgradeData;
				if (((upgradeData != null) ? upgradeData.upgradeModifiers : null) != null)
				{
					AddUpgradeStat(upgradeData.upgradeModifiers, (EStat)18, 0.1f);
					AddUpgradeStat(upgradeData.upgradeModifiers, (EStat)19, 0.2f);
					RemoveStat(upgradeData.upgradeModifiers, 24);
				}
			}
		}
		ItemData item = __instance.GetItem((EItem)58);
		if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null)
		{
			((UnlockableBase)item).canAlwaysToggle = true;
		}
		IconRecolor.Original.Clear();
		var unsortedItems = __instance.unsortedItems;
		if (unsortedItems != null)
		{
			for (int k = 0; k < unsortedItems.Count; k++)
			{
				ItemData val = unsortedItems[k];
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					IconRecolor.Original[val.eItem] = val.rarity;
				}
			}
		}
		ItemData item2 = __instance.GetItem((EItem)85);
		ItemData item3 = __instance.GetItem((EItem)44);
		if ((UnityEngine.Object)(object)item2 != (UnityEngine.Object)null)
		{
			item2.rarity = (EItemRarity)3;
		}
		if ((UnityEngine.Object)(object)item3 != (UnityEngine.Object)null)
		{
			item3.rarity = (EItemRarity)1;
		}
		ItemData item4 = __instance.GetItem((EItem)24);
		ItemData item5 = __instance.GetItem((EItem)74);
		ItemData item6 = __instance.GetItem((EItem)37);
		if ((UnityEngine.Object)(object)item4 != (UnityEngine.Object)null)
		{
			item4.rarity = (EItemRarity)2;
		}
		if ((UnityEngine.Object)(object)item5 != (UnityEngine.Object)null)
		{
			item5.rarity = (EItemRarity)2;
			((UnlockableBase)item5).canAlwaysToggle = true;
		}
		if ((UnityEngine.Object)(object)item6 != (UnityEngine.Object)null)
		{
			item6.rarity = (EItemRarity)1;
			((UnlockableBase)item6).canAlwaysToggle = false;
		}
		ItemData item7 = __instance.GetItem((EItem)45);
		ItemData item8 = __instance.GetItem((EItem)2);
		if ((UnityEngine.Object)(object)item7 != (UnityEngine.Object)null)
		{
			item7.rarity = (EItemRarity)2;
		}
		if ((UnityEngine.Object)(object)item8 != (UnityEngine.Object)null)
		{
			item8.rarity = (EItemRarity)3;
			((UnlockableBase)item8).canAlwaysToggle = true;
		}
		ItemData item9 = __instance.GetItem((EItem)33);
		ItemData item10 = __instance.GetItem((EItem)40);
		if ((UnityEngine.Object)(object)item9 != (UnityEngine.Object)null)
		{
			item9.rarity = (EItemRarity)2;
			((UnlockableBase)item9).canAlwaysToggle = false;
			((UnlockableBase)item9).sortingPriority = -1000;
		}
		if ((UnityEngine.Object)(object)item10 != (UnityEngine.Object)null)
		{
			item10.rarity = (EItemRarity)1;
			((UnlockableBase)item10).canAlwaysToggle = false;
		}
		ItemData item11 = __instance.GetItem((EItem)34);
		ItemData item12 = __instance.GetItem((EItem)43);
		if ((UnityEngine.Object)(object)item11 != (UnityEngine.Object)null)
		{
			item11.rarity = (EItemRarity)0;
			((UnlockableBase)item11).canAlwaysToggle = false;
		}
		if ((UnityEngine.Object)(object)item12 != (UnityEngine.Object)null)
		{
			item12.rarity = (EItemRarity)3;
			((UnlockableBase)item12).canAlwaysToggle = true;
		}
		ItemData item13 = __instance.GetItem((EItem)1);
		if ((UnityEngine.Object)(object)item13 != (UnityEngine.Object)null)
		{
			((UnlockableBase)item13).canAlwaysToggle = true;
		}
		ItemData item14 = __instance.GetItem((EItem)32);
		ItemData item15 = __instance.GetItem((EItem)55);
		ItemData item16 = __instance.GetItem((EItem)56);
		ItemData item17 = __instance.GetItem((EItem)19);
		if ((UnityEngine.Object)(object)item14 != (UnityEngine.Object)null)
		{
			((UnlockableBase)item14).canAlwaysToggle = false;
		}
		if ((UnityEngine.Object)(object)item15 != (UnityEngine.Object)null)
		{
			((UnlockableBase)item15).canAlwaysToggle = false;
		}
		if ((UnityEngine.Object)(object)item16 != (UnityEngine.Object)null)
		{
			((UnlockableBase)item16).canAlwaysToggle = false;
		}
		if ((UnityEngine.Object)(object)item17 != (UnityEngine.Object)null)
		{
			((UnlockableBase)item17).canAlwaysToggle = false;
		}
		ItemData item18 = __instance.GetItem((EItem)72);
		ItemData item19 = __instance.GetItem((EItem)8);
		if ((UnityEngine.Object)(object)item18 != (UnityEngine.Object)null)
		{
			((UnlockableBase)item18).canAlwaysToggle = true;
		}
		if ((UnityEngine.Object)(object)item19 != (UnityEngine.Object)null)
		{
			item19.rarity = (EItemRarity)1;
			((UnlockableBase)item19).canAlwaysToggle = true;
		}
		EItem[] array = Array.Empty<EItem>();
		foreach (EItem val2 in array)
		{
			ItemData item20 = __instance.GetItem(val2);
			if ((UnityEngine.Object)(object)item20 != (UnityEngine.Object)null)
			{
				((UnlockableBase)item20).canAlwaysToggle = true;
			}
		}
		EItem[] array3 = Array.Empty<EItem>();
		foreach (EItem val3 in array3)
		{
			ItemData item21 = __instance.GetItem(val3);
			if ((UnityEngine.Object)(object)item21 != (UnityEngine.Object)null)
			{
				((UnlockableBase)item21).canAlwaysToggle = false;
			}
		}
		if (unsortedItems != null)
		{
			for (int n = 0; n < unsortedItems.Count; n++)
			{
				IconRecolor.RecolorSource(unsortedItems[n]);
			}
		}
		var unsortedCharacterData = __instance.unsortedCharacterData;
		if (unsortedCharacterData != null)
		{
			for (int num = 0; num < unsortedCharacterData.Count; num++)
			{
				CharacterData val4 = unsortedCharacterData[num];
				if (((val4 != null) ? val4.statModifiers : null) == null)
				{
					continue;
				}
				Il2CppSystem.Collections.Generic.List<StatModifier> statModifiers = val4.statModifiers;
				float num2 = 0f;
				float num3 = 0f;
				float num4 = 0f;
				for (int num5 = 0; num5 < statModifiers.Count; num5++)
				{
					StatModifier val5 = statModifiers[num5];
					if (val5 != null)
					{
						int num6 = *(int*)(void*)(((Il2CppObjectBase)val5).Pointer + 16);
						float num7 = *(float*)(void*)(((Il2CppObjectBase)val5).Pointer + 24);
						switch (num6)
						{
						case 25:
							num2 += num7;
							break;
						case 26:
							num3 += num7;
							break;
						case 29:
							num4 += num7;
							break;
						}
					}
				}
				float num8 = 0.2f - num2;
				float value = 3f - num3;
				float value2 = 5f - num4;
				if (num8 > 0.001f)
				{
					AppendFlat(val4.statModifiers, (EStat)25, num8);
				}
				if (Math.Abs(value) > 0.001f)
				{
					AppendFlat(val4.statModifiers, (EStat)26, value);
				}
				if (Math.Abs(value2) > 0.001f)
				{
					AppendFlat(val4.statModifiers, (EStat)29, value2);
				}
				float num9 = 0f;
				for (int num10 = 0; num10 < statModifiers.Count; num10++)
				{
					StatModifier val6 = statModifiers[num10];
					if (val6 != null && *(int*)(void*)(((Il2CppObjectBase)val6).Pointer + 16) == 2)
					{
						num9 += *(float*)(void*)(((Il2CppObjectBase)val6).Pointer + 24);
					}
				}
				if (num9 < 1f)
				{
					AppendFlat(val4.statModifiers, (EStat)2, 10f);
				}
			}
		}
		EWeapon[] array5 = (EWeapon[])(object)new EWeapon[2]
		{
			(EWeapon)6,
			(EWeapon)3
		};
		foreach (EWeapon val7 in array5)
		{
			WeaponData weapon = __instance.GetWeapon(val7);
			object obj;
			if (weapon == null)
			{
				obj = null;
			}
			else
			{
				UpgradeData upgradeData2 = weapon.upgradeData;
				obj = ((upgradeData2 != null) ? upgradeData2.upgradeModifiers : null);
			}
			if (obj == null)
			{
				continue;
			}
			bool flag = false;
			for (int num12 = 0; num12 < weapon.upgradeData.upgradeModifiers.Count; num12++)
			{
				StatModifier val8 = weapon.upgradeData.upgradeModifiers[num12];
				if (val8 != null && (int)val8.stat == 16)
				{
					val8.modification = 2f;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				AddUpgradeStat(weapon.upgradeData.upgradeModifiers, (EStat)16, 2f);
			}
		}
		bool flag2 = default(bool);
		try
		{
			WeaponData weapon2 = __instance.GetWeapon((EWeapon)6);
			if ((UnityEngine.Object)(object)weapon2 != (UnityEngine.Object)null)
			{
				weapon2.endCooldown = 0.45f;
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val9 = new BepInExErrorLogInterpolatedStringHandler(10, 1, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val9).AppendLiteral("[BowBuff] ");
				((BepInExLogInterpolatedStringHandler)val9).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val9);
		}
		try
		{
			WeaponData weapon3 = __instance.GetWeapon((EWeapon)15);
			if ((UnityEngine.Object)(object)weapon3 != (UnityEngine.Object)null)
			{
				weapon3.minBurstInterval = 0.1f;
				weapon3.endCooldown = 0.5f;
			}
		}
		catch (Exception ex2)
		{
			ManualLogSource log2 = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val9 = new BepInExErrorLogInterpolatedStringHandler(13, 1, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val9).AppendLiteral("[SniperBuff] ");
				((BepInExLogInterpolatedStringHandler)val9).AppendFormatted<string>(ex2.Message);
			}
			log2.LogError(val9);
		}
		(EWeapon, float)[] array6 = new(EWeapon, float)[6]
		{
			((EWeapon)22, 3f),
			((EWeapon)20, 2f),
			((EWeapon)27, 2f),
			((EWeapon)21, 2f),
			((EWeapon)2, 2f),
			((EWeapon)28, 2f)
		};
		for (int num13 = 0; num13 < array6.Length; num13++)
		{
			(EWeapon, float) tuple = array6[num13];
			try
			{
				WeaponData weapon4 = __instance.GetWeapon(tuple.Item1);
				if ((UnityEngine.Object)(object)weapon4 != (UnityEngine.Object)null)
				{
					weapon4.endCooldown /= tuple.Item2;
					if (weapon4.burstTime > 0f)
					{
						weapon4.burstTime /= tuple.Item2;
					}
					weapon4.minBurstInterval /= tuple.Item2;
				}
			}
			catch (Exception ex3)
			{
				ManualLogSource log3 = Plugin.Log;
				BepInExErrorLogInterpolatedStringHandler val9 = new BepInExErrorLogInterpolatedStringHandler(16, 2, out flag2);
				if (flag2)
				{
					((BepInExLogInterpolatedStringHandler)val9).AppendLiteral("[FireRateBuff:");
					((BepInExLogInterpolatedStringHandler)val9).AppendFormatted<EWeapon>(tuple.Item1);
					((BepInExLogInterpolatedStringHandler)val9).AppendLiteral("] ");
					((BepInExLogInterpolatedStringHandler)val9).AppendFormatted<string>(ex3.Message);
				}
				log3.LogError(val9);
			}
		}
		try
		{
			WeaponData weapon5 = __instance.GetWeapon((EWeapon)30);
			if ((UnityEngine.Object)(object)weapon5 != (UnityEngine.Object)null)
			{
				weapon5.endCooldown = 0.425f;
				weapon5.burstTime = 0.75f;
			}
		}
		catch (Exception ex4)
		{
			ManualLogSource log4 = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val9 = new BepInExErrorLogInterpolatedStringHandler(17, 1, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val9).AppendLiteral("[ScytheCooldown] ");
				((BepInExLogInterpolatedStringHandler)val9).AppendFormatted<string>(ex4.Message);
			}
			log4.LogError(val9);
		}
		try
		{
			CharacterData characterData = __instance.GetCharacterData((ECharacter)12);
			PassiveData val10 = ((characterData != null) ? characterData.passive : null);
			if ((UnityEngine.Object)(object)val10 != (UnityEngine.Object)null)
			{
				val10.Init();
				IntPtr intPtr = *(IntPtr*)(void*)(((Il2CppObjectBase)val10).Pointer + 56);
				if (intPtr != IntPtr.Zero)
				{
					Patch_Hoarder_EliteScaling.NoelleSizePerLevel = *(float*)(void*)(intPtr + 24);
					Patch_Hoarder_EliteScaling.NoelleMaxSize = *(float*)(void*)(intPtr + 28);
				}
			}
		}
		catch
		{
		}
		try
		{
			IntPtr intPtr2 = IL2CPP.il2cpp_object_get_class(((Il2CppObjectBase)__instance).Pointer);
			IntPtr intPtr3 = IL2CPP.il2cpp_class_get_image(intPtr2);
			IntPtr intPtr4 = IL2CPP.il2cpp_class_from_name(intPtr3, "Assets.Scripts.Game.Combat", "CombatScaling");
			if (intPtr4 != IntPtr.Zero)
			{
				IntPtr intPtr5 = *(IntPtr*)(void*)(intPtr4 + 184);
				if (intPtr5 != IntPtr.Zero)
				{
					*(float*)(void*)(intPtr5 + 12) = 0f;
				}
			}
		}
		catch
		{
		}
		foreach (EItem forcedPoolItem in Plugin.ForcedPoolItems)
		{
			ItemData item22 = __instance.GetItem(forcedPoolItem);
			if ((UnityEngine.Object)(object)item22 != (UnityEngine.Object)null)
			{
				item22.inItemPool = true;
				((UnlockableBase)item22).isEnabled = true;
			}
		}
	}

	private unsafe static void RemoveStat(Il2CppSystem.Collections.Generic.List<StatModifier> mods, int statInt)
	{
		for (int num = mods.Count - 1; num >= 0; num--)
		{
			StatModifier val = mods[num];
			if (val != null && *(int*)(void*)(((Il2CppObjectBase)val).Pointer + 16) == statInt)
			{
				mods.RemoveAt(num);
			}
		}
	}

	private static void AppendFlat(Il2CppSystem.Collections.Generic.List<StatModifier> mods, EStat stat, float value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		StatModifier val = new StatModifier();
		val.stat = stat;
		val.modifyType = (EStatModifyType)2;
		val.modification = value;
		mods.Add(val);
	}

	private static void AddUpgradeStat(Il2CppSystem.Collections.Generic.List<StatModifier> mods, EStat stat, float modification)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < mods.Count; i++)
		{
			if (mods[i].stat == stat)
			{
				return;
			}
		}
		StatModifier val = new StatModifier();
		val.stat = stat;
		val.modifyType = (EStatModifyType)2;
		val.modification = modification;
		mods.Add(val);
	}
}
