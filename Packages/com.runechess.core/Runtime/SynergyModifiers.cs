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
        public const double WildAttackSpeedBonus = 0.10;
        public const double WildChainLifestealFraction = 0.15;
        public const int WildChainLifestealDurationMilliseconds = 3000;

        private readonly double armorMultiplier;
        private readonly double attackSpeedMultiplier;
        private readonly bool empireYellowRuneFrontlineShield;
        private readonly bool wildChainReactionLifesteal;

        public SynergyModifiers(
            double armorMultiplier,
            double attackSpeedMultiplier = 1.0,
            bool empireYellowRuneFrontlineShield = false,
            bool wildChainReactionLifesteal = false)
        {
            if (armorMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(armorMultiplier), "Synergy armor multiplier must be positive.");
            }

            if (attackSpeedMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSpeedMultiplier), "Synergy attack speed multiplier must be positive.");
            }

            this.armorMultiplier = armorMultiplier;
            this.attackSpeedMultiplier = attackSpeedMultiplier;
            this.empireYellowRuneFrontlineShield = empireYellowRuneFrontlineShield;
            this.wildChainReactionLifesteal = wildChainReactionLifesteal;
        }

        public double ArmorMultiplier => armorMultiplier <= 0.0 ? 1.0 : armorMultiplier;
        public double AttackSpeedMultiplier => attackSpeedMultiplier <= 0.0 ? 1.0 : attackSpeedMultiplier;
        public bool EmpireYellowRuneFrontlineShield => empireYellowRuneFrontlineShield;
        public bool WildChainReactionLifesteal => wildChainReactionLifesteal;

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

            var attackSpeedMultiplier = 1.0;
            if (HasActiveTier(progress, FactionCatalog.Wild.Id, requiredCount: 2))
            {
                attackSpeedMultiplier *= 1.0 + WildAttackSpeedBonus;
            }

            return new SynergyModifiers(
                armorMultiplier,
                attackSpeedMultiplier,
                empireYellowRuneFrontlineShield: HasActiveTier(progress, FactionCatalog.Empire.Id, requiredCount: 4),
                wildChainReactionLifesteal: HasActiveTier(progress, FactionCatalog.Wild.Id, requiredCount: 4));
        }

        public HeroStats ApplyToStats(HeroStats stats)
        {
            if (stats is null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            return stats with
            {
                Armor = stats.Armor * ArmorMultiplier,
                BaseAttackSpeed = stats.BaseAttackSpeed * AttackSpeedMultiplier
            };
        }

        public bool Equals(SynergyModifiers other)
        {
            return Math.Abs(ArmorMultiplier - other.ArmorMultiplier) < 1e-9
                && Math.Abs(AttackSpeedMultiplier - other.AttackSpeedMultiplier) < 1e-9
                && EmpireYellowRuneFrontlineShield == other.EmpireYellowRuneFrontlineShield
                && WildChainReactionLifesteal == other.WildChainReactionLifesteal;
        }

        public override bool Equals(object? obj)
        {
            return obj is SynergyModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                ArmorMultiplier,
                AttackSpeedMultiplier,
                EmpireYellowRuneFrontlineShield,
                WildChainReactionLifesteal);
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
