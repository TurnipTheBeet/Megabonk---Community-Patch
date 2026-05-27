using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Collections.Generic;

using Assets.Scripts.Inventory__Items__Pickups.Items;

namespace MegaBonkMod;

[BepInPlugin("com.megabonk.mod", "MegaBonk Mod", "1.3.15")]
public class Plugin : BasePlugin
{
    internal static string LeaderboardServer = "http://megabonkcommunitypatch.duckdns.org:9000";
    internal const  string ModVersion        = "1.3.15";

    internal const bool PatchGrandmasTonic = true;

    internal static readonly HashSet<EItem> ActiveUncappedItems   = new() { EItem.Anvil, EItem.OverpoweredLamp, EItem.ZaWarudo };
    internal static readonly HashSet<EItem> ForcedPoolItems       = new() { EItem.Battery, EItem.Skuleg, EItem.OldMask, EItem.BrassKnuckles, EItem.DemonicBlood, EItem.IdleJuice, EItem.SuckyMagnet, EItem.Key, EItem.EchoShard };
    internal static readonly HashSet<int>   BlacklistedStats       = new() { 1,2,3,4,5,10,11,24 };
    internal static readonly HashSet<int>   BlacklistedShrineStats = new() { 1,2,3,4,5,10,11,24 };
    internal static readonly List<int>      FullStatPool           = new();
    internal static readonly List<int>      FullShrineStatPool     = new();

    internal static new BepInEx.Logging.ManualLogSource Log { get; private set; }

    public override void Load()
    {
        Log = base.Log;

        var cfgServer = Config.Bind("Leaderboard", "ServerUrl", LeaderboardServer,
            "DO NOT CHANGE THIS. Only modify if you are the server host running locally (use http://localhost:9000).");
        // Override stale IPs on update — preserve localhost override for server host
        if (!cfgServer.Value.Contains("localhost"))
            cfgServer.Value = LeaderboardServer;
        LeaderboardServer = cfgServer.Value;

        ClassInjector.RegisterTypeInIl2Cpp<ModGui>();
        var harmony = new Harmony("com.megabonk.mod");
        harmony.PatchAll(typeof(Plugin).Assembly);
        AddComponent<ModGui>();

        Log.LogInfo($"[MegaBonkMod] Loaded. Server={LeaderboardServer}");
    }

    internal static int GetItemCap(EItem item) => item switch
    {
        EItem.Anvil           => 2,
        EItem.OverpoweredLamp => 3,
        EItem.ZaWarudo        => 10,
        _                     => 5,
    };
}
