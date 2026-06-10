using UnityEngine;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// DIAGNOSTIC: capture full managed stack traces for Unity errors/exceptions.
//
// IL2CPP logs game-code NREs as a single line ("NullReferenceException: ...")
// with NO stack trace, so the BepInEx log can't tell us where they came from.
// This forces full stack-trace logging for Error/Exception and mirrors every
// such message (with its stack) into our own logger, so a force-closed session
// still leaves the trace in LogOutput.log.
//
// Cheap and harmless to leave on, but it's only here to pin the settings-close
// NRE — remove once that's fixed.
// ─────────────────────────────────────────────────────────────────────────
internal static class Diag
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.Diag");

    static bool _installed;

    internal static void Install()
    {
        if (_installed) return;
        _installed = true;
        try
        {
            Application.SetStackTraceLogType(LogType.Error,     StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            Application.add_logMessageReceived(
                Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Application.LogCallback>(
                    (System.Action<string, string, LogType>)OnUnityLog));
            Log.LogInfo("[Diag] Unity log capture installed.");
        }
        catch (System.Exception e) { Log.LogWarning($"[Diag] install failed: {e}"); }
    }

    static void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        try
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Log.LogError($"[UnityLog/{type}] {condition}\n---- stack ----\n{stackTrace}---------------");
        }
        catch { }
    }
}
