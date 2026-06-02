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
        public const double AbyssalAbilityWeaknessAttackPenalty = 0.10;
        public const int AbyssalAbilityWeaknessDurationMilliseconds = 3000;
        public const double AbyssalPurpleRuneDamageBonus = 0.25;
        public const double SpiritDodgeChanceBonus = 0.10;

        private readonly double armorMultiplier;
        private readonly double attackSpeedMultiplier;
        private readonly bool empireYellowRuneFrontlineShield;
        private readonly bool wildChainReactionLifesteal;
        private readonly bool abyssalAbilityWeakness;
        private readonly bool abyssalPurpleRuneBonusDamage;
        private readonly bool mechanistOpeningDrone;
        private readonly bool mechanistMatch4Turret;
        private readonly double dodgeChance;

        public SynergyModifiers(
            double armorMultiplier,
            double attackSpeedMultiplier = 1.0,
            bool empireYellowRuneFrontlineShield = false,
            bool wildChainReactionLifesteal = false,
            bool abyssalAbilityWeakness = false,
            bool abyssalPurpleRuneBonusDamage = false,
            bool mechanistOpeningDrone = false,
            bool mechanistMatch4Turret = false,
            double dodgeChance = 0.0)
        {
            if (armorMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(armorMultiplier), "Synergy armor multiplier must be positive.");
            }

            if (attackSpeedMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSpeedMultiplier), "Synergy attack speed multiplier must be positive.");
            }

            if (dodgeChance < 0.0 || dodgeChance > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(dodgeChance), "Synergy dodge chance must be between zero and one.");
            }

            this.armorMultiplier = armorMultiplier;
            this.attackSpeedMultiplier = attackSpeedMultiplier;
            this.empireYellowRuneFrontlineShield = empireYellowRuneFrontlineShield;
            this.wildChainReactionLifesteal = wildChainReactionLifesteal;
            this.abyssalAbilityWeakness = abyssalAbilityWeakness;
            this.abyssalPurpleRuneBonusDamage = abyssalPurpleRuneBonusDamage;
            this.mechanistOpeningDrone = mechanistOpeningDrone;
            this.mechanistMatch4Turret = mechanistMatch4Turret;
            this.dodgeChance = dodgeChance;
        }

        public double ArmorMultiplier => armorMultiplier <= 0.0 ? 1.0 : armorMultiplier;
        public double AttackSpeedMultiplier => attackSpeedMultiplier <= 0.0 ? 1.0 : attackSpeedMultiplier;
        public bool EmpireYellowRuneFrontlineShield => empireYellowRuneFrontlineShield;
        public bool WildChainReactionLifesteal => wildChainReactionLifesteal;
        public bool AbyssalAbilityWeakness => abyssalAbilityWeakness;
        public bool AbyssalPurpleRuneBonusDamage => abyssalPurpleRuneBonusDamage;
        public bool MechanistOpeningDrone => mechanistOpeningDrone;
        public bool MechanistMatch4Turret => mechanistMatch4Turret;
        public double DodgeChance => dodgeChance;

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
                wildChainReactionLifesteal: HasActiveTier(progress, FactionCatalog.Wild.Id, requiredCount: 4),
                abyssalAbilityWeakness: HasActiveTier(progress, FactionCatalog.Abyssal.Id, requiredCount: 2),
                abyssalPurpleRuneBonusDamage: HasActiveTier(progress, FactionCatalog.Abyssal.Id, requiredCount: 4),
                mechanistOpeningDrone: HasActiveTier(progress, FactionCatalog.Mechanist.Id, requiredCount: 2),
                mechanistMatch4Turret: HasActiveTier(progress, FactionCatalog.Mechanist.Id, requiredCount: 4),
                dodgeChance: HasActiveTier(progress, FactionCatalog.Spirit.Id, requiredCount: 2) ? SpiritDodgeChanceBonus : 0.0);
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
                && WildChainReactionLifesteal == other.WildChainReactionLifesteal
                && AbyssalAbilityWeakness == other.AbyssalAbilityWeakness
                && AbyssalPurpleRuneBonusDamage == other.AbyssalPurpleRuneBonusDamage
                && MechanistOpeningDrone == other.MechanistOpeningDrone
                && MechanistMatch4Turret == other.MechanistMatch4Turret
                && Math.Abs(DodgeChance - other.DodgeChance) < 1e-9;
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
                WildChainReactionLifesteal,
                AbyssalAbilityWeakness,
                AbyssalPurpleRuneBonusDamage,
                MechanistOpeningDrone,
                HashCode.Combine(MechanistMatch4Turret, DodgeChance));
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
