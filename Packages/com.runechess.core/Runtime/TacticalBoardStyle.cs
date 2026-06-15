using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Pointer/selection interaction state of a tactical-board cell, independent of its
    /// logical <see cref="TacticalCellState"/>. Drives the hover/selection overlay so the
    /// Unity layer only renders what this resolves (see <see cref="TacticalBoardStyle"/>).
    /// </summary>
    public enum TacticalCellInteraction
    {
        None,
        Hovered,
        Selected
    }

    /// <summary>
    /// Fully resolved visual styling for one tactical-board cell: packed <c>0xRRGGBB</c>
    /// fill/border colours, border thickness and the depth-aware row scale that gives the
    /// grid its flat-perspective (pseudo-3D) look. Pure data so the Unity layer renders
    /// without owning any styling rules.
    /// </summary>
    public sealed record TacticalCellStyle(
        uint FillColor,
        uint BorderColor,
        float BorderThickness,
        float RowScale);

    /// <summary>
    /// Engine-agnostic single source of truth for the tactical-board presentation overhaul:
    /// the cell-state palette, the mid-line dividing the player and enemy halves, the
    /// hover/selection overlay and the flat-perspective row projection. Colours are packed
    /// as <c>0xRRGGBB</c> so the core package stays free of any engine type; the Unity layer
    /// maps them via <c>GameColors</c>. All values are original art direction for this
    /// project (codex.md IP rule — no Dota/Valve names, lore or visual style) and follow the
    /// "Tactical arena" section of <c>docs/visual-style.md</c>.
    ///
    /// Rendering itself is Unity-only and remains a documented verification gap in this
    /// environment; the styling *contract* (distinct states, ordered overlay strength,
    /// monotonic depth scale, arena mapping) is verified headless by <c>tools/CoreSmoke</c>.
    /// </summary>
    public static class TacticalBoardStyle
    {
        // --- Cell-state fill palette (single source of truth; the Unity GameColors layer
        // delegates here so the board and its smoke checks never drift). ---

        /// <summary>Empty enemy-half cell (upper board, opponent territory).</summary>
        public const uint EnemyHalfColor = 0x342638u;

        /// <summary>Empty player-half cell (lower board, your territory).</summary>
        public const uint PlayerHalfColor = 0x20352Fu;

        /// <summary>Player-half cell highlighted as a legal placement target.</summary>
        public const uint PlacementAvailableColor = 0x2F5C46u;

        /// <summary>Cell that cannot be used (enemy half during prep, locked cells).</summary>
        public const uint UnavailableColor = 0x25282Eu;

        /// <summary>Cell occupied by an allied unit.</summary>
        public const uint AllyOccupiedColor = 0x255B4Bu;

        /// <summary>Cell occupied by an enemy unit.</summary>
        public const uint EnemyOccupiedColor = 0x5B2C36u;

        /// <summary>Fallback fill for an unexpected state (defensive; never the happy path).</summary>
        public const uint FallbackFillColor = 0x2E333Au;

        /// <summary>Resolve the fill colour for a logical cell state.</summary>
        public static uint CellFillColor(TacticalCellState state)
        {
            return state switch
            {
                TacticalCellState.Free => PlayerHalfColor,
                TacticalCellState.OccupiedAlly => AllyOccupiedColor,
                TacticalCellState.OccupiedEnemy => EnemyOccupiedColor,
                TacticalCellState.AvailableForPlacement => PlacementAvailableColor,
                TacticalCellState.Unavailable => UnavailableColor,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown tactical cell state.")
            };
        }

        // --- Cell border palette. A placement target glows with a bright accent so it reads
        // as actionable; occupied/idle cells use a quiet outline. ---

        /// <summary>Default quiet outline for idle/occupied cells.</summary>
        public const uint CellBorderColor = 0x515761u;

        /// <summary>Bright accent outline for a legal placement target.</summary>
        public const uint PlacementBorderColor = 0x5BC07Au;

        /// <summary>Resolve the border colour for a logical cell state.</summary>
        public static uint CellBorderColorFor(TacticalCellState state)
        {
            // Touch the enum so an unknown value is rejected consistently with the fill path.
            _ = CellFillColor(state);
            return state == TacticalCellState.AvailableForPlacement ? PlacementBorderColor : CellBorderColor;
        }

        /// <summary>Default cell outline thickness (density-independent units).</summary>
        public const float CellBorderThickness = UiTheme.BorderThin;

        /// <summary>A placement target draws a slightly heavier outline to stand out.</summary>
        public const float PlacementBorderThickness = UiTheme.BorderRegular;

        /// <summary>Resolve the border thickness for a logical cell state.</summary>
        public static float CellBorderThicknessFor(TacticalCellState state)
        {
            _ = CellFillColor(state);
            return state == TacticalCellState.AvailableForPlacement ? PlacementBorderThickness : CellBorderThickness;
        }

        // --- Mid-line dividing the player half (lower) from the enemy half (upper). ---

        /// <summary>Colour of the explicit divider drawn across the board's mid-line.</summary>
        public const uint MidLineColor = 0x6E6256u;

        /// <summary>Thickness of the mid-line divider (density-independent units).</summary>
        public const float MidLineThickness = UiTheme.BorderThick;

        // --- Hover / selection overlay (interaction feedback). A translucent white veil
        // brightens the cell; selection is stronger than a passing hover. ---

        /// <summary>Overlay tint laid over a cell to convey hover/selection.</summary>
        public const uint InteractionOverlayColor = 0xFFFFFFu;

        /// <summary>Opacity of the hover overlay (0..1).</summary>
        public const float HoverOverlayOpacity = 0.12f;

        /// <summary>Opacity of the selection overlay (0..1); stronger than hover.</summary>
        public const float SelectedOverlayOpacity = 0.28f;

        /// <summary>Resolve the overlay opacity for an interaction state (0 when idle).</summary>
        public static float InteractionOverlayOpacity(TacticalCellInteraction interaction)
        {
            return interaction switch
            {
                TacticalCellInteraction.None => 0f,
                TacticalCellInteraction.Hovered => HoverOverlayOpacity,
                TacticalCellInteraction.Selected => SelectedOverlayOpacity,
                _ => throw new ArgumentOutOfRangeException(nameof(interaction), interaction, "Unknown cell interaction.")
            };
        }

        // --- Flat-perspective (pseudo-3D) projection. The board tilts away from the viewer:
        // the near player rows (bottom) render at full size, the far enemy rows (top) shrink
        // and inset, giving an original isometric-ish read without owning real 3D. ---

        /// <summary>Scale of the nearest row (player frontline, bottom of the board).</summary>
        public const float NearRowScale = 1.0f;

        /// <summary>Scale of the farthest row (enemy backline, top of the board).</summary>
        public const float FarRowScale = 0.82f;

        /// <summary>Horizontal inset (fraction of cell width) applied to the farthest row.</summary>
        public const float FarRowHorizontalInset = 0.10f;

        /// <summary>
        /// Depth scale for a row, interpolating from <see cref="FarRowScale"/> at the top
        /// (row 0, far enemy side) to <see cref="NearRowScale"/> at the bottom (nearest player
        /// side). A single-row board renders at near scale.
        /// </summary>
        public static float RowDepthScale(int row, int rows)
        {
            return NearRowScale - (DepthFraction(row, rows) * (NearRowScale - FarRowScale));
        }

        /// <summary>
        /// Horizontal inset for a row (fraction of cell width), 0 at the near edge growing to
        /// <see cref="FarRowHorizontalInset"/> at the far edge — the trapezoid that sells depth.
        /// </summary>
        public static float RowHorizontalInset(int row, int rows)
        {
            return DepthFraction(row, rows) * FarRowHorizontalInset;
        }

        /// <summary>How far back a row sits, 0 (nearest, bottom) .. 1 (farthest, top).</summary>
        private static float DepthFraction(int row, int rows)
        {
            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), rows, "Board must have at least one row.");
            }

            if (row < 0 || row >= rows)
            {
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row is outside the board.");
            }

            if (rows == 1)
            {
                return 0f;
            }

            // Top row (0) is farthest; bottom row (rows-1) is nearest.
            return (float)(rows - 1 - row) / (rows - 1);
        }

        // --- Arena background. Delegates to the placeholder asset catalog so the board and the
        // asset pipeline share one mapping from battle archetype to background key. ---

        /// <summary>Arena background placeholder spec for a battle round archetype.</summary>
        public static PlaceholderAssetSpec ArenaBackground(PveRoundType roundType)
        {
            return PlaceholderAssetCatalog.ArenaBackgroundFor(roundType);
        }

        // --- Full cell resolution (fill + border + depth scale) for the Unity renderer. ---

        /// <summary>
        /// Resolve the complete styling for a board cell from its logical state and the row it
        /// occupies. Interaction (hover/selection) is layered as a separate translucent overlay
        /// (<see cref="InteractionOverlayOpacity"/>) so it composes over any state.
        /// </summary>
        public static TacticalCellStyle ResolveCell(TacticalCellState state, int row, int rows)
        {
            return new TacticalCellStyle(
                CellFillColor(state),
                CellBorderColorFor(state),
                CellBorderThicknessFor(state),
                RowDepthScale(row, rows));
        }
    }
}
