namespace RuneChess.Core
{
    public sealed record HeroDefinition(
        string Id,
        string Name,
        HeroRarity Rarity,
        int Cost,
        string Faction,
        string Class,
        RuneType RuneAffinity,
        HeroRole Role,
        string AttackType,
        string Targeting,
        int Stars,
        string Ability,
        string Passive,
        HeroStats BaseStats
    )
    {
        /// <summary>
        /// How readable the hero's ability is for a new player (GDD onboarding: "стартовые
        /// герои должны иметь простые способности"). Defaults to <see cref="AbilityComplexity.Simple"/>;
        /// heroes whose ability adds positioning, control, debuffs or board-wide tricks set this
        /// to <see cref="AbilityComplexity.Advanced"/> in <see cref="HeroCatalog"/>.
        /// </summary>
        public AbilityComplexity AbilityComplexity { get; init; } = AbilityComplexity.Simple;

        /// <summary>True when the hero's ability is beginner-friendly (a single, obvious effect).</summary>
        public bool HasSimpleAbility => AbilityComplexity == AbilityComplexity.Simple;

        /// <summary>Match-3 effect category linked to the hero's preferred rune color.</summary>
        public RuneEffectKind PreferredEffectKind => RuneEffects.GetEffectKind(RuneAffinity);

        public HeroAbility AbilityForStars(int stars)
        {
            return HeroAbilities.ForHero(this, StatsForStars(stars));
        }

        public HeroPassive PassiveForStars(int stars)
        {
            _ = StatsForStars(stars);
            return HeroPassives.ForHero(this);
        }

        public HeroStats StatsForStars(int stars)
        {
            return BaseStats.ScaledByStars(stars);
        }
    }
}
