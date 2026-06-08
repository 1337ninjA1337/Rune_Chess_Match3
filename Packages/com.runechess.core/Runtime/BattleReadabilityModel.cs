using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Importance tier of a combat visual event, so the presentation layer can decide what to
    /// emphasise when many effects resolve at once (codex UI rule: "приоритет у читаемости",
    /// "Выделять важные события боя визуально").
    /// </summary>
    public enum BattleEventSalience
    {
        /// <summary>Routine effect (a base match-3 support tick). Safe to under-draw or batch.</summary>
        Minor,

        /// <summary>Notable effect (damage, an enhanced match-4, an early chain). Worth a clear cue.</summary>
        Major,

        /// <summary>Fight-swinging effect (great rune, T/L mass effect, deep chain). Always highlight.</summary>
        Critical
    }

    /// <summary>
    /// Pure-data readability contract for the combat screen (GDD/codex: "Бой должен быть читаемым: не
    /// перегружай экран одновременными эффектами"; "Ограничить количество одновременных визуальных
    /// эффектов"; "Выделять важные события боя визуально"; "замедление при больших combo-событиях").
    /// It owns the decisions the presentation layer must respect — how many effects may be shown at
    /// once, which effects matter most, and what counts as a big-combo slowdown event — so the Unity
    /// render layer stays a thin consumer. Live attention/overload playtests remain the documented
    /// verification gap (no Unity runtime in this environment).
    /// </summary>
    public static class BattleReadabilityModel
    {
        /// <summary>
        /// Maximum rune effects the combat screen may surface simultaneously before it overloads the
        /// player's attention. Kept small so big chains read as one emphasised beat, not a wall of cues.
        /// </summary>
        public const int MaxSimultaneousEffects = 3;

        /// <summary>
        /// The big-combo slowdown threshold and timing are owned by <see cref="CombatState"/>
        /// (70% speed for 1s on a match-4+/chain). This mirrors that trigger for a single resolved
        /// effect so the readability layer can flag the same fight-swinging beats the clock slows for.
        /// </summary>
        public static bool IsLargeComboEvent(RuneEffect effect)
        {
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            return effect.MatchedRunesCount >= CombatState.LargeComboMatchedRunesThreshold
                || effect.ChainNumber - 1 >= CombatState.LargeComboComboDepthThreshold;
        }

        /// <summary>Rank a resolved rune effect by how strongly it should be emphasised on screen.</summary>
        public static BattleEventSalience SalienceOf(RuneEffect effect)
        {
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if (effect.IsGreatRuneActivation
                || effect.CreatesGreatRune
                || effect.IsMassEffect
                || effect.ChainNumber >= CombatState.ChainFourGoldBonusMinimumChainNumber)
            {
                return BattleEventSalience.Critical;
            }

            if (effect.IsDamage
                || effect.Tier != RuneMatchTier.Match3
                || effect.ChainNumber >= 2)
            {
                return BattleEventSalience.Major;
            }

            return BattleEventSalience.Minor;
        }

        /// <summary>
        /// Choose which effects to surface when several resolve together: keep the most important
        /// ones, capped at <paramref name="cap"/> (defaults to <see cref="MaxSimultaneousEffects"/>).
        /// Ordering is by salience, then chain depth, then power, so the most fight-relevant beats win
        /// the limited on-screen budget. Stable for the presentation layer to lay out deterministically.
        /// </summary>
        public static IReadOnlyList<RuneEffect> SelectVisibleEffects(
            IReadOnlyList<RuneEffect> effects,
            int? cap = null)
        {
            if (effects is null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            var limit = cap ?? MaxSimultaneousEffects;
            if (limit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cap), "Effect cap cannot be negative.");
            }

            return effects
                .OrderByDescending(effect => (int)SalienceOf(effect))
                .ThenByDescending(effect => effect.ChainNumber)
                .ThenByDescending(effect => effect.Power)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// True when an on-screen set respects the attention budget (codex: "Проверить, что UI не
        /// перегружает внимание игрока"). The render layer must never show more than the cap at once.
        /// </summary>
        public static bool RespectsAttentionBudget(IReadOnlyList<RuneEffect> shownEffects, int? cap = null)
        {
            if (shownEffects is null)
            {
                throw new ArgumentNullException(nameof(shownEffects));
            }

            var limit = cap ?? MaxSimultaneousEffects;
            return shownEffects.Count <= limit;
        }
    }
}
