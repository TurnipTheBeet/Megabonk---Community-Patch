using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts._Data;
using Assets.Scripts._Data.Tomes;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class AutoLevelPick
{
	private struct Choice
	{
		public UpgradeButton Btn;

		public bool IsTome;

		public bool IsWeapon;

		public ETome Tome;

		public ERarity Rarity;

		public bool IsNew;

		public bool IsMaxed;

		public float WeaponRoleValue;

		public bool EarlyProj;

		public string Name;
	}

	private static ConfigEntry<bool> _enabled;

	private static bool _selecting;

	private static readonly Dictionary<IntPtr, int> _projLevel = new Dictionary<IntPtr, int>();

	private static UpgradePicker _picker;

	internal const int LogMax = 10;

	internal static readonly List<string> Log = new List<string>();

	internal static bool Visible;

	private const float WinW = 460f;

	private const float PadX = 12f;

	private const float LineH = 22f;

	private static readonly GuiWindowFrame _frame = new GuiWindowFrame(new Vector2(60f, 200f)).Persist("AutoUpgradeLog");

	private static float _lastWinH;

	private const float RoleXp = 35f;

	private const float RoleDiff = 32f;

	private const float RoleLuck = 36f;

	private const float RoleChaos = 40f;

	private const float RoleOtherTome = 12f;

	private const float RoleDump = 10f;

	private const float RoleJunkWpn = 8f;

	private const float EarlyProjHeadroom = 0.5f;

	private const float DifficultyCap = 600f;

	private static ServerFeed _feed;

	internal static bool Enabled
	{
		get
		{
			return _enabled != null && _enabled.Value;
		}
		set
		{
			if (_enabled != null)
			{
				_enabled.Value = value;
			}
		}
	}

	private static UpgradePicker Picker
	{
		get
		{
			if ((UnityEngine.Object)(object)_picker == (UnityEngine.Object)null)
			{
				_picker = Object.FindObjectOfType<UpgradePicker>();
			}
			return _picker;
		}
	}

	private static ServerFeed Feed
	{
		get
		{
			if ((UnityEngine.Object)(object)_feed == (UnityEngine.Object)null)
			{
				_feed = Object.FindObjectOfType<ServerFeed>();
			}
			return _feed;
		}
	}

	internal static void Init(ConfigFile cfg)
	{
		_enabled = cfg.Bind<bool>("AutoUpgrade", "Enabled", false, "Scaling-aware auto-upgrade on level up. Prefers snowball picks (XP / difficulty / luck / new weapons) early and premium Legendary scaling rolls, instead of the game's plain highest-rarity pick. Toggle in-game with the Auto Upgrade hotkey.");
	}

	internal static bool TryAutoPick()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		if (!Enabled || _selecting)
		{
			return false;
		}
		UpgradePicker picker = Picker;
		MyPlayer instance = MyPlayer.Instance;
		PlayerInventory val = ((instance != null) ? instance.inventory : null);
		if ((UnityEngine.Object)(object)picker == (UnityEngine.Object)null || val == null)
		{
			return false;
		}
		_selecting = true;
		try
		{
			picker.ShuffleUpgrades((EEncounter)0);
			return Decide(picker, val);
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(16, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[AutoLevelPick] ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val2);
			return false;
		}
		finally
		{
			_selecting = false;
		}
	}

	private static IntPtr KeyOf(IUpgradable up)
	{
		try
		{
			Object val = ((Il2CppObjectBase)up).TryCast<Object>();
			return (val != null) ? ((Il2CppObjectBase)val).Pointer : IntPtr.Zero;
		}
		catch
		{
			return IntPtr.Zero;
		}
	}

	private static int ProjectedLevel(IUpgradable up)
	{
		IntPtr intPtr = KeyOf(up);
		if (intPtr != IntPtr.Zero && _projLevel.TryGetValue(intPtr, out var value))
		{
			int realLevel = 0;
			try
			{
				realLevel = up.GetLevel();
			}
			catch
			{
			}
			if (realLevel > value)
			{
				_projLevel[intPtr] = realLevel;
				return realLevel;
			}
			return value;
		}
		int num = 0;
		try
		{
			num = up.GetLevel();
		}
		catch
		{
		}
		if (intPtr != IntPtr.Zero)
		{
			_projLevel[intPtr] = num;
		}
		return num;
	}

	private static void BumpProjected(IUpgradable up)
	{
		IntPtr intPtr = KeyOf(up);
		if (!(intPtr == IntPtr.Zero))
		{
			_projLevel.TryGetValue(intPtr, out var value);
			_projLevel[intPtr] = value + 1;
		}
	}

	internal static void Toggle()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		Enabled = !Enabled;
		string text = (Enabled ? "ON" : "OFF");
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(16, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[AutoLevelPick] ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
		}
		log.LogInfo(val);
		Toast.Show("Scaling Auto-Upgrade: " + text, Enabled ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.6f, 0.45f, 1f));
	}

	private static void Record(string line)
	{
		Log.Add(line);
		if (Log.Count > 10)
		{
			Log.RemoveRange(0, Log.Count - 10);
		}
	}

	internal static void ToggleWindow()
	{
		Visible = !Visible;
	}

	private static float WinHeight()
	{
		return 74f + (float)Mathf.Max(1, Log.Count) * 23f + 10f;
	}

	internal static void HandleInput()
	{
		_frame.HandleInput(460f, (_lastWinH > 0f) ? _lastWinH : WinHeight(), 26f);
	}

	internal static void Draw()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		float num = (_lastWinH = WinHeight());
		Matrix4x4 old = _frame.Begin();
		float x = _frame.Pivot.x;
		float y = _frame.Pivot.y;
		UiTheme.Backdrop(new Rect(x, y, 460f, num), "AutoUpgrade");
		GUI.Box(new Rect(x, y, 460f, num), "Scaling Auto-Upgrade");
		float num2 = 436f;
		float num3 = x + 12f;
		float num4 = y + 22f + 2f;
		GUI.Label(new Rect(num3, num4, num2, 22f), Enabled ? "Status: ON (auto-picking level-ups)" : "Status: OFF");
		num4 += 22f;
		GUI.Label(new Rect(num3, num4, num2, 22f), "Recent picks (newest first):");
		num4 += 22f;
		if (Log.Count == 0)
		{
			GUI.Label(new Rect(num3, num4, num2, 22f), "  (none yet)");
		}
		else
		{
			for (int num5 = Log.Count - 1; num5 >= 0; num5--)
			{
				GUI.Label(new Rect(num3, num4, num2, 22f), Log[num5]);
				num4 += 23f;
			}
		}
		_frame.End(old);
		_frame.DrawGrip(460f, num);
	}

	private static int RarityWeight(ERarity r)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected I4, but got Unknown
		if (1 == 0)
		{
		}
		int result = (((int)r > 5) ? 7 : ((int)r switch
		{
			5 => 6, 
			4 => 5, 
			0 => 4, 
			3 => 3, 
			2 => 2, 
			1 => 1, 
			_ => 0, 
		}));
		if (1 == 0)
		{
		}
		return result;
	}

	private static float WeaponStatValue(EStat s)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected I4, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		if (1 == 0)
		{
		}
		float result;
		if ((int)s <= 12)
		{
			if ((int)s != 9)
			{
				if ((int)s != 12)
				{
					goto IL_0069;
				}
				result = 30f;
			}
			else
			{
				result = 24f;
			}
		}
		else
		{
		switch ((int)s - 16)
		{
		case 3:
			goto IL_0039;
		case 2:
			goto IL_0049;
		case 0:
			goto IL_0059;
		case 1:
			goto IL_0069;
		}
			if ((int)s != 45)
			{
				goto IL_0069;
			}
			result = 18f;
		}
		goto IL_0071;
		IL_0069:
		result = 0f;
		goto IL_0071;
		IL_0071:
		if (1 == 0)
		{
		}
		return result;
		IL_0059:
		result = 21f;
		goto IL_0071;
		IL_0049:
		result = 27f;
		goto IL_0071;
		IL_0039:
		result = 33f;
		goto IL_0071;
	}

	private static float ProjHeadroom(WeaponData w)
	{
		try
		{
			float minBurstInterval = w.minBurstInterval;
			float burstTime = w.burstTime;
			if (minBurstInterval <= 0f || burstTime <= 0f)
			{
				return 1f;
			}
			float num = burstTime / minBurstInterval;
			if (num <= 0f)
			{
				return 1f;
			}
			float num2 = w.projectiles;
			try
			{
				num2 += PlayerStats.GetStat((EStat)16);
			}
			catch
			{
			}
			return Mathf.Clamp01(1f - num2 / num);
		}
		catch
		{
			return 1f;
		}
	}

	private static float WeaponRole(UpgradeButton b, WeaponData w, out bool earlyProj)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Invalid comparison between Unknown and I4
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Invalid comparison between Unknown and I4
		float num = 8f;
		earlyProj = false;
		try
		{
			var upgradeOffer = b.upgradeOffer;
			if (upgradeOffer == null)
			{
				return num;
			}
			float num2 = -1f;
			int count = upgradeOffer.Count;
			for (int i = 0; i < count; i++)
			{
				StatModifier val = upgradeOffer[i];
				if (val == null)
				{
					continue;
				}
				float num3 = WeaponStatValue(val.stat);
				if (num3 <= 0f)
				{
					continue;
				}
				if ((int)val.stat == 16 || (int)val.stat == 45)
				{
					if (num2 < 0f)
					{
						num2 = ProjHeadroom(w);
					}
					if (num2 >= 0.5f)
					{
						earlyProj = true;
					}
					num3 = 8f + (num3 - 8f) * num2;
				}
				if (num3 > num)
				{
					num = num3;
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private static float Role(Choice c)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		float result;
		if (c.IsTome)
		{
			ETome tome = c.Tome;
			if (1 == 0)
			{
			}
			if ((int)tome <= 14)
			{
				if ((int)tome != 13)
				{
					if ((int)tome != 14)
					{
						goto IL_0088;
					}
					result = (IsCappedXp(c) ? 10f : 35f);
				}
				else
				{
					result = 36f;
				}
			}
			else if ((int)tome != 21)
			{
				if ((int)tome != 24)
				{
					goto IL_0088;
				}
				result = (((int)c.Rarity >= 5) ? 40f : 12f);
			}
			else
			{
				result = (IsCappedDiff(c) ? 10f : 32f);
			}
			goto IL_0090;
		}
		if (c.IsWeapon)
		{
			return c.WeaponRoleValue;
		}
		return 0f;
		IL_0090:
		if (1 == 0)
		{
		}
		return result;
		IL_0088:
		result = 12f;
		goto IL_0090;
	}

	private static float Score(Choice c)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Invalid comparison between Unknown and I4
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		float num = Role(c);
		float num2 = (c.IsNew ? 5000f : (((int)c.Rarity >= 5) ? 4000f : ((num == 35f || num == 32f || num == 36f || c.EarlyProj) ? 3000f : ((num != 10f) ? 2000f : 1000f))));
		return num2 + (float)RarityWeight(c.Rarity) * 100f + num;
	}

	private static float XpHeadroom()
	{
		try
		{
			float maxXpMultiplier = PlayerXp.maxXpMultiplier;
			if (maxXpMultiplier <= 0f)
			{
				return 1f;
			}
			float stat = PlayerStats.GetStat((EStat)32);
			return Mathf.Clamp01(1f - stat / maxXpMultiplier);
		}
		catch
		{
			return 1f;
		}
	}

	private static List<Choice> Gather(UpgradePicker picker)
	{
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		List<Choice> list = new List<Choice>();
		Il2CppReferenceArray<UpgradeButton> buttons = picker.buttons;
		if (buttons == null)
		{
			return list;
		}
		foreach (UpgradeButton item2 in (Il2CppArrayBase<UpgradeButton>)(object)buttons)
		{
			if ((UnityEngine.Object)(object)item2 == (UnityEngine.Object)null || item2.isItem)
			{
				continue;
			}
			IUpgradable upgradable = item2.upgradable;
			if (upgradable == null)
			{
				continue;
			}
			Choice choice = default(Choice);
			choice.Btn = item2;
			choice.Rarity = item2.rarity;
			Choice item = choice;
			try
			{
				item.IsNew = (int)item2.rarity == 0 || upgradable.GetLevel() <= 0;
			}
			catch
			{
			}
			try
			{
				int maxLevel = upgradable.GetMaxLevel();
				if (maxLevel <= 0)
				{
					if (item.IsTome)
						maxLevel = InventoryUtility.GetTomeMaxLevel();
					else if (item.IsWeapon)
						maxLevel = InventoryUtility.GetWeaponMaxLevel();
				}
				item.IsMaxed = maxLevel > 0 && ProjectedLevel(upgradable) >= maxLevel;
			}
			catch
			{
			}
			try
			{
				item.Name = upgradable.GetName() ?? "?";
			}
			catch
			{
				item.Name = "?";
			}
			TomeData val = ((Il2CppObjectBase)upgradable).TryCast<TomeData>();
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
			{
				item.IsTome = true;
				item.Tome = val.eTome;
			}
			else
			{
				WeaponData val2 = ((Il2CppObjectBase)upgradable).TryCast<WeaponData>();
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					item.IsWeapon = true;
					item.WeaponRoleValue = WeaponRole(item2, val2, out var earlyProj);
					item.EarlyProj = earlyProj;
				}
			}
			list.Add(item);
		}
		return list;
	}

	private static bool IsCappedXp(Choice c)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		return c.IsTome && (int)c.Tome == 14 && XpHeadroom() <= 0.15f;
	}

	private static bool IsCappedDiff(Choice c)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		if (!c.IsTome || (int)c.Tome != 21)
		{
			return false;
		}
		try
		{
			return PlayerStats.GetStat((EStat)38) >= 600f;
		}
		catch
		{
			return false;
		}
	}

	private static bool Decide(UpgradePicker picker, PlayerInventory inv)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Invalid comparison between Unknown and I4
		List<Choice> list = Gather(picker);
		if (list.Count == 0)
		{
			return false;
		}
		bool flag = false;
		Choice c = default(Choice);
		int num = -1;
		int num2 = -1;
		foreach (Choice item in list)
		{
			if (item.IsMaxed)
			{
				continue;
			}
			if (item.IsTome && (int)item.Tome == 13)
			{
				flag = true;
				c = item;
				continue;
			}
			int num3 = RarityWeight(item.Rarity);
			if (num3 > num)
			{
				num = num3;
			}
			if (item.IsTome && (((int)item.Tome == 14 && !IsCappedXp(item)) || ((int)item.Tome == 21 && !IsCappedDiff(item))) && num3 > num2)
			{
				num2 = num3;
			}
		}
		int num4 = ((num2 > RarityWeight((ERarity)1)) ? num2 : (-1));
		int num5 = (flag ? RarityWeight(c.Rarity) : (-1));
		if (flag && num5 >= num && num5 > num4)
		{
			return Commit(inv, list, c, "luck wildcard");
		}
		bool flag2 = false;
		Choice c2 = default(Choice);
		float num6 = float.MinValue;
		foreach (Choice item2 in list)
		{
			if (!item2.IsMaxed)
			{
				float num7 = Score(item2);
				if (num7 > num6)
				{
					num6 = num7;
					c2 = item2;
					flag2 = true;
				}
			}
		}
		if (!flag2)
		{
			return false;
		}
		return Commit(inv, list, c2, $"score {num6:0}");
	}

	private static bool Commit(PlayerInventory inv, List<Choice> choices, Choice c, string reason)
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected O, but got Unknown
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		UpgradeButton btn = c.Btn;
		if ((UnityEngine.Object)(object)btn == (UnityEngine.Object)null || btn.upgradable == null)
		{
			return false;
		}
		if (c.IsMaxed)
		{
			return false;
		}
		List<string> list = new List<string>();
		foreach (Choice choice in choices)
		{
			if ((UnityEngine.Object)(object)choice.Btn != (UnityEngine.Object)(object)btn)
			{
				list.Add($"{choice.Name} ({choice.Rarity})");
			}
		}
		string text = $"{c.Name} ({c.Rarity}) — {reason}";
		if (list.Count > 0)
		{
			text = text + "  | over: " + string.Join(", ", list);
		}
		Record(text);
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(23, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[AutoLevelPick] picked ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
		}
		log.LogInfo(val);
		inv.AddUpgrade(btn.upgradable, btn.upgradeOffer, btn.rarity);
		BumpProjected(btn.upgradable);
		ShowFeed(btn.upgradable, btn.rarity);
		return true;
	}

	private static void ShowFeed(IUpgradable up, ERarity rarity)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ServerFeed feed = Feed;
			if (!((UnityEngine.Object)(object)feed == (UnityEngine.Object)null) && up != null)
			{
				string value = up.GetName() ?? "?";
				string text = $"{value} ({rarity})";
				feed.SetFeed(text, 4f, up.GetIcon());
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(21, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[AutoLevelPick] feed ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val);
		}
	}
}
