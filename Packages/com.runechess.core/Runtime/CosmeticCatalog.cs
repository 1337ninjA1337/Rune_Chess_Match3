using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// MVP catalog of metaprogression cosmetics (GDD "Метапрогрессия": косметику и
    /// визуальные эффекты рун). Each entry is a purely visual <see cref="CosmeticDefinition"/>
    /// — board skins, rune visual effects and hero skins — that the account unlocks across
    /// runs through <see cref="CosmeticUnlockSchedule"/>. Cosmetics never touch combat or
    /// economy, so the reward stays non-pay-to-win. Pure data so the catalog can be
    /// smoke-tested without Unity.
    /// </summary>
    public static class CosmeticCatalog
    {
        private static readonly IReadOnlyList<CosmeticDefinition> Catalog = new List<CosmeticDefinition>
        {
            new("board_classic", "Классическая доска", CosmeticKind.BoardSkin,
                "Базовый вид match-3 доски."),
            new("rune_glow", "Сияние рун", CosmeticKind.RuneEffect,
                "Руны мягко светятся при совпадении."),
            new("board_obsidian", "Обсидиановая доска", CosmeticKind.BoardSkin,
                "Тёмный камень с прожилками."),
            new("rune_ember_trail", "Тлеющий след", CosmeticKind.RuneEffect,
                "Совпадения оставляют тлеющий шлейф."),
            new("hero_banner_gold", "Золотой штандарт", CosmeticKind.HeroSkin,
                "Золотая рамка-штандарт для портрета героя.")
        };

        /// <summary>Every cosmetic definition, in catalog (declaration) order.</summary>
        public static IReadOnlyList<CosmeticDefinition> All { get; } = Array.AsReadOnly(Catalog.ToArray());

        /// <summary>Rune visual-effect cosmetics (GDD "визуальные эффекты рун").</summary>
        public static IReadOnlyList<CosmeticDefinition> RuneEffects { get; } =
            All.Where(cosmetic => cosmetic.IsRuneEffect).ToList();

        /// <summary>Look up a cosmetic definition by id (case-insensitive).</summary>
        public static bool TryGet(string id, out CosmeticDefinition cosmetic)
        {
            cosmetic = All.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))!;
            return cosmetic is not null;
        }

        /// <summary>Look up a cosmetic definition by id, throwing when it is unknown.</summary>
        public static CosmeticDefinition Get(string id)
        {
            if (!TryGet(id, out var cosmetic))
            {
                throw new ArgumentException($"Unknown cosmetic id '{id}'.", nameof(id));
            }

            return cosmetic;
        }
    }
}
