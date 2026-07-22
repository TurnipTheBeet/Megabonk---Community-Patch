using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;

namespace MegabonkCommunityPatch;

/// <summary>
/// Banishment Weight Redistribution:
/// When all items in a rarity tier are banished, zero out that tier's weight
/// so the probability mass is redistributed to remaining tiers proportionally.
/// </summary>
[HarmonyPatch(typeof(Rarity), "CalculateRarityWeights")]
internal static class Patch_Rarity_ZeroEmptyTiers
{
    [HarmonyPostfix]
    private static void Postfix(float[] rarityWeights, float luck)
    {
        // rarityWeights is float[] with indices:
        // 0 = Common, 1 = Rare, 2 = Epic, 3 = Legendary
        // (Corrupted=4 and Quest=5 are special and not handled here)

        var available = RunUnlockables.availableItems;
        if (available == null)
            return;

        bool anyZeroed = false;

        for (int i = 0; i < rarityWeights.Length && i < 4; i++)
        {
            var tier = (EItemRarity)i;
            if (available.TryGetValue(tier, out var list) && (list == null || list.Count == 0))
            {
                if (rarityWeights[i] > 0f)
                {
                    rarityWeights[i] = 0f;
                    anyZeroed = true;
                }
            }
        }

        // If we zeroed anything, renormalize so remaining weights sum to 1
        if (anyZeroed)
        {
            float total = 0f;
            for (int i = 0; i < rarityWeights.Length && i < 4; i++)
                total += rarityWeights[i];

            if (total > 0f)
            {
                float invTotal = 1f / total;
                for (int i = 0; i < rarityWeights.Length && i < 4; i++)
                    rarityWeights[i] *= invTotal;
            }
        }
    }
}