namespace RuneChess.Core
{
    /// <summary>
    /// Round archetypes for the 10-round MVP PvE run, mirroring the GDD
    /// "Первые 10 раундов" table (tutorial, combat, event, elite, boss, shop, final).
    /// </summary>
    public enum PveRoundType
    {
        Tutorial,
        Combat,
        Event,
        Elite,
        Boss,
        EnhancedShop,
        FinalBoss
    }
}
