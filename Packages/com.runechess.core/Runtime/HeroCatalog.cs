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

        public static HeroDefinition OathArcher { get; } = new(
            Id: "oath_archer",
            Name: "Лучница Присяги",
            Rarity: HeroRarity.Common,
            Cost: 1,
            Faction: "Империя",
            Class: "Стрелок",
            RuneAffinity: RuneType.Red,
            Role: HeroRole.Carry,
            AttackType: "ranged",
            Targeting: "current",
            Stars: 1,
            Ability: "Серия быстрых выстрелов по текущей цели.",
            Passive: "Боевой фокус: наносит больше урона базовыми атаками.",
            BaseStats: new HeroStats(
                BaseHealth: 520,
                Attack: 65,
                Armor: 3,
                MagicResist: 2,
                BaseAttackSpeed: 1.05,
                ManaMax: 60)
        );

        public static HeroDefinition FieldMedic { get; } = new(
            Id: "field_medic",
            Name: "Полевой Медик",
            Rarity: HeroRarity.Common,
            Cost: 1,
            Faction: "Империя",
            Class: "Целитель",
            RuneAffinity: RuneType.Green,
            Role: HeroRole.Healer,
            AttackType: "ranged",
            Targeting: "lowest_health_ally",
            Stars: 1,
            Ability: "Лечит самого раненого союзника.",
            Passive: "Медицинская подготовка: лучше сопротивляется магическому урону.",
            BaseStats: new HeroStats(
                BaseHealth: 560,
                Attack: 35,
                Armor: 2,
                MagicResist: 5,
                BaseAttackSpeed: 0.85,
                ManaMax: 70)
        );

        public static IReadOnlyList<HeroDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            IronGuard,
            OathArcher,
            FieldMedic
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
