using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MegabonkCommunityPatch;

internal static class SpaceNoodleState
{
	private static ConfigEntry<bool> _enabled;

	internal static readonly Dictionary<IntPtr, List<Vector3>> ExtraTargetPos = new Dictionary<IntPtr, List<Vector3>>();

	internal static readonly Dictionary<IntPtr, List<LineRenderer>> ExtraRenderers = new Dictionary<IntPtr, List<LineRenderer>>();

	internal static readonly Collider[] Buf = (Collider[])(object)new Collider[64];

	internal static readonly HashSet<IntPtr> UsedRbs = new HashSet<IntPtr>();

	internal static readonly object[] HitArgs = new object[1];

	private static Action<LaserBeamAttack, Collider> _hitEnemyFast;

	private static MethodInfo _hitEnemyMi;

	private static bool _bindTried;

	internal static bool Enabled => _enabled != null && _enabled.Value;

	internal static void Init(ConfigFile cfg)
	{
		_enabled = cfg.Bind<bool>("Experimental", "SpaceNoodleExtraBeams", false, "UNFINISHED/EXPERIMENTAL: extra Space Noodle laser beams from projectile-count stacks. Known not to work reliably — off by default.");
	}

	internal static void Reset()
	{
		ExtraTargetPos.Clear();
		ExtraRenderers.Clear();
	}

	internal static void HitEnemy(LaserBeamAttack inst, Collider col)
	{
		if (!_bindTried)
		{
			_bindTried = true;
			_hitEnemyMi = AccessTools.Method(typeof(LaserBeamAttack), "HitEnemy", (Type[])null, (Type[])null);
			try
			{
				_hitEnemyFast = AccessTools.MethodDelegate<Action<LaserBeamAttack, Collider>>(_hitEnemyMi, (object)null, true);
			}
			catch
			{
				_hitEnemyFast = null;
			}
		}
		if (_hitEnemyFast != null)
		{
			_hitEnemyFast(inst, col);
			return;
		}
		HitArgs[0] = col;
		_hitEnemyMi?.Invoke(inst, HitArgs);
	}
}
