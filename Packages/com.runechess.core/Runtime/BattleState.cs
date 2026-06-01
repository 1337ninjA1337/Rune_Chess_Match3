using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core;

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
    BattleOutcome Outcome
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

            units[targetIndex] = MaybeCastAbility(target with
            {
                CurrentHealth = newHealth,
                Shield = remainingShield,
                CurrentMana = targetMana
            });

            units[attackerIndex] = MaybeCastAbility(attacker with
            {
                CurrentMana = Math.Min(attacker.ManaMax, attacker.CurrentMana + CombatFormulas.ManaFromAttack),
                AttackCooldownRemaining = attacker.AttackInterval
            });
        }

        var elapsed = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
        return new BattleState(units, elapsed, DurationSeconds, ResolveOutcome(units, elapsed, DurationSeconds));
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
        units[targetIndex] = MaybeCastAbility(unit with
        {
            CurrentMana = Math.Min(unit.ManaMax, unit.CurrentMana + mana)
        });

        return this with { Units = units };
    }

    private static BattleUnit MaybeCastAbility(BattleUnit unit)
    {
        if (unit.ManaMax > 0.0 && unit.CurrentMana >= unit.ManaMax)
        {
            return unit with
            {
                CurrentMana = 0.0,
                AbilitiesCast = unit.AbilitiesCast + 1
            };
        }

        return unit;
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
