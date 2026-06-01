namespace RuneChess.Core;

/// <summary>
/// Base (one-star) combat stats for a hero. Star scaling applies the GDD star
/// multiplier to health and attack; defensive, speed, and mana values stay flat
/// in the MVP so balance is easy to read and extend later.
/// </summary>
public sealed record HeroStats(
    double BaseHealth,
    double Attack,
    double Armor,
    double MagicResist,
    double BaseAttackSpeed,
    double ManaMax
)
{
    public HeroStats ScaledByStars(int stars)
    {
        var multiplier = CombatFormulas.GetStarMultiplier(stars);
        return this with
        {
            BaseHealth = BaseHealth * multiplier,
            Attack = Attack * multiplier
        };
    }
}
