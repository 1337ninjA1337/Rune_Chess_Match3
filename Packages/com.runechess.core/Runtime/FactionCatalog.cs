using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The five MVP factions and their GDD synergy breakpoints. Names match the
    /// labels stored on <see cref="HeroDefinition.Faction"/>.
    /// </summary>
    public static class FactionCatalog
    {
        public static SynergyDefinition Empire { get; } = new(
            Id: "empire",
            Name: "Империя",
            Kind: SynergyKind.Faction,
            Focus: "Броня, щиты, строй.",
            Tiers: new[]
            {
                new SynergyTier(2, "Союзники получают +10% брони."),
                new SynergyTier(4, "При сборе желтой руны передняя линия получает щит.")
            });

        public static SynergyDefinition Wild { get; } = new(
            Id: "wild",
            Name: "Дикие",
            Kind: SynergyKind.Faction,
            Focus: "Скорость атаки, вампиризм, агрессия.",
            Tiers: new[]
            {
                new SynergyTier(2, "+10% скорости атаки."),
                new SynergyTier(4, "После цепной реакции герои получают временный вампиризм.")
            });

        public static SynergyDefinition Abyssal { get; } = new(
            Id: "abyssal",
            Name: "Бездонные",
            Kind: SynergyKind.Faction,
            Focus: "Магический урон, проклятия, ослабление врагов.",
            Tiers: new[]
            {
                new SynergyTier(2, "Способности накладывают слабый дебафф."),
                new SynergyTier(4, "Фиолетовые руны наносят дополнительный урон.")
            });

        public static SynergyDefinition Mechanist { get; } = new(
            Id: "mechanist",
            Name: "Механисты",
            Kind: SynergyKind.Faction,
            Focus: "Турели, дроны, взрывы, стабильный урон.",
            Tiers: new[]
            {
                new SynergyTier(2, "В начале боя появляется малый дрон."),
                new SynergyTier(4, "Match-4 создает временную турель.")
            });

        public static SynergyDefinition Spirit { get; } = new(
            Id: "spirit",
            Name: "Духи",
            Kind: SynergyKind.Faction,
            Focus: "Уклонение, иллюзии, контроль.",
            Tiers: new[]
            {
                new SynergyTier(2, "Шанс уклонения для союзников."),
                new SynergyTier(4, "Белые руны могут создать иллюзию случайного героя.")
            });

        public static IReadOnlyList<SynergyDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            Empire,
            Wild,
            Abyssal,
            Mechanist,
            Spirit
        });

        private static IReadOnlyDictionary<string, SynergyDefinition> ByName { get; } = All.ToDictionary(
            faction => faction.Name,
            faction => faction,
            StringComparer.OrdinalIgnoreCase);

        public static SynergyDefinition Get(string name)
        {
            if (TryGet(name, out var faction))
            {
                return faction;
            }

            throw new ArgumentException($"Unknown faction '{name}'.", nameof(name));
        }

        public static bool TryGet(string? name, out SynergyDefinition faction)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                faction = null!;
                return false;
            }

            return ByName.TryGetValue(name.Trim(), out faction!);
        }
    }
}
