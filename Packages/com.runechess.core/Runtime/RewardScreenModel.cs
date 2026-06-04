using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One labelled gold line on the reward screen (e.g. "Награда за раунд" / "+5").
    /// Pure data so the Unity layer draws the row without re-deriving the breakdown.
    /// </summary>
    public sealed record RewardGoldLine(string Label, int Amount, string Meta = "");

    /// <summary>
    /// View-model for the post-combat reward screen (GDD UI screen "Экран награды").
    /// It surfaces the four screen elements the GDD lists: the gold earned for the round
    /// (with a small breakdown), the choice of one of three artifacts, the possible hero
    /// reward and the continue control. The reward composition is read from the round's
    /// <see cref="PveRoundReward"/>; the artifact choices come from <see cref="ArtifactCatalog"/>
    /// keyed on the round seed so they are deterministic and smoke-testable without Unity.
    /// </summary>
    public sealed record RewardScreenModel(
        int Round,
        PveRoundType RoundType,
        string EnemyName,
        bool IsVictory,
        string ResultLabel,
        int BaseGold,
        int BonusGold,
        int TotalGold,
        IReadOnlyList<RewardGoldLine> GoldLines,
        bool OffersArtifactChoice,
        bool ArtifactIsRare,
        IReadOnlyList<RewardArtifactOption> ArtifactOptions,
        bool OffersHeroReward,
        string HeroRewardLabel,
        bool IsRunVictory,
        string ContinueLabel,
        string ContinueMeta)
    {
        /// <summary>Headline label for the reward screen.</summary>
        public static string DescribeResult(bool isVictory) => isVictory ? "НАГРАДА ЗА РАУНД" : "РАУНД ЗАВЕРШЁН";

        /// <summary>Whether a given artifact id is one of the offered choices.</summary>
        public bool IsOfferedArtifact(string id) =>
            ArtifactOptions.Any(option => string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Build the reward screen from a round definition and the gold actually granted.
        /// <paramref name="baseGold"/> is the round's base payout and <paramref name="bonusGold"/>
        /// the combat/commander bonus on top (chain reaction, alchemist, event). An optional
        /// <paramref name="artifactSeed"/> overrides the round combat seed used to pick the
        /// three artifact choices.
        /// </summary>
        public static RewardScreenModel Build(
            PveRoundDefinition round,
            bool isVictory,
            int baseGold,
            int bonusGold = 0,
            int? artifactSeed = null)
        {
            if (round is null)
            {
                throw new ArgumentNullException(nameof(round));
            }

            if (baseGold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseGold), "Base gold reward cannot be negative.");
            }

            if (bonusGold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bonusGold), "Bonus gold reward cannot be negative.");
            }

            var reward = round.RoundReward;
            var totalGold = baseGold + bonusGold;

            var goldLines = new List<RewardGoldLine>
            {
                new("Награда за раунд", baseGold, "ROUND")
            };
            if (bonusGold > 0)
            {
                goldLines.Add(new RewardGoldLine("Бонус боя", bonusGold, "BONUS"));
            }

            var offersArtifact = reward.Artifact || reward.RareArtifact || reward.ArtifactOrGold;
            var artifactOptions = offersArtifact
                ? ArtifactCatalog.OfferThree(artifactSeed ?? round.CombatRuneSeed, rare: reward.RareArtifact)
                : (IReadOnlyList<RewardArtifactOption>)Array.Empty<RewardArtifactOption>();

            var offersHero = reward.GrantsStarterHero || reward.HeroChoice;
            var heroRewardLabel = reward.GrantsStarterHero
                ? "Стартовый герой (1 стоимости)"
                : reward.HeroChoice
                    ? "Выбор одного из героев"
                    : string.Empty;

            var isRunVictory = reward.RunVictory || round.Round >= PveRunSchedule.FinalRound;

            return new RewardScreenModel(
                Round: round.Round,
                RoundType: round.Type,
                EnemyName: round.EnemyName,
                IsVictory: isVictory,
                ResultLabel: DescribeResult(isVictory),
                BaseGold: baseGold,
                BonusGold: bonusGold,
                TotalGold: totalGold,
                GoldLines: goldLines,
                OffersArtifactChoice: offersArtifact,
                ArtifactIsRare: reward.RareArtifact,
                ArtifactOptions: artifactOptions,
                OffersHeroReward: offersHero,
                HeroRewardLabel: heroRewardLabel,
                IsRunVictory: isRunVictory,
                ContinueLabel: isRunVictory ? "Итог забега" : "Продолжить",
                ContinueMeta: isRunVictory ? "SUMMARY" : "CONTINUE");
        }

        /// <summary>
        /// Convenience overload that reads the current round and result from a run that has
        /// just resolved combat (phase Reward, or Victory on the final round). The base gold
        /// is the round payout and <paramref name="bonusGold"/> the bonus already folded into
        /// the run's gold by <see cref="RunState.ClaimReward"/>.
        /// </summary>
        public static RewardScreenModel Build(RunState run, int bonusGold = 0)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var round = run.CurrentRoundDefinition;
            var isVictory = run.Phase != RunPhase.Defeat;
            return Build(round, isVictory, round.BaseGoldReward, bonusGold);
        }
    }
}
