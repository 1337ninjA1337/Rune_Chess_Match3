using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Start-of-combat stat modifiers contributed by the combat artifacts a run owns
    /// (GDD P1 "артефакты как модификаторы боя"). This slice covers the two CombatStart
    /// stat artifacts whose effect is a one-shot tweak of a unit's stats: extra frontline
    /// armor (<c>iron_banner</c>) and an ally attack-speed bonus (<c>swift_boots</c>).
    /// They are applied in <see cref="BattleState.Create"/>, the same place the Warlord
    /// commander already buffs its first defender. Triggered combat artifacts that need
    /// cross-tick event logic (revive, on-kill heal, etc.) are tracked as their own tasks.
    /// Magnitudes live here as named constants per the codex data rule so balance changes
    /// never touch the battle logic. Duplicates stack additively.
    /// </summary>
    public readonly struct ArtifactCombatModifiers : IEquatable<ArtifactCombatModifiers>
    {
        /// <summary>"Железное Знамя": flat armor added to each allied frontline unit.</summary>
        public const double IronBannerFrontlineArmorBonus = 8.0;

        /// <summary>"Сапоги Скорости": ally attack-speed bonus fraction.</summary>
        public const double SwiftBootsAttackSpeedBonus = 0.05;

        private readonly double frontlineArmorBonus;
        private readonly double attackSpeedMultiplier;

        public ArtifactCombatModifiers(
            double frontlineArmorBonus = 0.0,
            double attackSpeedMultiplier = 1.0)
        {
            if (frontlineArmorBonus < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(frontlineArmorBonus), "Frontline armor bonus cannot be negative.");
            }

            if (attackSpeedMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSpeedMultiplier), "Attack speed multiplier must be positive.");
            }

            this.frontlineArmorBonus = frontlineArmorBonus;
            this.attackSpeedMultiplier = attackSpeedMultiplier;
        }

        public double FrontlineArmorBonus => frontlineArmorBonus < 0.0 ? 0.0 : frontlineArmorBonus;
        public double AttackSpeedMultiplier => attackSpeedMultiplier <= 0.0 ? 1.0 : attackSpeedMultiplier;

        /// <summary>The neutral set used when a run owns no start-of-combat artifacts.</summary>
        public static ArtifactCombatModifiers None { get; } = new();

        /// <summary>True when every modifier is neutral (no relevant combat artifact owned).</summary>
        public bool IsNeutral => Equals(None);

        /// <summary>
        /// Aggregate the start-of-combat contributions of every owned artifact. Unknown ids
        /// and artifacts handled by other slices contribute nothing here. Duplicates stack
        /// additively (two iron banners give twice the frontline armor).
        /// </summary>
        public static ArtifactCombatModifiers From(IEnumerable<ArtifactState> artifacts)
        {
            if (artifacts is null)
            {
                throw new ArgumentNullException(nameof(artifacts));
            }

            var frontlineArmorBonus = 0.0;
            var attackSpeedMultiplier = 1.0;

            foreach (var artifact in artifacts)
            {
                switch (artifact.Id?.ToLowerInvariant())
                {
                    case "iron_banner":
                        frontlineArmorBonus += IronBannerFrontlineArmorBonus;
                        break;
                    case "swift_boots":
                        attackSpeedMultiplier += SwiftBootsAttackSpeedBonus;
                        break;
                }
            }

            return new ArtifactCombatModifiers(frontlineArmorBonus, attackSpeedMultiplier);
        }

        /// <summary>
        /// Apply the start-of-combat stat tweaks to a single unit: every unit gains the
        /// attack-speed bonus (re-clamped to the MVP range) and frontline units gain the
        /// extra armor. The attack cooldown is reset to the new interval so the faster
        /// attacker also fires its first hit sooner.
        /// </summary>
        public BattleUnit Apply(BattleUnit unit)
        {
            if (unit is null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            if (IsNeutral)
            {
                return unit;
            }

            var attacksPerSecond = CombatFormulas.CalculateAttacksPerSecond(unit.AttacksPerSecond, AttackSpeedMultiplier);
            var addsArmor = FrontlineArmorBonus > 0.0 && unit.Position.IsFrontline;

            return unit with
            {
                Armor = addsArmor ? unit.Armor + FrontlineArmorBonus : unit.Armor,
                AttacksPerSecond = attacksPerSecond,
                AttackCooldownRemaining = CombatFormulas.CalculateAttackInterval(attacksPerSecond)
            };
        }

        public bool Equals(ArtifactCombatModifiers other)
        {
            return Math.Abs(FrontlineArmorBonus - other.FrontlineArmorBonus) < 1e-9
                && Math.Abs(AttackSpeedMultiplier - other.AttackSpeedMultiplier) < 1e-9;
        }

        public override bool Equals(object? obj)
        {
            return obj is ArtifactCombatModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FrontlineArmorBonus, AttackSpeedMultiplier);
        }
    }
}
