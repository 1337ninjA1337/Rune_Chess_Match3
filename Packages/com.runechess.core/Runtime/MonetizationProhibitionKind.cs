namespace RuneChess.Core
{
    /// <summary>
    /// The monetization formats the game must never ship (GDD "Монетизация → Нежелательные
    /// форматы"). Each is a way to sell combat advantage or gate play behind payment, which
    /// would break the non-pay-to-win rule. Encoded as an enum so <see cref="MonetizationPolicy"/>
    /// can assert, in core, that none of these is offered.
    /// </summary>
    public enum MonetizationProhibitionKind
    {
        /// <summary>Selling raw hero power (GDD "продажа силы героев").</summary>
        SellHeroPower,

        /// <summary>Paid heroes that are stronger than earned ones (GDD "платные герои с преимуществом").</summary>
        PaidAdvantageHeroes,

        /// <summary>Paid artifacts that shift combat balance (GDD "платные артефакты, влияющие на боевой баланс").</summary>
        PaidCombatArtifacts,

        /// <summary>A mandatory stamina/energy gate on playing (GDD "обязательная энергия для игры").</summary>
        MandatoryEnergyToPlay,
    }
}
