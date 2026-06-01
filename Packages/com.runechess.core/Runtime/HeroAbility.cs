using System;

namespace RuneChess.Core
{
    public enum HeroAbilityKind
    {
        None,
        PhysicalDamage,
        MagicDamage,
        Healing,
        Shield
    }

    public readonly struct HeroAbility : IEquatable<HeroAbility>
    {
        public HeroAbility(HeroAbilityKind kind, double power)
        {
            if (power < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(power), "Ability power cannot be negative.");
            }

            Kind = kind;
            Power = power;
        }

        public HeroAbilityKind Kind { get; }
        public double Power { get; }
        public bool HasEffect => Kind != HeroAbilityKind.None && Power > 0.0;

        public static HeroAbility None { get; } = new(HeroAbilityKind.None, 0.0);

        public bool Equals(HeroAbility other)
        {
            return Kind == other.Kind && Math.Abs(Power - other.Power) < 1e-9;
        }

        public override bool Equals(object? obj)
        {
            return obj is HeroAbility other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ Power.GetHashCode();
            }
        }
    }

    public static class HeroAbilities
    {
        public static HeroAbility ForHero(HeroDefinition definition, HeroStats stats)
        {
            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return definition.Role switch
            {
                HeroRole.Healer or HeroRole.Support => new HeroAbility(
                    HeroAbilityKind.Healing,
                    Math.Max(10.0, stats.BaseHealth * 0.25)
                ),
                HeroRole.Tank => new HeroAbility(
                    HeroAbilityKind.Shield,
                    Math.Max(10.0, stats.BaseHealth * 0.2)
                ),
                HeroRole.Caster or HeroRole.Summoner => new HeroAbility(
                    HeroAbilityKind.MagicDamage,
                    Math.Max(10.0, stats.Attack * 2.0)
                ),
                HeroRole.Carry or HeroRole.Assassin or HeroRole.Bruiser => new HeroAbility(
                    HeroAbilityKind.PhysicalDamage,
                    Math.Max(10.0, stats.Attack * 1.5)
                ),
                _ => HeroAbility.None
            };
        }
    }
}
