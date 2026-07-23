using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Steamworks;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MegabonkCommunityPatch;

internal static class PersonalTab
{
	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.PersonalTab");

	private static LeaderboardUiNew _ui;

	private static MyButtonTabs _tabButton;

	private static GameObject _tabGo = null;

	private static LeaderboardInjector.ServerEntry[] _lastEntries = null;

	private static bool _fetching = false;

	private static int _offset = 0;

	internal static readonly Dictionary<int, int> PersonalBests = new Dictionary<int, int>();

	internal static bool IsActive { get; set; } = false;


	internal static bool HasData => _lastEntries != null;

	internal static void Init(LeaderboardUiNew ui)
	{
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Expected O, but got Unknown
		_ui = ui;
		try
		{
			if ((UnityEngine.Object)(object)_tabGo == (UnityEngine.Object)null)
			{
				Il2CppArrayBase<GameObject> val = Object.FindObjectsOfType<GameObject>(true);
				foreach (GameObject item in val)
				{
					if (((Object)item).name != "B_Effects" || !item.transform.IsChildOf(((Component)ui.leaderboardTypeButtons).transform))
					{
						continue;
					}
					MyButtonTabs component = item.GetComponent<MyButtonTabs>();
					if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
					{
						continue;
					}
					Button component2 = item.GetComponent<Button>();
					if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
					{
						((UnityEventBase)component2.onClick).RemoveAllListeners();
						((UnityEvent)component2.onClick).AddListener((UnityAction)((Action)OnClick));
					}
					item.SetActive(true);
					_tabButton = component;
					_tabGo = item;
					Log.LogInfo((object)"Personal tab wired.");
					break;
				}
			}
			if ((UnityEngine.Object)(object)_tabGo != (UnityEngine.Object)null)
			{
				TextMeshProUGUI componentInChildren = _tabGo.GetComponentInChildren<TextMeshProUGUI>();
				if ((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null)
				{
					((TMP_Text)componentInChildren).text = "Personal";
					((TMP_Text)componentInChildren).m_text = "Personal";
				}
			}
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(6, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Init: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val2);
		}
	}

	internal static void PrefetchIfNeeded()
	{
		if (!_fetching && _lastEntries == null)
		{
			BeginFetch();
		}
	}

	internal static void BeginFetch()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (_fetching)
		{
			return;
		}
		_fetching = true;
		_lastEntries = null;
		_offset = 0;
		string steamId = SteamUser.GetSteamID().m_SteamID.ToString();
		Task.Run<Task>((Func<Task>)async delegate
		{
			LeaderboardInjector.ServerEntry[] entries = ParsePersonal(await LeaderboardRelay.FetchPersonal(steamId));
			ModGui.MainThread.Enqueue(delegate
			{
				//IL_0079: Unknown result type (might be due to invalid IL or missing references)
				//IL_0080: Expected O, but got Unknown
				_fetching = false;
				PersonalBests.Clear();
				
				// Deduplicate to show only the best entry per character
				var bestEntries = new System.Collections.Generic.Dictionary<int, LeaderboardInjector.ServerEntry>();
				foreach (LeaderboardInjector.ServerEntry serverEntry in entries)
				{
					if (!bestEntries.TryGetValue(serverEntry.CharacterIndex, out var existing) || serverEntry.Score > existing.Score)
					{
						bestEntries[serverEntry.CharacterIndex] = serverEntry;
					}
					
					if (!PersonalBests.TryGetValue(serverEntry.CharacterIndex, out var value) || serverEntry.Score > value)
					{
						PersonalBests[serverEntry.CharacterIndex] = serverEntry.Score;
					}
				}
				
				// Update entries to only contain the deduplicated ones, sorted by rank/score
				entries = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderByDescending(bestEntries.Values, e => e.Score));
				_lastEntries = entries;

				bool flag = default(bool);
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(48, 2, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Personal pre-fetch: ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(entries.Length);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" entries, ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(PersonalBests.Count);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" characters cached");
				}
				Log.LogInfo(val);
				if (IsActive)
				{
					Display(entries);
				}
			});
		});
	}

	private static void OnClick()
	{
		IsActive = true;
		AllTimeTab.IsActive = false;
		AllTimeTab.HideCycle();
		if ((UnityEngine.Object)(object)_ui == (UnityEngine.Object)null)
		{
			return;
		}
		// Clear all existing UI slots first to prevent stale data overlap
		try
		{
			foreach (LeaderboardEntryUi slot in _ui.leaderboardEntries)
			{
				if ((UnityEngine.Object)(object)slot != (UnityEngine.Object)null)
				{
					slot.Clear();
				}
			}
		}
		catch { }
		if (_lastEntries != null)
		{
			Display(_lastEntries);
		}
		else
		{
			BeginFetch();
			try
			{
				((TMP_Text)_ui.leaderboardEntries[0].playerName).SetText("Loading…");
			}
			catch
			{
			}
		}
		ActivateTabButton();
	}

	private static void ActivateTabButton()
	{
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		try
		{
			LeaderboardUiNew ui = _ui;
			if ((UnityEngine.Object)(object)((ui != null) ? ui.leaderboardTypeButtons : null) == (UnityEngine.Object)null || (UnityEngine.Object)(object)_tabButton == (UnityEngine.Object)null)
			{
				return;
			}
			ButtonNavigationSelectionOnly leaderboardTypeButtons = _ui.leaderboardTypeButtons;
			if (leaderboardTypeButtons.buttons == null || ((Il2CppArrayBase<MyButtonTabs>)(object)leaderboardTypeButtons.buttons).Length < 3)
			{
				Il2CppReferenceArray<MyButtonTabs> val = new Il2CppReferenceArray<MyButtonTabs>(3L);
				if (leaderboardTypeButtons.buttons != null)
				{
					for (int i = 0; i < Math.Min(((Il2CppArrayBase<MyButtonTabs>)(object)leaderboardTypeButtons.buttons).Length, 2); i++)
					{
						((Il2CppArrayBase<MyButtonTabs>)(object)val)[i] = ((Il2CppArrayBase<MyButtonTabs>)(object)leaderboardTypeButtons.buttons)[i];
					}
				}
				((Il2CppArrayBase<MyButtonTabs>)(object)val)[2] = _tabButton;
				leaderboardTypeButtons.buttons = val;
			}
			leaderboardTypeButtons.ButtonPressed(2, false);
		}
		catch (Exception ex)
		{
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val2 = new BepInExWarningLogInterpolatedStringHandler(19, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("ActivateTabButton: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			Log.LogWarning(val2);
		}
	}

	internal static void Redisplay()
	{
		if (_lastEntries != null)
		{
			Display(_lastEntries);
		}
	}

	internal static void ScrollBy(int delta)
	{
		if (_lastEntries != null && _lastEntries.Length != 0)
		{
			LeaderboardUiNew ui = _ui;
			if (((ui != null) ? ui.leaderboardEntries : null) != null)
			{
				int count = _ui.leaderboardEntries.Count;
				_offset = Math.Max(0, Math.Min(_offset + delta, Math.Max(0, _lastEntries.Length - count)));
				Display(_lastEntries);
			}
		}
	}

	private static void Display(LeaderboardInjector.ServerEntry[] entries)
	{
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Expected O, but got Unknown
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Expected O, but got Unknown
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Invalid comparison between Unknown and I4
		if (!IsActive)
		{
			return;
		}
		LeaderboardUiNew ui = _ui;
		if (((ui != null) ? ui.leaderboardEntries : null) == null)
		{
			return;
		}
		foreach (LeaderboardInjector.ServerEntry serverEntry in entries)
		{
			if (ulong.TryParse(serverEntry.SteamId, out var result) && result != 0L && !string.IsNullOrEmpty(serverEntry.Name))
			{
				LeaderboardInjector.NameCache[result] = serverEntry.Name;
			}
		}
		DataManager instance = DataManager.Instance;
		int count = _ui.leaderboardEntries.Count;
		_offset = Math.Max(0, Math.Min(_offset, Math.Max(0, entries.Length - count)));
		LeaderboardInjector.SlotRunData = new string[count];
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(43, 3, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Personal Display: ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(entries.Length);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" entries, ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(count);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" slots, offset=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_offset);
		}
		Log.LogInfo(val);
		for (int j = 0; j < count; j++)
		{
			LeaderboardEntryUi val2 = _ui.leaderboardEntries[j];
			if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
			{
				BepInExWarningLogInterpolatedStringHandler val3 = new BepInExWarningLogInterpolatedStringHandler(15, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Slot ");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(j);
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(": null row");
				}
				Log.LogWarning(val3);
				continue;
			}
			int num = j + _offset;
			if (num >= entries.Length)
			{
				try
				{
					val2.Clear();
				}
				catch
				{
				}
				continue;
			}
			try
			{
				LeaderboardInjector.ServerEntry serverEntry2 = entries[num];
				LeaderboardInjector.SlotRunData[j] = serverEntry2.RunData;
				if (!((Component)val2).gameObject.activeSelf)
				{
					((Component)val2).gameObject.SetActive(true);
				}
				((TMP_Text)val2.rank).SetText($"#{num + 1}");
				((TMP_Text)val2.playerName).SetText(serverEntry2.Name ?? "?");
				((TMP_Text)val2.score).SetText(serverEntry2.Score.ToString("N0"));
				if (((instance != null) ? instance.unsortedCharacterData : null) != null)
				{
					for (int k = 0; k < instance.unsortedCharacterData.Count; k++)
					{
						CharacterData val4 = instance.unsortedCharacterData[k];
						if (!((UnityEngine.Object)(object)val4 == (UnityEngine.Object)null) && (int)val4.eCharacter == serverEntry2.CharacterIndex)
						{
							if ((UnityEngine.Object)(object)val4.icon != (UnityEngine.Object)null)
							{
								val2.characterIcon.texture = val4.icon;
							}
							break;
						}
					}
				}
				if ((UnityEngine.Object)(object)val2.playerIcon != (UnityEngine.Object)null && ulong.TryParse(serverEntry2.SteamId, out var result2))
				{
					Texture avatar = LeaderboardInjector.GetAvatar(result2);
					if ((UnityEngine.Object)(object)avatar != (UnityEngine.Object)null)
					{
						val2.playerIcon.texture = avatar;
					}
					else
					{
						LeaderboardInjector.NoteAvatarMiss(result2);
					}
				}
			}
			catch (Exception ex)
			{
				BepInExWarningLogInterpolatedStringHandler val3 = new BepInExWarningLogInterpolatedStringHandler(17, 2, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Slot ");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(j);
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(": EXCEPTION ");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(ex.Message);
				}
				Log.LogWarning(val3);
			}
		}
		val = new BepInExInfoLogInterpolatedStringHandler(33, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Personal tab: ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(entries.Length);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" entries displayed.");
		}
		Log.LogInfo(val);
		try
		{
			if ((UnityEngine.Object)(object)_ui != (UnityEngine.Object)null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(((Component)_ui).GetComponent<RectTransform>());
			}
		}
		catch
		{
		}
	}

	private static LeaderboardInjector.ServerEntry[] ParsePersonal(string json)
	{
		if (string.IsNullOrEmpty(json) || json == "[]")
		{
			return Array.Empty<LeaderboardInjector.ServerEntry>();
		}
		try
		{
			return JsonSerializer.Deserialize<LeaderboardInjector.ServerEntry[]>(json) ?? Array.Empty<LeaderboardInjector.ServerEntry>();
		}
		catch
		{
			return Array.Empty<LeaderboardInjector.ServerEntry>();
		}
	}
}
