using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{

/// <summary>
/// Deterministic MVP autobattle. A <see cref="Tick"/> advances the fight by a time
/// slice: units count down their attack cooldowns, pick the nearest enemy, move (melee)
/// or attack (physical), gain mana from attacking and from damage taken, and auto-cast
/// when full. Crit rolls are intentionally left out so the simulation is a pure function
/// of its inputs; <see cref="CombatFormulas"/> already provides the crit math for later.
/// </summary>
public sealed record BattleState(
    IReadOnlyList<BattleUnit> Units,
    double ElapsedSeconds,
    double DurationSeconds,
    BattleOutcome Outcome,
    double CommanderEnergy = 0.0,
    SynergyModifiers PlayerSynergyModifiers = default,
    SynergyModifiers EnemySynergyModifiers = default
)
{
    public const double DefaultDurationSeconds = 60.0;

    public IEnumerable<BattleUnit> AliveUnits => Units.Where(unit => unit.IsAlive);
    public IEnumerable<BattleUnit> AliveAllies => AliveUnits.Where(unit => unit.Side == TacticalSide.Player);
    public IEnumerable<BattleUnit> AliveEnemies => AliveUnits.Where(unit => unit.Side == TacticalSide.Enemy);
    public bool IsResolved => Outcome != BattleOutcome.Ongoing;
    public double RemainingSeconds => Math.Max(0.0, DurationSeconds - ElapsedSeconds);

    public static BattleState Create(
        IReadOnlyList<BattleUnit> units,
        double durationSeconds = DefaultDurationSeconds,
        SynergyModifiers playerSynergyModifiers = default,
        SynergyModifiers enemySynergyModifiers = default)
    {
        if (units is null)
        {
            throw new ArgumentNullException(nameof(units));
        }

        if (durationSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Battle duration must be positive.");
        }

        return new BattleState(
            units.ToList(),
            0.0,
            durationSeconds,
            BattleOutcome.Ongoing,
            CommanderEnergy: 0.0,
            PlayerSynergyModifiers: playerSynergyModifiers,
            EnemySynergyModifiers: enemySynergyModifiers);
    }

    /// <summary>Sum of current health fractions across the units that started on a side (dead units count as 0).</summary>
    public double TotalHealthPercent(TacticalSide side)
    {
        return Units.Where(unit => unit.Side == side).Sum(unit => unit.HealthPercent);
    }

    public BattleState Tick(double deltaSeconds)
    {
        if (deltaSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Tick delta must be positive.");
        }

        if (IsResolved)
        {
            return this;
        }

        var units = Units.ToList();
        var elapsedMilliseconds = SecondsToMilliseconds(deltaSeconds);

        for (var i = 0; i < units.Count; i += 1)
        {
            if (units[i].IsAlive)
            {
                units[i] = AdvanceTimedBuffs(units[i], elapsedMilliseconds) with
                {
                    AttackCooldownRemaining = Math.Max(0.0, units[i].AttackCooldownRemaining - deltaSeconds)
                };
            }
        }

        var order = Enumerable.Range(0, units.Count)
            .OrderBy(index => units[index].Side)
            .ThenBy(index => units[index].Position.Row)
            .ThenBy(index => units[index].Position.Column)
            .ToList();

        foreach (var attackerIndex in order)
        {
            var attacker = units[attackerIndex];
            if (!attacker.IsAlive || attacker.AttackCooldownRemaining > 0.0)
            {
                continue;
            }

            var targetIndex = SelectTargetIndex(units, attacker);
            if (targetIndex < 0)
            {
                continue;
            }

            var target = units[targetIndex];

            if (attacker.AttackType == BattleAttackType.Melee && !IsAdjacent(attacker.Position, target.Position))
            {
                var nextPosition = StepToward(attacker.Position, target.Position, units, attackerIndex);
                if (!nextPosition.Equals(attacker.Position))
                {
                    units[attackerIndex] = attacker with { Position = nextPosition };
                }

                continue;
            }

            var rawDamage = CombatFormulas.CalculatePhysicalDamage(attacker.EffectiveAttack, target.Armor);
            var absorbed = CombatFormulas.DamageAfterShield(rawDamage, target.Shield);
            var remainingShield = CombatFormulas.ShieldAfterDamage(target.Shield, rawDamage);
            var newHealth = Math.Max(0.0, target.CurrentHealth - absorbed);
            var targetMana = Math.Min(
                target.ManaMax,
                target.CurrentMana + CombatFormulas.CalculateManaFromDamageTaken(absorbed, target.MaxHealth));

            units[targetIndex] = target with
            {
                CurrentHealth = newHealth,
                Shield = remainingShield,
                CurrentMana = targetMana
            };
            TryCastAbility(units, targetIndex, ModifiersForSide(target.Side), ModifiersForSide(attacker.Side));

            var updatedAttacker = units[attackerIndex];
            if (!updatedAttacker.IsAlive)
            {
                continue;
            }

            var lifestealHealing = updatedAttacker.HasActiveLifesteal
                ? absorbed * updatedAttacker.LifestealFraction
                : 0.0;

            units[attackerIndex] = updatedAttacker with
            {
                CurrentHealth = lifestealHealing > 0.0
                    ? CombatFormulas.ApplyHealing(updatedAttacker.CurrentHealth, lifestealHealing, updatedAttacker.MaxHealth)
                    : updatedAttacker.CurrentHealth,
                CurrentMana = Math.Min(updatedAttacker.ManaMax, updatedAttacker.CurrentMana + CombatFormulas.ManaFromAttack),
                AttackCooldownRemaining = updatedAttacker.AttackInterval
            };
            TryCastAbility(units, attackerIndex, ModifiersForSide(updatedAttacker.Side), ModifiersForSide(target.Side));
        }

        var elapsed = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
        return new BattleState(
            units,
            elapsed,
            DurationSeconds,
            ResolveOutcome(units, elapsed, DurationSeconds),
            CommanderEnergy,
            PlayerSynergyModifiers,
            EnemySynergyModifiers);
    }

    /// <summary>
    /// Applies resolved rune effects (from <see cref="RuneEffectResolver"/>) to the battle:
    /// damage hits the opposing side, healing/shield/mana support the casting side, and
    /// white runes plus T/L bonuses accrue commander energy. The casting side is the player
    /// by default. Mass effects (T/L combos) hit every relevant unit; otherwise a single
    /// focused target is chosen.
    /// </summary>
    public BattleState ApplyRuneEffects(
        IEnumerable<RuneEffect> effects,
        TacticalSide casterSide = TacticalSide.Player,
        SynergyModifiers synergyModifiers = default)
    {
        if (effects is null)
        {
            throw new ArgumentNullException(nameof(effects));
        }

        var state = this;
        foreach (var effect in effects)
        {
            state = state.ApplyRuneEffect(effect, casterSide, synergyModifiers);
        }

        return state;
    }

    public BattleState ApplyRuneEffect(
        RuneEffect effect,
        TacticalSide casterSide = TacticalSide.Player,
        SynergyModifiers synergyModifiers = default)
    {
        if (effect is null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        if (IsResolved)
        {
            return this;
        }

        var units = Units.ToList();
        var enemySide = casterSide == TacticalSide.Player ? TacticalSide.Enemy : TacticalSide.Player;
        var commanderGain = (double)effect.CommanderEnergy;
        var casterSynergyModifiers = ResolveSynergyModifiers(casterSide, synergyModifiers);
        var enemySynergyModifiers = ModifiersForSide(enemySide);
        var combatEffect = ApplyAbyssalPurpleRuneBonus(effect, casterSynergyModifiers);

        switch (combatEffect.Kind)
        {
            case RuneEffectKind.PhysicalDamage:
                ApplyDamage(units, enemySide, combatEffect, physical: true, enemySynergyModifiers, casterSynergyModifiers);
                break;
            case RuneEffectKind.MagicDamage:
                ApplyDamage(units, enemySide, combatEffect, physical: false, enemySynergyModifiers, casterSynergyModifiers);
                break;
            case RuneEffectKind.Healing:
                ApplyRuneHealing(units, casterSide, combatEffect);
                break;
            case RuneEffectKind.Shield:
                ApplyRuneShield(units, casterSide, combatEffect, casterSynergyModifiers);
                break;
            case RuneEffectKind.Mana:
                ApplyRuneMana(units, casterSide, combatEffect.Power, combatEffect.IsMassEffect, casterSynergyModifiers, enemySynergyModifiers);
                break;
            case RuneEffectKind.CommanderEnergy:
                commanderGain += combatEffect.Power;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect), combatEffect.Kind, "Unknown rune effect kind.");
        }

        ApplyWildChainLifesteal(units, casterSide, combatEffect, casterSynergyModifiers);

        var outcome = ResolveOutcome(units, ElapsedSeconds, DurationSeconds);
        return new BattleState(
            units,
            ElapsedSeconds,
            DurationSeconds,
            outcome,
            CommanderEnergy + commanderGain,
            PlayerSynergyModifiers,
            EnemySynergyModifiers);
    }

    /// <summary>Grants blue-rune mana (matchedBlueRunes * 8) to a side's frontmost living unit.</summary>
    public BattleState AddManaFromBlueRunes(TacticalSide side, int matchedBlueRunes)
    {
        if (matchedBlueRunes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matchedBlueRunes), "Matched blue rune count cannot be negative.");
        }

        if (IsResolved || matchedBlueRunes == 0)
        {
            return this;
        }

        var mana = CombatFormulas.CalculateManaFromBlueRunes(matchedBlueRunes);
        var units = Units.ToList();

        var targetIndex = -1;
        for (var i = 0; i < units.Count; i += 1)
        {
            if (!units[i].IsAlive || units[i].Side != side)
            {
                continue;
            }

            if (targetIndex < 0 || ComparePosition(units[i].Position, units[targetIndex].Position) < 0)
            {
                targetIndex = i;
            }
        }

        if (targetIndex < 0)
        {
            return this;
        }

        var unit = units[targetIndex];
        units[targetIndex] = unit with
        {
            CurrentMana = Math.Min(unit.ManaMax, unit.CurrentMana + mana)
        };
        TryCastAbility(units, targetIndex, ModifiersForSide(side), ModifiersForSide(OpposingSide(side)));

        return this with { Units = units, Outcome = ResolveOutcome(units, ElapsedSeconds, DurationSeconds) };
    }

    private static void TryCastAbility(
        List<BattleUnit> units,
        int casterIndex,
        SynergyModifiers casterSynergyModifiers,
        SynergyModifiers opposingSynergyModifiers)
    {
        var caster = units[casterIndex];
        if (!caster.IsAlive || caster.ManaMax <= 0.0 || !CombatFormulas.IsAbilityReady(caster.CurrentMana, caster.ManaMax))
        {
            return;
        }

        caster = caster with
        {
            CurrentMana = 0.0,
            AbilitiesCast = caster.AbilitiesCast + 1
        };
        units[casterIndex] = caster;

        if (caster.ActiveAbility.HasEffect)
        {
            ApplyHeroAbility(units, caster, casterSynergyModifiers, opposingSynergyModifiers);
        }
    }

    private static void ApplyHeroAbility(
        List<BattleUnit> units,
        BattleUnit caster,
        SynergyModifiers casterSynergyModifiers,
        SynergyModifiers opposingSynergyModifiers)
    {
        var enemySide = OpposingSide(caster.Side);
        var ability = caster.ActiveAbility;
        List<int> debuffTargets;

        switch (ability.Kind)
        {
            case HeroAbilityKind.None:
                return;
            case HeroAbilityKind.PhysicalDamage:
                debuffTargets = ApplyDamage(
                    units,
                    enemySide,
                    AbilityEffect(RuneEffectKind.PhysicalDamage, ability.Power),
                    physical: true,
                    opposingSynergyModifiers,
                    casterSynergyModifiers);
                ApplyAbyssalWeakness(units, debuffTargets, casterSynergyModifiers);
                return;
            case HeroAbilityKind.MagicDamage:
                debuffTargets = ApplyDamage(
                    units,
                    enemySide,
                    AbilityEffect(RuneEffectKind.MagicDamage, ability.Power),
                    physical: false,
                    opposingSynergyModifiers,
                    casterSynergyModifiers);
                ApplyAbyssalWeakness(units, debuffTargets, casterSynergyModifiers);
                return;
            case HeroAbilityKind.Healing:
                ApplyRuneHealing(units, caster.Side, AbilityEffect(RuneEffectKind.Healing, ability.Power));
                ApplyAbyssalWeakness(units, SingleBy(units, enemySide, unit => unit.CurrentHealth), casterSynergyModifiers);
                return;
            case HeroAbilityKind.Shield:
                ApplyRuneShield(units, caster.Side, AbilityEffect(RuneEffectKind.Shield, ability.Power), casterSynergyModifiers);
                ApplyAbyssalWeakness(units, SingleBy(units, enemySide, unit => unit.CurrentHealth), casterSynergyModifiers);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(caster), ability.Kind, "Unknown hero ability kind.");
        }
    }

    private static RuneEffect AbilityEffect(RuneEffectKind kind, double power)
    {
        return new RuneEffect(
            Rune: RuneType.White,
            Kind: kind,
            Tier: RuneMatchTier.Match3,
            MatchedRunesCount: 3,
            ChainNumber: 1,
            IsMassEffect: false,
            ChargesHero: false,
            CreatesGreatRune: false,
            IsGreatRuneActivation: false,
            CommanderEnergy: 0,
            Power: power
        );
    }

    private static RuneEffect ApplyAbyssalPurpleRuneBonus(RuneEffect effect, SynergyModifiers synergyModifiers)
    {
        if (!synergyModifiers.AbyssalPurpleRuneBonusDamage
            || effect.Rune != RuneType.Purple
            || effect.Kind != RuneEffectKind.MagicDamage)
        {
            return effect;
        }

        return effect with
        {
            Power = effect.Power * (1.0 + SynergyModifiers.AbyssalPurpleRuneDamageBonus)
        };
    }

    private static int SelectTargetIndex(IReadOnlyList<BattleUnit> units, BattleUnit attacker)
    {
        var best = -1;
        var bestDistance = int.MaxValue;

        for (var i = 0; i < units.Count; i += 1)
        {
            var candidate = units[i];
            if (!candidate.IsAlive || candidate.Side == attacker.Side)
            {
                continue;
            }

            var distance = ManhattanDistance(attacker.Position, candidate.Position);
            if (distance < bestDistance
                || (distance == bestDistance && best >= 0 && ComparePosition(candidate.Position, units[best].Position) < 0))
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static TacticalPosition StepToward(
        TacticalPosition from,
        TacticalPosition to,
        IReadOnlyList<BattleUnit> units,
        int movingIndex)
    {
        var rowStep = Math.Sign(to.Row - from.Row);
        if (rowStep != 0)
        {
            var candidate = new TacticalPosition(from.Row + rowStep, from.Column);
            if (CanMoveInto(candidate, units, movingIndex))
            {
                return candidate;
            }
        }

        var columnStep = Math.Sign(to.Column - from.Column);
        if (columnStep != 0)
        {
            var candidate = new TacticalPosition(from.Row, from.Column + columnStep);
            if (CanMoveInto(candidate, units, movingIndex))
            {
                return candidate;
            }
        }

        return from;
    }

    private static bool CanMoveInto(TacticalPosition position, IReadOnlyList<BattleUnit> units, int movingIndex)
    {
        if (!TacticalField.Mvp.Contains(position))
        {
            return false;
        }

        for (var i = 0; i < units.Count; i += 1)
        {
            if (i != movingIndex && units[i].IsAlive && units[i].Position.Equals(position))
            {
                return false;
            }
        }

        return true;
    }

    private static BattleOutcome ResolveOutcome(IReadOnlyList<BattleUnit> units, double elapsed, double duration)
    {
        var anyEnemyAlive = units.Any(unit => unit.IsAlive && unit.Side == TacticalSide.Enemy);
        var anyAllyAlive = units.Any(unit => unit.IsAlive && unit.Side == TacticalSide.Player);

        if (!anyAllyAlive)
        {
            return BattleOutcome.PlayerDefeat;
        }

        if (!anyEnemyAlive)
        {
            return BattleOutcome.PlayerVictory;
        }

        if (elapsed >= duration)
        {
            var enemyHealth = SumHealthPercent(units, TacticalSide.Enemy);
            var allyHealth = SumHealthPercent(units, TacticalSide.Player);
            return enemyHealth > allyHealth ? BattleOutcome.PlayerDefeat : BattleOutcome.PlayerVictory;
        }

        return BattleOutcome.Ongoing;
    }

    private static double SumHealthPercent(IReadOnlyList<BattleUnit> units, TacticalSide side)
    {
        return units.Where(unit => unit.Side == side).Sum(unit => unit.HealthPercent);
    }

    private static List<int> ApplyDamage(
        List<BattleUnit> units,
        TacticalSide enemySide,
        RuneEffect effect,
        bool physical,
        SynergyModifiers damagedSideSynergyModifiers,
        SynergyModifiers opposingSynergyModifiers)
    {
        var targets = effect.IsMassEffect
            ? AliveIndices(units, enemySide)
            : SingleBy(units, enemySide, unit => unit.CurrentHealth);

        foreach (var index in targets)
        {
            var target = units[index];
            var rawDamage = physical
                ? CombatFormulas.CalculatePhysicalDamage(effect.Power, target.Armor)
                : CombatFormulas.CalculateMagicDamage(effect.Power, target.MagicResist);
            var absorbed = CombatFormulas.DamageAfterShield(rawDamage, target.Shield);
            var remainingShield = CombatFormulas.ShieldAfterDamage(target.Shield, rawDamage);
            var newHealth = Math.Max(0.0, target.CurrentHealth - absorbed);
            var mana = Math.Min(
                target.ManaMax,
                target.CurrentMana + CombatFormulas.CalculateManaFromDamageTaken(absorbed, target.MaxHealth));

            units[index] = target with
            {
                CurrentHealth = newHealth,
                Shield = remainingShield,
                CurrentMana = mana
            };
            TryCastAbility(units, index, damagedSideSynergyModifiers, opposingSynergyModifiers);
        }

        return targets;
    }

    private static void ApplyRuneHealing(List<BattleUnit> units, TacticalSide side, RuneEffect effect)
    {
        var targets = effect.IsMassEffect
            ? AliveIndices(units, side)
            : SingleBy(units, side, unit => unit.HealthPercent, unit => unit.HealthPercent < 1.0);

        foreach (var index in targets)
        {
            var unit = units[index];
            var healing = CombatFormulas.CalculateFinalHealing(effect.Power);
            units[index] = unit with
            {
                CurrentHealth = CombatFormulas.ApplyHealing(unit.CurrentHealth, healing, unit.MaxHealth)
            };
        }
    }

    private static void ApplyRuneShield(
        List<BattleUnit> units,
        TacticalSide side,
        RuneEffect effect,
        SynergyModifiers synergyModifiers)
    {
        var targets = ShouldShieldFrontline(effect, synergyModifiers)
            ? FrontlineIndices(units, side)
            : effect.IsMassEffect
            ? AliveIndices(units, side)
            : SingleBy(units, side, unit => side == TacticalSide.Player ? unit.Position.Row : -unit.Position.Row);

        foreach (var index in targets)
        {
            var unit = units[index];
            units[index] = unit with
            {
                Shield = CombatFormulas.CapShield(unit.Shield + effect.Power, unit.MaxHealth)
            };
        }
    }

    private static bool ShouldShieldFrontline(RuneEffect effect, SynergyModifiers synergyModifiers)
    {
        return !effect.IsMassEffect
            && effect.Rune == RuneType.Yellow
            && synergyModifiers.EmpireYellowRuneFrontlineShield;
    }

    private static List<int> FrontlineIndices(List<BattleUnit> units, TacticalSide side)
    {
        var indices = new List<int>();
        for (var i = 0; i < units.Count; i += 1)
        {
            if (units[i].IsAlive && units[i].Side == side && units[i].Position.IsFrontline)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private static void ApplyWildChainLifesteal(
        List<BattleUnit> units,
        TacticalSide side,
        RuneEffect effect,
        SynergyModifiers synergyModifiers)
    {
        if (!synergyModifiers.WildChainReactionLifesteal || effect.ChainNumber < 2)
        {
            return;
        }

        foreach (var index in AliveIndices(units, side))
        {
            var unit = units[index];
            units[index] = unit with
            {
                LifestealFraction = Math.Max(unit.LifestealFraction, SynergyModifiers.WildChainLifestealFraction),
                LifestealMillisecondsRemaining = SynergyModifiers.WildChainLifestealDurationMilliseconds
            };
        }
    }

    private static void ApplyAbyssalWeakness(
        List<BattleUnit> units,
        IEnumerable<int> targetIndices,
        SynergyModifiers synergyModifiers)
    {
        if (!synergyModifiers.AbyssalAbilityWeakness)
        {
            return;
        }

        foreach (var index in targetIndices)
        {
            var unit = units[index];
            if (!unit.IsAlive)
            {
                continue;
            }

            units[index] = unit with
            {
                WeaknessAttackPenaltyFraction = Math.Max(
                    unit.WeaknessAttackPenaltyFraction,
                    SynergyModifiers.AbyssalAbilityWeaknessAttackPenalty),
                WeaknessMillisecondsRemaining = SynergyModifiers.AbyssalAbilityWeaknessDurationMilliseconds
            };
        }
    }

    private static void ApplyRuneMana(
        List<BattleUnit> units,
        TacticalSide side,
        double amount,
        bool mass,
        SynergyModifiers sideSynergyModifiers,
        SynergyModifiers opposingSynergyModifiers)
    {
        var targets = mass
            ? AliveIndices(units, side)
            : SingleBy(units, side, unit => unit.CurrentMana);

        foreach (var index in targets)
        {
            var unit = units[index];
            units[index] = unit with
            {
                CurrentMana = Math.Min(unit.ManaMax, unit.CurrentMana + amount)
            };
            TryCastAbility(units, index, sideSynergyModifiers, opposingSynergyModifiers);
        }
    }

    private static List<int> AliveIndices(List<BattleUnit> units, TacticalSide side)
    {
        var indices = new List<int>();
        for (var i = 0; i < units.Count; i += 1)
        {
            if (units[i].IsAlive && units[i].Side == side)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private static List<int> SingleBy(
        List<BattleUnit> units,
        TacticalSide side,
        Func<BattleUnit, double> key,
        Func<BattleUnit, bool>? filter = null)
    {
        var best = -1;
        var bestKey = double.MaxValue;

        for (var i = 0; i < units.Count; i += 1)
        {
            var unit = units[i];
            if (!unit.IsAlive || unit.Side != side || (filter != null && !filter(unit)))
            {
                continue;
            }

            var candidateKey = key(unit);
            if (candidateKey < bestKey
                || (candidateKey == bestKey && best >= 0 && ComparePosition(unit.Position, units[best].Position) < 0))
            {
                best = i;
                bestKey = candidateKey;
            }
        }

        return best >= 0 ? new List<int> { best } : new List<int>();
    }

    private static int ManhattanDistance(TacticalPosition a, TacticalPosition b)
    {
        return Math.Abs(a.Row - b.Row) + Math.Abs(a.Column - b.Column);
    }

    private static BattleUnit AdvanceTimedBuffs(BattleUnit unit, int elapsedMilliseconds)
    {
        if (!unit.HasActiveLifesteal && !unit.HasActiveWeakness)
        {
            return unit;
        }

        var lifestealRemaining = Math.Max(0, unit.LifestealMillisecondsRemaining - elapsedMilliseconds);
        var weaknessRemaining = Math.Max(0, unit.WeaknessMillisecondsRemaining - elapsedMilliseconds);
        return unit with
        {
            LifestealFraction = lifestealRemaining > 0 ? unit.LifestealFraction : 0.0,
            LifestealMillisecondsRemaining = lifestealRemaining,
            WeaknessAttackPenaltyFraction = weaknessRemaining > 0 ? unit.WeaknessAttackPenaltyFraction : 0.0,
            WeaknessMillisecondsRemaining = weaknessRemaining
        };
    }

    private SynergyModifiers ResolveSynergyModifiers(TacticalSide side, SynergyModifiers overrideModifiers)
    {
        return overrideModifiers.Equals(SynergyModifiers.None)
            ? ModifiersForSide(side)
            : overrideModifiers;
    }

    private SynergyModifiers ModifiersForSide(TacticalSide side)
    {
        return side == TacticalSide.Player ? PlayerSynergyModifiers : EnemySynergyModifiers;
    }

    private static TacticalSide OpposingSide(TacticalSide side)
    {
        return side == TacticalSide.Player ? TacticalSide.Enemy : TacticalSide.Player;
    }

    private static int SecondsToMilliseconds(double seconds)
    {
        return seconds > int.MaxValue / 1000.0
            ? int.MaxValue
            : (int)Math.Ceiling(seconds * 1000.0);
    }

    private static bool IsAdjacent(TacticalPosition a, TacticalPosition b)
    {
        return ManhattanDistance(a, b) == 1;
    }

    private static int ComparePosition(TacticalPosition a, TacticalPosition b)
    {
        return a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column);
    }
}
}
