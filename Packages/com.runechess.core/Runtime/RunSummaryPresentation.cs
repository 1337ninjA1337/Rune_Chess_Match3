using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic presentation glue for the end-of-run summary screen overhaul
    /// (GDD UI screen «Итог забега»). The <see cref="RunSummaryModel"/> already says WHAT the
    /// screen shows (result, progress, final roster, best hero, meta rewards); this presentation
    /// says HOW it reads in the new portrait visual language: each roster hero borrows the shared
    /// rarity-tier border (<see cref="UiTheme.RarityColor"/> / <see cref="PlaceholderAssetCatalog.RarityFrame"/>,
    /// the same frame hero cards and reward artifacts use), the run's best hero gets the warm
    /// <see cref="UiTheme.GoldColor"/> spotlight (the shared «hero/go» accent), and the result headline
    /// takes a win/loss accent from the rune palette (green for a cleared run, red for a lost one).
    /// Rendering itself is Unity-only (documented gap); the contract is verified headless by
    /// <c>tools/CoreSmoke</c>. All art direction is original (codex.md IP rule).
    /// </summary>
    public static class RunSummaryPresentation
    {
        /// <summary>Warm spotlight accent for the run's best hero (the shared «hero/go» accent).</summary>
        public const uint BestHeroAccentColor = UiTheme.GoldColor;

        /// <summary>Result-headline accent: green for a cleared run, red for a lost one.</summary>
        public static uint OutcomeAccentColor(bool isVictory) =>
            UiTheme.RuneColor(isVictory ? RuneType.Green : RuneType.Red);

        /// <summary>Rarity-tier border colour for a roster hero card.</summary>
        public static uint HeroFrameColor(RunSummaryHero hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            return UiTheme.RarityColor(hero.Rarity);
        }

        /// <summary>Rarity-frame placeholder spec for a roster hero card.</summary>
        public static PlaceholderAssetSpec HeroFrame(RunSummaryHero hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            return PlaceholderAssetCatalog.RarityFrame(hero.Rarity);
        }

        /// <summary>
        /// True when this roster hero is the run's best hero (the one the screen spotlights).
        /// Compared by value so it matches the same hero entry the model selected.
        /// </summary>
        public static bool IsBestHero(RunSummaryModel model, RunSummaryHero hero)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            return model.BestHero is { } best && best == hero;
        }
    }
}
