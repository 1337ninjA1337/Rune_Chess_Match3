namespace RuneChess.Core
{
    /// <summary>
    /// The core mechanics the first run reveals one at a time (GDD "Первые раунды должны
    /// постепенно открывать механику"). Each value is the single new idea a tutorial round
    /// introduces, in the order the GDD "Первые 10 раундов" table teaches them.
    /// </summary>
    public enum OnboardingMechanic
    {
        BuyAndPlaceHero,
        RedAndBlueRunes,
        TankAndPositioning,
        RiskAndReward,
        ShieldsAndHealing,
        BacklineThreat,
        MagicDamage
    }
}
