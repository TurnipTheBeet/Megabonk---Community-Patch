using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Collections.Generic;

using Assets.Scripts.Inventory__Items__Pickups.Items;

namespace MegaBonkMod;

[BepInPlugin("com.megabonk.mod", "MegaBonk Mod", "1.4.1")]
public class Plugin : BasePlugin
{
    internal static string LeaderboardServer = "https://megabonk-lb.fly.dev";
    internal const  string ModVersion        = "1.4.1";

    internal const bool PatchGrandmasTonic = true;

    internal static readonly HashSet<EItem> ActiveUncappedItems   = new() { EItem.Anvil, EItem.OverpoweredLamp, EItem.ZaWarudo };
    internal static readonly HashSet<EItem> ForcedPoolItems       = new() { EItem.Battery, EItem.Skuleg, EItem.OldMask, EItem.BrassKnuckles, EItem.DemonicBlood, EItem.IdleJuice, EItem.SuckyMagnet, EItem.Key, EItem.EchoShard };
    internal static readonly HashSet<int>   BlacklistedStats       = new() { 1,2,3,4,5,11,24 };
    internal static readonly HashSet<int>   BlacklistedShrineStats = new() { 1,2,3,4,5,11,24 };
    internal static readonly List<int>      FullStatPool           = new();
    internal static readonly List<int>      FullShrineStatPool     = new();

    internal static new BepInEx.Logging.ManualLogSource Log { get; private set; }
    internal static ModGui GuiInstance { get; private set; }

    public override void Load()
    {
        Log = base.Log;

        Diag.Install();   // capture full Unity stack traces (diagnosing settings-close NRE)

        var cfgServer = Config.Bind("Leaderboard", "ServerUrl", LeaderboardServer,
            "DO NOT CHANGE THIS. Only modify if you are the server host running locally (use http://localhost:9000).");
        // Override stale IPs on update — preserve localhost override for server host
        if (!cfgServer.Value.Contains("localhost"))
            cfgServer.Value = LeaderboardServer;
        LeaderboardServer = cfgServer.Value;

        UiTheme.Init(Config);
        Hotkeys.Init(Config);
        ChaosMenu.Init(Config);
        MapScanner.Init(Config);
        SkipChestSync.Init(Config);
        SmartTargeting.Init(Config);
        CursedSwordAim.Init(Config);
        WeaponSfxVolume.Init(Config);
        AutoLevelPick.Init(Config);

        ClassInjector.RegisterTypeInIl2Cpp<ModGui>();
        ClassInjector.RegisterTypeInIl2Cpp<ScrollWheelDetector>();
        var harmony = new Harmony("com.megabonk.mod");
        harmony.PatchAll(typeof(Plugin).Assembly);
        GuiInstance = AddComponent<ModGui>();

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
