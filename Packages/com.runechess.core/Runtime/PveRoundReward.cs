namespace RuneChess.Core
{
    /// <summary>
    /// Describes the non-gold reward components promised by a PvE round in the GDD
    /// "Награда" column. The gold portion lives on <see cref="PveRoundDefinition.BaseGoldReward"/>;
    /// these flags capture the heroes, artifacts, rerolls and run victory granted on top.
    /// </summary>
    public sealed record PveRoundReward(
        bool GrantsStarterHero = false,
        bool HeroChoice = false,
        bool Artifact = false,
        bool RareArtifact = false,
        bool ArtifactOrGold = false,
        bool FreeReroll = false,
        bool RunVictory = false)
    {
        /// <summary>A round whose only reward is the base gold payout.</summary>
        public static PveRoundReward GoldOnly { get; } = new();

        /// <summary>True when the round grants anything beyond its base gold.</summary>
        public bool HasBonusReward =>
            GrantsStarterHero
            || HeroChoice
            || Artifact
            || RareArtifact
            || ArtifactOrGold
            || FreeReroll
            || RunVictory;
    }
}
