using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Aggregated economy modifiers contributed by the artifacts a run currently owns
    /// (GDD P1 "артефакты как модификаторы ... экономики"). Each owned artifact adds its
    /// tunable contribution and duplicates stack. Magnitudes live here as named constants
    /// per the codex data rule so balance can change without touching the run logic.
    /// Rune and combat artifact modifiers are tracked as their own tasks; this slice wires
    /// the economy artifacts into the live gold and experience paths.
    /// </summary>
    public sealed record ArtifactModifiers(
        int RoundEndGoldBonus,
        int BuyXpDiscount)
    {
        /// <summary>"Печать Торговца": +1 gold after each battle (economy / RoundEnd).</summary>
        public const int MerchantSealRoundEndGold = 1;

        /// <summary>"Том Ученика": buying experience costs one gold less (economy / Passive).</summary>
        public const int ApprenticeTomeXpDiscount = 1;

        /// <summary>The neutral modifier set used when a run owns no economy artifacts.</summary>
        public static ArtifactModifiers None { get; } = new(0, 0);

        /// <summary>
        /// Sum the economy contributions of every owned artifact. Unknown ids (artifacts
        /// whose effect is combat or rune based) contribute nothing here.
        /// </summary>
        public static ArtifactModifiers From(IEnumerable<ArtifactState> artifacts)
        {
            if (artifacts is null)
            {
                throw new ArgumentNullException(nameof(artifacts));
            }

            var roundEndGoldBonus = 0;
            var buyXpDiscount = 0;
            foreach (var artifact in artifacts)
            {
                switch (artifact.Id?.ToLowerInvariant())
                {
                    case "merchant_seal":
                        roundEndGoldBonus += MerchantSealRoundEndGold;
                        break;
                    case "apprentice_tome":
                        buyXpDiscount += ApprenticeTomeXpDiscount;
                        break;
                }
            }

            return new ArtifactModifiers(roundEndGoldBonus, buyXpDiscount);
        }
    }
}
