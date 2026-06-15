using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// One star-tier row on the hero detail screen: the star count, its tier pip colour
    /// (bronze/silver/gold, shared with the on-board star pips), the projected stats at that
    /// tier and a readable one-line stat summary. Pure data so the Unity layer only draws the row.
    /// </summary>
    public sealed record HeroStarStatLine(int Stars, uint PipColor, HeroStats Stats, string StatsLabel);

    /// <summary>
    /// Engine-agnostic presentation glue for the hero detail screen overhaul (GDD UI screen 7
    /// «Экран деталей героя»: large portrait, stats per star, ability/passive). The hero data lives
    /// on <see cref="HeroDefinition"/> / <see cref="HeroCollectionEntry"/>; this presentation says HOW
    /// the detail screen reads in the new portrait visual language: the large portrait card borrows
    /// the shared rarity-tier frame (<see cref="UiTheme.RarityColor"/> /
    /// <see cref="PlaceholderAssetCatalog.RarityFrame"/>), the preferred-rune highlight takes the
    /// matching rune-palette token, and the per-star stat rows reuse the on-board star pip colours
    /// (<see cref="UnitBoardPresentation"/>) so a hero's growth reads with the same star language as
    /// the battlefield. Rendering itself is Unity-only (documented gap); the contract is verified
    /// headless by <c>tools/CoreSmoke</c>. All art direction is original (codex.md IP rule).
    /// </summary>
    public static class HeroDetailPresentation
    {
        /// <summary>Generic portrait sprite placeholder; the rarity frame and tints apply on top.</summary>
        public static PlaceholderAssetSpec Portrait => PlaceholderAssetCatalog.UnitSprite;

        /// <summary>Highlight accent for the hero's preferred rune colour.</summary>
        public static uint RuneAffinityColor(HeroDefinition hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            return UiTheme.RuneColor(hero.RuneAffinity);
        }

        /// <summary>Rarity-tier border colour for the large portrait card.</summary>
        public static uint PortraitFrameColor(HeroDefinition hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            return UiTheme.RarityColor(hero.Rarity);
        }

        /// <summary>Rarity-frame placeholder spec for the large portrait card.</summary>
        public static PlaceholderAssetSpec PortraitFrame(HeroDefinition hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            return PlaceholderAssetCatalog.RarityFrame(hero.Rarity);
        }

        /// <summary>One-line stat summary for a stat block (same shape as the collection card).</summary>
        public static string FormatStats(HeroStats stats)
        {
            if (stats is null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            return $"HP {stats.BaseHealth:0}  ATK {stats.Attack:0}  AS {stats.BaseAttackSpeed:0.00}  "
                + $"ARM {stats.Armor:0}  MR {stats.MagicResist:0}  MP {stats.ManaMax:0}";
        }

        /// <summary>
        /// The per-star stat rows for a hero (1★ → 3★): each carries its tier pip colour and the
        /// stats projected at that star level, so the detail screen shows how the hero grows.
        /// </summary>
        public static IReadOnlyList<HeroStarStatLine> StarStats(HeroDefinition hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            var lines = new List<HeroStarStatLine>(UnitBoardPresentation.MaxStars - UnitBoardPresentation.MinStars + 1);
            for (var stars = UnitBoardPresentation.MinStars; stars <= UnitBoardPresentation.MaxStars; stars += 1)
            {
                var stats = hero.StatsForStars(stars);
                lines.Add(new HeroStarStatLine(stars, UnitBoardPresentation.StarTierColor(stars), stats, FormatStats(stats)));
            }

            return lines;
        }
    }
}
