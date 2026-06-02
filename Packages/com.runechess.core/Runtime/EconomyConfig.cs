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
        int XpPerPurchase
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

        public static EconomyConfig Default { get; } = new(
            StartingRunHealth: 20,
            StartingGold: 5,
            StartingXp: 0,
            StartingPlayerLevel: 1,
            StartingBenchSize: 8,
            StartingShopSize: 3,
            RerollCost: 2,
            BuyXpCost: 4,
            XpPerPurchase: 4
        );
    }
}
