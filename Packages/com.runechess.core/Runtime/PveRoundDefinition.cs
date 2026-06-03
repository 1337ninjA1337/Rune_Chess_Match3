using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// A single MVP PvE round. The first five members keep the original combat
    /// contract (enemy id, rune seed, gold, tutorial protection); the next block
    /// encodes the GDD "Первые 10 раундов" table (round type, enemy name, design
    /// goal, whether the round fights, and the non-gold reward); the final member
    /// is the data-driven enemy roster used to resolve the round's autobattle.
    /// </summary>
    public sealed record PveRoundDefinition(
        int Round,
        string EnemyId,
        int CombatRuneSeed,
        int BaseGoldReward,
        bool PreventsRunDefeat = false,
        PveRoundType Type = PveRoundType.Combat,
        string EnemyName = "",
        string DesignGoal = "",
        bool HasCombat = true,
        PveRoundReward? Reward = null,
        IReadOnlyList<PveEnemyUnit>? EnemyRoster = null
    )
    {
        /// <summary>Non-gold reward for the round; never null for callers.</summary>
        public PveRoundReward RoundReward => Reward ?? PveRoundReward.GoldOnly;

        /// <summary>
        /// Data-driven enemy composition for the round; never null for callers.
        /// Non-combat rounds (event, enhanced shop) keep this empty.
        /// </summary>
        public IReadOnlyList<PveEnemyUnit> Roster => EnemyRoster ?? Array.Empty<PveEnemyUnit>();

        /// <summary>True when the round defines at least one data-driven enemy to fight.</summary>
        public bool HasEnemyRoster => Roster.Count > 0;

        /// <summary>
        /// Difficulty pacing tier from the GDD "Темп сложности" section.
        /// 1-3 fundamentals, 4-6 choices, 7-8 synergy, 9-10 full build.
        /// </summary>
        public PveDifficultyTier DifficultyTier => Round switch
        {
            <= 3 => PveDifficultyTier.Fundamentals,
            <= 6 => PveDifficultyTier.ChoicesAndCounters,
            <= 8 => PveDifficultyTier.SynergyCheck,
            _ => PveDifficultyTier.FullBuildCheck
        };
    }
}
