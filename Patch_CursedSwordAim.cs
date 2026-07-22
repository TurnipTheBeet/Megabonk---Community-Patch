using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

[HarmonyPatch(typeof(ProjectileCringeSword), "TryInit")]
internal static class Patch_CursedSwordAim
{
	internal struct AimState
	{
		public bool Changed;

		public Transform Renderer;

		public Quaternion Saved;
	}

	[HarmonyPrefix]
	private static void Prefix(ProjectileCringeSword __instance, out AimState __state)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		__state = default(AimState);
		try
		{
			if (!CursedSwordAim.Enabled)
			{
				return;
			}
			WeaponBase weaponBase = ((ProjectileBase)__instance).weaponBase;
			if (weaponBase == null)
			{
				return;
			}
			WeaponData weaponData = weaponBase.weaponData;
			if ((UnityEngine.Object)(object)weaponData == (UnityEngine.Object)null || (int)weaponData.eWeapon != 28)
			{
				return;
			}
			MyPlayer instance = MyPlayer.Instance;
			if ((UnityEngine.Object)(object)instance == (UnityEngine.Object)null)
			{
				return;
			}
			PlayerRenderer playerRenderer = instance.playerRenderer;
			if ((UnityEngine.Object)(object)playerRenderer == (UnityEngine.Object)null)
			{
				return;
			}
			Transform transform = ((Component)playerRenderer).transform;
			Vector3 position = transform.position;
			Enemy enemy = EnemyTargeting.GetEnemy(position, 30f, 0, false, (GameObject)null);
			if (!((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null))
			{
				Vector3 val = enemy.GetCenterPosition() - position;
				val.y = 0f;
				if (!(val.sqrMagnitude < 0.0001f))
				{
					val.Normalize();
				__state.Renderer = transform;
				__state.Saved = transform.rotation;
				__state.Changed = true;
				transform.rotation = Quaternion.LookRotation(-val, transform.up);
				}
			}
		}
		catch
		{
			__state = default(AimState);
		}
	}

	[HarmonyPostfix]
	private static void Postfix(AimState __state)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (__state.Changed && (UnityEngine.Object)(object)__state.Renderer != (UnityEngine.Object)null)
			{
				__state.Renderer.rotation = __state.Saved;
			}
		}
		catch
		{
		}
	}
}
