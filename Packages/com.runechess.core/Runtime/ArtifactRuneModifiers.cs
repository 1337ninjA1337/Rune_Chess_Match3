using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Rune (match-3) modifiers contributed by the artifacts a run currently owns
    /// (GDD P1 "артефакты как модификаторы рун"). Each owned rune artifact amplifies the
    /// resolved <see cref="RuneEffect"/> that the matching color produces, so the bonus is
    /// felt the moment the player makes the match. Duplicate artifacts stack additively.
    /// Magnitudes live here as named constants per the codex data rule so balance can change
    /// without touching the rune-application logic.
    ///
    /// The run owns the artifacts and exposes <see cref="RunState.RuneModifiers"/>; combat
    /// passes the resulting modifiers into <see cref="BattleState.ApplyRuneEffects"/>, which is
    /// the same modifier-passing pattern already used by <see cref="SynergyModifiers"/>.
    /// </summary>
    public readonly struct ArtifactRuneModifiers : IEquatable<ArtifactRuneModifiers>
    {
        /// <summary>"Кровавый Кубок": green-rune healing is amplified (Rune / OnRuneMatch).</summary>
        public const double BloodChaliceGreenHealingBonus = 0.25;

        /// <summary>"Искровой Конденсатор": blue-rune mana is amplified (Rune / OnRuneMatch).</summary>
        public const double SparkCapacitorBlueManaBonus = 0.25;

        /// <summary>"Тлеющее Ядро": red-rune physical damage gains a small flat bonus (Rune / OnRuneMatch).</summary>
        public const double EmberCoreRedPhysicalFlatBonus = 2.0;

        /// <summary>"Оберегающий Тотем": yellow-rune shields are amplified (Rune / OnRuneMatch).</summary>
        public const double WardingTotemYellowShieldBonus = 0.30;

        /// <summary>"Печать Бездны": purple-rune magic damage is amplified (Rune / OnRuneMatch).</summary>
        public const double AbyssalSigilPurpleMagicBonus = 0.25;

        /// <summary>
        /// "Призменная Линза": white runes feed the commander harder. The GDD framing is that
        /// white runes boost the next color; the MVP slice models this as extra white-rune
        /// commander energy, leaving the full next-color amplification as a future refinement.
        /// </summary>
        public const double PrismLensWhiteEnergyBonus = 0.50;

        /// <summary>"Проводник Цепей": chain-reaction effects (chain 2+) are amplified (Rune / OnChainReaction).</summary>
        public const double ChainConduitChainReactionBonus = 0.20;

        private readonly double greenHealingBonus;
        private readonly double blueManaBonus;
        private readonly double redPhysicalFlatBonus;
        private readonly double yellowShieldBonus;
        private readonly double purpleMagicBonus;
        private readonly double whiteEnergyBonus;
        private readonly double chainReactionBonus;

        public ArtifactRuneModifiers(
            double greenHealingBonus = 0.0,
            double blueManaBonus = 0.0,
            double redPhysicalFlatBonus = 0.0,
            double yellowShieldBonus = 0.0,
            double purpleMagicBonus = 0.0,
            double whiteEnergyBonus = 0.0,
            double chainReactionBonus = 0.0)
        {
            if (greenHealingBonus < 0.0
                || blueManaBonus < 0.0
                || redPhysicalFlatBonus < 0.0
                || yellowShieldBonus < 0.0
                || purpleMagicBonus < 0.0
                || whiteEnergyBonus < 0.0
                || chainReactionBonus < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(greenHealingBonus), "Artifact rune bonuses cannot be negative.");
            }

            this.greenHealingBonus = greenHealingBonus;
            this.blueManaBonus = blueManaBonus;
            this.redPhysicalFlatBonus = redPhysicalFlatBonus;
            this.yellowShieldBonus = yellowShieldBonus;
            this.purpleMagicBonus = purpleMagicBonus;
            this.whiteEnergyBonus = whiteEnergyBonus;
            this.chainReactionBonus = chainReactionBonus;
        }

        public double GreenHealingBonus => greenHealingBonus;
        public double BlueManaBonus => blueManaBonus;
        public double RedPhysicalFlatBonus => redPhysicalFlatBonus;
        public double YellowShieldBonus => yellowShieldBonus;
        public double PurpleMagicBonus => purpleMagicBonus;
        public double WhiteEnergyBonus => whiteEnergyBonus;
        public double ChainReactionBonus => chainReactionBonus;

        /// <summary>True when no owned artifact changes any rune effect.</summary>
        public bool IsEmpty =>
            greenHealingBonus == 0.0
            && blueManaBonus == 0.0
            && redPhysicalFlatBonus == 0.0
            && yellowShieldBonus == 0.0
            && purpleMagicBonus == 0.0
            && whiteEnergyBonus == 0.0
            && chainReactionBonus == 0.0;

        /// <summary>The neutral modifier set used when a run owns no rune artifacts.</summary>
        public static ArtifactRuneModifiers None { get; } = new();

        /// <summary>
        /// Sum the rune contributions of every owned artifact. Unknown ids (artifacts whose
        /// effect is combat or economy based) contribute nothing here.
        /// </summary>
        public static ArtifactRuneModifiers From(IEnumerable<ArtifactState> artifacts)
        {
            if (artifacts is null)
            {
                throw new ArgumentNullException(nameof(artifacts));
            }

            var greenHealing = 0.0;
            var blueMana = 0.0;
            var redPhysical = 0.0;
            var yellowShield = 0.0;
            var purpleMagic = 0.0;
            var whiteEnergy = 0.0;
            var chainReaction = 0.0;

            foreach (var artifact in artifacts)
            {
                switch (artifact.Id?.ToLowerInvariant())
                {
                    case "blood_chalice":
                        greenHealing += BloodChaliceGreenHealingBonus;
                        break;
                    case "spark_capacitor":
                        blueMana += SparkCapacitorBlueManaBonus;
                        break;
                    case "ember_core":
                        redPhysical += EmberCoreRedPhysicalFlatBonus;
                        break;
                    case "warding_totem":
                        yellowShield += WardingTotemYellowShieldBonus;
                        break;
                    case "abyssal_sigil":
                        purpleMagic += AbyssalSigilPurpleMagicBonus;
                        break;
                    case "prism_lens":
                        whiteEnergy += PrismLensWhiteEnergyBonus;
                        break;
                    case "chain_conduit":
                        chainReaction += ChainConduitChainReactionBonus;
                        break;
                }
            }

            return new ArtifactRuneModifiers(
                greenHealing,
                blueMana,
                redPhysical,
                yellowShield,
                purpleMagic,
                whiteEnergy,
                chainReaction);
        }

        /// <summary>
        /// Return <paramref name="effect"/> with its power amplified by the owned rune
        /// artifacts. Color/kind-specific bonuses and the chain-reaction bonus combine into a
        /// single multiplier; the red-rune artifact adds a flat physical bonus on top. The
        /// effect is unchanged when no relevant artifact is owned.
        /// </summary>
        public RuneEffect Amplify(RuneEffect effect)
        {
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if (IsEmpty)
            {
                return effect;
            }

            var multiplier = 1.0;
            var flatBonus = 0.0;

            switch (effect.Kind)
            {
                case RuneEffectKind.Healing:
                    multiplier += greenHealingBonus;
                    break;
                case RuneEffectKind.Mana:
                    multiplier += blueManaBonus;
                    break;
                case RuneEffectKind.Shield:
                    multiplier += yellowShieldBonus;
                    break;
                case RuneEffectKind.MagicDamage when effect.Rune == RuneType.Purple:
                    multiplier += purpleMagicBonus;
                    break;
                case RuneEffectKind.PhysicalDamage when effect.Rune == RuneType.Red:
                    flatBonus += redPhysicalFlatBonus;
                    break;
                case RuneEffectKind.CommanderEnergy when effect.Rune == RuneType.White:
                    multiplier += whiteEnergyBonus;
                    break;
            }

            if (effect.ChainNumber >= 2)
            {
                multiplier += chainReactionBonus;
            }

            if (multiplier == 1.0 && flatBonus == 0.0)
            {
                return effect;
            }

            return effect with { Power = (effect.Power * multiplier) + flatBonus };
        }

        public bool Equals(ArtifactRuneModifiers other)
        {
            return Math.Abs(greenHealingBonus - other.greenHealingBonus) < 1e-9
                && Math.Abs(blueManaBonus - other.blueManaBonus) < 1e-9
                && Math.Abs(redPhysicalFlatBonus - other.redPhysicalFlatBonus) < 1e-9
                && Math.Abs(yellowShieldBonus - other.yellowShieldBonus) < 1e-9
                && Math.Abs(purpleMagicBonus - other.purpleMagicBonus) < 1e-9
                && Math.Abs(whiteEnergyBonus - other.whiteEnergyBonus) < 1e-9
                && Math.Abs(chainReactionBonus - other.chainReactionBonus) < 1e-9;
        }

        public override bool Equals(object? obj)
        {
            return obj is ArtifactRuneModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                greenHealingBonus,
                blueManaBonus,
                redPhysicalFlatBonus,
                yellowShieldBonus,
                purpleMagicBonus,
                whiteEnergyBonus,
                chainReactionBonus);
        }
    }
}
