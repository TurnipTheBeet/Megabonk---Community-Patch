using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Assets.Scripts._Data;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

namespace MegabonkCommunityPatch;

internal static class RunDataCollector
{
    internal class RunData
    {
        [JsonPropertyName("weapons")]   public List<string> Weapons   { get; set; } = new();
        [JsonPropertyName("tomes")]     public List<string> Tomes     { get; set; } = new();
        [JsonPropertyName("items")]     public List<string> Items     { get; set; } = new();
        [JsonPropertyName("runTime")]   public float        RunTime   { get; set; }
        [JsonPropertyName("kills")]     public int          Kills     { get; set; }
        [JsonPropertyName("eliteKills")] public int         EliteKills { get; set; }
        [JsonPropertyName("bossKills")] public int          BossKills { get; set; }
        [JsonPropertyName("level")]     public int          Level     { get; set; }
        [JsonPropertyName("stats")]     public StatsData    Stats     { get; set; } = new();
    }

    internal class StatsData
    {
        [JsonPropertyName("hp")]          public float Hp          { get; set; }
        [JsonPropertyName("damage")]      public float Damage      { get; set; }
        [JsonPropertyName("armor")]       public float Armor       { get; set; }
        [JsonPropertyName("speed")]       public float Speed       { get; set; }
        [JsonPropertyName("critChance")]  public float CritChance  { get; set; }
        [JsonPropertyName("lifesteal")]   public float Lifesteal   { get; set; }
        [JsonPropertyName("evasion")]     public float Evasion     { get; set; }
        [JsonPropertyName("luck")]        public float Luck        { get; set; }
        [JsonPropertyName("projSpeed")]   public float ProjSpeed   { get; set; }
        [JsonPropertyName("size")]        public float Size        { get; set; }
    }

    private static JsonSerializerOptions _jsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    internal static string Collect()
    {
        var data = new RunData();

        var gm = GameManager.Instance;
        if (gm == null) return JsonSerializer.Serialize(data, _jsonOpts);

        var inv = gm.GetPlayerInventory();
        if (inv == null) return JsonSerializer.Serialize(data, _jsonOpts);

        // Weapons
        if (inv.weaponInventory != null)
        {
            foreach (var kvp in inv.weaponInventory.weapons)
            {
                var wb = kvp.Value;
                if (wb?.weaponData != null)
                    data.Weapons.Add($"{wb.weaponData.GetName()} Lv.{wb.level}");
            }
        }

        // Tomes
        if (inv.tomeInventory != null)
        {
            foreach (var kvp in inv.tomeInventory.tomeLevels)
            {
                var tomeData = DataManager.Instance?.GetTome(kvp.Key);
                if (tomeData != null)
                    data.Tomes.Add($"{tomeData.GetName()} Lv.{kvp.Value}");
            }
        }

        // Items
        if (inv.itemInventory != null)
        {
            foreach (var kvp in inv.itemInventory.items)
            {
                var itemData = DataManager.Instance?.GetItem(kvp.Key);
                if (itemData != null)
                {
                    int amt = inv.itemInventory.GetAmount(kvp.Key);
                    string name = itemData.GetName();
                    data.Items.Add(amt > 1 ? $"{name} x{amt}" : name);
                }
            }
        }

        // Run time
        data.RunTime = MyTime.runTimer;

        // Stats
        data.Kills     = RunStats.GetStat((EMyStat)0);
        data.EliteKills = RunStats.GetStat((EMyStat)5);
        data.BossKills = RunStats.GetStat((EMyStat)6);
        data.Level     = inv.GetCharacterLevel();

        data.Stats = new StatsData
        {
            Hp         = PlayerStats.GetStat((EStat)0),
            Damage     = PlayerStats.GetStat((EStat)12),
            Armor      = PlayerStats.GetStat((EStat)4),
            Speed      = PlayerStats.GetStat((EStat)25),
            CritChance = PlayerStats.GetStat((EStat)18),
            Lifesteal  = PlayerStats.GetStat((EStat)17),
            Evasion    = PlayerStats.GetStat((EStat)5),
            Luck       = PlayerStats.GetStat((EStat)30),
            ProjSpeed  = PlayerStats.GetStat((EStat)9),
            Size       = PlayerStats.GetStat((EStat)8),
        };

        return JsonSerializer.Serialize(data, _jsonOpts);
    }
}
