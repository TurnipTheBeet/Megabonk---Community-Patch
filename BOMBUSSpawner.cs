using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using Actors.Enemies;

namespace MegaBonkMod;

internal static class BOMBUSSpawner
{
    static MethodInfo _spawnEnemy;
    static Type _eEnemyType;
    static Type _eFlagType;

    // ── Overtime auto-spawn tracking ────────────────────────────────
    internal static bool  Enabled = true;
    internal static bool  PhaseActive = false;  // true when a BOMBUS is in the world
    static float _nextSpawnTime;
    static int   _spawnCount;
    static bool _runActive;
    static int _stageIndex;

    // Track which BOMBUS enemies have already dropped a chest (by instance ID)
    static readonly System.Collections.Generic.HashSet<int> _chestDropped = new();

    const float RepeatSpawnInterval = 300f;  // 5 min between each subsequent spawn

    internal static void ResetRun()
    {
        _spawnCount    = 0;
        _stageIndex    = GetStageIndex();
        _nextSpawnTime = 900f; // 15 min overtime in every tier
        _runActive     = true;
        PhaseActive    = false;
        _chestDropped.Clear();
    }

    internal static void EndRun()
    {
        _runActive    = false;
        PhaseActive   = false;
        _chestDropped.Clear();
    }

    internal static bool HasDroppedChest(Enemy enemy)
    {
        if (enemy == null) return true;
        return _chestDropped.Contains(enemy.GetInstanceID());
    }

    internal static void MarkChestDropped(Enemy enemy)
    {
        if (enemy == null) return;
        _chestDropped.Add(enemy.GetInstanceID());
    }

    internal static int GetStageIndex()
    {
        try
        {
            var mi = typeof(MapController).GetMethod("GetStageIndex",
                BindingFlags.Public | BindingFlags.Static);
            if (mi != null) return (int)mi.Invoke(null, null);
        }
        catch { }
        return 0;
    }

    /// <summary>Called every MyTime.Update — checks overtime timer and spawns BOMBUS.</summary>
    internal static void CheckOvertimeSpawn()
    {
        if (!Enabled || !_runActive) return;
        if (EnemyManager.Instance == null) return;
        if (PlayerMovement.Instance == null) return;

        float swarmTimer = StageTimerHelper.GetFinalSwarmTimer();
        if (swarmTimer < _nextSpawnTime) return;

        _spawnCount++;
        TrySpawn();

        _nextSpawnTime += RepeatSpawnInterval;
    }

    internal static void TrySpawn()
    {
        try
        {
            var em = EnemyManager.Instance;
            if (em == null) { Toast.Show("EnemyManager not available", Color.red); return; }

            var player = PlayerMovement.Instance;
            if (player == null) { Toast.Show("Player not in scene", Color.red); return; }

            if (_spawnEnemy == null) Discover(em);
            if (_spawnEnemy == null) { Toast.Show("SpawnEnemy not found", Color.yellow); return; }

            // Use SpawnEnemy with Bee's EnemyData — NOT SpawnBoss, which
            // modifies the shared EnemyData.rendererScale and causes
            // all subsequent spawns to be permanently enlarged.
            var dm = DataManager.Instance;
            if (dm == null) { Toast.Show("DataManager not available", Color.red); return; }

            var beeData = dm.GetEnemyData((EEnemy)24); // BOMBUS enemy type (not Bee)
            if (beeData == null) { Toast.Show("Bee EnemyData not found", Color.yellow); return; }

            Vector3 pos = player.transform.position + player.transform.forward * 5f;

            // SpawnEnemy(EnemyData, Vector3, int wave, bool forceSpawn, EEnemyFlag, bool canBeElite, float extraSizeMultiplier)
            var result = _spawnEnemy.Invoke(em, new object[] { beeData, pos, 0, true, EEnemyFlag.Boss, false, 5f });
            if (result == null) { Toast.Show("SpawnEnemy returned null", Color.yellow); return; }

            var asEnemy = result as Enemy;
            if (asEnemy == null) { Toast.Show("Not an Enemy: " + result.GetType().Name, Color.yellow); return; }

            asEnemy.gameObject.name = "BOMBUS";
            PhaseActive = true;

            // Override HP to float.MaxValue (Spooky Steve-tier)
            try { SetBossHp(asEnemy); }
            catch (Exception ex) { Plugin.Log.LogError($"[BOMBUS] HP error: {ex}"); }

            // Boss HP bar + name come from EEnemyFlag.Boss (value 2)
            try { FixMinimapIcon(asEnemy.transform); }
            catch (Exception ex) { Plugin.Log.LogError($"[BOMBUS] Icon error: {ex}"); }

            Toast.Show("BOMBUS spawned!", Color.green);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[BOMBUS] {e}");
            Toast.Show($"BOMBUS error: {e.Message}", Color.red);
        }
    }

