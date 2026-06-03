using System;

namespace RuneChess.Core
{
    /// <summary>
    /// A single labelled stat shown on the level-complete screen (e.g. "MATCHES" / "7").
    /// Pure data so the Unity layer only draws the pill without knowing where the
    /// number came from.
    /// </summary>
    public sealed record LevelCompleteStat(string Label, string Value, string Meta = "");

    /// <summary>
    /// View-model for the post-combat level-complete screen (GDD UI screens: per-level
    /// results). It surfaces the battle result, how long the fight took, how many match-3
    /// moves the player made and the player-centric combat totals (damage, healing,
    /// shields) plus the gold reward. It reads from the resolved <see cref="BattleState"/>
    /// and <see cref="CombatState"/> so the Unity layer renders without owning any
    /// formatting rules; keeping it in core lets the numbers be smoke-tested.
    /// </summary>
    public sealed record LevelCompleteModel(
        BattleOutcome Outcome,
        string ResultLabel,
        bool IsVictory,
        int DurationSeconds,
        string DurationLabel,
        int Match3MovesUsed,
        int DamageDealt,
        int HealingDone,
        int ShieldGranted,
        int GoldEarned)
    {
        /// <summary>Human-readable headline for the battle result.</summary>
        public static string DescribeOutcome(BattleOutcome outcome) => outcome switch
        {
            BattleOutcome.PlayerVictory => "ПОБЕДА",
            BattleOutcome.PlayerDefeat => "ПОРАЖЕНИЕ",
            BattleOutcome.Ongoing => "БОЙ НЕ ЗАВЕРШЁН",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown battle outcome.")
        };

        /// <summary>Format a whole-second battle length as a <c>m:ss</c> label.</summary>
        public static string FormatDuration(int totalSeconds) => CombatHudModel.FormatTimer(totalSeconds);

        /// <summary>Round a combat total to a readable whole number (never negative).</summary>
        private static int RoundTotal(double value) => (int)Math.Round(Math.Max(0.0, value), MidpointRounding.AwayFromZero);

        /// <summary>
        /// Build the model from explicit numbers. Used directly when the caller already
        /// has the totals (for example the lightweight presentation flow) and by the
        /// <see cref="BattleState"/> overload below.
        /// </summary>
        public static LevelCompleteModel Build(
            BattleOutcome outcome,
            int durationSeconds,
            int match3MovesUsed,
            double damageDealt,
            double healingDone,
            double shieldGranted,
            int goldEarned)
        {
            if (durationSeconds < 0)
            {
                durationSeconds = 0;
            }

            if (match3MovesUsed < 0)
            {
                match3MovesUsed = 0;
            }

            if (goldEarned < 0)
            {
                goldEarned = 0;
            }

            return new LevelCompleteModel(
                Outcome: outcome,
                ResultLabel: DescribeOutcome(outcome),
                IsVictory: outcome == BattleOutcome.PlayerVictory,
                DurationSeconds: durationSeconds,
                DurationLabel: FormatDuration(durationSeconds),
                Match3MovesUsed: match3MovesUsed,
                DamageDealt: RoundTotal(damageDealt),
                HealingDone: RoundTotal(healingDone),
                ShieldGranted: RoundTotal(shieldGranted),
                GoldEarned: goldEarned);
        }

        /// <summary>
        /// Build the model from the resolved battle and the live match-3 state. The combat
        /// totals come from the player-centric accumulators on <see cref="BattleState"/>,
        /// the match-3 move count from <see cref="CombatState"/>, and the duration from the
        /// elapsed battle time.
        /// </summary>
        public static LevelCompleteModel Build(BattleState battle, CombatState combat, int goldEarned)
        {
            if (battle is null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (combat is null)
            {
                throw new ArgumentNullException(nameof(combat));
            }

            var durationSeconds = (int)Math.Round(battle.ElapsedSeconds, MidpointRounding.AwayFromZero);
            return Build(
                outcome: battle.Outcome,
                durationSeconds: durationSeconds,
                match3MovesUsed: combat.Match3MovesUsed,
                damageDealt: battle.PlayerDamageDealt,
                healingDone: battle.PlayerHealingDone,
                shieldGranted: battle.PlayerShieldGranted,
                goldEarned: goldEarned);
        }

        /// <summary>The stat pills to render in reading order on the screen.</summary>
        public LevelCompleteStat[] StatRow() => new[]
        {
            new LevelCompleteStat("ВРЕМЯ", DurationLabel, "TIME"),
            new LevelCompleteStat("ХОДЫ", Match3MovesUsed.ToString(), "MATCH-3"),
            new LevelCompleteStat("УРОН", DamageDealt.ToString(), "DAMAGE"),
            new LevelCompleteStat("ЛЕЧЕНИЕ", HealingDone.ToString(), "HEAL"),
            new LevelCompleteStat("ЩИТЫ", ShieldGranted.ToString(), "SHIELD"),
            new LevelCompleteStat("ЗОЛОТО", GoldEarned.ToString(), "GOLD")
        };
    }
}
