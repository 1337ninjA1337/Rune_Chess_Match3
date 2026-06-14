using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>The category of placeholder art a <see cref="PlaceholderAssetSpec"/> stands in for.</summary>
    public enum PlaceholderAssetKind
    {
        UnitSprite,
        RarityFrame,
        RuneIcon,
        FactionIcon,
        ClassIcon,
        ArenaBackground,
        HudIcon
    }

    /// <summary>
    /// One required placeholder asset described as pure data. Until original art exists the Unity
    /// layer ships neutral primitives (coloured quads/frames/glyphs) keyed to these stable
    /// <see cref="Key"/>s; real sprites later drop in behind the same key without touching view code
    /// (see <c>docs/visual-style.md</c> "Placeholder asset pipeline"). <see cref="TokenColor"/> is the
    /// packed <see cref="UiTheme"/> colour the placeholder tints to when one applies (rune/rarity);
    /// it is <c>null</c> for assets whose colour is decided at runtime (faction/class/arena tints).
    /// </summary>
    public sealed record PlaceholderAssetSpec(
        string Key,
        PlaceholderAssetKind Kind,
        string DisplayName,
        uint? TokenColor,
        string Description)
    {
        public string Key { get; init; } = string.IsNullOrWhiteSpace(Key)
            ? throw new ArgumentException("Placeholder asset key cannot be blank.", nameof(Key))
            : Key.Trim();

        public string DisplayName { get; init; } = string.IsNullOrWhiteSpace(DisplayName)
            ? throw new ArgumentException("Placeholder asset display name cannot be blank.", nameof(DisplayName))
            : DisplayName.Trim();

        public string Description { get; init; } = string.IsNullOrWhiteSpace(Description)
            ? throw new ArgumentException("Placeholder asset description cannot be blank.", nameof(Description))
            : Description.Trim();

        /// <summary>True when this asset tints to a fixed <see cref="UiTheme"/> token colour.</summary>
        public bool HasTokenColor => TokenColor.HasValue;
    }

    /// <summary>
    /// Engine-agnostic manifest of every placeholder asset the portrait auto-battler presentation
    /// needs: a neutral unit sprite, rarity card frames, the six rune icons, faction/class icons and
    /// arena backgrounds. This is the single source of truth the Unity import/generation pipeline and
    /// any tooling enumerate so the placeholder set stays complete and keyed to <see cref="UiTheme"/>.
    /// Generating the sprites themselves is a Unity-side task (documented verification gap — no
    /// Unity/.NET render available in the automation environment); this catalog makes the *contract*
    /// verifiable headless via <c>tools/CoreSmoke</c>. All art is original to this project (codex.md IP
    /// rule — no Dota/Valve names, lore or visual style).
    /// </summary>
    public static class PlaceholderAssetCatalog
    {
        /// <summary>Neutral facing unit silhouette; the view flips it so allies and enemies face off.</summary>
        public static PlaceholderAssetSpec UnitSprite { get; } = new(
            Key: "unit.placeholder",
            Kind: PlaceholderAssetKind.UnitSprite,
            DisplayName: "Юнит (плейсхолдер)",
            TokenColor: null,
            Description: "Нейтральный силуэт юнита; вид зеркалит спрайт, чтобы союзники и враги смотрели друг на друга. Рамка редкости и тинты накладываются отдельно.");

        /// <summary>Card/board frame per rarity, tinted to the rarity token colour.</summary>
        public static IReadOnlyList<PlaceholderAssetSpec> RarityFrames { get; } = Array.AsReadOnly(
            HeroRarities.All
                .Select(rarity => new PlaceholderAssetSpec(
                    Key: $"frame.{HeroRarities.GetId(rarity)}",
                    Kind: PlaceholderAssetKind.RarityFrame,
                    DisplayName: $"Рамка: {HeroRarities.GetId(rarity)}",
                    TokenColor: UiTheme.RarityColor(rarity),
                    Description: "Рамка карточки/юнита по редкости. Тинт берётся из UiTheme.RarityColor (единый источник правды)."))
                .ToArray());

        /// <summary>Icon per rune colour, tinted to the rune palette token colour.</summary>
        public static IReadOnlyList<PlaceholderAssetSpec> RuneIcons { get; } = Array.AsReadOnly(
            RuneTypes.All
                .Select(rune => new PlaceholderAssetSpec(
                    Key: $"rune.{RuneTypes.GetId(rune)}",
                    Kind: PlaceholderAssetKind.RuneIcon,
                    DisplayName: $"Руна: {RuneTypes.GetId(rune)}",
                    TokenColor: UiTheme.RuneColor(rune),
                    Description: "Иконка-глиф руны match-3. Цвет берётся из UiTheme.RuneColor (единый источник правды для доски и иконок)."))
                .ToArray());

        /// <summary>Icon per MVP faction (colour decided at runtime by synergy tier).</summary>
        public static IReadOnlyList<PlaceholderAssetSpec> FactionIcons { get; } = Array.AsReadOnly(
            FactionCatalog.All
                .Select(faction => new PlaceholderAssetSpec(
                    Key: $"faction.{faction.Id}",
                    Kind: PlaceholderAssetKind.FactionIcon,
                    DisplayName: faction.Name,
                    TokenColor: null,
                    Description: $"Иконка фракции «{faction.Name}» для панели синергий. Тинт уровня синергии накладывается в рантайме (UiTheme.SynergyTierColor)."))
                .ToArray());

        /// <summary>Icon per MVP class (colour decided at runtime by synergy tier).</summary>
        public static IReadOnlyList<PlaceholderAssetSpec> ClassIcons { get; } = Array.AsReadOnly(
            ClassCatalog.All
                .Select(heroClass => new PlaceholderAssetSpec(
                    Key: $"class.{heroClass.Id}",
                    Kind: PlaceholderAssetKind.ClassIcon,
                    DisplayName: heroClass.Name,
                    TokenColor: null,
                    Description: $"Иконка класса «{heroClass.Name}» для панели синергий. Тинт уровня синергии накладывается в рантайме (UiTheme.SynergyTierColor)."))
                .ToArray());

        /// <summary>
        /// Arena backgrounds keyed to battle archetypes. Non-combat rounds (event, enhanced shop)
        /// reuse no arena and are intentionally excluded; <see cref="ArenaBackgroundFor"/> rejects them.
        /// </summary>
        public static IReadOnlyList<PlaceholderAssetSpec> ArenaBackgrounds { get; } = Array.AsReadOnly(new[]
        {
            new PlaceholderAssetSpec(
                Key: "arena.field",
                Kind: PlaceholderAssetKind.ArenaBackground,
                DisplayName: "Арена: поле",
                TokenColor: null,
                Description: "Фон обычной арены боя (туториал и рядовые бои)."),
            new PlaceholderAssetSpec(
                Key: "arena.elite",
                Kind: PlaceholderAssetKind.ArenaBackground,
                DisplayName: "Арена: элита",
                TokenColor: null,
                Description: "Фон арены элитного боя."),
            new PlaceholderAssetSpec(
                Key: "arena.throne",
                Kind: PlaceholderAssetKind.ArenaBackground,
                DisplayName: "Арена: трон",
                TokenColor: null,
                Description: "Фон арены боссов и финального босса.")
        });

        /// <summary>Coin/economy icon for the combat HUD gold readout, tinted to the gold token colour.</summary>
        public static PlaceholderAssetSpec GoldHudIcon { get; } = new(
            Key: "hud.gold",
            Kind: PlaceholderAssetKind.HudIcon,
            DisplayName: "Иконка: золото",
            TokenColor: UiTheme.GoldColor,
            Description: "Иконка-монета рядом с числом золота в HUD. Тинт берётся из UiTheme.GoldColor (единый источник правды для золотого акцента).");

        /// <summary>Top-bar HUD icons (gold). Colour-fixed to their UiTheme accent token.</summary>
        public static IReadOnlyList<PlaceholderAssetSpec> HudIcons { get; } = Array.AsReadOnly(new[]
        {
            GoldHudIcon
        });

        /// <summary>Every placeholder asset across all categories.</summary>
        public static IReadOnlyList<PlaceholderAssetSpec> All { get; } = Array.AsReadOnly(
            new[] { UnitSprite }
                .Concat(RarityFrames)
                .Concat(RuneIcons)
                .Concat(FactionIcons)
                .Concat(ClassIcons)
                .Concat(ArenaBackgrounds)
                .Concat(HudIcons)
                .ToArray());

        private static IReadOnlyDictionary<string, PlaceholderAssetSpec> ByKey { get; } =
            All.ToDictionary(spec => spec.Key, StringComparer.Ordinal);

        /// <summary>The placeholder spec for a rune colour.</summary>
        public static PlaceholderAssetSpec RuneIcon(RuneType rune) => RuneIcons[(int)rune];

        /// <summary>The placeholder spec for a rarity card frame.</summary>
        public static PlaceholderAssetSpec RarityFrame(HeroRarity rarity) => RarityFrames[(int)rarity];

        /// <summary>
        /// The arena background for a battle round archetype. Tutorial/Combat share the field arena,
        /// Boss/FinalBoss share the throne arena. Non-combat rounds (Event, EnhancedShop) have no arena.
        /// </summary>
        public static PlaceholderAssetSpec ArenaBackgroundFor(PveRoundType roundType)
        {
            return roundType switch
            {
                PveRoundType.Tutorial or PveRoundType.Combat => ByKey["arena.field"],
                PveRoundType.Elite => ByKey["arena.elite"],
                PveRoundType.Boss or PveRoundType.FinalBoss => ByKey["arena.throne"],
                PveRoundType.Event or PveRoundType.EnhancedShop =>
                    throw new ArgumentException($"Round type '{roundType}' is not a battle and has no arena background.", nameof(roundType)),
                _ => throw new ArgumentOutOfRangeException(nameof(roundType), roundType, "Unknown round type.")
            };
        }

        /// <summary>Look up a placeholder spec by its stable key.</summary>
        public static bool TryGet(string? key, out PlaceholderAssetSpec spec)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                spec = null!;
                return false;
            }

            return ByKey.TryGetValue(key.Trim(), out spec!);
        }
    }
}
