using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic presentation glue for the roguelite event screen overhaul
    /// (GDD UI screen «Экран события»). The <see cref="EventScreenModel"/> already says WHAT the
    /// card shows (title, description, risk/reward copy, accept/decline controls); this
    /// presentation says HOW it reads in the new portrait visual language by tying the event's
    /// defining risk-vs-reward contrast to the shared palette: the cost/downside line takes the
    /// red rune token, the reward/gain line takes the green rune token, and the accept
    /// call-to-action reuses the project's one warm «go» accent (<see cref="UiTheme.GoldColor"/>,
    /// shared with the HUD gold readout, the main-menu run tile and the reward CTA). A
    /// <see cref="HasRisk"/> flag lets a no-downside event (a free windfall) drop the risk accent
    /// entirely. Rendering itself is Unity-only (documented gap); the contract is verified headless
    /// by <c>tools/CoreSmoke</c>. All art direction is original (codex.md IP rule).
    /// </summary>
    public static class EventScreenPresentation
    {
        /// <summary>Warm accent for the accept call-to-action (shared «go» accent).</summary>
        public const uint AcceptAccentColor = UiTheme.GoldColor;

        /// <summary>Cost/downside accent: the red rune token reads as a risk.</summary>
        public static uint RiskAccentColor => UiTheme.RuneColor(RuneType.Red);

        /// <summary>Reward/gain accent: the green rune token reads as a positive outcome.</summary>
        public static uint RewardAccentColor => UiTheme.RuneColor(RuneType.Green);

        /// <summary>
        /// True when accepting the event carries a downside (run-health cost, gold cost, losing a
        /// hero or taking a curse). A no-downside event (a free windfall) returns false so the
        /// view can drop the risk accent and read as a pure gain.
        /// </summary>
        public static bool HasRisk(EventOption choice)
        {
            if (choice is null)
            {
                throw new ArgumentNullException(nameof(choice));
            }

            return choice.CostsHealth || choice.CostsGold || choice.RemovesHero || choice.AppliesCurse;
        }

        /// <summary>Convenience overload: whether the screen's offered event carries a downside.</summary>
        public static bool HasRisk(EventScreenModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return HasRisk(model.Choice);
        }
    }
}
