using System;

namespace RuneChess.Core
{
    public enum HeroPassiveKind
    {
        None,
        FrontlineGuard,
        BonusHealth,
        BonusAttack,
        OpeningMana,
        BonusMagicResist
    }

    public readonly struct HeroPassive : IEquatable<HeroPassive>
    {
        public HeroPassive(HeroPassiveKind kind, double power)
        {
            if (power < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(power), "Passive power cannot be negative.");
            }

            Kind = kind;
            Power = power;
        }

        public HeroPassiveKind Kind { get; }
        public double Power { get; }
        public bool HasEffect => Kind != HeroPassiveKind.None && Power > 0.0;

        public static HeroPassive None { get; } = new(HeroPassiveKind.None, 0.0);

        public bool Equals(HeroPassive other)
        {
            return Kind == other.Kind && Math.Abs(Power - other.Power) < 1e-9;
        }

        public override bool Equals(object? obj)
        {
            return obj is HeroPassive other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ Power.GetHashCode();
            }
        }
    }

    public static class HeroPassives
    {
        public const double FrontlineGuardBonus = 0.25;
        public const double BruiserHealthBonus = 0.12;
        public const double DamageRoleAttackBonus = 0.15;
        public const double CasterOpeningManaFraction = 0.20;
        public const double SupportMagicResistBonus = 0.20;

        public static HeroPassive ForHero(HeroDefinition definition)
        {
            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return definition.Role switch
            {
                HeroRole.Tank => new HeroPassive(HeroPassiveKind.FrontlineGuard, FrontlineGuardBonus),
                HeroRole.Bruiser => new HeroPassive(HeroPassiveKind.BonusHealth, BruiserHealthBonus),
                HeroRole.Carry or HeroRole.Assassin => new HeroPassive(HeroPassiveKind.BonusAttack, DamageRoleAttackBonus),
                HeroRole.Caster or HeroRole.Summoner => new HeroPassive(HeroPassiveKind.OpeningMana, CasterOpeningManaFraction),
                HeroRole.Healer or HeroRole.Support => new HeroPassive(HeroPassiveKind.BonusMagicResist, SupportMagicResistBonus),
                _ => HeroPassive.None
            };
        }

        public static HeroStats ApplyToStats(HeroStats stats, HeroPassive passive, TacticalPosition position)
        {
            if (stats is null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (!passive.HasEffect)
            {
                return stats;
            }

            return passive.Kind switch
            {
                HeroPassiveKind.FrontlineGuard when position.IsInsideMvpField && position.IsFrontline => stats with
                {
                    Armor = stats.Armor * (1.0 + passive.Power),
                    MagicResist = stats.MagicResist * (1.0 + passive.Power)
                },
                HeroPassiveKind.FrontlineGuard => stats,
                HeroPassiveKind.BonusHealth => stats with
                {
                    BaseHealth = stats.BaseHealth * (1.0 + passive.Power)
                },
                HeroPassiveKind.BonusAttack => stats with
                {
                    Attack = stats.Attack * (1.0 + passive.Power)
                },
                HeroPassiveKind.OpeningMana => stats,
                HeroPassiveKind.BonusMagicResist => stats with
                {
                    MagicResist = stats.MagicResist * (1.0 + passive.Power)
                },
                HeroPassiveKind.None => stats,
                _ => throw new ArgumentOutOfRangeException(nameof(passive), passive.Kind, "Unknown hero passive kind.")
            };
        }

        public static double CalculateStartingMana(HeroStats stats, HeroPassive passive)
        {
            if (stats is null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (passive.Kind != HeroPassiveKind.OpeningMana || passive.Power <= 0.0 || stats.ManaMax <= 0.0)
            {
                return 0.0;
            }

            return Math.Min(stats.ManaMax, stats.ManaMax * passive.Power);
        }
    }
}
