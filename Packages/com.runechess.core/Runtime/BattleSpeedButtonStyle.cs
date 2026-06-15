using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic styling for the in-combat battle speed-up button (GDD "кнопка ускорения боя";
    /// faster speeds are a later-version pacing convenience). <see cref="BattleSpeedButtonModel"/>
    /// owns WHAT the button says — the current speed, its label, and what the next tap toggles to;
    /// this style owns HOW it reads — a calm idle tint at normal speed and a warm "engaged" tint plus
    /// glow while the battle is sped up, so a single glance tells the player the clock is running
    /// fast. The accent is tied to the model's <see cref="BattleSpeedButtonModel.IsSpedUp"/> so the
    /// styling can never disagree with the speed actually applied. Rendering is Unity-only (documented
    /// verification gap); the contract is verified headless via <c>tools/CoreSmoke</c>. All values are
    /// original tuning for this project.
    /// </summary>
    public static class BattleSpeedButtonStyle
    {
        /// <summary>Calm, recessive tint for the button face while the battle runs at normal speed.</summary>
        public const uint IdleTint = 0x3A4254u;

        /// <summary>Warm "engaged" tint while the battle is sped up, so fast-forward reads at a glance.</summary>
        public const uint EngagedTint = 0xD9A441u;

        /// <summary>Glow opacity around the button while sped up (zero at normal speed).</summary>
        public const double EngagedGlowOpacity = 0.55;

        /// <summary>
        /// The button-face tint for a speed-button state: the warm engaged tint while sped up, the
        /// calm idle tint otherwise. Tied to <see cref="BattleSpeedButtonModel.IsSpedUp"/>.
        /// </summary>
        public static uint TintFor(BattleSpeedButtonModel button)
        {
            if (button is null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            return button.IsSpedUp ? EngagedTint : IdleTint;
        }

        /// <summary>
        /// The glow opacity for a speed-button state: the engaged glow while sped up, zero otherwise,
        /// so the fast-forward emphasis appears exactly when the battle is actually sped up.
        /// </summary>
        public static double GlowOpacityFor(BattleSpeedButtonModel button)
        {
            if (button is null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            return button.IsSpedUp ? EngagedGlowOpacity : 0.0;
        }

        /// <summary>The face caption for the button: the current speed multiplier label (e.g. "x1.5").</summary>
        public static string FaceLabel(BattleSpeedButtonModel button)
        {
            if (button is null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            return button.Label;
        }

        /// <summary>A compact toggle hint previewing what the next tap switches to (e.g. "→ x1.0").</summary>
        public static string ToggleHint(BattleSpeedButtonModel button)
        {
            if (button is null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            return "→ " + button.NextLabel;
        }
    }
}
