namespace RuneChess.Core
{
    /// <summary>
    /// A single MVP PvE round. The first five members keep the original combat
    /// contract (enemy id, rune seed, gold, tutorial protection); the remaining
    /// members encode the GDD "Первые 10 раундов" table (round type, enemy name,
    /// design goal, whether the round fights, and the non-gold reward).
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
        PveRoundReward? Reward = null
    )
    {
        /// <summary>Non-gold reward for the round; never null for callers.</summary>
        public PveRoundReward RoundReward => Reward ?? PveRoundReward.GoldOnly;

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
