using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    public static class HeroCatalog
    {
        public static HeroDefinition IronGuard { get; } = new(
            Id: "iron_guard",
            Name: "Железный Страж",
            Rarity: HeroRarity.Common,
            Cost: 1,
            Faction: "Империя",
            Class: "Защитник",
            RuneAffinity: RuneType.Yellow,
            Role: HeroRole.Tank,
            AttackType: "melee",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Стальной заслон: щит на себя и ближайшего союзника.",
            Passive: "Стойка авангарда: получает меньше урона в первом ряду.",
            BaseStats: new HeroStats(
                BaseHealth: 750,
                Attack: 45,
                Armor: 8,
                MagicResist: 3,
                BaseAttackSpeed: 0.75,
                ManaMax: 80)
        );

        public static IReadOnlyList<HeroDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            IronGuard
        });

        private static IReadOnlyDictionary<string, HeroDefinition> ById { get; } = All.ToDictionary(
            hero => hero.Id,
            hero => hero,
            StringComparer.OrdinalIgnoreCase);

        public static HeroDefinition Get(string id)
        {
            if (TryGet(id, out var hero))
            {
                return hero;
            }

            throw new ArgumentException($"Unknown hero id '{id}'.", nameof(id));
        }

        public static bool TryGet(string? id, out HeroDefinition hero)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                hero = null!;
                return false;
            }

            return ById.TryGetValue(id.Trim(), out hero!);
        }
    }
}
