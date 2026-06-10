namespace RuneChess.Core
{
    /// <summary>
    /// The acceptable monetization formats for the game (GDD "Монетизация → Подходящие
    /// форматы"). Every one of these is purely cosmetic or convenience: none sells combat
    /// power, so the non-pay-to-win rule holds (GDD: "Баланс должен избегать pay-to-win.
    /// Монетизация не должна давать прямое преимущество в PvP"). Keeping the list an explicit
    /// enum lets the cosmetic-only invariant be smoke-tested.
    /// </summary>
    public enum MonetizationFormatKind
    {
        /// <summary>Seasonal cosmetic track (GDD "battle pass").</summary>
        BattlePass,

        /// <summary>Hero appearance reskin (GDD "скины героев").</summary>
        HeroSkin,

        /// <summary>Match-3 board reskin (GDD "скины доски").</summary>
        BoardSkin,

        /// <summary>Visual effect for rune matches (GDD "эффекты рун").</summary>
        RuneEffect,

        /// <summary>Commander avatar art (GDD "портреты командиров").</summary>
        CommanderPortrait,

        /// <summary>Expressive emote (GDD "эмоции").</summary>
        Emote,

        /// <summary>Cosmetic finishing flourish on a kill/win (GDD "косметические добивания").</summary>
        CosmeticFinisher,

        /// <summary>Faster cosmetic-only progression (GDD "ускорение косметического прогресса").</summary>
        CosmeticProgressBoost,
    }
}
