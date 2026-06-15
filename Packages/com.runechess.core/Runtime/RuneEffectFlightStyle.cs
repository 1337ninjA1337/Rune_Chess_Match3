using System;

namespace RuneChess.Core
{
    /// <summary>Where a rune-effect icon/number flies when its match resolves.</summary>
    public enum RuneEffectFlightTarget
    {
        /// <summary>Offensive effects (physical/magic damage) fly to an enemy unit.</summary>
        EnemyUnit,
        /// <summary>Supportive effects (heal, shield, mana) fly to an allied unit.</summary>
        AllyUnit,
        /// <summary>Commander energy flies to the commander energy bar, not a unit.</summary>
        Commander
    }

    /// <summary>
    /// Engine-agnostic single source of truth for the visual link between a triggered rune and its
    /// effect on units: an icon/number flies from the matched rune along a gentle arc to the unit (or
    /// commander bar) the effect lands on. Colour/icon come from <see cref="RuneVisualStyle"/>, the
    /// destination from the effect kind. The Unity layer spawns and tweens the flying token;
    /// rendering is Unity-only (documented verification gap). The contract — a positive travel time,
    /// a clamped arc, full destination coverage and palette/icon parity — is verified headless via
    /// <c>tools/CoreSmoke</c>. All values are original tuning for this project.
    /// </summary>
    public static class RuneEffectFlightStyle
    {
        /// <summary>How long the icon/number takes to travel from the rune to its target.</summary>
        public const double TravelSeconds = 0.35;

        /// <summary>Peak height of the travel arc (relative to a board cell), reached at the midpoint.</summary>
        public const double ArcHeight = 0.6;

        /// <summary>Where the effect's flying icon/number lands.</summary>
        public static RuneEffectFlightTarget TargetFor(RuneEffectKind kind)
        {
            return kind switch
            {
                RuneEffectKind.PhysicalDamage or RuneEffectKind.MagicDamage => RuneEffectFlightTarget.EnemyUnit,
                RuneEffectKind.Healing or RuneEffectKind.Shield or RuneEffectKind.Mana => RuneEffectFlightTarget.AllyUnit,
                RuneEffectKind.CommanderEnergy => RuneEffectFlightTarget.Commander,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown rune effect kind.")
            };
        }

        /// <summary>Where a rune colour's effect flies, derived via its GDD effect kind.</summary>
        public static RuneEffectFlightTarget TargetForRune(RuneType rune) => TargetFor(RuneEffects.GetEffectKind(rune));

        /// <summary>The flying token colour for a rune (inherits the rune palette).</summary>
        public static uint ColorFor(RuneType rune) => RuneVisualStyle.Color(rune);

        /// <summary>The flying token glyph for a rune (matches the board rune glyph).</summary>
        public static string GlyphFor(RuneType rune) => RuneVisualStyle.GlyphFor(rune);

        /// <summary>The placeholder icon key the flying token's real sprite drops in behind.</summary>
        public static string IconKeyFor(RuneType rune) => RuneVisualStyle.IconKey(rune);

        /// <summary>Linear travel progress 0..1 over <see cref="TravelSeconds"/>, clamped after arrival.</summary>
        public static double ProgressAt(double secondsIntoFlight)
        {
            if (secondsIntoFlight < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsIntoFlight), "Flight time cannot be negative.");
            }

            return Math.Min(1.0, secondsIntoFlight / TravelSeconds);
        }

        /// <summary>
        /// The vertical arc offset at a travel progress (0..1): a parabola that is zero at both ends
        /// and peaks at <see cref="ArcHeight"/> in the middle, so the token lobs toward its target.
        /// </summary>
        public static double ArcOffsetAt(double progress)
        {
            if (progress < 0.0 || progress > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(progress), "Travel progress must be within [0,1].");
            }

            return ArcHeight * 4.0 * progress * (1.0 - progress);
        }
    }
}
