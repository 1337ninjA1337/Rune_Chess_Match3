using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>One GDD balance milestone and whether the shipped economy can reach it.</summary>
    public sealed record BalanceMilestone(
        string Name,
        int Round,
        int RequiredGold,
        int AvailableGold)
    {
        /// <summary>True when the player can afford this milestone's plan with the gold the run pays out by its round.</summary>
        public bool IsReachable => AvailableGold >= RequiredGold;

        /// <summary>Spare gold left after funding the milestone's minimum plan (rerolls, mistakes, extra heroes).</summary>
        public int GoldMargin => AvailableGold - RequiredGold;
    }

    /// <summary>
    /// A deterministic feasibility model for the GDD "Баланс целей MVP" goals
    /// (see <see cref="BalanceTargets"/>). It does not simulate RNG or average play; instead it
    /// derives, purely from <see cref="EconomyConfig"/> and <see cref="PveRunSchedule"/>, the gold
    /// an on-track player who wins every round has by each milestone round, and the minimum gold a
    /// concrete plan needs to hit that milestone. If every milestone's available gold covers its
    /// required gold (with margin), the shipped economy provably supports the GDD pacing goals.
    /// The margins are what give the GDD's "обычно/должен" wording room for rerolls and imperfect play.
    /// </summary>
    public static class BalanceProjection
    {
        /// <summary>Common-hero shop price (GDD "Стоимость действий": купить героя Common = 1).</summary>
        public const int CommonHeroCost = 1;

        /// <summary>Heroes the run hands the player for free before any purchase (round-1 starter hero).</summary>
        public const int StarterHeroes = 1;

        /// <summary>
        /// Total gold an always-winning player has earned by the end of each round: starting gold
        /// plus the cumulative base gold payouts from the schedule. Index 0 is round 1.
        /// Interest and win-streak bonuses are intentionally omitted so the model is a lower bound.
        /// </summary>
        public static IReadOnlyList<int> CumulativeGoldByRound(EconomyConfig? economy = null)
        {
            var config = economy ?? EconomyConfig.Default;
            var cumulative = new List<int>(PveRunSchedule.FinalRound);
            var running = config.StartingGold;
            for (var round = PveRunSchedule.FirstRound; round <= PveRunSchedule.FinalRound; round += 1)
            {
                running += PveRunSchedule.GetRound(round).BaseGoldReward;
                cumulative.Add(running);
            }

            return cumulative;
        }

        /// <summary>Cumulative gold earned by the end of the given round.</summary>
        public static int GoldAvailableByRound(int round, EconomyConfig? economy = null)
        {
            if (round is < PveRunSchedule.FirstRound or > PveRunSchedule.FinalRound)
            {
                throw new ArgumentOutOfRangeException(nameof(round), "Round is outside the MVP run.");
            }

            return CumulativeGoldByRound(economy)[round - 1];
        }

        /// <summary>Smallest player level whose field limit can hold <paramref name="heroCount"/> heroes.</summary>
        public static int RequiredLevelToField(int heroCount, EconomyConfig? economy = null)
        {
            if (heroCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(heroCount), "Hero count starts at one.");
            }

            var config = economy ?? EconomyConfig.Default;
            for (var level = 1; level <= config.MaxPlayerLevel; level += 1)
            {
                if (config.GetHeroLimitForLevel(level) >= heroCount)
                {
                    return level;
                }
            }

            throw new InvalidOperationException($"No configured player level can field {heroCount} heroes.");
        }

        /// <summary>Gold needed to buy enough experience to reach <paramref name="targetLevel"/> from level 1.</summary>
        public static int GoldToReachLevel(int targetLevel, EconomyConfig? economy = null)
        {
            var config = economy ?? EconomyConfig.Default;
            if (targetLevel < config.StartingPlayerLevel || targetLevel > config.MaxPlayerLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(targetLevel), "Target level is outside the configured range.");
            }

            var xpNeeded = config.GetXpThresholdForLevel(targetLevel) - config.GetXpThresholdForLevel(config.StartingPlayerLevel);
            if (xpNeeded <= 0)
            {
                return 0;
            }

            var purchases = (int)Math.Ceiling(xpNeeded / (double)config.XpPerPurchase);
            return purchases * config.BuyXpCost;
        }

        /// <summary>The four economy-grounded GDD milestones with their required and available gold.</summary>
        public static IReadOnlyList<BalanceMilestone> Milestones(EconomyConfig? economy = null)
        {
            var config = economy ?? EconomyConfig.Default;

            // R3: own the top of the ownership band (4 heroes). One is the free starter hero.
            var ownTarget = BalanceTargets.HeroesOwnedByRound3.Max;
            var heroesByRound3Cost = Math.Max(0, ownTarget - StarterHeroes) * CommonHeroCost;

            // R5: additionally turn one owned hero into a 2-star by buying it up to a full merge trio.
            var extraCopiesForTwoStar = Math.Max(0, HeroEconomy.CopiesPerStarUpgrade - 1) * CommonHeroCost;
            var twoStarByRound5Cost = heroesByRound3Cost + extraCopiesForTwoStar;

            // R8: 1-2 active synergies fall out of already owning several heroes; no spend beyond R5's plan.
            var synergiesByRound8Cost = twoStarByRound5Cost;

            // Final: field the bottom of the band (5 heroes) — level up to that field limit and own that many.
            var fieldTarget = BalanceTargets.HeroesFieldedByFinal.Min;
            var fieldLevelCost = GoldToReachLevel(RequiredLevelToField(fieldTarget, config), config);
            var fieldHeroCost = Math.Max(0, fieldTarget - StarterHeroes) * CommonHeroCost;
            var fieldedByFinalCost = fieldLevelCost + fieldHeroCost;

            return new List<BalanceMilestone>
            {
                new("Own 3-4 heroes by round 3", BalanceTargets.HeroOwnershipMilestoneRound,
                    heroesByRound3Cost, GoldAvailableByRound(BalanceTargets.HeroOwnershipMilestoneRound, config)),
                new("One 2-star hero by round 5", BalanceTargets.FirstTwoStarMilestoneRound,
                    twoStarByRound5Cost, GoldAvailableByRound(BalanceTargets.FirstTwoStarMilestoneRound, config)),
                new("1-2 active synergies by round 8", BalanceTargets.SynergyMilestoneRound,
                    synergiesByRound8Cost, GoldAvailableByRound(BalanceTargets.SynergyMilestoneRound, config)),
                new("Field 5-6 heroes by the final", PveRunSchedule.FinalRound,
                    fieldedByFinalCost, GoldAvailableByRound(PveRunSchedule.FinalRound, config))
            };
        }

        /// <summary>True when the shipped economy lets an on-track player reach every GDD milestone.</summary>
        public static bool AllMilestonesReachable(EconomyConfig? economy = null)
        {
            return Milestones(economy).All(milestone => milestone.IsReachable);
        }
    }
}