    static void SetBossHp(Enemy enemy)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var t = enemy.GetType();
        float hp = float.MaxValue;

        // Only set explicitly named HP fields/properties — broad patterns like
        // "max" or "hit" falsely match size/effect fields (maxScale, hitEffectSize)
        // and setting them to float.MaxValue blows up enemy sizes globally.
        string[] hpNames = { "maxHp", "hp", "currentHp", "CurrentHp", "health", "maxHealth", "hitPoints", "HitPoints" };

        void TrySet(Type type)
        {
            foreach (var f in type.GetFields(flags))
            {
                if (f.FieldType == typeof(float) && hpNames.Any(p => f.Name.Equals(p, StringComparison.OrdinalIgnoreCase)))
                {
                    f.SetValue(enemy, hp);
                    Plugin.Log.LogInfo($"[BOMBUS] SET field {type.Name}.{f.Name} = {hp}");
                }
            }
            foreach (var p in type.GetProperties(flags))
            {
                if (p.CanWrite && p.PropertyType == typeof(float) && hpNames.Any(n => p.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                {
                    try { p.SetValue(enemy, hp); Plugin.Log.LogInfo($"[BOMBUS] SET prop {type.Name}.{p.Name} = {hp}"); } catch { }
                }
            }
        }

        TrySet(t);
        var baseType = t.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            TrySet(baseType);
            baseType = baseType.BaseType;
        }

        try
        {
            var healMethod = t.GetMethod("Heal", flags, null, new Type[] { typeof(float), typeof(bool) }, null);
            if (healMethod != null)
            {
                healMethod.Invoke(enemy, new object[] { hp, false });
                Plugin.Log.LogInfo("[BOMBUS] Called Heal(float.MaxValue)");
            }
        }
        catch { }
    }

    static void FixMinimapIcon(Transform enemy)
    {
        // For a 5x boss, the icon at 5x parent scale is reasonable.
        // Set icon localScale to 1 (world ~5x) so it's visible but not oversized.
        for (int i = 0; i < enemy.childCount; i++)
        {
            var child = enemy.GetChild(i);
            if (child.gameObject.layer == 14)
            {
                child.localScale = Vector3.one;
                Plugin.Log.LogInfo($"[BOMBUS] Set minimap icon scale: {child.name}");
                return;
            }
        }

        var icon = enemy.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t != null && t.gameObject.layer == 14);
        if (icon != null)
        {
            icon.localScale = Vector3.one;
            Plugin.Log.LogInfo($"[BOMBUS] Set minimap icon scale (recursive): {icon.name}");
        }
    }

    static void Discover(EnemyManager em)
    {
        try
        {
            var emType = em.GetType();
            var method = emType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "SpawnEnemy" && m.GetParameters().Length == 7);
            if (method == null) return;

            _spawnEnemy = method;
            var ps = method.GetParameters();
            _eEnemyType = ps.FirstOrDefault(p => p.ParameterType.IsEnum)?.ParameterType;
            _eFlagType  = ps.LastOrDefault(p => p.ParameterType.IsEnum)?.ParameterType;
        }
        catch { }
    }
}
