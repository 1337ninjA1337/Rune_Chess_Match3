namespace RuneChess.Core
{
    /// <summary>
    /// Data model for an MVP artifact, with the GDD-required fields: id, name, rarity,
    /// effect (the system it modifies), trigger (when it fires) and a player-facing
    /// description. Effect magnitudes stay in the description text for the MVP; wiring
    /// the modifier into combat/economy/rune systems is a separate task. Pure data so
    /// the catalog can be smoke-tested without Unity.
    /// </summary>
    public sealed record ArtifactDefinition(
        string Id,
        string Name,
        ArtifactRarity Rarity,
        ArtifactEffectKind Effect,
        ArtifactTrigger Trigger,
        string Description)
    {
        /// <summary>True for any artifact above Common rarity (the rare reward pool).</summary>
        public bool IsRare => Rarity != ArtifactRarity.Common;

        /// <summary>Project this definition onto a reward-screen choice card.</summary>
        public RewardArtifactOption ToRewardOption() => new(Id, Name, Description, IsRare);

        /// <summary>Convert this artifact into the run's stored artifact record.</summary>
        public ArtifactState ToArtifactState() => new(Id, Name);
    }
}
