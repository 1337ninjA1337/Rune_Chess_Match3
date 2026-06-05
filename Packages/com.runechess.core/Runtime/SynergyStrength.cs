namespace RuneChess.Core
{
    /// <summary>
    /// Colour-coded strength of a synergy on the synergy panel ("Панель синергий":
    /// цветовая индикация силы синергии). The presentation layer maps each level to a
    /// colour: a synergy that is represented but has no active tier is still building,
    /// one with an active tier and a higher tier to reach is active, and one with every
    /// tier unlocked is maxed.
    /// </summary>
    public enum SynergyStrength
    {
        Building,
        Active,
        Maxed
    }
}
