using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>The five GDD main-screen entry points the menu presents.</summary>
    public enum MainMenuDestination
    {
        StartRun,
        Commander,
        Collection,
        CosmeticShop,
        Settings
    }

    /// <summary>
    /// One rendered tile on the main menu: where it leads, its title and meta line, the
    /// navigation placeholder icon to draw, whether it is the primary call-to-action (the
    /// run tile, drawn as the hero button) and the accent colour to tint the primary tile.
    /// Pure data so the Unity layer only lays out the tiles without re-deriving any labels.
    /// </summary>
    public sealed record MainMenuTile(
        MainMenuDestination Destination,
        string Title,
        string Meta,
        string IconKey,
        bool IsPrimary,
        uint? AccentColor);

    /// <summary>
    /// Engine-agnostic presentation glue for the main menu overhaul (GDD UI screen 1
    /// «Главный экран»). It turns a <see cref="MainMenuModel"/> into the ordered entry tiles
    /// the new portrait menu draws — the start-run hero tile plus the commander, collection,
    /// cosmetics-shop and settings shortcuts — each with its title, meta line and navigation
    /// placeholder icon. The run tile is the single primary call-to-action and carries the
    /// warm <see cref="UiTheme.GoldColor"/> accent (the project's one "go" accent, shared with
    /// the HUD gold readout); the other tiles are neutral (accent <c>null</c>, surface decided
    /// by the view). Rendering itself is Unity-only (documented gap); the contract is verified
    /// headless by <c>tools/CoreSmoke</c>. All art direction is original (codex.md IP rule).
    /// </summary>
    public static class MainMenuPresentation
    {
        /// <summary>Stable navigation placeholder icon key for a menu destination.</summary>
        public static string IconKey(MainMenuDestination destination)
        {
            return destination switch
            {
                MainMenuDestination.StartRun => "nav.run",
                MainMenuDestination.Commander => "nav.commander",
                MainMenuDestination.Collection => "nav.collection",
                MainMenuDestination.CosmeticShop => "nav.cosmetics",
                MainMenuDestination.Settings => "nav.settings",
                _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, "Unknown main menu destination.")
            };
        }

        /// <summary>Navigation placeholder icon spec for a destination, resolved from the asset catalog.</summary>
        public static PlaceholderAssetSpec Icon(MainMenuDestination destination)
        {
            var key = IconKey(destination);
            if (!PlaceholderAssetCatalog.TryGet(key, out var spec))
            {
                throw new InvalidOperationException($"No placeholder icon registered for main menu destination key '{key}'.");
            }

            return spec;
        }

        /// <summary>
        /// The main-menu tiles in reading order: the start-run hero tile first, then the
        /// commander, collection, cosmetics-shop and settings shortcuts. Labels and meta lines
        /// come straight from the <paramref name="model"/> so the view never re-derives them.
        /// </summary>
        public static IReadOnlyList<MainMenuTile> Tiles(MainMenuModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return new[]
            {
                new MainMenuTile(
                    MainMenuDestination.StartRun,
                    model.StartRunLabel,
                    model.StartRunMeta,
                    IconKey(MainMenuDestination.StartRun),
                    IsPrimary: true,
                    AccentColor: UiTheme.GoldColor),
                new MainMenuTile(
                    MainMenuDestination.Commander,
                    "Командир",
                    model.CommanderName,
                    IconKey(MainMenuDestination.Commander),
                    IsPrimary: false,
                    AccentColor: null),
                new MainMenuTile(
                    MainMenuDestination.Collection,
                    "Коллекция",
                    model.CollectionLabel,
                    IconKey(MainMenuDestination.Collection),
                    IsPrimary: false,
                    AccentColor: null),
                new MainMenuTile(
                    MainMenuDestination.CosmeticShop,
                    model.CosmeticShopLabel,
                    model.CosmeticShopMeta,
                    IconKey(MainMenuDestination.CosmeticShop),
                    IsPrimary: false,
                    AccentColor: null),
                new MainMenuTile(
                    MainMenuDestination.Settings,
                    "Настройки",
                    string.Empty,
                    IconKey(MainMenuDestination.Settings),
                    IsPrimary: false,
                    AccentColor: null)
            };
        }
    }
}
