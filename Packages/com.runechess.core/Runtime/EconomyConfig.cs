using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    public sealed record EconomyConfig(
        int StartingRunHealth,
        int StartingGold,
        int StartingXp,
        int StartingPlayerLevel,
        int StartingBenchSize,
        int StartingShopSize,
        int RerollCost,
        int BuyXpCost,
        int XpPerPurchase,
        int BaseIncome,
        int WinBonus,
        int StreakBonusThreeWins,
        int StreakBonusFiveWins,
        int InterestGoldStep,
        int InterestBonusCap,
        IReadOnlyList<int> PlayerLevelXpThresholds,
        IReadOnlyList<int> PlayerLevelHeroLimits,
        IReadOnlyList<ShopRarityOdds> ShopRarityOddsByLevel
    )
    {
        public IReadOnlyList<int> PlayerLevelXpThresholds { get; init; } =
            ValidatePlayerLevelXpThresholds(PlayerLevelXpThresholds);

        public IReadOnlyList<int> PlayerLevelHeroLimits { get; init; } =
            ValidatePlayerLevelHeroLimits(PlayerLevelHeroLimits);

        public IReadOnlyList<ShopRarityOdds> ShopRarityOddsByLevel { get; init; } =
            ValidateShopRarityOdds(ShopRarityOddsByLevel);

        public int MaxPlayerLevel => PlayerLevelXpThresholds.Count;

        public int GetXpThresholdForLevel(int playerLevel)
        {
            if (playerLevel < 1 || playerLevel > MaxPlayerLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(playerLevel), "Player level is outside the configured level range.");
            }

            return PlayerLevelXpThresholds[playerLevel - 1];
        }

        public int GetXpCostForNextLevel(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= MaxPlayerLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel), "Current level cannot advance within the configured level range.");
            }

            return GetXpThresholdForLevel(currentLevel + 1) - GetXpThresholdForLevel(currentLevel);
        }

        public int GetHeroLimitForLevel(int playerLevel)
        {
            if (playerLevel < 1 || playerLevel > PlayerLevelHeroLimits.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(playerLevel), "Player level is outside the configured hero-limit range.");
            }

            return PlayerLevelHeroLimits[playerLevel - 1];
        }

        public ShopRarityOdds GetShopRarityOddsForLevel(int playerLevel)
        {
            if (playerLevel < 1 || playerLevel > ShopRarityOddsByLevel.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(playerLevel), "Player level is outside the configured shop odds range.");
            }

            return ShopRarityOddsByLevel[playerLevel - 1];
        }

        public int GetShopSizeForLevel(int playerLevel)
        {
            if (playerLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerLevel), "Player level starts at one.");
            }

            return playerLevel <= 2 ? StartingShopSize : StartingShopSize + 1;
        }

        public int CalculateGoldIncome(
            bool wonCombat,
            int winStreak,
            int currentGold,
            int eventBonus = 0)
        {
            if (winStreak < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(winStreak), "Win streak cannot be negative.");
            }

            if (currentGold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentGold), "Current gold cannot be negative.");
            }

            return BaseIncome
                + (wonCombat ? WinBonus : 0)
                + CalculateStreakBonus(winStreak)
                + CalculateInterestBonus(currentGold)
                + eventBonus;
        }

        public int CalculateStreakBonus(int winStreak)
        {
            if (winStreak < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(winStreak), "Win streak cannot be negative.");
            }

            if (winStreak >= 5)
            {
                return StreakBonusFiveWins;
            }

            return winStreak >= 3 ? StreakBonusThreeWins : 0;
        }

        public int CalculateInterestBonus(int currentGold)
        {
            if (currentGold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentGold), "Current gold cannot be negative.");
            }

            return Math.Min(InterestBonusCap, currentGold / InterestGoldStep);
        }

        private static IReadOnlyList<int> ValidatePlayerLevelXpThresholds(IReadOnlyList<int> thresholds)
        {
            if (thresholds is null || thresholds.Count == 0)
            {
                throw new ArgumentException("At least one player level XP threshold is required.", nameof(thresholds));
            }

            if (thresholds[0] != 0)
            {
                throw new ArgumentException("Player level XP thresholds must start at zero.", nameof(thresholds));
            }

            for (var i = 1; i < thresholds.Count; i += 1)
            {
                if (thresholds[i] <= thresholds[i - 1])
                {
                    throw new ArgumentException("Player level XP thresholds must be strictly increasing.", nameof(thresholds));
                }
            }

            return thresholds.ToList();
        }

        private static IReadOnlyList<int> ValidatePlayerLevelHeroLimits(IReadOnlyList<int> limits)
        {
            if (limits is null || limits.Count == 0)
            {
                throw new ArgumentException("At least one player level hero limit is required.", nameof(limits));
            }

            for (var i = 0; i < limits.Count; i += 1)
            {
                if (limits[i] <= 0)
                {
                    throw new ArgumentException("Player level hero limits must be positive.", nameof(limits));
                }

                if (i > 0 && limits[i] < limits[i - 1])
                {
                    throw new ArgumentException("Player level hero limits must not decrease.", nameof(limits));
                }
            }

            return limits.ToList();
        }

        private static IReadOnlyList<ShopRarityOdds> ValidateShopRarityOdds(IReadOnlyList<ShopRarityOdds> oddsByLevel)
        {
            if (oddsByLevel is null || oddsByLevel.Count == 0)
            {
                throw new ArgumentException("At least one shop rarity odds row is required.", nameof(oddsByLevel));
            }

            foreach (var odds in oddsByLevel)
            {
                if (odds is null)
                {
                    throw new ArgumentException("Shop rarity odds rows cannot be null.", nameof(oddsByLevel));
                }

                if (odds.TotalChance != 100)
                {
                    throw new ArgumentException("Each shop rarity odds row must sum to 100 percent.", nameof(oddsByLevel));
                }
            }

            return oddsByLevel.ToList();
        }

        public static EconomyConfig Default { get; } = new(
            StartingRunHealth: 20,
            StartingGold: 5,
            StartingXp: 0,
            StartingPlayerLevel: 1,
            StartingBenchSize: 6,
            StartingShopSize: 3,
            RerollCost: 2,
            BuyXpCost: 4,
            XpPerPurchase: 4,
            BaseIncome: 3,
            WinBonus: 1,
            StreakBonusThreeWins: 1,
            StreakBonusFiveWins: 2,
            InterestGoldStep: 10,
            InterestBonusCap: 3,
            PlayerLevelXpThresholds: Array.AsReadOnly(new[] { 0, 4, 8, 12, 16 }),
            PlayerLevelHeroLimits: Array.AsReadOnly(new[] { 2, 3, 4, 5, 6 }),
            ShopRarityOddsByLevel: Array.AsReadOnly(new[]
            {
                new ShopRarityOdds(Common: 80, Rare: 20, Epic: 0, Legendary: 0),
                new ShopRarityOdds(Common: 65, Rare: 30, Epic: 5, Legendary: 0),
                new ShopRarityOdds(Common: 45, Rare: 40, Epic: 15, Legendary: 0),
                new ShopRarityOdds(Common: 30, Rare: 40, Epic: 25, Legendary: 5),
                new ShopRarityOdds(Common: 20, Rare: 35, Epic: 35, Legendary: 10)
            })
        );
    }
}
