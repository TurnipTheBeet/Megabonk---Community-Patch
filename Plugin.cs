using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Collections.Generic;

using Assets.Scripts.Inventory__Items__Pickups.Items;

namespace MegaBonkMod;

[BepInPlugin("com.megabonk.mod", "MegaBonk Mod", "1.3.7")]
public class Plugin : BasePlugin
{
    internal static string LeaderboardServer = "http://67.5.111.0:9000";
    internal const  string ModVersion        = "1.3.7";

    internal const bool PatchGrandmasTonic = true;

    internal static readonly HashSet<EItem> ActiveUncappedItems   = new() { EItem.Anvil, EItem.OverpoweredLamp, EItem.ZaWarudo };
    internal static readonly HashSet<EItem> ForcedPoolItems       = new() { EItem.Battery, EItem.Skuleg, EItem.OldMask, EItem.BrassKnuckles, EItem.DemonicBlood, EItem.IdleJuice, EItem.SuckyMagnet };
    internal static readonly HashSet<int>   BlacklistedStats       = new() { 0,1,2,3,4,5,10,11,24,29 };
    internal static readonly HashSet<int>   BlacklistedShrineStats = new() { 0,1,2,3,4,5,10,11,24,29 };
    internal static readonly List<int>      FullStatPool           = new();
    internal static readonly List<int>      FullShrineStatPool     = new();

    internal static new BepInEx.Logging.ManualLogSource Log { get; private set; }

    public override void Load()
    {
        Log = base.Log;

        var cfgServer = Config.Bind("Leaderboard", "ServerUrl", "http://67.5.111.0:9000",
            "DO NOT CHANGE THIS. Only modify if you are the server host running locally (use http://localhost:9000).");
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
