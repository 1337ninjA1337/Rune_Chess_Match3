using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Distinct silhouette per rune colour. The match-3 board's new visual language gives every
    /// rune an original symbol shape so the six runes stay readable even when colour alone is hard
    /// to tell apart (colour-blind accessibility, small-tile glance reads). Each shape is tied to
    /// the rune's GDD combat role, not to any third-party art (codex.md IP rule).
    /// </summary>
    public enum RuneShape
    {
        /// <summary>Red — physical damage: an upward blade chevron.</summary>
        Blade,
        /// <summary>Blue — mana: a droplet.</summary>
        Droplet,
        /// <summary>Green — healing: a leaf.</summary>
        Leaf,
        /// <summary>Yellow — shields/armour: a guarding crest.</summary>
        Crest,
        /// <summary>Purple — magic/curse: a four-point star.</summary>
        Star,
        /// <summary>White — universal energy: a radiant ring.</summary>
        Ring
    }

    /// <summary>
    /// Resolved presentation tokens for one rune colour: its palette colour, its distinct
    /// <see cref="RuneShape"/>, a compact placeholder glyph the primitive renderer draws until real
    /// art exists, and the stable <see cref="PlaceholderAssetCatalog"/> icon key the final sprite
    /// drops in behind.
    /// </summary>
    public sealed record RuneVisual(
        RuneType Rune,
        uint Color,
        RuneShape Shape,
        string Glyph,
        string IconKey);

    /// <summary>
    /// Engine-agnostic single source of truth for how the six runes look under the restyled match-3
    /// board: colour (from <see cref="UiTheme.RuneColor"/>), a unique silhouette per rune, and a
    /// placeholder glyph keyed to the <see cref="PlaceholderAssetCatalog"/> rune icon. The Unity
    /// layer draws the glyph/shape on the tile and swaps the real sprite in behind the same key;
    /// rendering itself is a Unity-side task (documented verification gap — no Unity render in the
    /// automation environment). The *contract* (distinct shapes, distinct glyphs, palette parity,
    /// icon-key parity) is verified headless via <c>tools/CoreSmoke</c>.
    /// </summary>
    public static class RuneVisualStyle
    {
        /// <summary>The distinct silhouette for a rune colour.</summary>
        public static RuneShape ShapeFor(RuneType rune)
        {
            return rune switch
            {
                RuneType.Red => RuneShape.Blade,
                RuneType.Blue => RuneShape.Droplet,
                RuneType.Green => RuneShape.Leaf,
                RuneType.Yellow => RuneShape.Crest,
                RuneType.Purple => RuneShape.Star,
                RuneType.White => RuneShape.Ring,
                _ => throw new ArgumentOutOfRangeException(nameof(rune), rune, "Unknown rune type.")
            };
        }

        /// <summary>
        /// A compact placeholder glyph the primitive renderer stamps on the tile so runes read by
        /// shape, not only colour, before the real sprite exists.
        /// </summary>
        public static string GlyphFor(RuneType rune)
        {
            return rune switch
            {
                RuneType.Red => "▲",
                RuneType.Blue => "◆",
                RuneType.Green => "♣",
                RuneType.Yellow => "⬣",
                RuneType.Purple => "✦",
                RuneType.White => "◎",
                _ => throw new ArgumentOutOfRangeException(nameof(rune), rune, "Unknown rune type.")
            };
        }

        /// <summary>The palette colour for a rune (delegates to the <see cref="UiTheme"/> source of truth).</summary>
        public static uint Color(RuneType rune) => UiTheme.RuneColor(rune);

        /// <summary>The stable placeholder icon key the final rune sprite drops in behind.</summary>
        public static string IconKey(RuneType rune) => PlaceholderAssetCatalog.RuneIcon(rune).Key;

        /// <summary>Resolve every presentation token for a rune colour in one call.</summary>
        public static RuneVisual For(RuneType rune) => new(
            Rune: rune,
            Color: Color(rune),
            Shape: ShapeFor(rune),
            Glyph: GlyphFor(rune),
            IconKey: IconKey(rune));

        /// <summary>The resolved visuals for all six runes, in <see cref="RuneTypes.All"/> order.</summary>
        public static IReadOnlyList<RuneVisual> All { get; } = BuildAll();

        private static IReadOnlyList<RuneVisual> BuildAll()
        {
            var visuals = new RuneVisual[RuneTypes.All.Count];
            for (var i = 0; i < RuneTypes.All.Count; i++)
            {
                visuals[i] = For(RuneTypes.All[i]);
            }

            return Array.AsReadOnly(visuals);
        }
    }
}
