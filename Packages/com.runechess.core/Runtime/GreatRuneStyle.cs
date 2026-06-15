using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Resolved presentation tokens for a great rune (the match-5 result) of a given colour: it
    /// keeps the colour's base palette and silhouette but adds a distinct burst glyph and a bright
    /// aura so it reads as an empowered, stored rune on the board.
    /// </summary>
    public sealed record GreatRuneVisual(
        RuneType Rune,
        uint Color,
        uint AuraColor,
        string BaseGlyph,
        string Glyph,
        double Multiplier);

    /// <summary>
    /// Engine-agnostic single source of truth for how a great rune (match-5) and its activation look.
    /// A great rune reuses its colour's <see cref="RuneVisualStyle"/> base, overlays a burst glyph and
    /// a bright aura, and carries the x2.5 activation multiplier (from <see cref="RuneEffects"/>) for
    /// its label. The Unity layer paints the aura/glyph and plays the activation flash; rendering is
    /// Unity-only (documented verification gap). The contract — multiplier parity, an aura that reads
    /// over every rune, a burst glyph distinct from the base runes, and a positive activation flash —
    /// is verified headless via <c>tools/CoreSmoke</c>. All art is original to this project.
    /// </summary>
    public static class GreatRuneStyle
    {
        /// <summary>Great-rune activation multiplier (mirrors <see cref="RuneEffects.GreatRuneMultiplier"/>).</summary>
        public static double Multiplier => RuneEffects.GreatRuneMultiplier;

        /// <summary>Player-facing multiplier label, e.g. "x2.5".</summary>
        public static string MultiplierLabel =>
            "x" + Multiplier.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Bright pale-gold halo overlaid behind the rune to mark it as a stored great rune.</summary>
        public const uint AuraColor = 0xFFF4C2u;

        /// <summary>Burst glyph overlaid on the rune to mark it as a great rune (distinct from all base runes).</summary>
        public const string GreatGlyph = "✸";

        /// <summary>How long the activation flash plays when a stored great rune is triggered.</summary>
        public const double ActivationFlashSeconds = 0.40;

        /// <summary>Resolve every great-rune presentation token for a colour.</summary>
        public static GreatRuneVisual For(RuneType rune) => new(
            Rune: rune,
            Color: RuneVisualStyle.Color(rune),
            AuraColor: AuraColor,
            BaseGlyph: RuneVisualStyle.GlyphFor(rune),
            Glyph: GreatGlyph,
            Multiplier: Multiplier);
    }
}
