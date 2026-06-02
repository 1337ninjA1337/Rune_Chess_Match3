using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The three MVP commanders from the GDD "Командиры" section. Passive text is taken
    /// verbatim from the GDD; the mechanical passives are implemented in their own tasks.
    /// </summary>
    public static class CommanderCatalog
    {
        public static CommanderDefinition RuneArchon { get; } = new(
            Id: "rune_archon",
            Name: "Архонт Рун",
            Passive: "Каждое третье match-4 комбо создает дополнительную синюю руну.",
            MaxEnergy: 100,
            StartingBonus: new CommanderStartingBonus(
                Description: "Начинает забег с 20 энергии командира.",
                Kind: CommanderStartingBonusKind.CommanderEnergy,
                Amount: 20),
            RecommendedStyles: Array.AsReadOnly(new[] { "Маги", "Синие руны", "Способности" }));

        public static CommanderDefinition Warlord { get; } = new(
            Id: "warlord",
            Name: "Воевода",
            Passive: "Первый защитник в каждом бою получает +20% здоровья.",
            MaxEnergy: 100,
            StartingBonus: new CommanderStartingBonus(
                Description: "Начинает забег с Железным Стражем на скамейке.",
                Kind: CommanderStartingBonusKind.BenchHero,
                HeroId: "iron_guard"),
            RecommendedStyles: Array.AsReadOnly(new[] { "Защитники", "Передняя линия", "Танки" }));

        public static CommanderDefinition Alchemist { get; } = new(
            Id: "alchemist",
            Name: "Алхимик",
            Passive: "После каждого раунда получает +1 золото, если игрок сделал хотя бы одну цепную реакцию.",
            MaxEnergy: 100,
            StartingBonus: new CommanderStartingBonus(
                Description: "Начинает забег с +2 золота.",
                Kind: CommanderStartingBonusKind.Gold,
                Amount: 2),
            RecommendedStyles: Array.AsReadOnly(new[] { "Экономика", "Цепные реакции", "Гибкий состав" }));

        public static IReadOnlyList<CommanderDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            RuneArchon,
            Warlord,
            Alchemist
        });

        public static CommanderDefinition Default { get; } = RuneArchon;

        private static IReadOnlyDictionary<string, CommanderDefinition> ById { get; } = All.ToDictionary(
            commander => commander.Id,
            commander => commander,
            StringComparer.OrdinalIgnoreCase);

        public static CommanderDefinition Get(string id)
        {
            if (TryGet(id, out var commander))
            {
                return commander;
            }

            throw new ArgumentException($"Unknown commander '{id}'.", nameof(id));
        }

        public static bool TryGet(string? id, out CommanderDefinition commander)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                commander = null!;
                return false;
            }

            return ById.TryGetValue(id.Trim(), out commander!);
        }
    }
}
