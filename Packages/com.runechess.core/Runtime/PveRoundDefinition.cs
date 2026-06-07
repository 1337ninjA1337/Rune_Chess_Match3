using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// A single MVP PvE round. The first five members keep the original combat
    /// contract (enemy id, rune seed, gold, tutorial protection); the next members
    /// encode the GDD "Первые 10 раундов" table (round type, enemy name, design goal,
    /// whether the round fights, and the non-gold reward); the final member is the
    /// data-driven enemy composition this round fields against the player.
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
        IReadOnlyList<PveEnemyUnit>? EnemyComposition = null
    )
    {
        private static readonly IReadOnlyList<PveEnemyUnit> NoEnemies = Array.Empty<PveEnemyUnit>();

        /// <summary>Non-gold reward for the round; never null for callers.</summary>
        public PveRoundReward RoundReward => Reward ?? PveRoundReward.GoldOnly;

        /// <summary>Data-driven enemy roster for the round; never null for callers.</summary>
        public IReadOnlyList<PveEnemyUnit> EnemyUnits => EnemyComposition ?? NoEnemies;

        /// <summary>True when the round actually fields enemies the player can fight.</summary>
        public bool HasEnemyComposition => HasCombat && EnemyUnits.Count > 0;

        /// <summary>Total enemy stars on the board, useful for run-health damage scaling.</summary>
        public int EnemyStarTotal => EnemyUnits.Sum(unit => unit.Stars);

        /// <summary>Compatibility alias for callers/tests that still use roster wording.</summary>
        public IReadOnlyList<PveEnemyUnit> Roster => EnemyUnits;

        /// <summary>Compatibility alias for callers/tests that still use roster wording.</summary>
        public bool HasEnemyRoster => HasEnemyComposition;

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
