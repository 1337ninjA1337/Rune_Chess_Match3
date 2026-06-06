using System;

namespace RuneChess.Core
{
    /// <summary>
    /// The aggregate reward a round pays out (GDD "итоговый расчёт наград за раунд"). It is the
    /// single source of truth for how the round's gold is composed — the base payout plus the
    /// chain-reaction, Alchemist-commander and round-end artifact bonuses — and which non-gold
    /// rewards the round offers. <see cref="RunState.ClaimReward"/> computes it at combat
    /// resolution (while the combat state still records the chain bonuses) and the reward
    /// screen reads the same numbers, so the gold credited and the gold shown never diverge.
    /// </summary>
    public sealed record RoundRewardBreakdown(
        int BaseGold,
        int ChainBonusGold,
        int AlchemistBonusGold,
        int ArtifactBonusGold,
        bool OffersArtifactChoice,
        bool ArtifactIsRare,
        bool OffersHeroReward,
        bool GrantsFreeReroll,
        bool IsRunVictory)
    {
        /// <summary>Total bonus gold layered on top of the base round payout.</summary>
        public int BonusGold => ChainBonusGold + AlchemistBonusGold + ArtifactBonusGold;

        /// <summary>Total gold the round credits to the run.</summary>
        public int TotalGold => BaseGold + BonusGold;

        /// <summary>
        /// Compute the reward a round pays when its combat resolves. <paramref name="goldReward"/>
        /// overrides the round's base gold payout when provided. The chain and Alchemist bonuses
        /// are read from the run's live combat state, so this must be called before the combat
        /// state is cleared.
        /// </summary>
        public static RoundRewardBreakdown ForCombatResolution(RunState run, int? goldReward = null)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var round = run.CurrentRoundDefinition;
            var baseGold = goldReward ?? round.BaseGoldReward;
            if (baseGold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goldReward), "Gold reward cannot be negative.");
            }

            var chainBonus = run.Combat?.EarnedChainFourGoldBonus == true
                ? CombatState.ChainFourGoldBonus
                : 0;
            var alchemistBonus = run.Commander.Id == CommanderCatalog.Alchemist.Id && run.Combat?.HadChainReaction == true
                ? RunState.AlchemistChainReactionGoldBonus
                : 0;
            var artifactBonus = run.Modifiers.RoundEndGoldBonus;

            var reward = round.RoundReward;
            var offersArtifact = reward.Artifact || reward.RareArtifact || reward.ArtifactOrGold;
            var offersHero = reward.GrantsStarterHero || reward.HeroChoice;
            var isRunVictory = reward.RunVictory || run.IsFinalRound;

            return new RoundRewardBreakdown(
                BaseGold: baseGold,
                ChainBonusGold: chainBonus,
                AlchemistBonusGold: alchemistBonus,
                ArtifactBonusGold: artifactBonus,
                OffersArtifactChoice: offersArtifact,
                ArtifactIsRare: reward.RareArtifact,
                OffersHeroReward: offersHero,
                GrantsFreeReroll: reward.FreeReroll,
                IsRunVictory: isRunVictory);
        }
    }
}
