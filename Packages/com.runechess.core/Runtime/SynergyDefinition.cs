using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>Whether a synergy comes from a hero faction or a hero class.</summary>
    public enum SynergyKind
    {
        Faction,
        Class
    }

    /// <summary>
    /// A single synergy breakpoint: how many distinct heroes are required and the
    /// effect text taken from the GDD. Effect numbers stay descriptive in the MVP
    /// data layer; combat integration applies them separately.
    /// </summary>
    public sealed record SynergyTier(int RequiredCount, string Effect)
    {
        public int RequiredCount { get; init; } = RequiredCount >= 1
            ? RequiredCount
            : throw new ArgumentOutOfRangeException(nameof(RequiredCount), RequiredCount, "Synergy tier needs at least one hero.");
    }

    /// <summary>
    /// Data description of a faction or class synergy. <see cref="Name"/> matches the
    /// Russian label stored on <see cref="HeroDefinition.Faction"/> / <see cref="HeroDefinition.Class"/>
    /// so the calculator can group heroes without extra mapping.
    /// </summary>
    public sealed record SynergyDefinition(
        string Id,
        string Name,
        SynergyKind Kind,
        string Focus,
        IReadOnlyList<SynergyTier> Tiers)
    {
        /// <summary>Tiers whose breakpoint is met by <paramref name="unitCount"/>.</summary>
        public IReadOnlyList<SynergyTier> ActiveTiers(int unitCount)
        {
            return Tiers.Where(tier => unitCount >= tier.RequiredCount).ToList();
        }

        /// <summary>The next unmet breakpoint, or null if every tier is already active.</summary>
        public SynergyTier? NextTier(int unitCount)
        {
            return Tiers.FirstOrDefault(tier => unitCount < tier.RequiredCount);
        }
    }
}
