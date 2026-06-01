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
