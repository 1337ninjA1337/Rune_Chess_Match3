using System;
using System.Collections.Generic;

namespace RuneChess.Core;

public enum HeroRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

/// <summary>Shop cost range for a rarity. Epic spans 3-4 gold per the GDD.</summary>
public readonly record struct HeroCostRange(int Min, int Max);

public static class HeroRarities
{
    public const string CommonId = "common";
    public const string RareId = "rare";
    public const string EpicId = "epic";
    public const string LegendaryId = "legendary";

    public static IReadOnlyList<HeroRarity> All { get; } = Array.AsReadOnly(new[]
    {
        HeroRarity.Common,
        HeroRarity.Rare,
        HeroRarity.Epic,
        HeroRarity.Legendary
    });

    public static string GetId(HeroRarity rarity)
    {
        return rarity switch
        {
            HeroRarity.Common => CommonId,
            HeroRarity.Rare => RareId,
            HeroRarity.Epic => EpicId,
            HeroRarity.Legendary => LegendaryId,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown hero rarity.")
        };
    }

    /// <summary>Rarity prices: Common 1, Rare 2, Epic 3-4, Legendary 5.</summary>
    public static HeroCostRange GetCostRange(HeroRarity rarity)
    {
        return rarity switch
        {
            HeroRarity.Common => new HeroCostRange(1, 1),
            HeroRarity.Rare => new HeroCostRange(2, 2),
            HeroRarity.Epic => new HeroCostRange(3, 4),
            HeroRarity.Legendary => new HeroCostRange(5, 5),
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown hero rarity.")
        };
    }

    /// <summary>Canonical (minimum) shop cost for a rarity.</summary>
    public static int GetCost(HeroRarity rarity)
    {
        return GetCostRange(rarity).Min;
    }

    public static HeroRarity ParseId(string id)
    {
        if (TryParseId(id, out var rarity))
        {
            return rarity;
        }

        throw new ArgumentException($"Unknown hero rarity id '{id}'.", nameof(id));
    }

    public static bool TryParseId(string? id, out HeroRarity rarity)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            rarity = default;
            return false;
        }

        var normalizedId = id.Trim();
        if (normalizedId.Equals(CommonId, StringComparison.OrdinalIgnoreCase))
        {
            rarity = HeroRarity.Common;
            return true;
        }

        if (normalizedId.Equals(RareId, StringComparison.OrdinalIgnoreCase))
        {
            rarity = HeroRarity.Rare;
            return true;
        }

        if (normalizedId.Equals(EpicId, StringComparison.OrdinalIgnoreCase))
        {
            rarity = HeroRarity.Epic;
            return true;
        }

        if (normalizedId.Equals(LegendaryId, StringComparison.OrdinalIgnoreCase))
        {
            rarity = HeroRarity.Legendary;
            return true;
        }

        rarity = default;
        return false;
    }
}
