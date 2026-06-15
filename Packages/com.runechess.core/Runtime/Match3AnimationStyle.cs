using System;

namespace RuneChess.Core
{
    /// <summary>
    /// A discrete match-3 board animation the Unity layer plays. Timings live here so the
    /// presentation overhaul has one engine-agnostic source of truth for how swaps, clears, falls
    /// and chain reactions feel; the actual tweening is a Unity-side task (documented verification
    /// gap — no Unity render in the automation environment).
    /// </summary>
    public enum Match3AnimationCue
    {
        /// <summary>Two adjacent runes sliding past each other on a valid swap.</summary>
        Swap,
        /// <summary>An invalid swap springing back to its original cells.</summary>
        SwapReject,
        /// <summary>Matched runes clearing (pop/dissolve) before the board collapses.</summary>
        Clear,
        /// <summary>New and surviving runes falling down to fill emptied cells.</summary>
        Fall,
        /// <summary>The brief settle beat between one cleared step and the next chain reaction.</summary>
        Chain
    }

    /// <summary>
    /// Engine-agnostic timing tokens for the restyled match-3 board animations (swap, clear, fall,
    /// chain reactions). The Unity layer reads <see cref="CueDurationSeconds"/> to drive its tweens;
    /// rendering itself is Unity-only. The contract — every cue has a positive duration and the swap
    /// animation fits inside the 0.25s global swap cooldown so input never feels laggy — is verified
    /// headless via <c>tools/CoreSmoke</c>. All values are original tuning for this project.
    /// </summary>
    public static class Match3AnimationStyle
    {
        /// <summary>Valid swap slide. Kept within the global swap cooldown so the next input is ready in time.</summary>
        public const double SwapSeconds = 0.18;

        /// <summary>Invalid swap bounce-back; matches the swap slide so the round trip reads symmetric.</summary>
        public const double SwapRejectSeconds = 0.18;

        /// <summary>Matched-rune clear pop.</summary>
        public const double ClearSeconds = 0.22;

        /// <summary>Runes falling to fill emptied cells.</summary>
        public const double FallSeconds = 0.20;

        /// <summary>Settle beat between chain-reaction steps.</summary>
        public const double ChainSettleSeconds = 0.12;

        /// <summary>The global swap cooldown expressed in seconds (mirrors <see cref="CombatState.SwapGlobalCooldownMilliseconds"/>).</summary>
        public static double SwapCooldownSeconds => CombatState.SwapGlobalCooldownMilliseconds / 1000.0;

        /// <summary>The animation length, in seconds, the Unity layer should play for a given cue.</summary>
        public static double CueDurationSeconds(Match3AnimationCue cue)
        {
            return cue switch
            {
                Match3AnimationCue.Swap => SwapSeconds,
                Match3AnimationCue.SwapReject => SwapRejectSeconds,
                Match3AnimationCue.Clear => ClearSeconds,
                Match3AnimationCue.Fall => FallSeconds,
                Match3AnimationCue.Chain => ChainSettleSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, "Unknown match-3 animation cue.")
            };
        }
    }
}
