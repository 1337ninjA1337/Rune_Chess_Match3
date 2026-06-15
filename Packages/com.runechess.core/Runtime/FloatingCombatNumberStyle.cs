using System;
using System.Globalization;

namespace RuneChess.Core
{
    /// <summary>The kind of floating combat number shown over a unit.</summary>
    public enum FloatingNumberKind
    {
        /// <summary>Physical damage dealt.</summary>
        PhysicalDamage,
        /// <summary>Magic damage dealt.</summary>
        MagicDamage,
        /// <summary>Healing received.</summary>
        Heal,
        /// <summary>Damage absorbed by a shield.</summary>
        ShieldAbsorbed
    }

    /// <summary>
    /// Engine-agnostic single source of truth for the floating numbers that pop over a unit when it
    /// takes damage, is healed, or absorbs a hit with a shield. Colours reuse the <see cref="UiTheme"/>
    /// rune palette (red/purple/green/yellow) so the feedback colour matches the effect's element;
    /// the label carries a sign so gains read "+" and losses "−". The Unity layer spawns the number
    /// and tweens its rise/fade; rendering is Unity-only (documented verification gap). The contract —
    /// distinct colours, a positive rise, a clamped fade and signed labels — is verified headless via
    /// <c>tools/CoreSmoke</c>. All values are original tuning for this project.
    /// </summary>
    public static class FloatingCombatNumberStyle
    {
        /// <summary>How long the number floats up and fades out.</summary>
        public const double RiseSeconds = 0.8;

        /// <summary>How far the number floats up (relative to a board cell) over its lifetime.</summary>
        public const double RiseDistance = 1.0;

        /// <summary>Colour for a floating number kind (reuses the rune palette as the single source of truth).</summary>
        public static uint ColorFor(FloatingNumberKind kind)
        {
            return kind switch
            {
                FloatingNumberKind.PhysicalDamage => UiTheme.RuneColor(RuneType.Red),
                FloatingNumberKind.MagicDamage => UiTheme.RuneColor(RuneType.Purple),
                FloatingNumberKind.Heal => UiTheme.RuneColor(RuneType.Green),
                FloatingNumberKind.ShieldAbsorbed => UiTheme.RuneColor(RuneType.Yellow),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown floating number kind.")
            };
        }

        /// <summary>The sign prefix for a kind: damage/absorb read "−", healing reads "+".</summary>
        public static string SignFor(FloatingNumberKind kind)
        {
            return kind switch
            {
                FloatingNumberKind.PhysicalDamage or FloatingNumberKind.MagicDamage or FloatingNumberKind.ShieldAbsorbed => "−",
                FloatingNumberKind.Heal => "+",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown floating number kind.")
            };
        }

        /// <summary>The player-facing label for an amount of a given kind, e.g. "−12" or "+34".</summary>
        public static string Format(FloatingNumberKind kind, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "A combat number amount cannot be negative.");
            }

            return SignFor(kind) + amount.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Vertical rise offset at a point in time (ease-out), clamped at <see cref="RiseDistance"/>.</summary>
        public static double OffsetAt(double secondsIntoRise)
        {
            if (secondsIntoRise < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsIntoRise), "Rise time cannot be negative.");
            }

            var t = Math.Min(1.0, secondsIntoRise / RiseSeconds);
            // Ease-out so the number darts up then settles.
            var eased = 1.0 - (1.0 - t) * (1.0 - t);
            return RiseDistance * eased;
        }

        /// <summary>Opacity at a point in time: fully opaque, fading to transparent as it finishes rising.</summary>
        public static double OpacityAt(double secondsIntoRise)
        {
            if (secondsIntoRise < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsIntoRise), "Rise time cannot be negative.");
            }

            var t = Math.Min(1.0, secondsIntoRise / RiseSeconds);
            return 1.0 - t;
        }
    }
}
