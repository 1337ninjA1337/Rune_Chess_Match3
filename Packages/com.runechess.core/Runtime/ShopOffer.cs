namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

namespace RuneChess.Core
{
    public sealed record ShopOffer(
        string OfferId,
        string HeroId,
        int Cost
    );
}
