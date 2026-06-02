using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The seven MVP hero classes from the GDD class list. Names match the labels
    /// stored on <see cref="HeroDefinition.Class"/>.
    ///
    /// Resolved product decision: the GDD MVP-scope summary says "6 classes" but the
    /// detailed class list enumerates 7 (Защитник, Убийца, Маг, Стрелок, Целитель,
    /// Призыватель, Берсерк). Per the codex rule that the GDD is the source of truth,
    /// the explicit list wins, so the MVP ships 7 classes. Only Защитник, Маг and
    /// Убийца have GDD-defined synergy breakpoints; the remaining classes are still
    /// real classes but carry no class synergy yet.
    /// </summary>
    public static class ClassCatalog
    {
        public static SynergyDefinition Defender { get; } = new(
            Id: "defender",
            Name: "Защитник",
            Kind: SynergyKind.Class,
            Focus: "Передняя линия, здоровье, броня.",
            Tiers: new[]
            {
                new SynergyTier(2, "Передняя линия получает +15% здоровья."),
                new SynergyTier(4, "Желтые руны дополнительно усиливают броню.")
            });

        public static SynergyDefinition Assassin { get; } = new(
            Id: "assassin",
            Name: "Убийца",
            Kind: SynergyKind.Class,
            Focus: "Прыжки к задней линии, крит, бурст.",
            Tiers: new[]
            {
                new SynergyTier(3, "Убийцы прыгают к задней линии врага."),
                new SynergyTier(6, "Критические удары дополнительно заряжают красные руны.")
            });

        public static SynergyDefinition Mage { get; } = new(
            Id: "mage",
            Name: "Маг",
            Kind: SynergyKind.Class,
            Focus: "Способности, магический урон, синие руны.",
            Tiers: new[]
            {
                new SynergyTier(3, "Способности наносят +20% урона."),
                new SynergyTier(5, "Match-4 синей руной дает дополнительный заряд случайному магу.")
            });

        public static SynergyDefinition Marksman { get; } = new(
            Id: "marksman",
            Name: "Стрелок",
            Kind: SynergyKind.Class,
            Focus: "Дальний стабильный урон.",
            Tiers: Array.Empty<SynergyTier>());

        public static SynergyDefinition Healer { get; } = new(
            Id: "healer",
            Name: "Целитель",
            Kind: SynergyKind.Class,
            Focus: "Лечение и поддержка.",
            Tiers: Array.Empty<SynergyTier>());

        public static SynergyDefinition Summoner { get; } = new(
            Id: "summoner",
            Name: "Призыватель",
            Kind: SynergyKind.Class,
            Focus: "Призыв юнитов, давление полем.",
            Tiers: Array.Empty<SynergyTier>());

        public static SynergyDefinition Berserker { get; } = new(
            Id: "berserker",
            Name: "Берсерк",
            Kind: SynergyKind.Class,
            Focus: "Ближний бой, ярость, вампиризм.",
            Tiers: Array.Empty<SynergyTier>());

        public static IReadOnlyList<SynergyDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            Defender,
            Assassin,
            Mage,
            Marksman,
            Healer,
            Summoner,
            Berserker
        });

        private static IReadOnlyDictionary<string, SynergyDefinition> ByName { get; } = All.ToDictionary(
            heroClass => heroClass.Name,
            heroClass => heroClass,
            StringComparer.OrdinalIgnoreCase);

        public static SynergyDefinition Get(string name)
        {
            if (TryGet(name, out var heroClass))
            {
                return heroClass;
            }

            throw new ArgumentException($"Unknown class '{name}'.", nameof(name));
        }

        public static bool TryGet(string? name, out SynergyDefinition heroClass)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                heroClass = null!;
                return false;
            }

            return ByName.TryGetValue(name.Trim(), out heroClass!);
        }
    }
}
