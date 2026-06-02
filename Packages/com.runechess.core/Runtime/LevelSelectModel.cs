using System;
using System.Collections.Generic;
using System.Text;

namespace RuneChess.Core
{
    /// <summary>
    /// View-model data for a single card on the level-select screen: the round's
    /// type, enemy, design goal, reward and progression status. Pure C# so the
    /// Unity layer only renders it.
    /// </summary>
    public sealed record LevelCard(
        int Round,
        PveRoundType Type,
        string EnemyName,
        string DesignGoal,
        int BaseGoldReward,
        PveRoundReward Reward,
        PveDifficultyTier DifficultyTier,
        bool HasCombat,
        LevelCardStatus Status)
    {
        /// <summary>Human-readable reward summary for the card (gold plus bonuses).</summary>
        public string RewardSummary
        {
            get
            {
                if (Reward.RunVictory)
                {
                    return "Победа в забеге";
                }

                var parts = new List<string>();
                if (BaseGoldReward > 0)
                {
                    parts.Add($"{BaseGoldReward} золота");
                }

                if (Reward.GrantsStarterHero)
                {
                    parts.Add("герой 1 стоимости");
                }

                if (Reward.HeroChoice)
                {
                    parts.Add("выбор героя");
                }

                if (Reward.Artifact)
                {
                    parts.Add("артефакт");
                }

                if (Reward.RareArtifact)
                {
                    parts.Add("редкий артефакт");
                }

                if (Reward.ArtifactOrGold)
                {
                    parts.Add("артефакт или золото");
                }

                if (Reward.FreeReroll)
                {
                    parts.Add("бесплатный reroll");
                }

                if (parts.Count == 0)
                {
                    return "—";
                }

                var summary = new StringBuilder();
                for (var i = 0; i < parts.Count; i += 1)
                {
                    if (i > 0)
                    {
                        summary.Append(", ");
                    }

                    summary.Append(parts[i]);
                }

                return summary.ToString();
            }
        }
    }

    /// <summary>
    /// Builds the ordered list of level-select cards for the 10-round MVP run from
    /// <see cref="PveRunSchedule"/> and the current run progress.
    /// </summary>
    public static class LevelSelectModel
    {
        /// <summary>
        /// Build cards for every scheduled round. Rounds before <paramref name="currentRound"/>
        /// are completed, the current round is selectable, and later rounds are locked.
        /// When <paramref name="runComplete"/> is true every round reads as completed.
        /// </summary>
        public static IReadOnlyList<LevelCard> Build(int currentRound, bool runComplete = false)
        {
            if (currentRound < PveRunSchedule.FirstRound || currentRound > PveRunSchedule.FinalRound)
            {
                throw new ArgumentOutOfRangeException(nameof(currentRound), "Current round is outside the MVP schedule.");
            }

            var cards = new List<LevelCard>(PveRunSchedule.Rounds.Count);
            foreach (var round in PveRunSchedule.Rounds)
            {
                var status = runComplete || round.Round < currentRound
                    ? LevelCardStatus.Completed
                    : round.Round == currentRound
                        ? LevelCardStatus.Current
                        : LevelCardStatus.Locked;

                cards.Add(new LevelCard(
                    round.Round,
                    round.Type,
                    round.EnemyName,
                    round.DesignGoal,
                    round.BaseGoldReward,
                    round.RoundReward,
                    round.DifficultyTier,
                    round.HasCombat,
                    status));
            }

            return cards;
        }

        /// <summary>Build the level-select cards for a live run.</summary>
        public static IReadOnlyList<LevelCard> Build(RunState run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return Build(run.Round, run.IsRunWon);
        }
    }
}
