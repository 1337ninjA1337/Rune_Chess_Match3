using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    public enum HeroRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>Shop cost range for a rarity. Epic spans 3-4 gold per the GDD.</summary>
    public readonly struct HeroCostRange : IEquatable<HeroCostRange>
    {
        public HeroCostRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Min { get; }
        public int Max { get; }

        public void Deconstruct(out int min, out int max)
        {
            min = Min;
            max = Max;
        }

        public bool Equals(HeroCostRange other)
        {
            return Min == other.Min && Max == other.Max;
        }

        public override bool Equals(object? obj)
        {
            return obj is HeroCostRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Min * 397) ^ Max;
            }
        }

        public override string ToString()
        {
            return $"{nameof(HeroCostRange)} {{ {nameof(Min)} = {Min}, {nameof(Max)} = {Max} }}";
        }

        public static bool operator ==(HeroCostRange left, HeroCostRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HeroCostRange left, HeroCostRange right)
        {
            return !left.Equals(right);
        }
    }

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
            switch (rarity)
            {
                case HeroRarity.Common:
                    return CommonId;
                case HeroRarity.Rare:
                    return RareId;
                case HeroRarity.Epic:
                    return EpicId;
                case HeroRarity.Legendary:
                    return LegendaryId;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown hero rarity.");
            }
        }

        /// <summary>Rarity prices: Common 1, Rare 2, Epic 3-4, Legendary 5.</summary>
        public static HeroCostRange GetCostRange(HeroRarity rarity)
        {
            switch (rarity)
            {
                case HeroRarity.Common:
                    return new HeroCostRange(1, 1);
                case HeroRarity.Rare:
                    return new HeroCostRange(2, 2);
                case HeroRarity.Epic:
                    return new HeroCostRange(3, 4);
                case HeroRarity.Legendary:
                    return new HeroCostRange(5, 5);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown hero rarity.");
            }
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
}
