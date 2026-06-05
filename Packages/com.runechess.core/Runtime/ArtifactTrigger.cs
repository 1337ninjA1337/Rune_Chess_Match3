namespace RuneChess.Core
{
    /// <summary>
    /// When an artifact effect fires. The MVP keeps a small set of triggers covering
    /// the catalog: always-on passives, the start of combat, collecting a matching
    /// rune, a chain reaction, the death of an ally, and the end of a round.
    /// </summary>
    public enum ArtifactTrigger
    {
        Passive,
        CombatStart,
        OnRuneMatch,
        OnChainReaction,
        OnAllyDeath,
        RoundEnd
    }
}
