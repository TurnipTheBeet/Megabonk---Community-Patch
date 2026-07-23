using System;
using System.Runtime.InteropServices;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegabonkCommunityPatch;

internal static class NativeHooks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EnemyDamageFunc(IntPtr thisPtr, IntPtr dc, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EnemyDamageFromPlayerOtherFunc(IntPtr thisPtr, IntPtr dc, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate float EnemyGetSpeedFunc(IntPtr thisPtr, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EnemyAddDebuffFunc(IntPtr thisPtr, int debuff, IntPtr dc, float duration, int stacks, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SphereCast7Func(Vector3 origin, float radius, Vector3 direction, IntPtr results, float maxDistance, int layerMask, int queryTrigger, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SphereCast6Func(Vector3 origin, float radius, Vector3 direction, IntPtr results, float maxDistance, int layerMask, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EnemyDamageFromPlayerWeaponFunc(IntPtr thisPtr, IntPtr dc, IntPtr method);

    private static IntPtr _gameAssembly;

    private static EnemyDamageFunc _origEnemyDamage;
    private static EnemyDamageFromPlayerOtherFunc _origDamageFromPlayerOther;
    private static EnemyGetSpeedFunc _origGetSpeed;
    private static EnemyAddDebuffFunc _origAddDebuff;
    private static SphereCast7Func _origSphereCast7;
    private static SphereCast6Func _origSphereCast6;
    private static EnemyDamageFromPlayerWeaponFunc _origDamageFromPlayerWeapon;

    [DllImport("dobby", CallingConvention = CallingConvention.Cdecl)]
    private static extern int DobbyHook(IntPtr address, IntPtr replaceFunc, out IntPtr originFunc);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_method_from_name(IntPtr klass, string name, int paramCount);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_method_get_pointer(IntPtr method);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_from_name(IntPtr image, string namespaze, string name);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_image(IntPtr klass);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_assembly_get_image(IntPtr assembly);

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_domain_get();

    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_domain_get_assemblies(out IntPtr count);

    private static IntPtr FindClass(string ns, string name)
    {
        IntPtr count;
        IntPtr ptr = il2cpp_domain_get_assemblies(out count);
        int num = (int)count;
        for (int i = 0; i < num; i++)
        {
            IntPtr assembly = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);
            IntPtr img = il2cpp_assembly_get_image(assembly);
            if (img != IntPtr.Zero)
            {
                IntPtr cls = il2cpp_class_from_name(img, ns, name);
                if (cls != IntPtr.Zero) return cls;
            }
        }
        Plugin.Log.LogWarning($"[NativeHooks] FindClass: class {ns}.{name} NOT FOUND");
        return IntPtr.Zero;
    }

    private static IntPtr ResolveMethod(Type type, string methodName, int paramCount)
    {
        try
        {
            string fn = type.FullName;
            int dot = fn.LastIndexOf('.');
            string ns = dot > 0 ? fn.Substring(0, dot) : "";
            string name = dot > 0 ? fn.Substring(dot + 1) : fn;
            IntPtr klass = FindClass(ns, name);
            if (klass == IntPtr.Zero)
            {
                Plugin.Log.LogWarning($"[NativeHooks] IL2CPP class not found for {fn}");
                return IntPtr.Zero;
            }
            IntPtr method = il2cpp_class_get_method_from_name(klass, methodName, paramCount);
            if (method == IntPtr.Zero)
            {
                Plugin.Log.LogWarning($"[NativeHooks] IL2CPP method not found: {fn}.{methodName}({paramCount})");
                return IntPtr.Zero;
            }
            IntPtr ptr = il2cpp_method_get_pointer(method);
            Plugin.Log.LogInfo($"[NativeHooks] Resolved {fn}.{methodName}({paramCount}) -> 0x{ptr:X}");
            return ptr;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[NativeHooks] Failed to resolve {type.FullName}.{methodName}: {ex}");
            return IntPtr.Zero;
        }
    }

    private static bool TryHook(IntPtr target, IntPtr hook, out IntPtr original, string label)
    {
        original = IntPtr.Zero;
        if (target == IntPtr.Zero)
        {
            Plugin.Log.LogWarning($"[NativeHooks] {label}: target is NULL - skipping");
            return false;
        }
        if (target.ToInt64() < _gameAssembly.ToInt64())
        {
            Plugin.Log.LogWarning($"[NativeHooks] {label}: target 0x{target:X} is before GameAssembly base - skipping");
            return false;
        }
        int result = DobbyHook(target, hook, out original);
        if (result != 0)
        {
            Plugin.Log.LogWarning($"[NativeHooks] {label}: DobbyHook FAILED (result={result})");
            return false;
        }
        Plugin.Log.LogInfo($"[NativeHooks] {label}: OK -> original=0x{original:X}");
        return true;
    }

    private static void HookEnemyDamage(IntPtr thisPtr, IntPtr dc, IntPtr method)
    {
        if (!GiantBeeState._active.ContainsKey(thisPtr) || (thisPtr == GiantBeeState.WeaponDamageTarget && !GiantBeeState.WeaponHitExecute))
            _origEnemyDamage(thisPtr, dc, method);
    }

    private static void HookDamageFromPlayerOther(IntPtr thisPtr, IntPtr dc, IntPtr method)
    {
        if (!ModGui.InstaKill) { _origDamageFromPlayerOther(thisPtr, dc, method); return; }
        try { new Enemy(thisPtr).Kill("instakill"); }
        catch (Exception ex) { HotErr.Once("InstaKill.Other", ex); }
    }

    private static float HookGetSpeed(IntPtr thisPtr, IntPtr method)
    {
        float result = _origGetSpeed(thisPtr, method);
        if (ModGui.FreezeEnemies) return 0f;
        if (GiantBeeState._active.ContainsKey(thisPtr))
            return GiantBeeState.GetBombusSpeedRaw(thisPtr);
        return result;
    }

    private static void HookAddDebuff(IntPtr thisPtr, int debuff, IntPtr dc, float duration, int stacks, IntPtr method)
    {
        if (!GiantBeeState._active.ContainsKey(thisPtr))
            _origAddDebuff(thisPtr, debuff, dc, duration, stacks, method);
    }

    // Fast flag set by cactus ownership patches — avoids PlayerStats.GetStat call
    // on every SphereCast when cactus isn't active.
    internal static bool CactusPickupRangeActive;

    private static int HookSphereCast7(Vector3 origin, float radius, Vector3 direction, IntPtr results, float maxDistance, int layerMask, int queryTrigger, IntPtr method)
    {
        if (CactusPickupRangeActive)
        {
            try
            {
                float stat = PlayerStats.GetStat((EStat)9);
                if (stat > 0f) maxDistance *= stat;
            }
            catch (Exception ex) { HotErr.Once("Cactus.Scale", ex); }
        }
        return _origSphereCast7(origin, radius, direction, results, maxDistance, layerMask, queryTrigger, method);
    }

    private static int HookSphereCast6(Vector3 origin, float radius, Vector3 direction, IntPtr results, float maxDistance, int layerMask, IntPtr method)
    {
        if (CactusPickupRangeActive)
        {
            try
            {
                float stat = PlayerStats.GetStat((EStat)9);
                if (stat > 0f) maxDistance *= stat;
            }
            catch (Exception ex) { HotErr.Once("Cactus.Scale", ex); }
        }
        return _origSphereCast6(origin, radius, direction, results, maxDistance, layerMask, method);
    }

    private unsafe static void HookDamageFromPlayerWeapon(IntPtr thisPtr, IntPtr dc, IntPtr method)
    {
        GiantBeeState.WeaponDamageTarget = thisPtr;
        GiantBeeState.WeaponHitExecute = dc != IntPtr.Zero && *(byte*)(void*)(dc + 33) != 0;
        try
        {
            if (ModGui.InstaKill)
            {
                try { new Enemy(thisPtr).Kill("instakill"); return; }
                catch (Exception ex) { HotErr.Once("InstaKill.Weapon", ex); return; }
            }
            _origDamageFromPlayerWeapon(thisPtr, dc, method);
        }
        finally
        {
            GiantBeeState.WeaponDamageTarget = IntPtr.Zero;
            GiantBeeState.WeaponHitExecute = false;
        }
    }

    internal static void Install()
    {
        Plugin.Log.LogInfo("[NativeHooks] Install() starting...");
        _gameAssembly = GetModuleHandle("GameAssembly.dll");
        if (_gameAssembly == IntPtr.Zero)
        {
            Plugin.Log.LogError("[NativeHooks] GameAssembly.dll not found - native hooks disabled");
            return;
        }
        int n = 0;
        try
        {
            if (TryHook(ResolveMethod(typeof(Enemy), "Damage", 1),
                Marshal.GetFunctionPointerForDelegate<EnemyDamageFunc>(HookEnemyDamage),
                out var orig, "Enemy.Damage"))
            { _origEnemyDamage = Marshal.GetDelegateForFunctionPointer<EnemyDamageFunc>(orig); n++; }
        }
        catch (Exception ex) { Plugin.Log.LogError($"[NativeHooks] Enemy.Damage CRASHED: {ex}"); }
        try
        {
            if (TryHook(ResolveMethod(typeof(Enemy), "DamageFromPlayerOther", 1),
                Marshal.GetFunctionPointerForDelegate<EnemyDamageFromPlayerOtherFunc>(HookDamageFromPlayerOther),
                out var orig, "Enemy.DamageFromPlayerOther"))
            { _origDamageFromPlayerOther = Marshal.GetDelegateForFunctionPointer<EnemyDamageFromPlayerOtherFunc>(orig); n++; }
        }
        catch (Exception ex) { Plugin.Log.LogError($"[NativeHooks] DamageFromPlayerOther CRASHED: {ex}"); }
        try
        {
            if (TryHook(ResolveMethod(typeof(Enemy), "GetSpeed", 0),
                Marshal.GetFunctionPointerForDelegate<EnemyGetSpeedFunc>(HookGetSpeed),
                out var orig, "Enemy.GetSpeed"))
            { _origGetSpeed = Marshal.GetDelegateForFunctionPointer<EnemyGetSpeedFunc>(orig); n++; }
        }
        catch (Exception ex) { Plugin.Log.LogError($"[NativeHooks] Enemy.GetSpeed CRASHED: {ex}"); }
        try
        {
            if (TryHook(ResolveMethod(typeof(Enemy), "AddDebuff", 4),
                Marshal.GetFunctionPointerForDelegate<EnemyAddDebuffFunc>(HookAddDebuff),
                out var orig, "Enemy.AddDebuff"))
            { _origAddDebuff = Marshal.GetDelegateForFunctionPointer<EnemyAddDebuffFunc>(orig); n++; }
        }
        catch (Exception ex) { Plugin.Log.LogError($"[NativeHooks] Enemy.AddDebuff CRASHED: {ex}"); }
        try
        {
            if (TryHook(ResolveMethod(typeof(Physics), "SphereCastNonAlloc", 7),
                Marshal.GetFunctionPointerForDelegate<SphereCast7Func>(HookSphereCast7),
                out var orig, "SphereCastNonAlloc(7)"))
            { _origSphereCast7 = Marshal.GetDelegateForFunctionPointer<SphereCast7Func>(orig); n++; }
        }
        catch (Exception ex) { Plugin.Log.LogError($"[NativeHooks] SphereCastNonAlloc 7 CRASHED: {ex}"); }
        try
        {
            if (TryHook(ResolveMethod(typeof(Physics), "SphereCastNonAlloc", 6),
                Marshal.GetFunctionPointerForDelegate<SphereCast6Func>(HookSphereCast6),
                out var orig, "SphereCastNonAlloc(6)"))
            { _origSphereCast6 = Marshal.GetDelegateForFunctionPointer<SphereCast6Func>(orig); n++; }
        }
        catch (Exception ex) { Plugin.Log.LogError($"[NativeHooks] SphereCastNonAlloc 6 CRASHED: {ex}"); }
        try
        {
            if (TryHook(ResolveMethod(typeof(Enemy), "DamageFromPlayerWeapon", 1),
                Marshal.GetFunctionPointerForDelegate<EnemyDamageFromPlayerWeaponFunc>(HookDamageFromPlayerWeapon),
                out var orig, "Enemy.DamageFromPlayerWeapon"))
            { _origDamageFromPlayerWeapon = Marshal.GetDelegateForFunctionPointer<EnemyDamageFromPlayerWeaponFunc>(orig); n++; }
        }
        catch (Exception ex) { Plugin.Log.LogError($"[NativeHooks] DamageFromPlayerWeapon CRASHED: {ex}"); }
        Plugin.Log.LogInfo($"[NativeHooks] Install() complete: {n}/7 native hooks installed");
    }
}
