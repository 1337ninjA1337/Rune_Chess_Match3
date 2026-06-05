using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Rune-effect multipliers contributed by the rune artifacts a run currently owns
    /// (GDD P1 "артефакты как модификаторы ... рун"). Each owned rune artifact scales the
    /// matching colour's match-3 effect, and duplicates stack additively. Magnitudes live
    /// here as named constants per the codex data rule so balance changes never touch the
    /// battle logic. Combat-only and economy artifacts contribute nothing here; they are
    /// tracked by their own tasks. Applied inside <see cref="BattleState.ApplyRuneEffect"/>
    /// so the same match-3 → battle path that already honours synergies also honours the
    /// run's rune artifacts.
    /// </summary>
    public readonly struct ArtifactRuneModifiers : IEquatable<ArtifactRuneModifiers>
    {
        /// <summary>"Кровавый Кубок": green-rune healing bonus fraction.</summary>
        public const double BloodChaliceHealingBonus = 0.30;

        /// <summary>"Искровой Конденсатор": blue-rune mana bonus fraction.</summary>
        public const double SparkCapacitorManaBonus = 0.25;

        /// <summary>"Оберегающий Тотем": yellow-rune shield bonus fraction.</summary>
        public const double WardingTotemShieldBonus = 0.30;

        /// <summary>"Тлеющее Ядро": red-rune physical-damage bonus fraction.</summary>
        public const double EmberCorePhysicalBonus = 0.20;

        /// <summary>"Печать Бездны": purple-rune magic-damage bonus fraction.</summary>
        public const double AbyssalSigilMagicBonus = 0.30;

        /// <summary>"Призменная Линза": white-rune commander-energy bonus fraction.</summary>
        public const double PrismLensCommanderBonus = 0.30;

        /// <summary>"Проводник Цепей": chain-reaction (chain >= 2) bonus fraction for any colour.</summary>
        public const double ChainConduitChainBonus = 0.25;

        private readonly double redPhysicalMultiplier;
        private readonly double blueManaMultiplier;
        private readonly double greenHealingMultiplier;
        private readonly double yellowShieldMultiplier;
        private readonly double purpleMagicMultiplier;
        private readonly double whiteCommanderMultiplier;
        private readonly double chainBonusMultiplier;

        public ArtifactRuneModifiers(
            double redPhysicalMultiplier = 1.0,
            double blueManaMultiplier = 1.0,
            double greenHealingMultiplier = 1.0,
            double yellowShieldMultiplier = 1.0,
            double purpleMagicMultiplier = 1.0,
            double whiteCommanderMultiplier = 1.0,
            double chainBonusMultiplier = 1.0)
        {
            RequirePositive(redPhysicalMultiplier, nameof(redPhysicalMultiplier));
            RequirePositive(blueManaMultiplier, nameof(blueManaMultiplier));
            RequirePositive(greenHealingMultiplier, nameof(greenHealingMultiplier));
            RequirePositive(yellowShieldMultiplier, nameof(yellowShieldMultiplier));
            RequirePositive(purpleMagicMultiplier, nameof(purpleMagicMultiplier));
            RequirePositive(whiteCommanderMultiplier, nameof(whiteCommanderMultiplier));
            RequirePositive(chainBonusMultiplier, nameof(chainBonusMultiplier));

            this.redPhysicalMultiplier = redPhysicalMultiplier;
            this.blueManaMultiplier = blueManaMultiplier;
            this.greenHealingMultiplier = greenHealingMultiplier;
            this.yellowShieldMultiplier = yellowShieldMultiplier;
            this.purpleMagicMultiplier = purpleMagicMultiplier;
            this.whiteCommanderMultiplier = whiteCommanderMultiplier;
            this.chainBonusMultiplier = chainBonusMultiplier;
        }

        public double RedPhysicalMultiplier => redPhysicalMultiplier <= 0.0 ? 1.0 : redPhysicalMultiplier;
        public double BlueManaMultiplier => blueManaMultiplier <= 0.0 ? 1.0 : blueManaMultiplier;
        public double GreenHealingMultiplier => greenHealingMultiplier <= 0.0 ? 1.0 : greenHealingMultiplier;
        public double YellowShieldMultiplier => yellowShieldMultiplier <= 0.0 ? 1.0 : yellowShieldMultiplier;
        public double PurpleMagicMultiplier => purpleMagicMultiplier <= 0.0 ? 1.0 : purpleMagicMultiplier;
        public double WhiteCommanderMultiplier => whiteCommanderMultiplier <= 0.0 ? 1.0 : whiteCommanderMultiplier;
        public double ChainBonusMultiplier => chainBonusMultiplier <= 0.0 ? 1.0 : chainBonusMultiplier;

        /// <summary>The neutral set used when a run owns no rune artifacts.</summary>
        public static ArtifactRuneModifiers None { get; } = new();

        /// <summary>True when every modifier is neutral (no rune artifact owned).</summary>
        public bool IsNeutral => Equals(None);

        /// <summary>
        /// Aggregate the rune contributions of every owned artifact. Unknown ids and
        /// artifacts whose effect is combat or economy based contribute nothing here.
        /// Duplicates stack additively (two ember cores give twice the red bonus).
        /// </summary>
        public static ArtifactRuneModifiers From(IEnumerable<ArtifactState> artifacts)
        {
            if (artifacts is null)
            {
                throw new ArgumentNullException(nameof(artifacts));
            }

            var red = 1.0;
            var blue = 1.0;
            var green = 1.0;
            var yellow = 1.0;
            var purple = 1.0;
            var white = 1.0;
            var chain = 1.0;

            foreach (var artifact in artifacts)
            {
                switch (artifact.Id?.ToLowerInvariant())
                {
                    case "blood_chalice":
                        green += BloodChaliceHealingBonus;
                        break;
                    case "spark_capacitor":
                        blue += SparkCapacitorManaBonus;
                        break;
                    case "warding_totem":
                        yellow += WardingTotemShieldBonus;
                        break;
                    case "ember_core":
                        red += EmberCorePhysicalBonus;
                        break;
                    case "abyssal_sigil":
                        purple += AbyssalSigilMagicBonus;
                        break;
                    case "prism_lens":
                        white += PrismLensCommanderBonus;
                        break;
                    case "chain_conduit":
                        chain += ChainConduitChainBonus;
                        break;
                }
            }

            return new ArtifactRuneModifiers(red, blue, green, yellow, purple, white, chain);
        }

        /// <summary>
        /// Scale a resolved rune effect's power by the owning run's rune artifacts: the
        /// colour multiplier for the effect's kind, and the chain-conduit bonus when the
        /// effect comes from a chain reaction (chain number two or higher).
        /// </summary>
        public RuneEffect Apply(RuneEffect effect)
        {
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            var multiplier = ColorMultiplier(effect.Kind);
            if (effect.ChainNumber >= 2)
            {
                multiplier *= ChainBonusMultiplier;
            }

            return Math.Abs(multiplier - 1.0) < 1e-9
                ? effect
                : effect with { Power = effect.Power * multiplier };
        }

        private double ColorMultiplier(RuneEffectKind kind)
        {
            return kind switch
            {
                RuneEffectKind.PhysicalDamage => RedPhysicalMultiplier,
                RuneEffectKind.Mana => BlueManaMultiplier,
                RuneEffectKind.Healing => GreenHealingMultiplier,
                RuneEffectKind.Shield => YellowShieldMultiplier,
                RuneEffectKind.MagicDamage => PurpleMagicMultiplier,
                RuneEffectKind.CommanderEnergy => WhiteCommanderMultiplier,
                _ => 1.0
            };
        }

        public bool Equals(ArtifactRuneModifiers other)
        {
            return Math.Abs(RedPhysicalMultiplier - other.RedPhysicalMultiplier) < 1e-9
                && Math.Abs(BlueManaMultiplier - other.BlueManaMultiplier) < 1e-9
                && Math.Abs(GreenHealingMultiplier - other.GreenHealingMultiplier) < 1e-9
                && Math.Abs(YellowShieldMultiplier - other.YellowShieldMultiplier) < 1e-9
                && Math.Abs(PurpleMagicMultiplier - other.PurpleMagicMultiplier) < 1e-9
                && Math.Abs(WhiteCommanderMultiplier - other.WhiteCommanderMultiplier) < 1e-9
                && Math.Abs(ChainBonusMultiplier - other.ChainBonusMultiplier) < 1e-9;
        }

        public override bool Equals(object? obj)
        {
            return obj is ArtifactRuneModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                RedPhysicalMultiplier,
                BlueManaMultiplier,
                GreenHealingMultiplier,
                YellowShieldMultiplier,
                PurpleMagicMultiplier,
                WhiteCommanderMultiplier,
                ChainBonusMultiplier);
        }

        private static void RequirePositive(double value, string parameterName)
        {
            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Rune artifact multiplier must be positive.");
            }
        }
    }
}
