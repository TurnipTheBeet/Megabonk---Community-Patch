using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

#pragma warning disable CS0619

namespace MegabonkCommunityPatch;

internal static class PatchModules
{
    internal sealed class Module
    {
        internal string Name;
        internal string Group;
        internal Type[] PatchClasses;
        internal Func<bool> AutoCondition;
        internal Action<bool> CustomApply;
        internal bool MasterEnabled = true;
        internal bool DefaultMaster = true;
        internal bool Installed;
    }

    private sealed class Resolved
    {
        internal MethodBase Original;
        internal HarmonyMethod Prefix;
        internal HarmonyMethod Postfix;
        internal HarmonyMethod Finalizer;
        internal HarmonyMethod Transpiler;
        internal MethodInfo[] PatchMethods;
    }

    private static Harmony _h;
    private static readonly List<Module> _modules = new List<Module>();
    private static readonly Dictionary<Type, Resolved> _resolved = new Dictionary<Type, Resolved>();

    internal static IReadOnlyList<Module> All => _modules;

    internal static void Init(Harmony h) { _h = h; }

    internal static void SetMaster(Module m, bool on)
    {
        if (m == null) return;
        m.MasterEnabled = on;
        Reevaluate(m);
    }

    internal static void RestoreDefaults()
    {
        foreach (var m in _modules)
        {
            m.MasterEnabled = m.DefaultMaster;
        }
        ReevaluateAll();
    }

    internal static void RegisterAll()
    {
        Register("BOMBUS", "Hot", new[] { typeof(Patch_MyTime_Update_GiantBee), typeof(Patch_BOMBUS_Death) });
        Register("God Mode", "Hot", new[] { typeof(Patch_GodMode) }, () => ModGui.GodMode);
        Register("BOMBUS Player Defense", "Hot", new[] { typeof(Patch_BOMBUS_OneShot), typeof(Patch_EnemyData_GetName_BOMBUS) }, () => GiantBeeState.AnyAlive);
        Register("Upgrade Stat Tooltip", "Cosmetic", new[] { typeof(Patch_UpgradeButton_SetUpgrade), typeof(Patch_UpgradeButton_SetItem), typeof(Patch_UpgradeButton_SetItemPriced), typeof(Patch_InventorySlot_SetItem) });
        Register("Leaderboard UI", "Cosmetic", new[] { typeof(Patch_LeaderboardUiNew_Refresh), typeof(Patch_LBTypeSelected) });
        RegisterDynamic("BOMBUS Damage Gate [dyn]", "Dynamic", want => HotPatches.SetGateSuppressed(!want), () => GiantBeeState.AnyAlive);
        RegisterDynamic("InstaKill Detour [dyn]", "Dynamic", want => HotPatches.SetInstaKill(want && ModGui.InstaKill), () => ModGui.InstaKill);
        RegisterDynamic("Bluetooth Element Detour [dyn]", "Dynamic", want => { if (!want) HotPatches.SetBTFix(false); });
        AutoRegisterRemaining();
        WarmCache();
        ReevaluateAll();
    }

    private static void AutoRegisterRemaining()
    {
        var registered = new HashSet<Type>();
        foreach (var m in _modules)
            if (m.PatchClasses != null)
                foreach (var t in m.PatchClasses)
                    registered.Add(t);

        int count = 0;
        foreach (var type in typeof(PatchModules).Assembly.GetTypes())
        {
            if (registered.Contains(type)) continue;
            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
            string name = type.Name.StartsWith("Patch_") ? type.Name.Substring(6) : type.Name;
            Register(name, "Other", new[] { type });
            count++;
        }
        Plugin.Log.LogInfo($"[Modules] auto-registered {count} additional patch classes (total {_modules.Count}).");
    }

    internal static Module Register(string name, string group, Type[] patchClasses, Func<bool> autoCondition = null)
    {
        var m = new Module { Name = name, Group = group, PatchClasses = patchClasses, AutoCondition = autoCondition, Installed = false };
        _modules.Add(m);
        return m;
    }

    internal static Module RegisterDynamic(string name, string group, Action<bool> apply, Func<bool> autoCondition = null)
    {
        var m = new Module { Name = name, Group = group, CustomApply = apply, AutoCondition = autoCondition, Installed = false };
        _modules.Add(m);
        return m;
    }

    internal static void ReevaluateAll()
    {
        for (int i = 0; i < _modules.Count; i++)
            Reevaluate(_modules[i]);
    }

    internal static void Reevaluate(Module m)
    {
        if (_h == null || m == null) return;
        bool want = m.MasterEnabled && (m.AutoCondition == null || SafeCond(m));
        if (want == m.Installed) return;

        if (m.CustomApply != null)
        {
            try { m.CustomApply(want); }
            catch (Exception ex) { Plugin.Log.LogError($"apply {m.Name} failed: {ex.Message}"); }
            m.Installed = want;
            Plugin.Log.LogInfo($"[Modules] {m.Name} {(want ? "installed" : "removed")} (dyn)");
        }
        else if (want) Install(m);
        else Remove(m);
    }

    private static bool SafeCond(Module m)
    {
        try { return m.AutoCondition(); }
        catch { return true; }
    }

