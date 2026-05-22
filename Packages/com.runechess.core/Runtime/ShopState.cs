using System.Collections.Generic;

namespace RuneChess.Core;

public sealed record ShopState(
    IReadOnlyList<ShopOffer> Offers,
    int RerollsThisRound
)
{
    public static ShopState StartingShop { get; } = new(
        Offers: new List<ShopOffer>
        {
            new("offer_iron_guard", "iron_guard", 1),
            new("offer_oath_archer", "oath_archer", 1),
            new("offer_field_medic", "field_medic", 1)
        },
        RerollsThisRound: 0
    );
}
