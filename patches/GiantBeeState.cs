using System;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class GiantBeeState
{
	internal class Bombus
	{
		public Enemy enemy;

		public int instanceId;

		public Vector3 spawnScale = Vector3.one;

		public float curScale = 1f;

		public float scaleVelocity = 0f;

		public float lastHitTime = -999f;

		public Transform mapIcon = null;

		public Vector3 mapIconBase = Vector3.one;

		public int mapIconTries = 0;
	}

	internal const float Scale = 15f;

	internal const float ScaleMin = 1.5f;

	internal const float IconScale = 10f;

	private const float FirstSpawnSwarm = 900f;

	private const float StageArrivalStep = 300f;

	private const float SpawnInterval = 300f;

	private const float BaseSpeed = 32f;

	private const float SpeedTimeRate = 0.005f;

	private const float MaxSpeed = 140f;

	private const float BossHpMult = 40f;

	private const float AoeRange = 40f;

	private const float AoeInterval = 0.5f;

	internal static readonly Dictionary<IntPtr, Bombus> _active = new Dictionary<IntPtr, Bombus>();

	private static readonly List<IntPtr> _deadScratch = new List<IntPtr>();

	internal static bool PhaseActive = false;

	internal static bool SpawningNow = false;

	internal static IntPtr WeaponDamageTarget = IntPtr.Zero;

	internal static bool WeaponHitExecute = false;

	internal static float RunTimerAtSpawn = -1f;

	internal static PlayerHealth PhInstance = null;

	private static float _nextSpawnSwarm = 900f;

	private static float _aoeTimer = 0f;

	private static float _lastDensityLog = 0f;

	private static readonly HashSet<EEnemy> GhostKinds = new HashSet<EEnemy>
	{
		(EEnemy)16,
		(EEnemy)17,
		(EEnemy)39,
		(EEnemy)40,
		(EEnemy)43,
		(EEnemy)44,
		(EEnemy)45,
		(EEnemy)46,
		(EEnemy)47,
		(EEnemy)48,
		(EEnemy)49
	};

	private static float MinFrac => 0.1f;

	internal static bool AnyAlive => _active.Count > 0;

	private static Transform FindMapIcon(Enemy e)
	{
		try
		{
			MinimapIcon componentInChildren = ((Component)e).gameObject.GetComponentInChildren<MinimapIcon>(true);
			return ((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null) ? ((Component)componentInChildren).transform : null;
		}
		catch
		{
			return null;
		}
	}

	private static bool TryGet(Enemy e, out Bombus b)
	{
		b = null;
		if (_active.Count == 0 || (UnityEngine.Object)(object)e == (UnityEngine.Object)null)
		{
			return false;
		}
		if (!_active.TryGetValue(((Il2CppObjectBase)e).Pointer, out b))
		{
			return false;
		}
		try
		{
			if (((Object)e).GetInstanceID() != b.instanceId)
			{
				_active.Remove(((Il2CppObjectBase)e).Pointer);
				if (_active.Count == 0)
				{
					HotPatches.SetBombus(on: false);
				}
				b = null;
				return false;
			}
		}
		catch
		{
			b = null;
			return false;
		}
		return true;
	}

	internal static bool IsBombus(Enemy e)
	{
		Bombus b;
		return TryGet(e, out b);
	}

	internal static bool IsBombusPtr(IntPtr ptr)
	{
		return _active.ContainsKey(ptr);
	}

	internal static float GetBombusSpeedRaw(IntPtr ptr)
	{
		if (_active.TryGetValue(ptr, out var value))
		{
			return SpeedForScale(value.curScale);
		}
		return 32f;
	}

	internal static bool IsGhost(EnemyData d)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		return (UnityEngine.Object)(object)d != (UnityEngine.Object)null && GhostKinds.Contains(d.enemyName);
	}

	internal static float GetBombusSpeed(Enemy e)
	{
		float cur = 1f;
		if (TryGet(e, out var b))
		{
			cur = b.curScale;
		}
		return SpeedForScale(cur);
	}

	private static float SpeedForScale(float cur)
	{
		float finalSwarmTimer = StageTimerHelper.GetFinalSwarmTimer();
		float num = Math.Max(0f, finalSwarmTimer - 900f);
		float num2 = 1f + 0.005f * num;
		float num3 = Mathf.Lerp(1f, 2f, (1f - cur) / (1f - MinFrac));
		return Mathf.Clamp(32f * num2 * num3, 32f, 140f);
	}

	internal static void OnHitPlayer(Enemy e)
	{
		if (TryGet(e, out var b))
		{
			b.lastHitTime = Time.time;
		}
	}

	internal static void Reset()
	{
		_active.Clear();
		HotPatches.SetBombus(on: false);
		PhaseActive = false;
		RunTimerAtSpawn = -1f;
		_nextSpawnSwarm = 900f + FirstArrivalOffset();
		_aoeTimer = 0f;
	}

	private static float FirstArrivalOffset()
	{
		try
		{
			int stageIndex = MapController.GetStageIndex();
			if (stageIndex > 0)
			{
				return (float)stageIndex * 300f;
			}
		}
		catch
		{
		}
		return 0f;
	}

	internal static void CheckNewRun()
	{
		if (PhaseActive && !(RunTimerAtSpawn < 0f) && MyTime.runTimer < RunTimerAtSpawn - 30f)
		{
			Reset();
		}
	}

	internal static void OnBombusDied(Enemy enemy)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (!TryGet(enemy, out var _))
		{
			return;
		}
		Vector3 pos = Vector3.zero;
		try
		{
			pos = ((Component)enemy).transform.position;
		}
		catch
		{
		}
		_active.Remove(((Il2CppObjectBase)enemy).Pointer);
		if (_active.Count == 0)
		{
			HotPatches.SetBombus(on: false);
		}
		try
		{
			foreach (Collider componentsInChild in ((Component)enemy).gameObject.GetComponentsInChildren<Collider>())
			{
				componentsInChild.enabled = false;
			}
		}
		catch
		{
		}
		DropCorruptChest(pos);
		ManualLogSource log = Plugin.Log;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(55, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[GiantBee] BOMBUS died, corrupt chest dropped (active=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_active.Count);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(")");
		}
		log.LogInfo(val);
	}

	private static void DropCorruptChest(Vector3 pos)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			EffectManager instance = EffectManager.Instance;
			if (!((UnityEngine.Object)(object)instance == (UnityEngine.Object)null) && !((UnityEngine.Object)(object)instance.openChestNormal == (UnityEngine.Object)null))
			{
				pos = GroundChestPos(pos);
				GameObject val = Object.Instantiate<GameObject>(instance.openChestNormal, pos, Quaternion.identity);
				OpenChest component = val.GetComponent<OpenChest>();
				if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
				{
					component.chestType = (EChest)1;
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Plugin.Log;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val2 = new BepInExErrorLogInterpolatedStringHandler(30, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("[GiantBee] chest drop failed: ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(ex.Message);
			}
			log.LogError(val2);
		}
	}

	private static Vector3 GroundChestPos(Vector3 pos)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Vector3 val = new Vector3(pos.x, pos.y + 50f, pos.z);
			RaycastHit val2 = default(RaycastHit);
			if (Physics.Raycast(val, Vector3.down, out val2, 500f, -5, (QueryTriggerInteraction)1))
			{
				pos.y = val2.point.y;
				return pos;
			}
		}
		catch
		{
		}
		PlayerMovement instance = PlayerMovement.Instance;
		if ((UnityEngine.Object)(object)instance != (UnityEngine.Object)null)
		{
			pos.y = ((Component)instance).transform.position.y;
		}
		return pos;
	}

	internal static void Tick()
	{
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		using (Perf.Measure("Bombus.Tick"))
		{
			CheckNewRun();
			if (MyTime.stageTimer >= 600f)
			{
				float finalSwarmTimer = StageTimerHelper.GetFinalSwarmTimer();
				int num = 0;
				while (finalSwarmTimer >= _nextSpawnSwarm && num++ < 8)
				{
					Spawn();
					_nextSpawnSwarm += 300f;
				}
			}
			if (_active.Count == 0)
			{
				return;
			}
			if (Perf.Enabled && Time.time - _lastDensityLog >= 5f)
			{
				_lastDensityLog = Time.time;
				try
				{
					EnemyManager instance = EnemyManager.Instance;
					if ((UnityEngine.Object)(object)instance != (UnityEngine.Object)null)
					{
						ManualLogSource log = Plugin.Log;
						bool flag = default(bool);
						BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(26, 2, out flag);
						if (flag)
						{
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral("[Diag] numEnemies=");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(instance.numEnemies);
							((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" bombus=");
							((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_active.Count);
						}
						log.LogInfo(val);
					}
				}
				catch
				{
				}
			}
			_aoeTimer += Time.deltaTime;
			bool flag2 = _aoeTimer >= 0.5f;
			if (flag2)
			{
				_aoeTimer = 0f;
			}
			PlayerMovement instance2 = PlayerMovement.Instance;
			_deadScratch.Clear();

			// Throttle scale/visual updates to every other frame — SmoothDamp,
			// transform ops, and map icon updates are not needed 60 times/sec.
			bool visFrame = (Time.frameCount & 1) == 0;

			foreach (KeyValuePair<IntPtr, Bombus> item in _active)
			{
				Bombus value = item.Value;
				Enemy enemy = value.enemy;
				if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || ((Il2CppObjectBase)enemy).Pointer == IntPtr.Zero)
				{
					_deadScratch.Add(item.Key);
					continue;
				}
				bool flag3;
				try
				{
					flag3 = enemy.hp > 0f;
				}
				catch
				{
					flag3 = false;
				}
				if (!flag3)
				{
					_deadScratch.Add(item.Key);
				}
				else
				{
					if ((UnityEngine.Object)(object)instance2 == (UnityEngine.Object)null)
					{
						continue;
					}
					try
					{
						Vector3 position = ((Component)instance2).transform.position;
						Vector3 val2 = ((Component)enemy).transform.position - position;
						float magnitude = val2.magnitude;
						float num2 = 40f * value.curScale;
						bool flag4 = magnitude <= num2 * 4f;
						bool flag5 = Time.time - value.lastHitTime < 5f;
						float num3 = ((flag4 && !flag5) ? MinFrac : 1f);
						float num4 = ((num3 < value.curScale) ? 4f : 1.5f);

						if (visFrame)
						{
							value.curScale = Mathf.SmoothDamp(value.curScale, num3, ref value.scaleVelocity, num4);
							((Component)enemy).transform.localScale = value.spawnScale * value.curScale;
						}
						else
						{
							// Still advance SmoothDamp velocity even on off-frames
							value.curScale = Mathf.SmoothDamp(value.curScale, num3, ref value.scaleVelocity, num4);
						}

						if ((UnityEngine.Object)(object)value.mapIcon == (UnityEngine.Object)null && value.mapIconTries < 180)
						{
							value.mapIconTries++;
							Transform val3 = FindMapIcon(enemy);
							if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
							{
								value.mapIcon = val3;
								value.mapIconBase = val3.localScale;
							}
						}
						if (visFrame && (UnityEngine.Object)(object)value.mapIcon != (UnityEngine.Object)null)
						{
							float num5 = 15f * value.curScale;
							if (num5 > 0.0001f)
							{
								value.mapIcon.localScale = value.mapIconBase * (10f / num5);
							}
						}
						if (flag2 && PhInstance != null && magnitude <= num2)
						{
							PhInstance.DamagePlayer(enemy, position, (DcFlags)0);
						}
					}
					catch
					{
					}
				}
			}
			for (int i = 0; i < _deadScratch.Count; i++)
			{
				_active.Remove(_deadScratch[i]);
			}
			if (_deadScratch.Count > 0 && _active.Count == 0)
			{
				HotPatches.SetBombus(on: false);
			}
		}
	}

	internal static void Spawn(bool manualOverride = false)
	{
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Expected O, but got Unknown
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		bool flag = default(bool);
		try
		{
			EnemyManager instance = EnemyManager.Instance;
			DataManager instance2 = DataManager.Instance;
			if ((UnityEngine.Object)(object)instance == (UnityEngine.Object)null || (UnityEngine.Object)(object)instance2 == (UnityEngine.Object)null)
			{
				return;
			}
			EnemyData enemyData = instance2.GetEnemyData((EEnemy)24);
			if ((UnityEngine.Object)(object)enemyData == (UnityEngine.Object)null)
			{
				return;
			}
			Vector3 val = SpawnPositions.GetEnemySpawnPosition(enemyData, 50, true, float.MaxValue);
			if (val == SpawnPositions.INVALID_POS || val == Vector3.zero)
			{
				PlayerMovement instance3 = PlayerMovement.Instance;
				val = (((UnityEngine.Object)(object)instance3 != (UnityEngine.Object)null) ? (((Component)instance3).transform.position + new Vector3(5f, 0f, 5f)) : Vector3.zero);
			}
			SpawningNow = true;
			Enemy val2 = instance.SpawnEnemy(enemyData, val, 0, true, (EEnemyFlag)2, false, 15f);
			SpawningNow = false;
			if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
			{
				try
				{
					float num = val2.maxHp;
					if (num <= 0f)
					{
						num = val2.hp;
					}
					float hp = (val2.maxHp = num * 40f);
					val2.hp = hp;
				}
				catch
				{
				}
				HotPatches.SetBombus(on: true);
				_active[((Il2CppObjectBase)val2).Pointer] = new Bombus
				{
					enemy = val2,
					instanceId = ((Object)val2).GetInstanceID(),
					spawnScale = ((Component)val2).transform.localScale,
					curScale = 1f,
					lastHitTime = -999f
				};
				try
				{
					foreach (CapsuleCollider componentsInChild in ((Component)val2).gameObject.GetComponentsInChildren<CapsuleCollider>())
					{
						componentsInChild.direction = 1;
						componentsInChild.height = componentsInChild.radius * 2f;
					}
				}
				catch
				{
				}
				try
				{
					if (_active.TryGetValue(((Il2CppObjectBase)val2).Pointer, out var value))
					{
						Transform val3 = FindMapIcon(val2);
						if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null)
						{
							value.mapIcon = val3;
							value.mapIconBase = val3.localScale;
							val3.localScale = value.mapIconBase * (2f / 3f);
						}
					}
				}
				catch
				{
				}
			}
			PhaseActive = true;
			RunTimerAtSpawn = MyTime.runTimer;
			ManualLogSource log = Plugin.Log;
			BepInExInfoLogInterpolatedStringHandler val4 = new BepInExInfoLogInterpolatedStringHandler(48, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("[GiantBee] BOMBUS spawned (active=");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<int>(_active.Count);
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral(") swarm=");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<float>(StageTimerHelper.GetFinalSwarmTimer(), "F0");
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("s pos=");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<Vector3>(val);
			}
			log.LogInfo(val4);
			try
			{
				AlertUi val5 = Object.FindObjectOfType<AlertUi>();
				if ((UnityEngine.Object)(object)val5 != (UnityEngine.Object)null)
				{
					val5.SetAlertBoss();
					if ((UnityEngine.Object)(object)val5.t_alert != (UnityEngine.Object)null)
					{
						((TMP_Text)val5.t_alert).text = "BOMBUS IS APPROACHING...";
					}
				}
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			SpawningNow = false;
			ManualLogSource log2 = Plugin.Log;
			BepInExErrorLogInterpolatedStringHandler val6 = new BepInExErrorLogInterpolatedStringHandler(25, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val6).AppendLiteral("[GiantBee] spawn failed: ");
				((BepInExLogInterpolatedStringHandler)val6).AppendFormatted<string>(ex.Message);
			}
			log2.LogError(val6);
		}
	}
}
