using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using HarmonyLib;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(PassiveAbilityZooma), "Init")]
internal static class Patch_Zooma_Init_Projectiles
{
	private static readonly MethodInfo _setStat = AccessTools.Method(typeof(PassiveAbility), "SetStat", (Type[])null, (Type[])null);

	private static Il2CppSystem.Action<int> _handler;

	[HarmonyPostfix]
	private static void Postfix(PassiveAbilityZooma __instance)
	{
		if (_handler != null)
		{
			PlayerXp.A_LevelUp -= (Il2CppSystem.Action<int>)(_handler);
		}
		PassiveAbilityZooma inst = __instance;
		System.Action<int> h = null;
		h = delegate(int level)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Expected O, but got Unknown
			try
			{
				if (inst == null)
				{
					PlayerXp.A_LevelUp -= (Il2CppSystem.Action<int>)(h);
				}
				else
				{
					StatModifier val = new StatModifier();
					val.stat = (EStat)16;
					val.modifyType = (EStatModifyType)2;
					val.modification = 0.25f * (float)level;
					_setStat.Invoke(inst, new object[1] { val });
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
