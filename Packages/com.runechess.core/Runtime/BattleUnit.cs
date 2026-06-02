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
    HeroAbility ActiveAbility = default,
    HeroPassive PassiveEffect = default,
    double LifestealFraction = 0.0,
    int LifestealMillisecondsRemaining = 0,
    double WeaknessAttackPenaltyFraction = 0.0,
    int WeaknessMillisecondsRemaining = 0
)
{
    public bool IsAlive => CurrentHealth > 0.0;
    public bool IsRanged => AttackType == BattleAttackType.Ranged;
    public bool HasActiveLifesteal => LifestealFraction > 0.0 && LifestealMillisecondsRemaining > 0;
    public bool HasActiveWeakness => WeaknessAttackPenaltyFraction > 0.0 && WeaknessMillisecondsRemaining > 0;
    public double EffectiveAttack => Attack * (HasActiveWeakness ? 1.0 - WeaknessAttackPenaltyFraction : 1.0);
    public double HealthPercent => MaxHealth <= 0.0 ? 0.0 : CurrentHealth / MaxHealth;
    public double AttackInterval => CombatFormulas.CalculateAttackInterval(AttacksPerSecond);

    /// <summary>Builds a ready-to-fight unit from a hero definition at the given star level.</summary>
    public static BattleUnit FromHero(
        HeroDefinition definition,
        int stars,
        string unitId,
        TacticalSide side,
        TacticalPosition position,
        SynergyModifiers synergyModifiers = default)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("Unit id is required.", nameof(unitId));
        }

        var baseStats = definition.StatsForStars(stars);
        var passive = definition.PassiveForStars(stars);
        var stats = synergyModifiers.ApplyToStats(HeroPassives.ApplyToStats(baseStats, passive, position));
        var attacksPerSecond = CombatFormulas.CalculateAttacksPerSecond(stats.BaseAttackSpeed);
        var startingMana = HeroPassives.CalculateStartingMana(stats, passive);

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
            CurrentMana: startingMana,
            ManaMax: stats.ManaMax,
            Shield: 0.0,
            AttackType: BattleAttackTypes.FromId(definition.AttackType),
            AttackCooldownRemaining: CombatFormulas.CalculateAttackInterval(attacksPerSecond),
            AbilitiesCast: 0,
            ActiveAbility: definition.AbilityForStars(stars),
            PassiveEffect: passive
        );
    }

    public static BattleUnit FromBoardHero(
        BoardHero boardHero,
        TacticalSide side,
        SynergyModifiers synergyModifiers = default)
    {
        if (boardHero is null)
        {
            throw new ArgumentNullException(nameof(boardHero));
        }

        var definition = HeroCatalog.Get(boardHero.Hero.HeroId);
        return FromHero(
            definition,
            boardHero.Hero.Stars,
            boardHero.Hero.InstanceId,
            side,
            boardHero.Position,
            synergyModifiers);
    }
}
}
