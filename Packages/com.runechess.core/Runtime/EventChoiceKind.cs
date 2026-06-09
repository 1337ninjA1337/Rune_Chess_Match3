namespace RuneChess.Core
{
    /// <summary>
    /// The roguelite event archetypes the MVP event screen ("Экран события") must
    /// support. The first four mirror the GDD "События" list: trade run health for
    /// gold, take a free hero that carries a curse, empower one faction for the next
    /// battle, or sacrifice a hero in exchange for an artifact. The remaining
    /// archetypes broaden the event pool with pure-economy choices that the
    /// data-driven event resolution applies straight from the offered option's
    /// deltas (heal run health with gold, find free gold, or train for XP).
    /// </summary>
    public enum EventChoiceKind
    {
        TradeHealthForGold,
        CursedFreeHero,
        FactionBoost,
        SacrificeHeroForArtifact,
        GoldForHealth,
        GoldWindfall,
        TrainingBoon
    }
}
