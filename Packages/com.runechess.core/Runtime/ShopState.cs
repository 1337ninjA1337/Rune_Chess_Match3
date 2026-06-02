using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    public sealed record ShopState(
        IReadOnlyList<ShopOffer> Offers,
        int RerollsThisRound
    )
    {
        private static IReadOnlyList<ShopOffer> DefaultOfferPool { get; } = Array.AsReadOnly(new[]
        {
            new ShopOffer("offer_iron_guard", "iron_guard", 1),
            new ShopOffer("offer_oath_archer", "oath_archer", 1),
            new ShopOffer("offer_field_medic", "field_medic", 1),
            new ShopOffer("offer_wild_claw", "wild_claw", 1)
        });

        public static ShopState StartingShop { get; } = ForPlayerLevel(1);

        public static ShopState ForPlayerLevel(int playerLevel, EconomyConfig? economy = null)
        {
            var config = economy ?? EconomyConfig.Default;
            return CreateWithSize(config.GetShopSizeForLevel(playerLevel));
        }

        public static ShopState CreateWithSize(int shopSize)
        {
            if (shopSize < 1 || shopSize > DefaultOfferPool.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(shopSize), "Shop size is outside the configured offer pool.");
            }

            return new ShopState(
                Offers: DefaultOfferPool.Take(shopSize).ToList(),
                RerollsThisRound: 0
            );
        }
    }
}
