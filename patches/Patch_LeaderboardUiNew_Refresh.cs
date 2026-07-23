using System;
using System.Collections.Generic;
using Assets.Scripts.Menu.Shop.Leaderboards;
using Assets.Scripts.Steam;
using Assets.Scripts.Steam.LeaderboardsNew;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Steamworks;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(LeaderboardUiNew), "Refresh")]
internal static class Patch_LeaderboardUiNew_Refresh
{
	[HarmonyPrefix]
	private unsafe static bool Prefix(LeaderboardUiNew __instance)
	{
		try
		{
			IntPtr intPtr = *(IntPtr*)(void*)(((Il2CppObjectBase)__instance).Pointer + 72);
			if (intPtr == IntPtr.Zero)
			{
				return true;
			}
			bool flag = *(byte*)(void*)(((Il2CppObjectBase)__instance).Pointer + 85) != 0;
			bool flag2 = *(byte*)(void*)(((Il2CppObjectBase)__instance).Pointer + 84) != 0;
			int num = *(int*)(void*)(((Il2CppObjectBase)__instance).Pointer + 80);
			var entriesKills = LeaderboardUtility.GetEntriesKills(flag, flag2, num);
			if (entriesKills == null || entriesKills.Count == 0)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	[HarmonyPostfix]
	private unsafe static void Postfix(LeaderboardUiNew __instance)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Invalid comparison between Unknown and I4
		LeaderboardInjector.LbUi = __instance;
		try
		{
			if ((UnityEngine.Object)(object)((Component)__instance).GetComponent<ScrollWheelDetector>() == (UnityEngine.Object)null)
			{
				((Component)__instance).gameObject.AddComponent<ScrollWheelDetector>();
			}
		}
		catch
		{
		}
		try
		{
			IntPtr intPtr = *(IntPtr*)(void*)(((Il2CppObjectBase)__instance).Pointer + 72);
			if (intPtr != IntPtr.Zero)
			{
				LeaderboardInjector.ActiveLb = new SteamLeaderboardNew(intPtr);
			}
		}
		catch
		{
		}
		try
		{
			PersonalTab.Init(__instance);
		}
		catch
		{
		}
		try
		{
			PersonalTab.PrefetchIfNeeded();
		}
		catch
		{
		}
		try
		{
			AllTimeTab.Init(__instance);
		}
		catch
		{
		}
		try
		{
			AllTimeTab.BeginFetch();
		}
		catch
		{
		}
		try
		{
			if (AllTimeTab.IsActive)
			{
				AllTimeTab.Redisplay();
			}
			else if (PersonalTab.IsActive)
			{
				PersonalTab.Redisplay();
			}
			else if (Patch_LBTypeSelected.CurrentTab == 0)
			{
				var leaderboardEntries = __instance.leaderboardEntries;
				LeaderboardInjector.ServerEntry[] cachedEntries = LeaderboardInjector.WeeklyCachedEntries ?? LeaderboardInjector.CachedEntries;
				if (leaderboardEntries != null && leaderboardEntries.Count >= 10 && cachedEntries != null)
				{
					int currentOffset = LeaderboardInjector.CurrentOffset;
					LeaderboardInjector.SlotRunData = new string[leaderboardEntries.Count];
					DataManager instance = DataManager.Instance;
					string text = SteamUser.GetSteamID().m_SteamID.ToString();
					for (int i = 0; i < 9 && i < leaderboardEntries.Count; i++)
					{
						LeaderboardEntryUi val = leaderboardEntries[i];
						if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
						{
							continue;
						}
						int num = currentOffset + i;
						if (num >= cachedEntries.Length)
						{
							try
							{
								val.Clear();
							}
							catch
							{
							}
							continue;
						}
						LeaderboardInjector.ServerEntry serverEntry = cachedEntries[num];
						LeaderboardInjector.SlotRunData[i] = serverEntry.RunData;
						try
						{
							((Component)val).gameObject.SetActive(true);
							((TMP_Text)val.rank).SetText($"#{num + 1}");
							((TMP_Text)val.playerName).SetText(serverEntry.Name ?? "?");
							((TMP_Text)val.score).SetText(serverEntry.Score.ToString("N0"));
							if (((instance != null) ? instance.unsortedCharacterData : null) != null)
							{
								for (int j = 0; j < instance.unsortedCharacterData.Count; j++)
								{
									CharacterData val2 = instance.unsortedCharacterData[j];
									if (!((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null) && (int)val2.eCharacter == serverEntry.CharacterIndex)
									{
										if ((UnityEngine.Object)(object)val2.icon != (UnityEngine.Object)null)
										{
											val.characterIcon.texture = val2.icon;
										}
										break;
									}
								}
							}
							bool flag = serverEntry.SteamId == text;
							if ((UnityEngine.Object)(object)val.localHighlight != (UnityEngine.Object)null)
							{
								((Component)val.localHighlight).gameObject.SetActive(flag);
							}
							if ((UnityEngine.Object)(object)val.playerIcon != (UnityEngine.Object)null && ulong.TryParse(serverEntry.SteamId, out var result))
							{
								Texture avatar = LeaderboardInjector.GetAvatar(result);
								if ((UnityEngine.Object)(object)avatar != (UnityEngine.Object)null)
								{
									val.playerIcon.texture = avatar;
								}
								else
								{
									val.playerIcon.texture = null;
									LeaderboardInjector.NoteAvatarMiss(result);
								}
							}
						}
						catch
						{
						}
					}
					try
					{
						LeaderboardEntryUi obj9 = leaderboardEntries[9];
						if (obj9 != null)
						{
							obj9.Clear();
						}
					}
					catch
					{
					}
				}
			}
			try
			{
				if ((PersonalTab.IsActive ? PersonalTab.HasData : (AllTimeTab.IsActive ? AllTimeTab.HasData : (LeaderboardInjector.CachedEntries != null))) && (UnityEngine.Object)(object)__instance.buffering != (UnityEngine.Object)null && __instance.buffering.activeSelf)
				{
					__instance.buffering.SetActive(false);
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
	}
}
