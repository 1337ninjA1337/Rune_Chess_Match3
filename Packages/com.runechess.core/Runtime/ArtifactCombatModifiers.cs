using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Combat-artifact contributions a run brings into a fight (GDD P1 "артефакты как
    /// модификаторы боя"). Three kinds live here: one-shot start-of-combat stat tweaks -
    /// extra frontline armor (<c>iron_banner</c>) and an ally attack-speed bonus
    /// (<c>swift_boots</c>) - applied in <see cref="BattleState.Create"/> via
    /// <see cref="Apply"/>, the same place the Warlord commander buffs its first defender;
    /// triggered cross-tick amounts - the number of allies the phoenix feather
    /// (<c>phoenix_feather</c>) can revive this battle, and the health each living ally
    /// regains per enemy slain from the soul harvest (<c>soul_harvest</c>), both seeded
    /// into the battle and resolved by death/kill detection in <see cref="BattleState.Tick"/>;
    /// and persistent battle multipliers that the battle reads back whenever the matching
    /// event fires - ranged auto-attack damage against the enemy backline
    /// (<c>hunters_mark</c>), timed-summon lifetime (<c>clockwork_heart</c>), commander
    /// energy gained from runes (<c>crown_of_command</c>) and the strength of granted shields
    /// (<c>guardian_aegis</c>). Magnitudes live here as named constants per the codex data
    /// rule so balance changes never touch the battle logic. Duplicates stack additively
    /// (two phoenix feathers grant two revives; two hunter's marks add twice the backline bonus).
    /// </summary>
    public readonly struct ArtifactCombatModifiers : IEquatable<ArtifactCombatModifiers>
    {
        /// <summary>"Железное Знамя": flat armor added to each allied frontline unit.</summary>
        public const double IronBannerFrontlineArmorBonus = 8.0;

        /// <summary>"Сапоги Скорости": ally attack-speed bonus fraction.</summary>
        public const double SwiftBootsAttackSpeedBonus = 0.05;

        /// <summary>"Перо Феникса": allies a single feather can revive over one battle.</summary>
        public const int PhoenixFeatherRevives = 1;

        /// <summary>Fraction of max health a revived ally returns to (the rest of the revive balance).</summary>
        public const double PhoenixReviveHealthFraction = 0.5;

        /// <summary>"Жатва Душ": health restored to each living ally per enemy slain.</summary>
        public const double SoulHarvestHealPerKill = 6.0;

        /// <summary>"Метка Охотника": extra ranged auto-attack damage fraction against backline targets.</summary>
        public const double HuntersMarkRangedBacklineBonus = 0.25;

        /// <summary>"Заводное Сердце": extra lifetime fraction granted to timed summons.</summary>
        public const double ClockworkHeartSummonDurationBonus = 0.5;

        /// <summary>"Венец Командования": extra commander-energy fraction gained from runes.</summary>
        public const double CrownOfCommandEnergyBonus = 0.2;

        /// <summary>"Эгида Стража": extra strength fraction added to granted shields.</summary>
        public const double GuardianAegisShieldBonus = 0.3;

        private readonly double frontlineArmorBonus;
        private readonly double attackSpeedMultiplier;
        private readonly int phoenixRevives;
        private readonly double soulHarvestHealPerKill;
        private readonly double rangedBacklineDamageMultiplier;
        private readonly double summonDurationMultiplier;
        private readonly double commanderEnergyMultiplier;
        private readonly double shieldStrengthMultiplier;

        public ArtifactCombatModifiers(
            double frontlineArmorBonus = 0.0,
            double attackSpeedMultiplier = 1.0,
            int phoenixRevives = 0,
            double soulHarvestHealPerKill = 0.0,
            double rangedBacklineDamageMultiplier = 1.0,
            double summonDurationMultiplier = 1.0,
            double commanderEnergyMultiplier = 1.0,
            double shieldStrengthMultiplier = 1.0)
        {
            if (frontlineArmorBonus < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(frontlineArmorBonus), "Frontline armor bonus cannot be negative.");
            }

            if (attackSpeedMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSpeedMultiplier), "Attack speed multiplier must be positive.");
            }

            if (phoenixRevives < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(phoenixRevives), "Revive charges cannot be negative.");
            }

            if (soulHarvestHealPerKill < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(soulHarvestHealPerKill), "Soul-harvest heal cannot be negative.");
            }

            if (rangedBacklineDamageMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(rangedBacklineDamageMultiplier), "Ranged backline damage multiplier must be positive.");
            }

            if (summonDurationMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(summonDurationMultiplier), "Summon duration multiplier must be positive.");
            }

            if (commanderEnergyMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(commanderEnergyMultiplier), "Commander energy multiplier must be positive.");
            }

            if (shieldStrengthMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(shieldStrengthMultiplier), "Shield strength multiplier must be positive.");
            }

            this.frontlineArmorBonus = frontlineArmorBonus;
            this.attackSpeedMultiplier = attackSpeedMultiplier;
            this.phoenixRevives = phoenixRevives;
            this.soulHarvestHealPerKill = soulHarvestHealPerKill;
            this.rangedBacklineDamageMultiplier = rangedBacklineDamageMultiplier;
            this.summonDurationMultiplier = summonDurationMultiplier;
            this.commanderEnergyMultiplier = commanderEnergyMultiplier;
            this.shieldStrengthMultiplier = shieldStrengthMultiplier;
        }

        public double FrontlineArmorBonus => frontlineArmorBonus < 0.0 ? 0.0 : frontlineArmorBonus;
        public double AttackSpeedMultiplier => attackSpeedMultiplier <= 0.0 ? 1.0 : attackSpeedMultiplier;

        /// <summary>Number of allied heroes the run's phoenix feathers can revive this battle.</summary>
        public int PhoenixRevives => phoenixRevives < 0 ? 0 : phoenixRevives;

        /// <summary>Health each living ally regains for every enemy killed (soul-harvest artifacts, additive).</summary>
        public double SoulHarvestHealPerKillTotal => soulHarvestHealPerKill < 0.0 ? 0.0 : soulHarvestHealPerKill;

        /// <summary>Multiplier on ranged auto-attack damage against backline targets (hunter's mark artifacts).</summary>
        public double RangedBacklineDamageMultiplier => rangedBacklineDamageMultiplier <= 0.0 ? 1.0 : rangedBacklineDamageMultiplier;

        /// <summary>Multiplier on the lifetime of timed summons (clockwork heart artifacts).</summary>
        public double SummonDurationMultiplier => summonDurationMultiplier <= 0.0 ? 1.0 : summonDurationMultiplier;

        /// <summary>Multiplier on commander energy gained from runes (crown of command artifacts).</summary>
        public double CommanderEnergyMultiplier => commanderEnergyMultiplier <= 0.0 ? 1.0 : commanderEnergyMultiplier;

        /// <summary>Multiplier on the strength of granted shields (guardian aegis artifacts).</summary>
        public double ShieldStrengthMultiplier => shieldStrengthMultiplier <= 0.0 ? 1.0 : shieldStrengthMultiplier;

        /// <summary>True when this set carries a start-of-combat stat tweak (armor/attack speed).</summary>
        private bool HasStatModifiers => FrontlineArmorBonus > 0.0 || Math.Abs(AttackSpeedMultiplier - 1.0) > 1e-9;

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
            var phoenixRevives = 0;
            var soulHarvestHealPerKill = 0.0;
            var rangedBacklineDamageMultiplier = 1.0;
            var summonDurationMultiplier = 1.0;
            var commanderEnergyMultiplier = 1.0;
            var shieldStrengthMultiplier = 1.0;

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
                    case "phoenix_feather":
                        phoenixRevives += PhoenixFeatherRevives;
                        break;
                    case "soul_harvest":
                        soulHarvestHealPerKill += SoulHarvestHealPerKill;
                        break;
                    case "hunters_mark":
                        rangedBacklineDamageMultiplier += HuntersMarkRangedBacklineBonus;
                        break;
                    case "clockwork_heart":
                        summonDurationMultiplier += ClockworkHeartSummonDurationBonus;
                        break;
                    case "crown_of_command":
                        commanderEnergyMultiplier += CrownOfCommandEnergyBonus;
                        break;
                    case "guardian_aegis":
                        shieldStrengthMultiplier += GuardianAegisShieldBonus;
                        break;
                }
            }

            return new ArtifactCombatModifiers(
                frontlineArmorBonus,
                attackSpeedMultiplier,
                phoenixRevives,
                soulHarvestHealPerKill,
                rangedBacklineDamageMultiplier,
                summonDurationMultiplier,
                commanderEnergyMultiplier,
                shieldStrengthMultiplier);
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

            if (!HasStatModifiers)
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
                && Math.Abs(AttackSpeedMultiplier - other.AttackSpeedMultiplier) < 1e-9
                && PhoenixRevives == other.PhoenixRevives
                && Math.Abs(SoulHarvestHealPerKillTotal - other.SoulHarvestHealPerKillTotal) < 1e-9
                && Math.Abs(RangedBacklineDamageMultiplier - other.RangedBacklineDamageMultiplier) < 1e-9
                && Math.Abs(SummonDurationMultiplier - other.SummonDurationMultiplier) < 1e-9
                && Math.Abs(CommanderEnergyMultiplier - other.CommanderEnergyMultiplier) < 1e-9
                && Math.Abs(ShieldStrengthMultiplier - other.ShieldStrengthMultiplier) < 1e-9;
        }

        public override bool Equals(object? obj)
        {
            return obj is ArtifactCombatModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                FrontlineArmorBonus,
                AttackSpeedMultiplier,
                PhoenixRevives,
                SoulHarvestHealPerKillTotal,
                RangedBacklineDamageMultiplier,
                SummonDurationMultiplier,
                CommanderEnergyMultiplier,
                ShieldStrengthMultiplier);
        }
    }
}
