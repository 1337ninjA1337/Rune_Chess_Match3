namespace RuneChess.Core
{
    /// <summary>
    /// One data-driven enemy unit in a PvE round roster: which hero definition it
    /// uses, at what star level, and where it stands on the enemy half of the MVP
    /// tactical field. Rosters live in <see cref="PveRunSchedule"/> so enemy
    /// compositions are authored as data, not derived from the player's own team.
    /// </summary>
    public sealed record PveEnemyUnit(
        string HeroId,
        int Stars,
        TacticalPosition Position
    );
}
