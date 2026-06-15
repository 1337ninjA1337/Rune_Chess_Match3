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

        // --- Adaptive attention-overload signal (task: apply adaptive speed on attention overload —
        // ties to BattlePacingModel.RecommendedSpeedPercent / AttentionExcess). Distinct from the
        // fixed big-combo slow-mo above: that reacts to one large combo's size, this reacts to a wall
        // of simultaneous must-watch beats the player can't track, easing the clock so the overflow
        // reads sequentially. ---

        /// <summary>
        /// Warm tint laid over the screen while the player is falling behind a beat pileup, kept
        /// deliberately distinct from the cool big-combo slow-mo vignette so the two "time is easing"
        /// signals never read as the same event.
        /// </summary>
        public const uint OverloadVignetteColor = 0xC8924Au;

        /// <summary>Opacity of the overload vignette at the mildest overload (one beat over budget).</summary>
        public const double OverloadVignetteBaseOpacity = 0.18;

        /// <summary>Extra opacity per must-watch beat over budget, so a heavier pileup reads stronger.</summary>
        public const double OverloadVignetteOpacityPerExcess = 0.06;

        /// <summary>Ceiling on the overload vignette opacity so it never blacks the screen out.</summary>
        public const double MaxOverloadVignetteOpacity = 0.40;

        /// <summary>
        /// The adaptive combat speed (percent of normal) for the current beat load — a passthrough to
        /// <see cref="BattlePacingModel.RecommendedSpeedPercent"/> so the visual signal and the pacing
        /// decision are read from one source and can never disagree.
        /// </summary>
        public static int AdaptiveSpeedPercent(IReadOnlyList<BattleCue> cues)
            => BattlePacingModel.RecommendedSpeedPercent(cues);

        /// <summary>
        /// The overload vignette opacity for the current beat load: zero within budget, otherwise the
        /// base opacity plus a step per beat over budget, capped at <see cref="MaxOverloadVignetteOpacity"/>.
        /// Ties the visual directly to <see cref="BattlePacingModel.AttentionExcess"/> so the signal
        /// only shows exactly when the pacing model eases the clock, and scales with how far behind the
        /// player has fallen.
        /// </summary>
        public static double OverloadVignetteOpacityFor(IReadOnlyList<BattleCue> cues)
        {
            if (cues is null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            var excess = BattlePacingModel.AttentionExcess(cues);
            if (excess <= 0)
            {
                return 0.0;
            }

            var opacity = OverloadVignetteBaseOpacity + (excess - 1) * OverloadVignetteOpacityPerExcess;
            return Math.Min(MaxOverloadVignetteOpacity, opacity);
        }
    }
}
