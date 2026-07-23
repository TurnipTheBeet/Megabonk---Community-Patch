using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using HarmonyLib;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(PassiveAbilityHoarder), "Init")]
internal static class Patch_Hoarder_EliteScaling
{
	private static readonly MethodInfo _setStat = AccessTools.Method(typeof(PassiveAbility), "SetStat", (Type[])null, (Type[])null);

	public static float NoelleSizePerLevel = 0f;

	public static float NoelleMaxSize = 0f;

	private static Il2CppSystem.Action<int> _handler;

	[HarmonyPostfix]
	private static void Postfix(PassiveAbilityHoarder __instance)
	{
		if (_handler != null)
		{
			PlayerXp.A_LevelUp -= (Il2CppSystem.Action<int>)(_handler);
		}
		PassiveAbilityHoarder inst = __instance;
		System.Action<int> h = null;
		h = delegate(int level)
		{
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Expected O, but got Unknown
			try
			{
				if (inst == null)
				{
					PlayerXp.A_LevelUp -= (Il2CppSystem.Action<int>)(h);
				}
				else
				{
					float modification = 0.005f * (float)level;
					StatModifier val = new StatModifier();
					val.stat = (EStat)23;
					val.modifyType = 0;
					val.modification = modification;
					_setStat.Invoke(inst, new object[1] { val });
					if (NoelleSizePerLevel > 0f)
					{
						float modification2 = Math.Min((float)level * (NoelleSizePerLevel / 30f), NoelleMaxSize / 30f);
						StatModifier val2 = new StatModifier();
						val2.stat = (EStat)9;
						val2.modifyType = (EStatModifyType)2;
						val2.modification = modification2;
						_setStat.Invoke(inst, new object[1] { val2 });
					}
				}
			}
			catch
			{
			}
		};
		_handler = h;
		PlayerXp.A_LevelUp += (Il2CppSystem.Action<int>)(h);
	}
}
