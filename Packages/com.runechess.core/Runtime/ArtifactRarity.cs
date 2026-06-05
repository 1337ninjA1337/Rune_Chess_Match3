namespace RuneChess.Core
{
    /// <summary>
    /// Rarity tiers for MVP artifacts. Common artifacts come from ordinary reward
    /// rounds; anything above Common is drawn from the rare pool offered by the boss
    /// and other rare-artifact rounds.
    /// </summary>
    public enum ArtifactRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
}
