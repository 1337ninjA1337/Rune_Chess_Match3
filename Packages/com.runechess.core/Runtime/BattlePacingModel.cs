using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Adaptive combat-pacing decisions for when the player can't keep up with on-screen events
    /// (GDD/codex: "Уменьшить скорость боя, если игрок не успевает следить за событиями";
    /// "Бой должен быть читаемым"). Where <see cref="CombatState"/> already slows the clock a fixed
    /// amount for a single large combo (size-driven), this model reacts to attention *overload*: when
    /// more must-watch beats arrive at once than the player can track (the shared attention budget),
    /// it recommends a slower speed and a longer slowdown window — deeper and longer the worse the
    /// pileup — so the overflow reads sequentially instead of as a blur. Pure data; the combat loop and
    /// Unity render layer consume the recommendation. Live "can't keep up" playtests remain the
    /// documented verification gap (no Unity runtime in this environment).
    /// </summary>
    public static class BattlePacingModel
    {
        /// <summary>
        /// The number of simultaneous must-watch beats the player can comfortably track at once,
        /// reused from the shared attention budget so pacing and emphasis agree on what "too much" is.
        /// </summary>
        public const int TrackableEventBudget = BattleAttentionModel.MaxSimultaneousCues;

        /// <summary>
        /// Combat speed (percent of normal) for heavy attention overload — slower than the existing
        /// single-combo slowdown so a wall of simultaneous beats gets real breathing room.
        /// </summary>
        public const int OverloadedCombatSpeedPercent = 50;

        /// <summary>
        /// How many must-watch beats past the trackable budget counts as *heavy* overload (drops to
        /// <see cref="OverloadedCombatSpeedPercent"/> rather than the eased single-combo speed).
        /// </summary>
        public const int HeavyOverloadExcess = TrackableEventBudget;

        /// <summary>Extra slowdown granted per must-watch beat over budget, so a bigger pileup reads longer.</summary>
        public const int SlowdownMillisecondsPerExcessEvent = 500;

        /// <summary>Ceiling on the adaptive slowdown window so combat never crawls to a halt.</summary>
        public const int MaxAdaptiveSlowdownMilliseconds = 2 * CombatState.LargeComboSlowdownMilliseconds;

        /// <summary>
        /// Count the must-watch beats among <paramref name="cues"/>: <see cref="BattleEventSalience.Major"/>
        /// or higher. Minor support ticks can be batched and do not pressure the player's attention.
        /// </summary>
        public static int CountMustWatch(IReadOnlyList<BattleCue> cues)
        {
            if (cues is null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            return cues.Count(cue => cue.Salience >= BattleEventSalience.Major);
        }

        /// <summary>
        /// How far the simultaneous must-watch load exceeds what the player can track at once
        /// (0 when within budget). The driver of every adaptive-pacing decision below.
        /// </summary>
        public static int AttentionExcess(IReadOnlyList<BattleCue> cues)
        {
            return Math.Max(0, CountMustWatch(cues) - TrackableEventBudget);
        }

        /// <summary>
        /// True when the player is being shown more must-watch beats at once than they can track,
        /// i.e. they are falling behind the fight and the clock should ease off.
        /// </summary>
        public static bool IsPlayerFallingBehind(IReadOnlyList<BattleCue> cues)
        {
            return AttentionExcess(cues) > 0;
        }

        /// <summary>
        /// Recommended combat speed (percent of normal) for the current beat load: full speed within
        /// budget, the existing eased single-combo speed for mild overload, and a deeper
        /// <see cref="OverloadedCombatSpeedPercent"/> once the pileup is heavy.
        /// </summary>
        public static int RecommendedSpeedPercent(IReadOnlyList<BattleCue> cues)
        {
            var excess = AttentionExcess(cues);
            if (excess <= 0)
            {
                return CombatState.NormalCombatSpeedPercent;
            }

            return excess >= HeavyOverloadExcess
                ? OverloadedCombatSpeedPercent
                : CombatState.LargeComboCombatSpeedPercent;
        }

        /// <summary>
        /// Recommended slowdown window (milliseconds) for the current beat load: none within budget,
        /// otherwise the single-combo window extended by every beat over budget, capped at
        /// <see cref="MaxAdaptiveSlowdownMilliseconds"/> so the fight never stalls.
        /// </summary>
        public static int RecommendedSlowdownMilliseconds(IReadOnlyList<BattleCue> cues)
        {
            var excess = AttentionExcess(cues);
            if (excess <= 0)
            {
                return 0;
            }

            var window = CombatState.LargeComboSlowdownMilliseconds
                + (excess - 1) * SlowdownMillisecondsPerExcessEvent;
            return Math.Min(MaxAdaptiveSlowdownMilliseconds, window);
        }
    }
}
