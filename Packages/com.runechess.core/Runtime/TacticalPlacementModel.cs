using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One cell of the preparation tactical board view-model: its position, the
    /// resolved <see cref="TacticalCellState"/> and the allied hero standing on it
    /// (if any). Pure data so the Unity layer only renders and dispatches taps.
    /// </summary>
    public sealed record TacticalPlacementCell(
        TacticalPosition Position,
        TacticalCellState State,
        string? HeroInstanceId,
        string? HeroId)
    {
        /// <summary>True when this empty player cell is a legal drop target for the picked bench hero.</summary>
        public bool IsPlacementTarget => State == TacticalCellState.AvailableForPlacement;

        /// <summary>True when an allied hero stands here and can be sent back to the bench.</summary>
        public bool IsOccupiedByAlly => State == TacticalCellState.OccupiedAlly;
    }

    /// <summary>
    /// View-model for the preparation tactical board. It resolves every cell to a
    /// <see cref="TacticalCellState"/> and, when the player has picked a bench hero
    /// to deploy, highlights the empty player-side cells that are legal drop targets.
    ///
    /// The rules mirror <see cref="RunState.PlaceHeroFromBench"/>: heroes deploy only
    /// onto free cells on the player half, and only while the team is under the
    /// player-level hero limit. Keeping the placement logic here (instead of in the
    /// Unity MonoBehaviour) lets it be smoke-tested without the editor.
    /// </summary>
    public sealed record TacticalPlacementModel(
        TacticalField Field,
        IReadOnlyList<TacticalPlacementCell> Cells,
        string? SelectedBenchInstanceId,
        int PlacedHeroCount,
        int FieldLimit)
    {
        /// <summary>True when the player has a bench hero picked for deployment.</summary>
        public bool HasSelection => SelectedBenchInstanceId is not null;

        /// <summary>True when the team already fills the player-level hero limit.</summary>
        public bool IsFieldFull => PlacedHeroCount >= FieldLimit;

        /// <summary>True when at least one more hero can be deployed onto the field.</summary>
        public bool CanPlaceMore => PlacedHeroCount < FieldLimit;

        /// <summary>Number of empty player cells (highlighted or plain).</summary>
        public int FreePlayerCellCount =>
            Cells.Count(cell => cell.State is TacticalCellState.Free or TacticalCellState.AvailableForPlacement);

        /// <summary>Number of cells currently highlighted as legal drop targets.</summary>
        public int HighlightedTargetCount => Cells.Count(cell => cell.IsPlacementTarget);

        /// <summary>
        /// Build the placement view-model from the live run. When
        /// <paramref name="selectedBenchInstanceId"/> names a hero that is actually
        /// on the bench, the free player cells light up as drop targets (unless the
        /// field is already full).
        /// </summary>
        public static TacticalPlacementModel Build(
            RunState run,
            string? selectedBenchInstanceId = null,
            EconomyConfig? economy = null)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var config = economy ?? EconomyConfig.Default;
            var field = TacticalField.Mvp;
            var fieldLimit = config.GetHeroLimitForLevel(run.PlayerLevel);

            // Only treat the selection as active when the id really sits on the bench;
            // a stale id (e.g. the hero was already placed) must not light up the board.
            var selectionActive = selectedBenchInstanceId is not null
                && run.Bench.Any(hero => hero.InstanceId == selectedBenchInstanceId);
            var normalizedSelection = selectionActive ? selectedBenchInstanceId : null;

            var canPlaceMore = run.Team.Count < fieldLimit;
            var occupied = new Dictionary<TacticalPosition, HeroInstance>();
            foreach (var slot in run.Team)
            {
                occupied[slot.Position] = slot.Hero;
            }

            var cells = new List<TacticalPlacementCell>(field.CellCount);
            foreach (var position in field.CreateCells())
            {
                if (occupied.TryGetValue(position, out var hero))
                {
                    cells.Add(new TacticalPlacementCell(
                        position,
                        TacticalCellState.OccupiedAlly,
                        hero.InstanceId,
                        hero.HeroId));
                    continue;
                }

                if (!field.IsPlayerSide(position))
                {
                    cells.Add(new TacticalPlacementCell(position, TacticalCellState.Unavailable, null, null));
                    continue;
                }

                var state = normalizedSelection is not null && canPlaceMore
                    ? TacticalCellState.AvailableForPlacement
                    : TacticalCellState.Free;
                cells.Add(new TacticalPlacementCell(position, state, null, null));
            }

            return new TacticalPlacementModel(
                field,
                cells,
                normalizedSelection,
                run.Team.Count,
                fieldLimit);
        }

        /// <summary>Get the resolved cell at a board position.</summary>
        public TacticalPlacementCell CellAt(TacticalPosition position)
        {
            foreach (var cell in Cells)
            {
                if (cell.Position == position)
                {
                    return cell;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(position), "Position is outside the tactical placement board.");
        }

        /// <summary>True when the cell at <paramref name="position"/> is a legal drop target.</summary>
        public bool IsPlacementTarget(TacticalPosition position) => CellAt(position).IsPlacementTarget;
    }
}
