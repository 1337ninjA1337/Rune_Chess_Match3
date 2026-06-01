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
        public static EconomyConfig Default { get; } = new(
            StartingRunHealth: 100,
            StartingGold: 6,
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
