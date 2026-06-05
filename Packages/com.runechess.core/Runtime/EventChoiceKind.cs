namespace RuneChess.Core
{
    /// <summary>
    /// The four roguelite event archetypes the MVP event screen ("Экран события")
    /// must support, mirroring the GDD "События" list: trade run health for gold,
    /// take a free hero that carries a curse, empower one faction for the next
    /// battle, or sacrifice a hero in exchange for an artifact.
    /// </summary>
    public enum EventChoiceKind
    {
        TradeHealthForGold,
        CursedFreeHero,
        FactionBoost,
        SacrificeHeroForArtifact
    }
}
