using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic single source of truth for screen-level combat feedback styling that the
    /// Unity layer overlays on the combat view. It styles the big-combo slow-motion signal (driven by
    /// <see cref="CombatState.IsCombatSlowed"/>); rendering the overlays is Unity-only (documented
    /// verification gap). The contract — the slow-mo vignette only shows while combat is slowed and
    /// uses a valid opacity tied to the combat speed rule — is verified headless via
    /// <c>tools/CoreSmoke</c>. All values are original tuning for this project.
    /// </summary>
    public static class BattleFeedbackStyle
    {
        // --- Big-combo slow-motion signal (task: visual cue for CombatState.IsCombatSlowed). ---

        /// <summary>Cool tint laid over the screen edges while combat is in big-combo slow motion.</summary>
        public const uint SlowdownVignetteColor = 0x4A6FA5u;

        /// <summary>Opacity of the slow-mo vignette while combat is slowed.</summary>
        public const double SlowdownVignetteOpacity = 0.25;

        /// <summary>The combat speed percent slow motion runs at (mirrors <see cref="CombatState.LargeComboCombatSpeedPercent"/>).</summary>
        public static int SlowdownSpeedPercent => CombatState.LargeComboCombatSpeedPercent;

        /// <summary>
        /// The slow-mo vignette opacity for a combat state: the styled opacity while combat is slowed,
        /// zero otherwise. Ties the visual directly to <see cref="CombatState.IsCombatSlowed"/> so the
        /// signal can never desync from the rule.
        /// </summary>
        public static double SlowdownVignetteOpacityFor(CombatState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.IsCombatSlowed ? SlowdownVignetteOpacity : 0.0;
        }

        // --- Focus-zone dim (task: highlight the focus zone, dim the other — ties to
        // BattleAttentionModel.PrimaryFocus / ShouldDimOffFocusZone). ---

        /// <summary>How strongly the off-focus zone is dimmed when one zone owns a clutch beat.</summary>
        public const double OffFocusDimOpacity = 0.45;

        /// <summary>
        /// The dim overlay opacity for a zone given the current cues: the styled dim only when the
        /// attention model says to dim the off-focus zone AND this zone is not the primary focus;
        /// zero otherwise. Ties the visual directly to <see cref="BattleAttentionModel"/> so the focus
        /// highlight can never disagree with the arbitration.
        /// </summary>
        public static double DimOpacityForZone(IReadOnlyList<BattleCue> cues, BattleZone zone)
        {
            if (cues is null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            if (!BattleAttentionModel.ShouldDimOffFocusZone(cues))
            {
                return 0.0;
            }

            return BattleAttentionModel.PrimaryFocus(cues) == zone ? 0.0 : OffFocusDimOpacity;
        }
    }
}
