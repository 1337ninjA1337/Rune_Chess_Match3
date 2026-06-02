using System;

namespace RuneChess.Core
{
    public sealed record ShopRarityOdds(
        int Common,
        int Rare,
        int Epic,
        int Legendary
    )
    {
        public int Common { get; init; } = ValidateChance(Common, nameof(Common));
        public int Rare { get; init; } = ValidateChance(Rare, nameof(Rare));
        public int Epic { get; init; } = ValidateChance(Epic, nameof(Epic));
        public int Legendary { get; init; } = ValidateChance(Legendary, nameof(Legendary));

        public int TotalChance => Common + Rare + Epic + Legendary;

        public int GetChance(HeroRarity rarity)
        {
            return rarity switch
            {
                HeroRarity.Common => Common,
                HeroRarity.Rare => Rare,
                HeroRarity.Epic => Epic,
                HeroRarity.Legendary => Legendary,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown hero rarity.")
            };
        }

        private static int ValidateChance(int chance, string paramName)
        {
            if (chance < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, "Shop rarity chance cannot be negative.");
            }

            return chance;
        }
    }
}
