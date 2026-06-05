namespace RuneChess.Core
{
    /// <summary>
    /// What an artifact modifies, per the GDD note that MVP artifacts act as modifiers
    /// of combat, the economy, or the match-3 runes. Applying the modifier to live
    /// systems is a separate task; this enum classifies the catalog so the UI and the
    /// future apply step can route each artifact.
    /// </summary>
    public enum ArtifactEffectKind
    {
        Combat,
        Economy,
        Rune
    }
}
