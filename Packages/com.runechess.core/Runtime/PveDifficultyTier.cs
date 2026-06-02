namespace RuneChess.Core
{
    /// <summary>
    /// Difficulty pacing tiers for the MVP run, mirroring the GDD "Темп сложности"
    /// section: fundamentals (1-3), choices and counters (4-6), synergy check (7-8)
    /// and the full build check (9-10).
    /// </summary>
    public enum PveDifficultyTier
    {
        Fundamentals,
        ChoicesAndCounters,
        SynergyCheck,
        FullBuildCheck
    }
}
