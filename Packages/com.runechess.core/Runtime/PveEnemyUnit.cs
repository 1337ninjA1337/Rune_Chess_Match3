namespace RuneChess.Core
{
    /// <summary>
    /// A single data-driven enemy in a PvE round composition: which hero definition to
    /// instantiate (from <see cref="HeroCatalog"/>), at what star level, and where on the
    /// enemy half of the MVP tactical field. Keeping the roster as plain data lets the GDD
    /// PvE encounters live in <see cref="PveRunSchedule"/> instead of being computed from
    /// the player's own team.
    /// </summary>
    public sealed record PveEnemyUnit(string HeroId, int Stars, TacticalPosition Position);
}
