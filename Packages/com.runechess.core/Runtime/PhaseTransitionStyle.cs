using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic styling and timing for the flourishes between run phases (GDD/codex visual
    /// overhaul: "Переходы фаз и адаптив"). It owns the original "Бой!" battle-start banner shown on
    /// the preparation→combat transition — its caption, how long it holds, and its fade-in/hold/
    /// fade-out ramp — so the Unity layer just plays the timing it is handed. Wording and timing are
    /// original to this project; no Dota/Valve references. Rendering the banner is Unity-only
    /// (documented verification gap); the timing contract is verified headless via <c>tools/CoreSmoke</c>.
    /// </summary>
    public static class PhaseTransitionStyle
    {
        /// <summary>Original battle-start banner caption shown on the preparation→combat transition.</summary>
        public const string BattleStartBannerText = "Бой!";

        /// <summary>How long the battle-start banner holds at full opacity before it fades out.</summary>
        public const double BattleBannerHoldSeconds = 0.6;

        /// <summary>How long the banner takes to fade in, and to fade out, around the hold.</summary>
        public const double BattleBannerFadeSeconds = 0.25;

        /// <summary>Total on-screen lifetime of the battle-start banner: fade-in + hold + fade-out.</summary>
        public static double BattleBannerTotalSeconds =>
            (BattleBannerFadeSeconds * 2.0) + BattleBannerHoldSeconds;

        /// <summary>Warm accent the banner text/sweep uses (shared gold accent token).</summary>
        public static uint BattleBannerColor => UiTheme.GoldColor;

        /// <summary>
        /// Banner opacity at <paramref name="secondsIntoBanner"/>: ramps 0→1 over the fade-in, holds
        /// at full through the hold window, then ramps 1→0 over the fade-out, and stays at 0 once the
        /// banner's lifetime has elapsed. Lets the Unity layer drive the banner from a single clock.
        /// </summary>
        public static double BattleBannerOpacityAt(double secondsIntoBanner)
        {
            if (secondsIntoBanner < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondsIntoBanner), "Time into the banner cannot be negative.");
            }

            if (secondsIntoBanner >= BattleBannerTotalSeconds)
            {
                return 0.0;
            }

            if (secondsIntoBanner < BattleBannerFadeSeconds)
            {
                return secondsIntoBanner / BattleBannerFadeSeconds;
            }

            var holdEnd = BattleBannerFadeSeconds + BattleBannerHoldSeconds;
            if (secondsIntoBanner <= holdEnd)
            {
                return 1.0;
            }

            var intoFadeOut = secondsIntoBanner - holdEnd;
            return 1.0 - (intoFadeOut / BattleBannerFadeSeconds);
        }
    }
}
