using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One unit drawn on the combat battlefield (GDD UI screen "Экран боя", element
    /// "поле боя с героями и врагами"). Carries the side, board position and the
    /// health/mana fractions the screen renders as bars, so the Unity layer only draws
    /// the unit and never reads <see cref="BattleUnit"/> combat math directly.
    /// </summary>
    public sealed record CombatFieldUnit(
        string UnitId,
        TacticalSide Side,
        TacticalPosition Position,
        string ClassName,
        bool IsFrontline,
        bool IsSummoned,
        double HealthFraction,
        double ManaFraction,
        bool HasMana)
    {
        /// <summary>True when the unit belongs to the player's half.</summary>
        public bool IsPlayer => Side == TacticalSide.Player;

        /// <summary>True when the unit belongs to the enemy half.</summary>
        public bool IsEnemy => Side == TacticalSide.Enemy;

        /// <summary>Health clamped to a renderable 0..1 bar fraction.</summary>
        public double HealthBar => Math.Clamp(HealthFraction, 0.0, 1.0);

        /// <summary>Mana clamped to a renderable 0..1 bar fraction (0 for unit with no mana pool).</summary>
        public double ManaBar => HasMana ? Math.Clamp(ManaFraction, 0.0, 1.0) : 0.0;

        /// <summary>Build the renderable unit from a live battle unit.</summary>
        public static CombatFieldUnit FromBattleUnit(BattleUnit unit)
        {
            if (unit is null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            var hasMana = unit.ManaMax > 0.0;
            return new CombatFieldUnit(
                UnitId: unit.UnitId,
                Side: unit.Side,
                Position: unit.Position,
                ClassName: unit.HeroClass,
                IsFrontline: unit.Position.IsInsideMvpField && unit.Position.IsFrontline,
                IsSummoned: unit.IsSummoned,
                HealthFraction: unit.HealthPercent,
                ManaFraction: hasMana ? unit.CurrentMana / unit.ManaMax : 0.0,
                HasMana: hasMana);
        }
    }

    /// <summary>
    /// One cell of the tactical field on the combat screen: its position, its visual
    /// state (free, ally-occupied, enemy-occupied) and the unit standing on it, if any.
    /// </summary>
    public sealed record CombatFieldCell(
        TacticalPosition Position,
        TacticalCellState State,
        CombatFieldUnit? Unit)
    {
        /// <summary>True when a living unit currently stands on the cell.</summary>
        public bool IsOccupied => Unit is not null;
    }

    /// <summary>
    /// One cell of the 7x7 match-3 board on the combat screen (element "match-3 поле 7x7").
    /// Carries the rune color, whether it is a stored great rune and whether the idle
    /// match hint is highlighting it.
    /// </summary>
    public sealed record CombatBoardCell(
        BoardPoint Point,
        RuneType Rune,
        bool IsGreatRune,
        bool IsHintHighlighted);

    /// <summary>
    /// One active rune-effect chip on the combat screen (element "активные эффекты рун"):
    /// the resolved effect of the player's most recent match, ready to render as a labelled
    /// pill. Pure data derived from <see cref="RuneEffect"/>.
    /// </summary>
    public sealed record CombatRuneEffectChip(
        RuneType Rune,
        RuneEffectKind Kind,
        RuneMatchTier Tier,
        int ChainNumber,
        bool IsMassEffect,
        int Power,
        string Label)
    {
        /// <summary>Russian rune-color label, matching the hero collection wording.</summary>
        public static string DescribeRune(RuneType rune) => rune switch
        {
            RuneType.Red => "Красная руна",
            RuneType.Blue => "Синяя руна",
            RuneType.Green => "Зелёная руна",
            RuneType.Yellow => "Жёлтая руна",
            RuneType.Purple => "Фиолетовая руна",
            RuneType.White => "Белая руна",
            _ => throw new ArgumentOutOfRangeException(nameof(rune), rune, "Unknown rune type.")
        };

        /// <summary>Short Russian label for the combat effect a rune match produced.</summary>
        public static string DescribeKind(RuneEffectKind kind) => kind switch
        {
            RuneEffectKind.PhysicalDamage => "Физ. урон",
            RuneEffectKind.MagicDamage => "Маг. урон",
            RuneEffectKind.Mana => "Мана",
            RuneEffectKind.Healing => "Лечение",
            RuneEffectKind.Shield => "Щит",
            RuneEffectKind.CommanderEnergy => "Энергия командира",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown rune effect kind.")
        };

        /// <summary>Build a chip from a resolved rune effect.</summary>
        public static CombatRuneEffectChip FromRuneEffect(RuneEffect effect)
        {
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            var power = (int)Math.Round(Math.Max(0.0, effect.Power), MidpointRounding.AwayFromZero);
            return new CombatRuneEffectChip(
                Rune: effect.Rune,
                Kind: effect.Kind,
                Tier: effect.Tier,
                ChainNumber: effect.ChainNumber,
                IsMassEffect: effect.IsMassEffect,
                Power: power,
                Label: $"{DescribeRune(effect.Rune)} · {DescribeKind(effect.Kind)}");
        }
    }

    /// <summary>
    /// Aggregate view-model for the combat screen (GDD UI screen "Экран боя", and the
    /// architecture "combat screen with tactical field, health/mana indicators, 7x7 rune
    /// board, timer, and pause"). It gathers every element the screen shows: the tactical
    /// battlefield with heroes and enemies, their health/mana bars, the 7x7 match-3 board,
    /// the active rune effects of the last match, the round timer (via the embedded
    /// <see cref="CombatHudModel"/>) and the pause control.
    ///
    /// The battlefield is read from a live <see cref="BattleState"/> and the match-3 board
    /// and timer from the <see cref="CombatState"/>, mirroring how
    /// <see cref="LevelCompleteModel"/> combines the two. Keeping it pure lets the whole
    /// screen be smoke-tested without the Unity editor.
    /// </summary>
    public sealed record CombatScreenModel(
        int Round,
        PveRoundType RoundType,
        string EnemyName,
        CombatHudModel Hud,
        int FieldColumns,
        int FieldRows,
        IReadOnlyList<CombatFieldCell> Battlefield,
        int BoardRows,
        int BoardColumns,
        IReadOnlyList<CombatBoardCell> RuneBoard,
        IReadOnlyList<CombatRuneEffectChip> ActiveRuneEffects,
        bool IsResolved,
        BattleOutcome Outcome,
        bool IsPaused)
    {
        /// <summary>Number of living player units currently on the field.</summary>
        public int AlivePlayerUnits => Battlefield.Count(cell => cell.Unit is { Side: TacticalSide.Player });

        /// <summary>Number of living enemy units currently on the field.</summary>
        public int AliveEnemyUnits => Battlefield.Count(cell => cell.Unit is { Side: TacticalSide.Enemy });

        /// <summary>Pause/resume button label for the combat screen.</summary>
        public string PauseButtonLabel => IsPaused ? "Продолжить" : "Пауза";

        /// <summary>True when the idle match hint should highlight legal-move cells on the board.</summary>
        public bool ShowMatchHint => RuneBoard.Any(cell => cell.IsHintHighlighted);

        /// <summary>
        /// Build the combat-screen model from the live battle and the match-3 combat state.
        /// <paramref name="round"/> supplies the round header (number, type, enemy name).
        /// Optional <paramref name="activeRuneEffects"/> are the resolved effects of the
        /// player's most recent match; <paramref name="isPaused"/> reflects the pause control.
        /// </summary>
        public static CombatScreenModel Build(
            BattleState battle,
            CombatState combat,
            PveRoundDefinition round,
            IReadOnlyList<RuneEffect>? activeRuneEffects = null,
            bool isPaused = false)
        {
            if (battle is null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (combat is null)
            {
                throw new ArgumentNullException(nameof(combat));
            }

            if (round is null)
            {
                throw new ArgumentNullException(nameof(round));
            }

            var field = TacticalField.Mvp;
            var unitsByPosition = new Dictionary<TacticalPosition, CombatFieldUnit>();
            foreach (var unit in battle.AliveUnits)
            {
                if (field.Contains(unit.Position))
                {
                    unitsByPosition[unit.Position] = CombatFieldUnit.FromBattleUnit(unit);
                }
            }

            var battlefield = new List<CombatFieldCell>(field.CellCount);
            foreach (var position in field.CreateCells())
            {
                unitsByPosition.TryGetValue(position, out var unit);
                battlefield.Add(new CombatFieldCell(position, ResolveCellState(unit), unit));
            }

            var hint = combat.CurrentMatchHint;
            var highlighted = hint is null
                ? new HashSet<BoardPoint>()
                : hint.HighlightedCells.ToHashSet();

            var runeBoard = new List<CombatBoardCell>(Match3Board.CellCount);
            foreach (var point in Match3Board.CreateCells())
            {
                runeBoard.Add(new CombatBoardCell(
                    point,
                    combat.RuneBoard[point],
                    combat.RuneBoard.IsGreatRune(point),
                    highlighted.Contains(point)));
            }

            var effects = activeRuneEffects is null
                ? (IReadOnlyList<CombatRuneEffectChip>)Array.Empty<CombatRuneEffectChip>()
                : activeRuneEffects.Select(CombatRuneEffectChip.FromRuneEffect).ToList();

            return new CombatScreenModel(
                Round: round.Round,
                RoundType: round.Type,
                EnemyName: round.EnemyName,
                Hud: CombatHudModel.Build(combat, BuildKeyUnits(battle)),
                FieldColumns: field.Columns,
                FieldRows: field.Rows,
                Battlefield: battlefield,
                BoardRows: Match3Board.Rows,
                BoardColumns: Match3Board.Columns,
                RuneBoard: runeBoard,
                ActiveRuneEffects: effects,
                IsResolved: battle.IsResolved,
                Outcome: battle.Outcome,
                IsPaused: isPaused);
        }

        /// <summary>
        /// Convenience overload that reads the match-3 state and round header from a run that
        /// is currently in combat, fighting the supplied live <paramref name="battle"/>.
        /// </summary>
        public static CombatScreenModel Build(
            RunState run,
            BattleState battle,
            IReadOnlyList<RuneEffect>? activeRuneEffects = null,
            bool isPaused = false)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (run.Combat is null)
            {
                throw new InvalidOperationException("The run has no active combat to render.");
            }

            return Build(battle, run.Combat, run.CurrentRoundDefinition, activeRuneEffects, isPaused);
        }

        /// <summary>
        /// The "key units" highlighted on the HUD bar strip: the most endangered living unit
        /// of each side (lowest health), so the player can read clutch moments at a glance.
        /// </summary>
        private static IReadOnlyList<CombatHudUnit> BuildKeyUnits(BattleState battle)
        {
            var keyUnits = new List<CombatHudUnit>(2);
            foreach (var side in new[] { TacticalSide.Player, TacticalSide.Enemy })
            {
                var endangered = battle.AliveUnits
                    .Where(unit => unit.Side == side)
                    .OrderBy(unit => unit.HealthPercent)
                    .ThenBy(unit => unit.Position.Row)
                    .ThenBy(unit => unit.Position.Column)
                    .FirstOrDefault();
                if (endangered is null)
                {
                    continue;
                }

                var hasMana = endangered.ManaMax > 0.0;
                keyUnits.Add(new CombatHudUnit(
                    Name: string.IsNullOrEmpty(endangered.HeroClass) ? endangered.UnitId : endangered.HeroClass,
                    IsPlayer: endangered.Side == TacticalSide.Player,
                    HealthFraction: endangered.HealthPercent,
                    ManaFraction: hasMana ? endangered.CurrentMana / endangered.ManaMax : 0.0));
            }

            return keyUnits;
        }

        private static TacticalCellState ResolveCellState(CombatFieldUnit? unit)
        {
            if (unit is null)
            {
                return TacticalCellState.Free;
            }

            return unit.Side == TacticalSide.Player
                ? TacticalCellState.OccupiedAlly
                : TacticalCellState.OccupiedEnemy;
        }
    }
}