    internal static void WarmCache()
    {
        foreach (var m in _modules)
        {
            if (m.PatchClasses == null) continue;
            foreach (var t in m.PatchClasses)
            {
                try { Resolve(t); }
                catch (Exception ex) { Plugin.Log.LogError($"resolve {t.Name} failed: {ex.Message}"); }
            }
        }
    }

    private static void Install(Module m)
    {
        bool anyFailed = false;
        foreach (var t in m.PatchClasses)
        {
            try
            {
                var r = Resolve(t);
                if (r.Original != null)
                {
                    ((dynamic)_h).Patch(r.Original, r.Prefix, r.Postfix, r.Transpiler, r.Finalizer);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"install {m.Name}/{t.Name} failed: {ex.Message}");
                anyFailed = true;
            }
        }
        if (!anyFailed)
        {
            m.Installed = true;
            Plugin.Log.LogInfo($"[Modules] {m.Name} installed");
        }
    }

    private static void Remove(Module m)
    {
        foreach (var t in m.PatchClasses)
        {
            try
            {
                var r = Resolve(t);
                if (r.Original == null) continue;
                foreach (var mi in r.PatchMethods)
                    _h.Unpatch(r.Original, mi);
            }
            catch (Exception ex) { Plugin.Log.LogError($"remove {m.Name}/{t.Name} failed: {ex.Message}"); }
        }
        m.Installed = false;
        Plugin.Log.LogInfo($"[Modules] {m.Name} removed");
    }

    private static Resolved Resolve(Type t)
    {
        if (_resolved.TryGetValue(t, out var cached)) return cached;

        var r = new Resolved { Original = GetOriginal(t) };
        var methods = new List<MethodInfo>();
        foreach (var mi in GetPatchMethods(t))
        {
            methods.Add(mi);
            var hm = new HarmonyMethod(mi);
            if (mi.GetCustomAttribute<HarmonyPrefix>() != null || mi.Name == "Prefix") r.Prefix = hm;
            else if (mi.GetCustomAttribute<HarmonyPostfix>() != null || mi.Name == "Postfix") r.Postfix = hm;
            else if (mi.GetCustomAttribute<HarmonyFinalizer>() != null || mi.Name == "Finalizer") r.Finalizer = hm;
            else if (mi.GetCustomAttribute<HarmonyTranspiler>() != null || mi.Name == "Transpiler") r.Transpiler = hm;
        }
        r.PatchMethods = methods.ToArray();
        _resolved[t] = r;
        return r;
    }

    private static MethodBase GetOriginal(Type patchClass)
    {
        var merged = HarmonyMethodExtensions.GetMergedFromType(patchClass);
        if (merged?.declaringType != null && !string.IsNullOrEmpty(merged.methodName))
            return merged.argumentTypes != null
                ? AccessTools.Method(merged.declaringType, merged.methodName, merged.argumentTypes)
                : AccessTools.Method(merged.declaringType, merged.methodName);

        var tm = AccessTools.Method(patchClass, "TargetMethod");
        if (tm != null)
        {
            try { return tm.Invoke(null, null) as MethodBase; }
            catch (Exception ex) { Plugin.Log.LogError($"TargetMethod() on {patchClass.Name} threw: {ex.Message}"); }
        }
        return null;
    }

    private static IEnumerable<MethodInfo> GetPatchMethods(Type patchClass)
    {
        var methods = new List<MethodInfo>();
        foreach (var mi in patchClass.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (mi.GetCustomAttribute<HarmonyPrefix>() != null ||
                mi.GetCustomAttribute<HarmonyPostfix>() != null ||
                mi.GetCustomAttribute<HarmonyFinalizer>() != null ||
                mi.GetCustomAttribute<HarmonyTranspiler>() != null ||
                mi.Name == "Prefix" || mi.Name == "Postfix" ||
                mi.Name == "Finalizer" || mi.Name == "Transpiler")
                methods.Add(mi);
        }
        if (methods.Count == 0)
        {
            // No op — class is empty or uses unconventional naming
        }
        return methods;
    }
}

internal static class HotPatches
{
    private static Harmony _h;
    private static bool _bombusOn;
    private static bool _instaOn;

    internal static bool GateSuppressed;

    internal static void Init(Harmony h) { _h = h; }

    internal static void SetBombus(bool on)
    {
        if (on != _bombusOn) { _bombusOn = on; PatchModules.ReevaluateAll(); }
    }

    internal static void SetGateSuppressed(bool v)
    {
        if (v != GateSuppressed) GateSuppressed = v;
    }

    internal static void SetBTFix(bool on)
    {
        Plugin.Log.LogInfo($"[HotPatches] BT element fix {(on ? "enabled" : "disabled")} (native hook)");
    }

    internal static void SetInstaKill(bool on)
    {
        if (on != _instaOn) { _instaOn = on; Plugin.Log.LogInfo($"[HotPatches] InstaKill {(on ? "enabled" : "disabled")} (native hook)"); }
    }
}

internal static class HotErr
{
    private static readonly HashSet<string> _seen = new HashSet<string>();
    internal static long Total;

    internal static void Once(string key, Exception ex)
    {
        Total++;
        if (_seen.Add(key))
            Plugin.Log.LogWarning($"[{key}] first error (logged once): {ex.Message}");
    }
}
