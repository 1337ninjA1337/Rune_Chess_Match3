using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Combat modifiers contributed by the artifacts a run currently owns
    /// (GDD P1 "артефакты как модификаторы боя"). Two effects apply at combat start
    /// (frontline armor, ally attack speed) and two apply during the fight (a once-per-battle
    /// revive, and on-kill healing for the allied side). Duplicate artifacts stack additively
    /// for the numeric bonuses; the revive is a single charge regardless of duplicates, matching
    /// the GDD "Один павший герой возрождается раз за бой".
    ///
    /// The run owns the artifacts and exposes <see cref="RunState.CombatModifiers"/>; combat
    /// passes the resulting modifiers into <see cref="BattleState.Create"/>, the same
    /// modifier-passing pattern already used by <see cref="SynergyModifiers"/> and
    /// <see cref="ArtifactRuneModifiers"/>. Only the player's artifacts apply.
    /// </summary>
    public readonly struct ArtifactCombatModifiers : IEquatable<ArtifactCombatModifiers>
    {
        /// <summary>"Железное Знамя": flat armor added to each frontline ally (Combat / CombatStart).</summary>
        public const double IronBannerFrontlineArmorBonus = 5.0;

        /// <summary>"Сапоги Скорости": +5% ally attack speed (Combat / CombatStart).</summary>
        public const double SwiftBootsAttackSpeedBonus = 0.05;

        /// <summary>"Жатва Душ": health restored to every alive ally per enemy killed (Combat / Passive).</summary>
        public const double SoulHarvestOnKillHeal = 6.0;

        /// <summary>"Перо Феникса": fraction of max health a revived ally returns with (Combat / OnAllyDeath).</summary>
        public const double PhoenixReviveHealthFraction = 0.5;

        private readonly double frontlineArmorBonus;
        private readonly double attackSpeedBonus;
        private readonly double onKillAllyHeal;
        private readonly bool hasPhoenixRevive;

        public ArtifactCombatModifiers(
            double frontlineArmorBonus = 0.0,
            double attackSpeedBonus = 0.0,
            double onKillAllyHeal = 0.0,
            bool hasPhoenixRevive = false)
        {
            if (frontlineArmorBonus < 0.0 || attackSpeedBonus < 0.0 || onKillAllyHeal < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(frontlineArmorBonus), "Artifact combat bonuses cannot be negative.");
            }

            this.frontlineArmorBonus = frontlineArmorBonus;
            this.attackSpeedBonus = attackSpeedBonus;
            this.onKillAllyHeal = onKillAllyHeal;
            this.hasPhoenixRevive = hasPhoenixRevive;
        }

        public double FrontlineArmorBonus => frontlineArmorBonus;
        public double AttackSpeedBonus => attackSpeedBonus;

        /// <summary>Multiplier applied to ally attack speed (1.0 when no speed artifact is owned).</summary>
        public double AttackSpeedMultiplier => 1.0 + attackSpeedBonus;
        public double OnKillAllyHeal => onKillAllyHeal;
        public bool HasPhoenixRevive => hasPhoenixRevive;
        public double ReviveHealthFraction => PhoenixReviveHealthFraction;

        /// <summary>True when no owned artifact changes the battle.</summary>
        public bool IsEmpty =>
            frontlineArmorBonus == 0.0
            && attackSpeedBonus == 0.0
            && onKillAllyHeal == 0.0
            && !hasPhoenixRevive;

        /// <summary>The neutral modifier set used when a run owns no combat artifacts.</summary>
        public static ArtifactCombatModifiers None { get; } = new();

        /// <summary>
        /// Sum the combat contributions of every owned artifact. Unknown ids (artifacts whose
        /// effect is rune or economy based) contribute nothing here.
        /// </summary>
        public static ArtifactCombatModifiers From(IEnumerable<ArtifactState> artifacts)
        {
            if (artifacts is null)
            {
                throw new ArgumentNullException(nameof(artifacts));
            }

            var frontlineArmor = 0.0;
            var attackSpeed = 0.0;
            var onKillHeal = 0.0;
            var phoenix = false;

            foreach (var artifact in artifacts)
            {
                switch (artifact.Id?.ToLowerInvariant())
                {
                    case "iron_banner":
                        frontlineArmor += IronBannerFrontlineArmorBonus;
                        break;
                    case "swift_boots":
                        attackSpeed += SwiftBootsAttackSpeedBonus;
                        break;
                    case "soul_harvest":
                        onKillHeal += SoulHarvestOnKillHeal;
                        break;
                    case "phoenix_feather":
                        phoenix = true;
                        break;
                }
            }

            return new ArtifactCombatModifiers(frontlineArmor, attackSpeed, onKillHeal, phoenix);
        }

        public bool Equals(ArtifactCombatModifiers other)
        {
            return Math.Abs(frontlineArmorBonus - other.frontlineArmorBonus) < 1e-9
                && Math.Abs(attackSpeedBonus - other.attackSpeedBonus) < 1e-9
                && Math.Abs(onKillAllyHeal - other.onKillAllyHeal) < 1e-9
                && hasPhoenixRevive == other.hasPhoenixRevive;
        }

        public override bool Equals(object? obj)
        {
            return obj is ArtifactCombatModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(frontlineArmorBonus, attackSpeedBonus, onKillAllyHeal, hasPhoenixRevive);
        }
    }
}
