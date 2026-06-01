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
    double CommanderEnergy = 0.0
)
{
    public const double DefaultDurationSeconds = 60.0;

    public IEnumerable<BattleUnit> AliveUnits => Units.Where(unit => unit.IsAlive);
    public IEnumerable<BattleUnit> AliveAllies => AliveUnits.Where(unit => unit.Side == TacticalSide.Player);
    public IEnumerable<BattleUnit> AliveEnemies => AliveUnits.Where(unit => unit.Side == TacticalSide.Enemy);
    public bool IsResolved => Outcome != BattleOutcome.Ongoing;
    public double RemainingSeconds => Math.Max(0.0, DurationSeconds - ElapsedSeconds);

    public static BattleState Create(IReadOnlyList<BattleUnit> units, double durationSeconds = DefaultDurationSeconds)
    {
        if (units is null)
        {
            throw new ArgumentNullException(nameof(units));
        }

        if (durationSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Battle duration must be positive.");
        }

        return new BattleState(units.ToList(), 0.0, durationSeconds, BattleOutcome.Ongoing);
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

        for (var i = 0; i < units.Count; i += 1)
        {
            if (units[i].IsAlive)
            {
                units[i] = units[i] with
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

            var rawDamage = CombatFormulas.CalculatePhysicalDamage(attacker.Attack, target.Armor);
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
            TryCastAbility(units, targetIndex);

            units[attackerIndex] = attacker with
            {
                CurrentMana = Math.Min(attacker.ManaMax, attacker.CurrentMana + CombatFormulas.ManaFromAttack),
                AttackCooldownRemaining = attacker.AttackInterval
            };
            TryCastAbility(units, attackerIndex);
        }

        var elapsed = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
        return new BattleState(units, elapsed, DurationSeconds, ResolveOutcome(units, elapsed, DurationSeconds), CommanderEnergy);
    }

    /// <summary>
    /// Applies resolved rune effects (from <see cref="RuneEffectResolver"/>) to the battle:
    /// damage hits the opposing side, healing/shield/mana support the casting side, and
    /// white runes plus T/L bonuses accrue commander energy. The casting side is the player
    /// by default. Mass effects (T/L combos) hit every relevant unit; otherwise a single
    /// focused target is chosen.
    /// </summary>
    public BattleState ApplyRuneEffects(IEnumerable<RuneEffect> effects, TacticalSide casterSide = TacticalSide.Player)
    {
        if (effects is null)
        {
            throw new ArgumentNullException(nameof(effects));
        }

        var state = this;
        foreach (var effect in effects)
        {
            state = state.ApplyRuneEffect(effect, casterSide);
        }

        return state;
    }

    public BattleState ApplyRuneEffect(RuneEffect effect, TacticalSide casterSide = TacticalSide.Player)
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

        switch (effect.Kind)
        {
            case RuneEffectKind.PhysicalDamage:
                ApplyDamage(units, enemySide, effect, physical: true);
                break;
            case RuneEffectKind.MagicDamage:
                ApplyDamage(units, enemySide, effect, physical: false);
                break;
            case RuneEffectKind.Healing:
                ApplyRuneHealing(units, casterSide, effect);
                break;
            case RuneEffectKind.Shield:
                ApplyRuneShield(units, casterSide, effect);
                break;
            case RuneEffectKind.Mana:
                ApplyRuneMana(units, casterSide, effect.Power, effect.IsMassEffect);
                break;
            case RuneEffectKind.CommanderEnergy:
                commanderGain += effect.Power;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect), effect.Kind, "Unknown rune effect kind.");
        }

        var outcome = ResolveOutcome(units, ElapsedSeconds, DurationSeconds);
        return new BattleState(units, ElapsedSeconds, DurationSeconds, outcome, CommanderEnergy + commanderGain);
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
        TryCastAbility(units, targetIndex);

        return this with { Units = units, Outcome = ResolveOutcome(units, ElapsedSeconds, DurationSeconds) };
    }

    private static void TryCastAbility(List<BattleUnit> units, int casterIndex)
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
            ApplyHeroAbility(units, caster);
        }
    }

    private static void ApplyHeroAbility(List<BattleUnit> units, BattleUnit caster)
    {
        var enemySide = caster.Side == TacticalSide.Player ? TacticalSide.Enemy : TacticalSide.Player;
        var ability = caster.ActiveAbility;

        switch (ability.Kind)
        {
            case HeroAbilityKind.None:
                return;
            case HeroAbilityKind.PhysicalDamage:
                ApplyDamage(units, enemySide, AbilityEffect(RuneEffectKind.PhysicalDamage, ability.Power), physical: true);
                return;
            case HeroAbilityKind.MagicDamage:
                ApplyDamage(units, enemySide, AbilityEffect(RuneEffectKind.MagicDamage, ability.Power), physical: false);
                return;
            case HeroAbilityKind.Healing:
                ApplyRuneHealing(units, caster.Side, AbilityEffect(RuneEffectKind.Healing, ability.Power));
                return;
            case HeroAbilityKind.Shield:
                ApplyRuneShield(units, caster.Side, AbilityEffect(RuneEffectKind.Shield, ability.Power));
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

    private static void ApplyDamage(List<BattleUnit> units, TacticalSide enemySide, RuneEffect effect, bool physical)
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
            TryCastAbility(units, index);
        }
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

    private static void ApplyRuneShield(List<BattleUnit> units, TacticalSide side, RuneEffect effect)
    {
        var targets = effect.IsMassEffect
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

    private static void ApplyRuneMana(List<BattleUnit> units, TacticalSide side, double amount, bool mass)
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
            TryCastAbility(units, index);
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
