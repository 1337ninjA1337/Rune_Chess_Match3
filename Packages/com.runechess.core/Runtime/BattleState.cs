using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{

/// <summary>
/// Deterministic MVP autobattle. A <see cref="Tick"/> advances the fight by a time
/// slice: units count down their attack cooldowns, pick the nearest enemy, move (melee)
/// or attack (physical), gain mana from attacking and from damage taken, and auto-cast
/// when full. Crits use a deterministic cadence rather than random rolls so the simulation
/// stays a pure function of its inputs; today only the Assassin 6 synergy taps that crit
/// path (to bank red-rune charge), while <see cref="CombatFormulas"/> holds the shared math.
/// </summary>
public sealed record BattleState(
    IReadOnlyList<BattleUnit> Units,
    double ElapsedSeconds,
    double DurationSeconds,
    BattleOutcome Outcome,
    double CommanderEnergy = 0.0,
    SynergyModifiers PlayerSynergyModifiers = default,
    SynergyModifiers EnemySynergyModifiers = default,
    double PlayerRedRuneCharge = 0.0,
    double EnemyRedRuneCharge = 0.0,
    double PlayerDamageDealt = 0.0,
    double PlayerHealingDone = 0.0,
    double PlayerShieldGranted = 0.0
)
{
    public const double DefaultDurationSeconds = 60.0;
    public const double MechanistDroneHealth = 60.0;
    public const double MechanistDroneAttack = 10.0;
    public const double MechanistDroneAttacksPerSecond = 1.0;
    public const double MechanistTurretHealth = 80.0;
    public const double MechanistTurretAttack = 14.0;
    public const double MechanistTurretAttacksPerSecond = 0.8;
    public const int MechanistTurretDurationMilliseconds = 6000;
    public const double SpiritIllusionStatMultiplier = 0.35;
    public const int SpiritIllusionDurationMilliseconds = 5000;
    public const double WarlordFirstDefenderHealthBonus = 0.20;

    public IEnumerable<BattleUnit> AliveUnits => Units.Where(unit => unit.IsAlive);
    public IEnumerable<BattleUnit> AliveAllies => AliveUnits.Where(unit => unit.Side == TacticalSide.Player);
    public IEnumerable<BattleUnit> AliveEnemies => AliveUnits.Where(unit => unit.Side == TacticalSide.Enemy);
    public bool IsResolved => Outcome != BattleOutcome.Ongoing;
    public double RemainingSeconds => Math.Max(0.0, DurationSeconds - ElapsedSeconds);

    public static BattleState Create(
        IReadOnlyList<BattleUnit> units,
        double durationSeconds = DefaultDurationSeconds,
        SynergyModifiers playerSynergyModifiers = default,
        SynergyModifiers enemySynergyModifiers = default,
        CommanderState? playerCommander = null,
        CommanderState? enemyCommander = null)
    {
        if (units is null)
        {
            throw new ArgumentNullException(nameof(units));
        }

        if (durationSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Battle duration must be positive.");
        }

        var battleUnits = units.ToList();
        AddMechanistOpeningDrone(battleUnits, TacticalSide.Player, playerSynergyModifiers);
        AddMechanistOpeningDrone(battleUnits, TacticalSide.Enemy, enemySynergyModifiers);
        ApplySpiritDodgeChance(battleUnits, TacticalSide.Player, playerSynergyModifiers);
        ApplySpiritDodgeChance(battleUnits, TacticalSide.Enemy, enemySynergyModifiers);
        ApplyWarlordFirstDefenderHealth(battleUnits, TacticalSide.Player, playerCommander);
        ApplyWarlordFirstDefenderHealth(battleUnits, TacticalSide.Enemy, enemyCommander);

        return new BattleState(
            battleUnits,
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

    /// <summary>
    /// Player-centric combat totals accumulated over the battle for the level-complete
    /// summary screen: damage the player dealt to enemies, healing restored on the player
    /// side, and shield granted to the player side. Healing/shield are summed from the
    /// per-unit positive deltas of each step, so they are a faithful MVP approximation
    /// even when an ally is also taking damage in the same step.
    /// </summary>
    private static (double damage, double healing, double shield) DiffPlayerCombatStats(
        IReadOnlyList<BattleUnit> before,
        IReadOnlyList<BattleUnit> after)
    {
        var beforeById = new Dictionary<string, BattleUnit>(before.Count);
        foreach (var unit in before)
        {
            beforeById[unit.UnitId] = unit;
        }

        double damage = 0.0;
        double healing = 0.0;
        double shield = 0.0;
        foreach (var unit in after)
        {
            if (!beforeById.TryGetValue(unit.UnitId, out var prior))
            {
                continue;
            }

            if (unit.Side == TacticalSide.Enemy)
            {
                var priorPool = prior.CurrentHealth + prior.Shield;
                var nowPool = unit.CurrentHealth + unit.Shield;
                if (priorPool > nowPool)
                {
                    damage += priorPool - nowPool;
                }
            }
            else
            {
                if (unit.CurrentHealth > prior.CurrentHealth)
                {
                    healing += unit.CurrentHealth - prior.CurrentHealth;
                }

                if (unit.Shield > prior.Shield)
                {
                    shield += unit.Shield - prior.Shield;
                }
            }
        }

        return (damage, healing, shield);
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

        var playerRedRuneChargeGain = 0.0;
        var enemyRedRuneChargeGain = 0.0;

        foreach (var attackerIndex in order)
        {
            var attacker = units[attackerIndex];
            if (!attacker.IsAlive || attacker.AttackCooldownRemaining > 0.0)
            {
                continue;
            }

            var targetIndex = SelectTargetIndex(units, attacker, ModifiersForSide(attacker.Side).AssassinBacklineStrike);
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

            target = target with { AttacksReceived = target.AttacksReceived + 1 };
            if (ShouldDodgeAttack(target))
            {
                units[targetIndex] = target;
                units[attackerIndex] = attacker with
                {
                    CurrentMana = Math.Min(attacker.ManaMax, attacker.CurrentMana + CombatFormulas.ManaFromAttack),
                    AttackCooldownRemaining = attacker.AttackInterval
                };
                TryCastAbility(units, attackerIndex, ModifiersForSide(attacker.Side), ModifiersForSide(target.Side));
                continue;
            }

            // Assassin 6 synergy: assassin crits (on the deterministic crit cadence)
            // hit harder and accrue red-rune charge for the attacker's side, which the
            // next red rune match consumes as bonus physical power.
            var isAssassinCrit = ModifiersForSide(attacker.Side).AssassinCritChargesRedRunes
                && attacker.HeroClass.Equals(ClassCatalog.Assassin.Name, StringComparison.OrdinalIgnoreCase)
                && CombatFormulas.WouldCritByCadence(attacker.AttacksLanded);

            var rawDamage = CombatFormulas.CalculatePhysicalDamage(attacker.EffectiveAttack, target.Armor);
            if (isAssassinCrit)
            {
                rawDamage = CombatFormulas.ApplyCrit(rawDamage);
                if (attacker.Side == TacticalSide.Player)
                {
                    playerRedRuneChargeGain += SynergyModifiers.AssassinCritRedRuneCharge;
                }
                else
                {
                    enemyRedRuneChargeGain += SynergyModifiers.AssassinCritRedRuneCharge;
                }
            }

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
                AttackCooldownRemaining = updatedAttacker.AttackInterval,
                AttacksLanded = updatedAttacker.AttacksLanded + 1
            };
            TryCastAbility(units, attackerIndex, ModifiersForSide(updatedAttacker.Side), ModifiersForSide(target.Side));
        }

        var elapsed = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
        var (tickDamage, tickHealing, tickShield) = DiffPlayerCombatStats(Units, units);
        return new BattleState(
            units,
            elapsed,
            DurationSeconds,
            ResolveOutcome(units, elapsed, DurationSeconds),
            CommanderEnergy,
            PlayerSynergyModifiers,
            EnemySynergyModifiers,
            PlayerRedRuneCharge + playerRedRuneChargeGain,
            EnemyRedRuneCharge + enemyRedRuneChargeGain,
            PlayerDamageDealt + tickDamage,
            PlayerHealingDone + tickHealing,
            PlayerShieldGranted + tickShield);
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
        SynergyModifiers synergyModifiers = default,
        ArtifactRuneModifiers runeArtifactModifiers = default)
    {
        if (effects is null)
        {
            throw new ArgumentNullException(nameof(effects));
        }

        var state = this;
        foreach (var effect in effects)
        {
            state = state.ApplyRuneEffect(effect, casterSide, synergyModifiers, runeArtifactModifiers);
        }

        return state;
    }

    public BattleState ApplyRuneEffect(
        RuneEffect effect,
        TacticalSide casterSide = TacticalSide.Player,
        SynergyModifiers synergyModifiers = default,
        ArtifactRuneModifiers runeArtifactModifiers = default)
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

        // Rune artifacts the run owns scale the matching colour's match-3 effect
        // (GDD P1 "артефакты как модификаторы рун"); the neutral default leaves the
        // effect untouched so existing call sites keep their behaviour.
        combatEffect = runeArtifactModifiers.Apply(combatEffect);

        // Assassin 6 synergy: spend the red-rune charge built up by assassin crits as
        // bonus power on the caster side's next red physical rune effect.
        var availableRedCharge = casterSide == TacticalSide.Player ? PlayerRedRuneCharge : EnemyRedRuneCharge;
        var consumedRedCharge = 0.0;
        if (availableRedCharge > 0.0
            && combatEffect.Rune == RuneType.Red
            && combatEffect.Kind == RuneEffectKind.PhysicalDamage)
        {
            consumedRedCharge = availableRedCharge;
            combatEffect = combatEffect with { Power = combatEffect.Power + availableRedCharge };
        }

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
        ApplyMechanistMatch4Turret(units, casterSide, combatEffect, casterSynergyModifiers);
        ApplySpiritWhiteRuneIllusion(units, casterSide, combatEffect, casterSynergyModifiers);
        ApplyMageBlueMatch4BonusCharge(units, casterSide, combatEffect, casterSynergyModifiers, enemySynergyModifiers);

        var outcome = ResolveOutcome(units, ElapsedSeconds, DurationSeconds);
        var (effectDamage, effectHealing, effectShield) = DiffPlayerCombatStats(Units, units);
        return new BattleState(
            units,
            ElapsedSeconds,
            DurationSeconds,
            outcome,
            CommanderEnergy + commanderGain,
            PlayerSynergyModifiers,
            EnemySynergyModifiers,
            casterSide == TacticalSide.Player ? PlayerRedRuneCharge - consumedRedCharge : PlayerRedRuneCharge,
            casterSide == TacticalSide.Enemy ? EnemyRedRuneCharge - consumedRedCharge : EnemyRedRuneCharge,
            PlayerDamageDealt + effectDamage,
            PlayerHealingDone + effectHealing,
            PlayerShieldGranted + effectShield);
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
                    AbilityEffect(RuneEffectKind.PhysicalDamage, ApplyAbilityDamageBonus(ability.Power, casterSynergyModifiers)),
                    physical: true,
                    opposingSynergyModifiers,
                    casterSynergyModifiers);
                ApplyAbyssalWeakness(units, debuffTargets, casterSynergyModifiers);
                return;
            case HeroAbilityKind.MagicDamage:
                debuffTargets = ApplyDamage(
                    units,
                    enemySide,
                    AbilityEffect(RuneEffectKind.MagicDamage, ApplyAbilityDamageBonus(ability.Power, casterSynergyModifiers)),
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

    private static double ApplyAbilityDamageBonus(double power, SynergyModifiers synergyModifiers)
    {
        return power * synergyModifiers.AbilityDamageMultiplier;
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

    private static void AddMechanistOpeningDrone(
        List<BattleUnit> units,
        TacticalSide side,
        SynergyModifiers synergyModifiers)
    {
        if (!synergyModifiers.MechanistOpeningDrone)
        {
            return;
        }

        var position = SelectBacklineSpawnPosition(units, side);
        if (position is null)
        {
            return;
        }

        var attacksPerSecond = CombatFormulas.CalculateAttacksPerSecond(MechanistDroneAttacksPerSecond);
        units.Add(new BattleUnit(
            UnitId: CreateMechanistDroneUnitId(units, side),
            Side: side,
            Position: position.Value,
            MaxHealth: MechanistDroneHealth,
            CurrentHealth: MechanistDroneHealth,
            Attack: MechanistDroneAttack,
            Armor: 0.0,
            MagicResist: 0.0,
            AttacksPerSecond: attacksPerSecond,
            CurrentMana: 0.0,
            ManaMax: 0.0,
            Shield: 0.0,
            AttackType: BattleAttackType.Ranged,
            AttackCooldownRemaining: CombatFormulas.CalculateAttackInterval(attacksPerSecond),
            AbilitiesCast: 0,
            DodgeChance: synergyModifiers.DodgeChance,
            IsSummoned: true));
    }

    private static void ApplyMechanistMatch4Turret(
        List<BattleUnit> units,
        TacticalSide side,
        RuneEffect effect,
        SynergyModifiers synergyModifiers)
    {
        if (!synergyModifiers.MechanistMatch4Turret || effect.Tier != RuneMatchTier.Match4)
        {
            return;
        }

        var position = SelectBacklineSpawnPosition(units, side);
        if (position is null)
        {
            return;
        }

        var attacksPerSecond = CombatFormulas.CalculateAttacksPerSecond(MechanistTurretAttacksPerSecond);
        units.Add(new BattleUnit(
            UnitId: CreateMechanistUnitId(units, side, "mechanist_turret"),
            Side: side,
            Position: position.Value,
            MaxHealth: MechanistTurretHealth,
            CurrentHealth: MechanistTurretHealth,
            Attack: MechanistTurretAttack,
            Armor: 0.0,
            MagicResist: 0.0,
            AttacksPerSecond: attacksPerSecond,
            CurrentMana: 0.0,
            ManaMax: 0.0,
            Shield: 0.0,
            AttackType: BattleAttackType.Ranged,
            AttackCooldownRemaining: CombatFormulas.CalculateAttackInterval(attacksPerSecond),
            AbilitiesCast: 0,
            SummonMillisecondsRemaining: MechanistTurretDurationMilliseconds,
            DodgeChance: synergyModifiers.DodgeChance,
            IsSummoned: true));
    }

    private static void ApplySpiritWhiteRuneIllusion(
        List<BattleUnit> units,
        TacticalSide side,
        RuneEffect effect,
        SynergyModifiers synergyModifiers)
    {
        if (!synergyModifiers.SpiritWhiteRuneIllusion
            || effect.Rune != RuneType.White
            || effect.Kind != RuneEffectKind.CommanderEnergy)
        {
            return;
        }

        var position = SelectBacklineSpawnPosition(units, side);
        if (position is null)
        {
            return;
        }

        var candidates = units
            .Where(unit => unit.IsAlive && unit.Side == side && !unit.IsSummoned)
            .OrderBy(unit => unit.UnitId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var sourceIndex = ((effect.MatchedRunesCount * 31) + effect.ChainNumber) % candidates.Count;
        var source = candidates[sourceIndex];
        units.Add(CreateSpiritIllusion(units, source, position.Value));
    }

    private static BattleUnit CreateSpiritIllusion(
        List<BattleUnit> units,
        BattleUnit source,
        TacticalPosition position)
    {
        var attacksPerSecond = CombatFormulas.CalculateAttacksPerSecond(source.AttacksPerSecond);
        var maxHealth = Math.Max(1.0, source.MaxHealth * SpiritIllusionStatMultiplier);
        return new BattleUnit(
            UnitId: CreateSideUnitId(units, source.Side, "spirit_illusion"),
            Side: source.Side,
            Position: position,
            MaxHealth: maxHealth,
            CurrentHealth: maxHealth,
            Attack: Math.Max(1.0, source.Attack * SpiritIllusionStatMultiplier),
            Armor: source.Armor * SpiritIllusionStatMultiplier,
            MagicResist: source.MagicResist * SpiritIllusionStatMultiplier,
            AttacksPerSecond: attacksPerSecond,
            CurrentMana: 0.0,
            ManaMax: 0.0,
            Shield: 0.0,
            AttackType: source.AttackType,
            AttackCooldownRemaining: CombatFormulas.CalculateAttackInterval(attacksPerSecond),
            AbilitiesCast: 0,
            SummonMillisecondsRemaining: SpiritIllusionDurationMilliseconds,
            DodgeChance: source.DodgeChance,
            IsSummoned: true,
            HeroClass: source.HeroClass);
    }

    private static void ApplyMageBlueMatch4BonusCharge(
        List<BattleUnit> units,
        TacticalSide side,
        RuneEffect effect,
        SynergyModifiers synergyModifiers,
        SynergyModifiers opposingSynergyModifiers)
    {
        if (!synergyModifiers.MageBlueMatch4BonusCharge
            || effect.Rune != RuneType.Blue
            || effect.Tier != RuneMatchTier.Match4)
        {
            return;
        }

        var candidates = units
            .Select((unit, index) => (Unit: unit, Index: index))
            .Where(item => item.Unit.IsAlive
                && item.Unit.Side == side
                && item.Unit.HeroClass.Equals(ClassCatalog.Mage.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Unit.UnitId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var target = candidates[((effect.MatchedRunesCount * 17) + effect.ChainNumber) % candidates.Count];
        units[target.Index] = target.Unit with
        {
            CurrentMana = Math.Min(target.Unit.ManaMax, target.Unit.CurrentMana + SynergyModifiers.MageBlueMatch4BonusMana)
        };
        TryCastAbility(units, target.Index, synergyModifiers, opposingSynergyModifiers);
    }

    private static void ApplySpiritDodgeChance(
        List<BattleUnit> units,
        TacticalSide side,
        SynergyModifiers synergyModifiers)
    {
        if (synergyModifiers.DodgeChance <= 0.0)
        {
            return;
        }

        for (var i = 0; i < units.Count; i += 1)
        {
            var unit = units[i];
            if (unit.Side != side)
            {
                continue;
            }

            units[i] = unit with
            {
                DodgeChance = Math.Max(unit.DodgeChance, synergyModifiers.DodgeChance)
            };
        }
    }

    private static bool ShouldDodgeAttack(BattleUnit target)
    {
        if (target.DodgeChance <= 0.0 || target.AttacksReceived <= 0)
        {
            return false;
        }

        var dodgeInterval = Math.Max(1, (int)Math.Round(1.0 / target.DodgeChance, MidpointRounding.AwayFromZero));
        return target.AttacksReceived % dodgeInterval == 0;
    }

    private static TacticalPosition? SelectBacklineSpawnPosition(List<BattleUnit> units, TacticalSide side)
    {
        foreach (var position in TacticalField.Mvp.CreateCells(side)
            .Where(position => position.IsBackline && !units.Any(unit => unit.Position.Equals(position)))
            .OrderBy(position => Math.Abs(position.Column - ((TacticalField.Mvp.Columns - 1) / 2.0)))
            .ThenBy(position => position.Column))
        {
            return position;
        }

        return null;
    }

    private static string CreateMechanistDroneUnitId(List<BattleUnit> units, TacticalSide side)
    {
        return CreateSideUnitId(units, side, "mechanist_drone");
    }

    private static string CreateMechanistUnitId(List<BattleUnit> units, TacticalSide side, string unitKind)
    {
        return CreateSideUnitId(units, side, unitKind);
    }

    private static string CreateSideUnitId(List<BattleUnit> units, TacticalSide side, string unitKind)
    {
        var prefix = side == TacticalSide.Player ? $"{unitKind}_player" : $"{unitKind}_enemy";
        if (units.All(unit => !unit.UnitId.Equals(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return prefix;
        }

        var index = 2;
        while (units.Any(unit => unit.UnitId.Equals($"{prefix}_{index}", StringComparison.OrdinalIgnoreCase)))
        {
            index += 1;
        }

        return $"{prefix}_{index}";
    }

    private static int SelectTargetIndex(
        IReadOnlyList<BattleUnit> units,
        BattleUnit attacker,
        bool assassinBacklineStrike = false)
    {
        // Assassin 3 synergy: assassins ignore the enemy front line and dive the
        // backline. We only restrict targeting when a living enemy backline unit
        // exists; otherwise the assassin falls back to the nearest enemy.
        var diveBackline = assassinBacklineStrike
            && attacker.HeroClass.Equals(ClassCatalog.Assassin.Name, StringComparison.OrdinalIgnoreCase)
            && units.Any(unit => unit.IsAlive && unit.Side != attacker.Side && unit.Position.IsBackline);

        var best = -1;
        var bestDistance = int.MaxValue;

        for (var i = 0; i < units.Count; i += 1)
        {
            var candidate = units[i];
            if (!candidate.IsAlive || candidate.Side == attacker.Side)
            {
                continue;
            }

            if (diveBackline && !candidate.Position.IsBackline)
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
                Shield = CombatFormulas.CapShield(unit.Shield + effect.Power, unit.MaxHealth),
                Armor = ShouldApplyDefenderYellowArmor(effect, synergyModifiers)
                    ? unit.Armor + SynergyModifiers.DefenderYellowRuneArmorBonus
                    : unit.Armor
            };
        }
    }

    private static bool ShouldShieldFrontline(RuneEffect effect, SynergyModifiers synergyModifiers)
    {
        return !effect.IsMassEffect
            && effect.Rune == RuneType.Yellow
            && synergyModifiers.EmpireYellowRuneFrontlineShield;
    }

    private static bool ShouldApplyDefenderYellowArmor(RuneEffect effect, SynergyModifiers synergyModifiers)
    {
        return effect.Rune == RuneType.Yellow
            && effect.Kind == RuneEffectKind.Shield
            && synergyModifiers.DefenderYellowRuneArmorBoost;
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
        if (!unit.HasActiveLifesteal && !unit.HasActiveWeakness && !unit.HasTimedSummon)
        {
            return unit;
        }

        var lifestealRemaining = Math.Max(0, unit.LifestealMillisecondsRemaining - elapsedMilliseconds);
        var weaknessRemaining = Math.Max(0, unit.WeaknessMillisecondsRemaining - elapsedMilliseconds);
        var summonRemaining = unit.HasTimedSummon
            ? Math.Max(0, unit.SummonMillisecondsRemaining - elapsedMilliseconds)
            : 0;
        var summonExpired = unit.HasTimedSummon && summonRemaining == 0;
        return unit with
        {
            CurrentHealth = summonExpired ? 0.0 : unit.CurrentHealth,
            Shield = summonExpired ? 0.0 : unit.Shield,
            LifestealFraction = lifestealRemaining > 0 ? unit.LifestealFraction : 0.0,
            LifestealMillisecondsRemaining = lifestealRemaining,
            WeaknessAttackPenaltyFraction = weaknessRemaining > 0 ? unit.WeaknessAttackPenaltyFraction : 0.0,
            WeaknessMillisecondsRemaining = weaknessRemaining,
            SummonMillisecondsRemaining = summonRemaining
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

    private static void ApplyWarlordFirstDefenderHealth(
        List<BattleUnit> units,
        TacticalSide side,
        CommanderState? commander)
    {
        if (commander?.Id != CommanderCatalog.Warlord.Id)
        {
            return;
        }

        var defender = units
            .Select((unit, index) => new { Unit = unit, Index = index })
            .Where(item => item.Unit.Side == side
                && !item.Unit.IsSummoned
                && item.Unit.HeroClass.Equals(ClassCatalog.Defender.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Unit.Position.IsFrontline)
            .ThenBy(item => item.Unit.Position.Row)
            .ThenBy(item => item.Unit.Position.Column)
            .FirstOrDefault();
        if (defender is null)
        {
            return;
        }

        var unit = defender.Unit;
        var healthMultiplier = 1.0 + WarlordFirstDefenderHealthBonus;
        var healthRatio = unit.MaxHealth <= 0.0 ? 0.0 : unit.CurrentHealth / unit.MaxHealth;
        var maxHealth = unit.MaxHealth * healthMultiplier;
        units[defender.Index] = unit with
        {
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth * healthRatio
        };
    }
}
}
