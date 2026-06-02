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
        int InterestBonusCap
    )
    {
        public int GetShopSizeForLevel(int playerLevel)
        {
            if (playerLevel < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(playerLevel), "Player level starts at one.");
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
                throw new System.ArgumentOutOfRangeException(nameof(winStreak), "Win streak cannot be negative.");
            }

            if (currentGold < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(currentGold), "Current gold cannot be negative.");
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
                throw new System.ArgumentOutOfRangeException(nameof(winStreak), "Win streak cannot be negative.");
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
                throw new System.ArgumentOutOfRangeException(nameof(currentGold), "Current gold cannot be negative.");
            }

            return System.Math.Min(InterestBonusCap, currentGold / InterestGoldStep);
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
            InterestBonusCap: 3
        );
    }
}
