using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic presentation glue for the post-combat reward screen overhaul
    /// (GDD UI screen «Экран награды»). The <see cref="RewardScreenModel"/> already says WHAT
    /// the screen shows (gold breakdown, three artifact choices, hero reward, continue control);
    /// this presentation says HOW it reads in the new portrait visual language: each artifact
    /// choice card gets the shared rarity-tier border (resolved from the artifact's full rarity
    /// via <see cref="ArtifactCatalog"/>, the same four tiers and frame asset hero cards use), and
    /// the gold total / continue CTA carry the project's one warm «go» accent
    /// (<see cref="UiTheme.GoldColor"/>, shared with the HUD gold readout and the main-menu run
    /// tile). Rendering itself is Unity-only (documented gap); the contract is verified headless
    /// by <c>tools/CoreSmoke</c>. All art direction is original (codex.md IP rule).
    /// </summary>
    public static class RewardScreenPresentation
    {
        /// <summary>Warm accent for the gold total and the continue call-to-action.</summary>
        public const uint AccentColor = UiTheme.GoldColor;

        /// <summary>
        /// Map an artifact rarity onto the shared rarity-tier visual. Artifacts and heroes use the
        /// same four tiers (common → legendary), so an artifact card borrows the hero rarity frame
        /// and tier colour rather than introducing a parallel palette.
        /// </summary>
        public static HeroRarity FrameRarity(ArtifactRarity rarity)
        {
            return rarity switch
            {
                ArtifactRarity.Common => HeroRarity.Common,
                ArtifactRarity.Rare => HeroRarity.Rare,
                ArtifactRarity.Epic => HeroRarity.Epic,
                ArtifactRarity.Legendary => HeroRarity.Legendary,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown artifact rarity.")
            };
        }

        /// <summary>The full rarity of an offered artifact option, resolved from the catalog.</summary>
        public static ArtifactRarity OptionRarity(RewardArtifactOption option)
        {
            if (option is null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            return ArtifactCatalog.Get(option.Id).Rarity;
        }

        /// <summary>Rarity-tier border colour for an artifact choice card.</summary>
        public static uint OptionFrameColor(RewardArtifactOption option) =>
            UiTheme.RarityColor(FrameRarity(OptionRarity(option)));

        /// <summary>Rarity-frame placeholder spec for an artifact choice card.</summary>
        public static PlaceholderAssetSpec OptionFrame(RewardArtifactOption option) =>
            PlaceholderAssetCatalog.RarityFrame(FrameRarity(OptionRarity(option)));
    }
}
