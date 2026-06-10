namespace RuneChess.Core
{
    /// <summary>
    /// Lifecycle of a <see cref="LivePvpMatch"/> (GDD "Будущие режимы": "Live PvP").
    /// A match is <see cref="InProgress"/> while two or more players are still alive and
    /// becomes <see cref="Finished"/> once one (or, in a rare mutual knockout, zero) remain.
    /// </summary>
    public enum LivePvpPhase
    {
        InProgress,
        Finished,
    }
}
