using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The monetization design as data (GDD "Монетизация"): the acceptable cosmetic formats
    /// the game may sell and the prohibited formats it must never sell. Mirrors the data-first
    /// pattern of <see cref="CosmeticCatalog"/> so the design is a single source of truth that
    /// <see cref="MonetizationPolicy"/> and smoke checks can verify. Every acceptable format is
    /// cosmetic-only; every prohibited format would break the non-pay-to-win rule. See
    /// docs/monetization.md.
    /// </summary>
    public static class MonetizationCatalog
    {
        private static readonly IReadOnlyList<MonetizationFormat> Formats = new List<MonetizationFormat>
        {
            new("battle_pass", "Боевой пропуск", MonetizationFormatKind.BattlePass,
                "Сезонный косметический трек наград, только внешний вид.", AppliesTo: null),
            new("hero_skins", "Скины героев", MonetizationFormatKind.HeroSkin,
                "Альтернативный вид героя без изменения характеристик.", AppliesTo: CosmeticKind.HeroSkin),
            new("board_skins", "Скины доски", MonetizationFormatKind.BoardSkin,
                "Альтернативный вид match-3 доски.", AppliesTo: CosmeticKind.BoardSkin),
            new("rune_effects", "Эффекты рун", MonetizationFormatKind.RuneEffect,
                "Визуальные эффекты совпадений рун.", AppliesTo: CosmeticKind.RuneEffect),
            new("commander_portraits", "Портреты командиров", MonetizationFormatKind.CommanderPortrait,
                "Альтернативная аватарка командира.", AppliesTo: null),
            new("emotes", "Эмоции", MonetizationFormatKind.Emote,
                "Выразительные эмоции в матче.", AppliesTo: null),
            new("cosmetic_finishers", "Косметические добивания", MonetizationFormatKind.CosmeticFinisher,
                "Косметический росчерк при добивании или победе.", AppliesTo: null),
            new("cosmetic_progress_boost", "Ускорение косметического прогресса", MonetizationFormatKind.CosmeticProgressBoost,
                "Ускоряет только косметический прогресс, не боевую силу.", AppliesTo: null),
        };

        private static readonly IReadOnlyDictionary<MonetizationProhibitionKind, string> ProhibitionReasons =
            new Dictionary<MonetizationProhibitionKind, string>
            {
                [MonetizationProhibitionKind.SellHeroPower] =
                    "Продажа силы героев даёт прямое боевое преимущество за деньги.",
                [MonetizationProhibitionKind.PaidAdvantageHeroes] =
                    "Платные герои с преимуществом ломают равенство составов.",
                [MonetizationProhibitionKind.PaidCombatArtifacts] =
                    "Платные артефакты, влияющие на боевой баланс, дают преимущество за деньги.",
                [MonetizationProhibitionKind.MandatoryEnergyToPlay] =
                    "Обязательная энергия для игры гейтит саму игру за оплату.",
            };

        /// <summary>Every acceptable monetization format, in GDD declaration order.</summary>
        public static IReadOnlyList<MonetizationFormat> AcceptableFormats { get; } =
            Array.AsReadOnly(Formats.ToArray());

        /// <summary>Every prohibited monetization format the game must never ship.</summary>
        public static IReadOnlyList<MonetizationProhibitionKind> Prohibitions { get; } =
            Array.AsReadOnly(ProhibitionReasons.Keys.ToArray());

        /// <summary>Look up an acceptable format by id (case-insensitive).</summary>
        public static bool TryGet(string id, out MonetizationFormat format)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                var trimmed = id.Trim();
                foreach (var candidate in Formats)
                {
                    if (string.Equals(candidate.Id, trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        format = candidate;
                        return true;
                    }
                }
            }

            format = null!;
            return false;
        }

        /// <summary>Get an acceptable format by id, or throw when unknown.</summary>
        public static MonetizationFormat Get(string id) => TryGet(id, out var format)
            ? format
            : throw new ArgumentException($"Unknown monetization format id '{id}'.", nameof(id));

        /// <summary>The single acceptable format of a given kind.</summary>
        public static MonetizationFormat ForKind(MonetizationFormatKind kind) =>
            Formats.SingleOrDefault(format => format.Kind == kind)
            ?? throw new ArgumentOutOfRangeException(nameof(kind), $"No monetization format for kind '{kind}'.");

        /// <summary>Why a given format is prohibited (GDD "Нежелательные форматы").</summary>
        public static string ReasonFor(MonetizationProhibitionKind prohibition) =>
            ProhibitionReasons.TryGetValue(prohibition, out var reason)
                ? reason
                : throw new ArgumentOutOfRangeException(nameof(prohibition));
    }
}
