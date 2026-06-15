using System;

namespace RuneChess.Core
{
    /// <summary>
    /// The screen margins reserved by a device's safe area, as fractions (0..1) of the screen's
    /// height (top/bottom) and width (left/right): the notch/status bar at the top, the home
    /// indicator and rounded corners at the bottom and sides. iOS portrait is the primary target
    /// (codex UI rule). Pure data so the layout math is engine-agnostic and smoke-testable; the Unity
    /// layer feeds it the real <c>Screen.safeArea</c> insets.
    /// </summary>
    public readonly record struct SafeAreaInsets(double Top, double Bottom, double Left, double Right)
    {
        /// <summary>A device with no safe-area insets (e.g. a classic flat rectangular screen).</summary>
        public static readonly SafeAreaInsets None = new(0.0, 0.0, 0.0, 0.0);

        /// <summary>Validate that the insets are non-negative and leave a usable region on each axis.</summary>
        public void Validate()
        {
            if (Top < 0.0 || Bottom < 0.0 || Left < 0.0 || Right < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(SafeAreaInsets), "Safe-area insets cannot be negative.");
            }

            if (Top + Bottom >= 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(SafeAreaInsets), "Vertical safe-area insets consume the whole screen.");
            }

            if (Left + Right >= 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(SafeAreaInsets), "Horizontal safe-area insets consume the whole screen.");
            }
        }
    }
}
