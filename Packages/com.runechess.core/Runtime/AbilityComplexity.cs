namespace RuneChess.Core
{
    /// <summary>
    /// How hard a hero's ability is to read for a new player (GDD "Слишком сложный onboarding":
    /// "стартовые герои должны иметь простые способности"). This drives onboarding gating so the
    /// early game only offers heroes whose ability has a single, obvious effect.
    /// </summary>
    public enum AbilityComplexity
    {
        /// <summary>
        /// A single, self-contained effect a beginner can read at a glance: direct damage,
        /// a heal, a shield, or a basic summon — no repositioning, control, conditional or
        /// stacking interactions.
        /// </summary>
        Simple,

        /// <summary>
        /// An effect that adds positioning, control, debuffs, illusions, conditional triggers
        /// or board-wide interactions — kept out of the starter pool so onboarding stays clear.
        /// </summary>
        Advanced
    }
}
