using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Collections.Generic;

using Assets.Scripts.Inventory__Items__Pickups.Items;

namespace MegaBonkMod;

[BepInPlugin("com.megabonk.mod", "MegaBonk Mod", "1.1.0")]
public class Plugin : BasePlugin
{
    private static ConfigFile _cfg;
    private static ConfigEntry<string> _cfgBlacklistedStats;
    private static ConfigEntry<string> _cfgBlacklistedShrineStats;
    private static ConfigEntry<string> _cfgLeaderboardServer;

    internal static string LeaderboardServer => _cfgLeaderboardServer.Value.Trim();

    internal const bool PatchGrandmasTonic = true;
    internal const bool PatchSpicyMeatball = true;

    internal static readonly HashSet<EItem>   ActiveUncappedItems    = new() { EItem.Anvil, EItem.OverpoweredLamp, EItem.ZaWarudo };
    internal static readonly HashSet<int>     BlacklistedStats        = new();
    internal static readonly HashSet<int>     BlacklistedShrineStats  = new();
    internal static readonly List<int>        FullStatPool            = new();
    internal static readonly List<int>        FullShrineStatPool      = new();

    internal static new BepInEx.Logging.ManualLogSource Log { get; private set; }

    public override void Load()
    {
        Log  = base.Log;
        _cfg = Config;

        _cfgBlacklistedStats       = Config.Bind("StatBlacklist",   "BlacklistedStats",       "0,1,2,4,3,5,10,11,24,29", "Stat indices excluded from chaos/gamble tome and Dicehead passive");
        _cfgBlacklistedShrineStats = Config.Bind("StatBlacklist",   "BlacklistedShrineStats", "0,1,2,3,4,5,10,11,24,29", "Stat indices excluded from charge shrines");
        _cfgLeaderboardServer      = Config.Bind("Leaderboard",     "ServerEndpoint",         "http://67.5.111.0:9000", "URL of the community leaderboard server (leave blank to disable)");
        LoadBlacklistedStats();
        LoadBlacklistedShrineStats();

        ClassInjector.RegisterTypeInIl2Cpp<ModGui>();
        var harmony = new Harmony("com.megabonk.mod");
        harmony.PatchAll(typeof(Plugin).Assembly);
        AddComponent<ModGui>();

        Log.LogInfo("[MegaBonkMod] Loaded.");
    }

    internal static void SaveConfig()
    {
        _cfgBlacklistedStats.Value       = JoinInts(BlacklistedStats,       i => i);
        _cfgBlacklistedShrineStats.Value = JoinInts(BlacklistedShrineStats, i => i);
        _cfg.Save();
    }

    private static void LoadBlacklistedStats()
    {
        BlacklistedStats.Clear();
        if (string.IsNullOrWhiteSpace(_cfgBlacklistedStats.Value)) return;
        foreach (var s in _cfgBlacklistedStats.Value.Split(','))
            if (int.TryParse(s.Trim(), out var i))
                BlacklistedStats.Add(i);
    }

    private static void LoadBlacklistedShrineStats()
    {
        BlacklistedShrineStats.Clear();
        if (string.IsNullOrWhiteSpace(_cfgBlacklistedShrineStats.Value)) return;
        foreach (var s in _cfgBlacklistedShrineStats.Value.Split(','))
            if (int.TryParse(s.Trim(), out var i))
                BlacklistedShrineStats.Add(i);
    }

    internal static int GetItemCap(EItem item) => item switch
    {
        EItem.Anvil           => 2,
        EItem.OverpoweredLamp => 3,
        EItem.ZaWarudo        => 10,
        _                     => 5,
    };



    private static string JoinInts<T>(IEnumerable<T> source, System.Func<T, int> selector)
    {
        var parts = new List<string>();
        foreach (var item in source)
            parts.Add(selector(item).ToString());
        return string.Join(",", parts);
    }
}
