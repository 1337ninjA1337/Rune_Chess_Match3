namespace RuneChess.Core;

public sealed record HeroDefinition(
    string Id,
    string Name,
    string Rarity,
    int Cost,
    string Faction,
    string Class,
    RuneType RuneAffinity,
    HeroRole Role,
    string AttackType,
    string Targeting,
    int Stars,
    string Ability,
    string Passive
);
