using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Numeric combat modifiers unlocked by active synergies. The MVP starts with
    /// always-on stat modifiers; triggered effects stay in their own future systems.
    /// </summary>
    public readonly struct SynergyModifiers : IEquatable<SynergyModifiers>
    {
        public const double EmpireArmorBonus = 0.10;

        private readonly double armorMultiplier;

        public SynergyModifiers(double armorMultiplier)
        {
            if (armorMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(armorMultiplier), "Synergy armor multiplier must be positive.");
            }

            this.armorMultiplier = armorMultiplier;
        }

        public double ArmorMultiplier => armorMultiplier <= 0.0 ? 1.0 : armorMultiplier;

        public static SynergyModifiers None { get; } = new(1.0);

        public static SynergyModifiers ForTeam(IEnumerable<BoardHero> team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            return FromProgress(SynergyCalculator.Evaluate(team));
        }

        public static SynergyModifiers FromProgress(IEnumerable<SynergyProgress> progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var armorMultiplier = 1.0;
            if (HasActiveTier(progress, FactionCatalog.Empire.Id, requiredCount: 2))
            {
                armorMultiplier *= 1.0 + EmpireArmorBonus;
            }

            return new SynergyModifiers(armorMultiplier);
        }

        public HeroStats ApplyToStats(HeroStats stats)
        {
            if (stats is null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            return stats with
            {
                Armor = stats.Armor * ArmorMultiplier
            };
        }

        public bool Equals(SynergyModifiers other)
        {
            return Math.Abs(ArmorMultiplier - other.ArmorMultiplier) < 1e-9;
        }

        public override bool Equals(object? obj)
        {
            return obj is SynergyModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ArmorMultiplier.GetHashCode();
        }

        private static bool HasActiveTier(
            IEnumerable<SynergyProgress> progress,
            string synergyId,
            int requiredCount)
        {
            return progress.Any(item =>
                item.Definition.Id.Equals(synergyId, StringComparison.OrdinalIgnoreCase)
                && item.UnitCount >= requiredCount);
        }
    }
}
