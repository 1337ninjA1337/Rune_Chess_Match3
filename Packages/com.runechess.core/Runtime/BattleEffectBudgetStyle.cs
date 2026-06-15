using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic styling for how the combat screen honours the simultaneous-effect limit
    /// (GDD/codex: "Бой должен быть читаемым: не перегружай экран одновременными эффектами";
    /// "Ограничить количество одновременных визуальных эффектов"). <see cref="BattleReadabilityModel"/>
    /// already decides WHICH effects win the limited on-screen budget; this style decides how the
    /// render layer SHOWS that the limit was hit — the dropped beats batch into one quiet "+N"
    /// overflow badge instead of vanishing silently or stacking into a blur. Tying the visible set and
    /// the overflow count to the model here means the Unity layer can never surface more than the cap
    /// or mislabel the overflow. Rendering the badge is Unity-only (documented verification gap); the
    /// contract is verified headless via <c>tools/CoreSmoke</c>. All values are original tuning.
    /// </summary>
    public static class BattleEffectBudgetStyle
    {
        /// <summary>
        /// The simultaneous-effect cap the screen honours, mirrored from the readability model so the
        /// budget the layer draws against can never drift from the budget the model selects against.
        /// </summary>
        public static int MaxSimultaneousEffects => BattleReadabilityModel.MaxSimultaneousEffects;

        /// <summary>
        /// Muted tint for the "+N" overflow badge — present enough to acknowledge the batched beats,
        /// quiet enough that it never competes with the live effects it summarises.
        /// </summary>
        public const uint OverflowBadgeColor = 0x8A93A6u;

        /// <summary>Opacity of the overflow badge so it reads as a footnote, not another effect.</summary>
        public const double OverflowBadgeOpacity = 0.7;

        /// <summary>
        /// The effects the screen actually shows, capped at the budget. Delegates to
        /// <see cref="BattleReadabilityModel.SelectVisibleEffects"/> so the most fight-relevant beats
        /// always win the limited on-screen slots.
        /// </summary>
        public static IReadOnlyList<RuneEffect> VisibleEffects(IReadOnlyList<RuneEffect> effects, int? cap = null)
            => BattleReadabilityModel.SelectVisibleEffects(effects, cap);

        /// <summary>How many resolved effects were batched off-screen by the budget (0 when all fit).</summary>
        public static int OverflowCount(IReadOnlyList<RuneEffect> effects, int? cap = null)
        {
            if (effects is null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            var visible = BattleReadabilityModel.SelectVisibleEffects(effects, cap).Count;
            return effects.Count - visible;
        }

        /// <summary>True when more effects resolved than the budget shows, so the overflow badge is needed.</summary>
        public static bool HasOverflow(IReadOnlyList<RuneEffect> effects, int? cap = null)
            => OverflowCount(effects, cap) > 0;

        /// <summary>The overflow badge label ("+2") for the batched beats, or empty when nothing overflowed.</summary>
        public static string OverflowLabel(IReadOnlyList<RuneEffect> effects, int? cap = null)
        {
            var overflow = OverflowCount(effects, cap);
            return overflow > 0
                ? "+" + overflow.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }
}
