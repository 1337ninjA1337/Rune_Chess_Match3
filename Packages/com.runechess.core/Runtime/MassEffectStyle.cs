using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic single source of truth for how a T/L combo reads as a mass effect on the
    /// board: an expanding shockwave ring in the combo colour, plus the player-facing bonus labels
    /// for the extra match power and commander energy a T/L combo grants (from <see cref="RuneEffects"/>,
    /// surfaced by <see cref="RuneMatchGroup.IsMassEffect"/>). The Unity layer plays the shockwave;
    /// rendering is Unity-only (documented verification gap). The contract — a positive, expanding
    /// shockwave and bonus parity with the combat rules — is verified headless via
    /// <c>tools/CoreSmoke</c>. All values are original tuning for this project.
    /// </summary>
    public static class MassEffectStyle
    {
        /// <summary>How long the shockwave ring takes to sweep out across the board.</summary>
        public const double ShockwaveSeconds = 0.5;

        /// <summary>Ring scale (relative to a board cell) at the start of the shockwave.</summary>
        public const double StartScale = 0.2;

        /// <summary>Ring scale (relative to a board cell) at the end of the shockwave.</summary>
        public const double EndScale = 1.6;

        /// <summary>Extra match power a T/L combo grants (mirrors <see cref="RuneEffects.TShapeMatchPowerBonus"/>).</summary>
        public static int MatchPowerBonus => RuneEffects.TShapeMatchPowerBonus;

        /// <summary>Commander energy a T/L combo grants (mirrors <see cref="RuneEffects.TShapeCommanderEnergy"/>).</summary>
        public static int CommanderEnergyBonus => RuneEffects.TShapeCommanderEnergy;

        /// <summary>Player-facing bonus summary for a T/L combo, e.g. "+2 силы · +10 энергии".</summary>
        public static string BonusLabel => $"+{MatchPowerBonus} силы · +{CommanderEnergyBonus} энергии";

        /// <summary>The shockwave ring colour for a combo of a given rune colour (inherits the rune palette).</summary>
        public static uint RingColorFor(RuneType rune) => RuneVisualStyle.Color(rune);

        /// <summary>
        /// The ring scale at a point in time, interpolated linearly from <see cref="StartScale"/> to
        /// <see cref="EndScale"/> over <see cref="ShockwaveSeconds"/> and clamped to the end scale
        /// afterwards. <paramref name="secondsIntoShockwave"/> is time since the combo fired.
        /// </summary>
        public static double ScaleAt(double secondsIntoShockwave)
        {
            if (secondsIntoShockwave < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsIntoShockwave), "Shockwave time cannot be negative.");
            }

            var t = Math.Min(1.0, secondsIntoShockwave / ShockwaveSeconds);
            return StartScale + (EndScale - StartScale) * t;
        }
    }
}
