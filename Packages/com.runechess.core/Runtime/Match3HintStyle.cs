using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic presentation tokens for the idle match hint — the highlight shown on a
    /// playable swap when the player has not acted for <see cref="HintDelaySeconds"/> seconds
    /// (the trigger logic lives on <see cref="CombatState.ShouldShowMatchHint"/>). The restyle gives
    /// the hint a soft pulsing glow; the Unity layer reads these tokens to paint and animate it
    /// (rendering is Unity-only — documented verification gap). The contract — delay parity with the
    /// combat rule, a valid opacity band, and a smooth periodic pulse — is verified headless via
    /// <c>tools/CoreSmoke</c>. All values are original tuning for this project.
    /// </summary>
    public static class Match3HintStyle
    {
        /// <summary>Idle seconds before the hint appears (mirrors <see cref="CombatState.MatchHintDelaySeconds"/>).</summary>
        public static int HintDelaySeconds => CombatState.MatchHintDelaySeconds;

        /// <summary>Warm pale-gold glow laid over the suggested swap runes.</summary>
        public const uint HighlightColor = 0xF2E6B0u;

        /// <summary>Length of one breathe-in/out pulse cycle of the highlight.</summary>
        public const double PulsePeriodSeconds = 0.9;

        /// <summary>Faintest the glow gets at the bottom of a pulse.</summary>
        public const double MinPulseOpacity = 0.20;

        /// <summary>Brightest the glow gets at the top of a pulse.</summary>
        public const double MaxPulseOpacity = 0.65;

        /// <summary>
        /// The glow opacity at a point in time, as a smooth (cosine) breathe between
        /// <see cref="MinPulseOpacity"/> and <see cref="MaxPulseOpacity"/> over
        /// <see cref="PulsePeriodSeconds"/>. <paramref name="secondsIntoHint"/> is the time elapsed
        /// since the hint appeared.
        /// </summary>
        public static double PulseOpacityAt(double secondsIntoHint)
        {
            if (secondsIntoHint < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsIntoHint), "Hint pulse time cannot be negative.");
            }

            var phase = (secondsIntoHint % PulsePeriodSeconds) / PulsePeriodSeconds;
            // 0 at phase 0, rising to 1 at phase 0.5, back to 0 at phase 1.
            var breathe = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * phase));
            return MinPulseOpacity + (MaxPulseOpacity - MinPulseOpacity) * breathe;
        }
    }
}
