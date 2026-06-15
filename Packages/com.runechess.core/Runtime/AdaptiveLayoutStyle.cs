using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic adaptive-layout math for the portrait UI (GDD/codex visual overhaul:
    /// "Адаптив"). It keeps HUD and board content clear of the device safe area (notch/rounded
    /// corners/home indicator) so nothing important sits under a cutout or off a curved edge, and it
    /// derives a clamped layout scale for different screen aspects so combat stays readable on very
    /// tall or wide phones. Pure data; the Unity layer feeds it the real safe area and screen aspect
    /// and applies the resulting rect/scale. iOS portrait is the primary target. All values are
    /// original tuning; the contract is verified headless via <c>tools/CoreSmoke</c>.
    /// </summary>
    public static class AdaptiveLayoutStyle
    {
        // --- Safe-area aware content region (task: учёт безопасных зон iOS, без перекрытия HUD). ---

        /// <summary>
        /// Extra padding (fraction of the screen) kept between content and the safe-area edge so the
        /// HUD never crowds right up against the notch or a rounded corner.
        /// </summary>
        public const double SafeAreaContentMarginFraction = 0.01;

        /// <summary>The top edge (fraction from the top) where content may begin, below the safe inset.</summary>
        public static double ContentTopFraction(SafeAreaInsets insets)
        {
            insets.Validate();
            return insets.Top + SafeAreaContentMarginFraction;
        }

        /// <summary>The bottom edge (fraction from the bottom) where content may end, above the safe inset.</summary>
        public static double ContentBottomFraction(SafeAreaInsets insets)
        {
            insets.Validate();
            return insets.Bottom + SafeAreaContentMarginFraction;
        }

        /// <summary>The usable vertical fraction of the screen for content between the safe insets.</summary>
        public static double UsableHeightFraction(SafeAreaInsets insets)
        {
            insets.Validate();
            return Math.Max(0.0, 1.0 - ContentTopFraction(insets) - ContentBottomFraction(insets));
        }

        /// <summary>The usable horizontal fraction of the screen between the left/right safe insets.</summary>
        public static double UsableWidthFraction(SafeAreaInsets insets)
        {
            insets.Validate();
            var margins = (2.0 * SafeAreaContentMarginFraction) + insets.Left + insets.Right;
            return Math.Max(0.0, 1.0 - margins);
        }

        // --- Aspect-driven layout scale (task: масштабирование макета под разные размеры экрана с
        // сохранением читаемости боя). ---

        /// <summary>Reference portrait aspect (width:height) the layout is authored against (~iPhone 9:19.5).</summary>
        public const double ReferenceAspect = 9.0 / 19.5;

        /// <summary>Lower clamp on the layout scale so a very tall/narrow screen never shrinks combat past readability.</summary>
        public const double MinLayoutScale = 0.85;

        /// <summary>Upper clamp on the layout scale so a wide screen never blows the board past the frame.</summary>
        public const double MaxLayoutScale = 1.15;

        /// <summary>
        /// Layout scale for a screen of the given <paramref name="aspect"/> (width / height): 1.0 at
        /// the reference aspect, scaling with how wide the screen is relative to the reference, and
        /// clamped to [<see cref="MinLayoutScale"/>, <see cref="MaxLayoutScale"/>] so the tactical
        /// board and match-3 grid stay readable on any portrait device.
        /// </summary>
        public static double LayoutScaleForAspect(double aspect)
        {
            if (aspect <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(aspect), "Screen aspect must be positive.");
            }

            var scale = aspect / ReferenceAspect;
            return Math.Clamp(scale, MinLayoutScale, MaxLayoutScale);
        }
    }
}
