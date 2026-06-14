using System;

namespace RuneChess.Core
{
    /// <summary>Whether the run is currently on a winning, losing or no active streak.</summary>
    public enum RunStreakKind
    {
        None,
        Winning,
        Losing
    }

    /// <summary>
    /// The run's active win/loss streak (GDD "серия побед/поражений") as pure data ready for the
    /// combat HUD. Only one direction can be active at a time because a round win clears the loss
    /// streak and a round loss clears the win streak, so this collapses the two raw counters into a
    /// single signed status the UI draws without owning any rules. The same count feeds the streak
    /// gold bonus (see <see cref="EconomyConfig.CalculateStreakBonus"/>).
    /// </summary>
    public sealed record RunStreakStatus(RunStreakKind Kind, int Count)
    {
        /// <summary>No active streak (the run has not yet won or lost a round).</summary>
        public static readonly RunStreakStatus None = new(RunStreakKind.None, 0);

        /// <summary>True when a winning or losing streak of at least one round is active.</summary>
        public bool IsActive => Kind != RunStreakKind.None && Count > 0;

        /// <summary>True when the active streak is a run of consecutive wins.</summary>
        public bool IsWinning => Kind == RunStreakKind.Winning && Count > 0;

        /// <summary>True when the active streak is a run of consecutive losses.</summary>
        public bool IsLosing => Kind == RunStreakKind.Losing && Count > 0;

        /// <summary>Compact HUD label, e.g. <c>W3</c>, <c>L2</c> or <c>—</c> when none is active.</summary>
        public string Label => Kind switch
        {
            RunStreakKind.Winning when Count > 0 => $"W{Count}",
            RunStreakKind.Losing when Count > 0 => $"L{Count}",
            _ => "—"
        };

        /// <summary>
        /// Collapse the two raw streak counters into a single status. Exactly one of the counters is
        /// expected to be non-zero (a win clears the loss streak and vice versa); if both are set the
        /// larger wins, and a tie resolves to the winning streak so the HUD never shows a blank.
        /// </summary>
        public static RunStreakStatus From(int winStreak, int lossStreak)
        {
            if (winStreak < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(winStreak), "Win streak cannot be negative.");
            }

            if (lossStreak < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lossStreak), "Loss streak cannot be negative.");
            }

            if (winStreak == 0 && lossStreak == 0)
            {
                return None;
            }

            return winStreak >= lossStreak
                ? new RunStreakStatus(RunStreakKind.Winning, winStreak)
                : new RunStreakStatus(RunStreakKind.Losing, lossStreak);
        }
    }
}
