using System;

namespace RuneChess.Core
{

/// <summary>
/// A unit participating in the autobattle. Immutable: combat updates produce new
/// instances. Stats are already star-scaled at construction time.
/// </summary>
public sealed record BattleUnit(
    string UnitId,
    TacticalSide Side,
    TacticalPosition Position,
    double MaxHealth,
    double CurrentHealth,
    double Attack,
    double Armor,
    double MagicResist,
    double AttacksPerSecond,
    double CurrentMana,
    double ManaMax,
    double Shield,
    BattleAttackType AttackType,
    double AttackCooldownRemaining,
    int AbilitiesCast,
    HeroAbility ActiveAbility = default
)
{
    public bool IsAlive => CurrentHealth > 0.0;
    public bool IsRanged => AttackType == BattleAttackType.Ranged;
    public double HealthPercent => MaxHealth <= 0.0 ? 0.0 : CurrentHealth / MaxHealth;
    public double AttackInterval => CombatFormulas.CalculateAttackInterval(AttacksPerSecond);

    /// <summary>Builds a ready-to-fight unit from a hero definition at the given star level.</summary>
    public static BattleUnit FromHero(
        HeroDefinition definition,
        int stars,
        string unitId,
        TacticalSide side,
        TacticalPosition position)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("Unit id is required.", nameof(unitId));
        }

        var stats = definition.StatsForStars(stars);
        var attacksPerSecond = CombatFormulas.CalculateAttacksPerSecond(stats.BaseAttackSpeed);

        return new BattleUnit(
            UnitId: unitId,
            Side: side,
            Position: position,
            MaxHealth: stats.BaseHealth,
            CurrentHealth: stats.BaseHealth,
            Attack: stats.Attack,
            Armor: stats.Armor,
            MagicResist: stats.MagicResist,
            AttacksPerSecond: attacksPerSecond,
            CurrentMana: 0.0,
            ManaMax: stats.ManaMax,
            Shield: 0.0,
            AttackType: BattleAttackTypes.FromId(definition.AttackType),
            AttackCooldownRemaining: CombatFormulas.CalculateAttackInterval(attacksPerSecond),
            AbilitiesCast: 0,
            ActiveAbility: definition.AbilityForStars(stars)
        );
    }
}
}
