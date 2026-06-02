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

        public static HeroDefinition WildClaw { get; } = new(
            Id: "wild_claw",
            Name: "Дикий Коготь",
            Rarity: HeroRarity.Common,
            Cost: 1,
            Faction: "Дикие",
            Class: "Берсерк",
            RuneAffinity: RuneType.Red,
            Role: HeroRole.Bruiser,
            AttackType: "melee",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Получает скорость атаки и вампиризм.",
            Passive: "Живучесть берсерка: получает дополнительное здоровье.",
            BaseStats: new HeroStats(
                BaseHealth: 700,
                Attack: 55,
                Armor: 5,
                MagicResist: 3,
                BaseAttackSpeed: 0.9,
                ManaMax: 65)
        );

        public static HeroDefinition ThornShaman { get; } = new(
            Id: "thorn_shaman",
            Name: "Терновый Шаман",
            Rarity: HeroRarity.Common,
            Cost: 1,
            Faction: "Дикие",
            Class: "Призыватель",
            RuneAffinity: RuneType.Green,
            Role: HeroRole.Summoner,
            AttackType: "ranged",
            Targeting: "summon_slot",
            Stars: 1,
            Ability: "Призывает тернового зверя.",
            Passive: "Зов чащи: начинает бой с частью маны.",
            BaseStats: new HeroStats(
                BaseHealth: 540,
                Attack: 40,
                Armor: 3,
                MagicResist: 5,
                BaseAttackSpeed: 0.8,
                ManaMax: 75)
        );

        public static HeroDefinition MistCutthroat { get; } = new(
            Id: "mist_cutthroat",
            Name: "Туманный Резчик",
            Rarity: HeroRarity.Rare,
            Cost: 2,
            Faction: "Духи",
            Class: "Убийца",
            RuneAffinity: RuneType.Purple,
            Role: HeroRole.Assassin,
            AttackType: "melee",
            Targeting: "farthest_enemy",
            Stars: 1,
            Ability: "Прыгает к дальнему врагу и наносит крит.",
            Passive: "Тень убийцы: повышенный шанс критического удара по задней линии.",
            BaseStats: new HeroStats(
                BaseHealth: 480,
                Attack: 78,
                Armor: 2,
                MagicResist: 3,
                BaseAttackSpeed: 1.0,
                ManaMax: 55)
        );

        public static HeroDefinition RuneApprentice { get; } = new(
            Id: "rune_apprentice",
            Name: "Ученик Рун",
            Rarity: HeroRarity.Rare,
            Cost: 2,
            Faction: "Империя",
            Class: "Маг",
            RuneAffinity: RuneType.Blue,
            Role: HeroRole.Caster,
            AttackType: "ranged",
            Targeting: "two_nearest_enemies",
            Stars: 1,
            Ability: "Бьет магическим снарядом по двум целям.",
            Passive: "Рунная подпитка: синие руны дают чуть больше маны.",
            BaseStats: new HeroStats(
                BaseHealth: 500,
                Attack: 50,
                Armor: 2,
                MagicResist: 4,
                BaseAttackSpeed: 0.8,
                ManaMax: 70)
        );

        public static HeroDefinition GearSquire { get; } = new(
            Id: "gear_squire",
            Name: "Шестеренный Сквайр",
            Rarity: HeroRarity.Rare,
            Cost: 2,
            Faction: "Механисты",
            Class: "Защитник",
            RuneAffinity: RuneType.Yellow,
            Role: HeroRole.Tank,
            AttackType: "melee",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Ставит малую турель-щит.",
            Passive: "Сборка на ходу: восстанавливает немного брони со временем.",
            BaseStats: new HeroStats(
                BaseHealth: 780,
                Attack: 42,
                Armor: 9,
                MagicResist: 4,
                BaseAttackSpeed: 0.7,
                ManaMax: 80)
        );

        public static HeroDefinition SparkTinker { get; } = new(
            Id: "spark_tinker",
            Name: "Искровой Мастер",
            Rarity: HeroRarity.Rare,
            Cost: 2,
            Faction: "Механисты",
            Class: "Маг",
            RuneAffinity: RuneType.Blue,
            Role: HeroRole.Caster,
            AttackType: "ranged",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Выпускает электрическую дугу по цепочке врагов.",
            Passive: "Перегрузка: периодически усиливает следующую способность.",
            BaseStats: new HeroStats(
                BaseHealth: 490,
                Attack: 52,
                Armor: 2,
                MagicResist: 4,
                BaseAttackSpeed: 0.85,
                ManaMax: 65)
        );

        public static HeroDefinition AbyssAcolyte { get; } = new(
            Id: "abyss_acolyte",
            Name: "Послушник Бездны",
            Rarity: HeroRarity.Rare,
            Cost: 2,
            Faction: "Бездонные",
            Class: "Маг",
            RuneAffinity: RuneType.Purple,
            Role: HeroRole.Caster,
            AttackType: "ranged",
            Targeting: "highest_health_enemy",
            Stars: 1,
            Ability: "Накладывает слабость и наносит магический урон.",
            Passive: "Шепот бездны: цели под дебаффом получают больше урона.",
            BaseStats: new HeroStats(
                BaseHealth: 500,
                Attack: 54,
                Armor: 2,
                MagicResist: 5,
                BaseAttackSpeed: 0.8,
                ManaMax: 70)
        );

        public static HeroDefinition SpiritDuelist { get; } = new(
            Id: "spirit_duelist",
            Name: "Духовный Дуэлянт",
            Rarity: HeroRarity.Rare,
            Cost: 2,
            Faction: "Духи",
            Class: "Берсерк",
            RuneAffinity: RuneType.White,
            Role: HeroRole.Bruiser,
            AttackType: "melee",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Создает иллюзию себя на короткое время.",
            Passive: "Призрачный шаг: небольшой шанс уклонения от атак.",
            BaseStats: new HeroStats(
                BaseHealth: 720,
                Attack: 56,
                Armor: 5,
                MagicResist: 4,
                BaseAttackSpeed: 0.95,
                ManaMax: 60)
        );

        public static HeroDefinition DuskRanger { get; } = new(
            Id: "dusk_ranger",
            Name: "Сумеречный Егерь",
            Rarity: HeroRarity.Epic,
            Cost: 3,
            Faction: "Дикие",
            Class: "Стрелок",
            RuneAffinity: RuneType.Red,
            Role: HeroRole.Carry,
            AttackType: "ranged",
            Targeting: "current",
            Stars: 1,
            Ability: "Метит цель, повышая получаемый ею урон.",
            Passive: "Охотничий взгляд: больше урона по помеченным целям.",
            BaseStats: new HeroStats(
                BaseHealth: 560,
                Attack: 72,
                Armor: 3,
                MagicResist: 3,
                BaseAttackSpeed: 1.05,
                ManaMax: 60)
        );

        public static HeroDefinition BulwarkCaptain { get; } = new(
            Id: "bulwark_captain",
            Name: "Капитан Бастиона",
            Rarity: HeroRarity.Epic,
            Cost: 3,
            Faction: "Империя",
            Class: "Защитник",
            RuneAffinity: RuneType.Yellow,
            Role: HeroRole.Tank,
            AttackType: "melee",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Поднимает общий щит для всей передней линии.",
            Passive: "Командный строй: соседние союзники получают немного брони.",
            BaseStats: new HeroStats(
                BaseHealth: 880,
                Attack: 48,
                Armor: 10,
                MagicResist: 6,
                BaseAttackSpeed: 0.7,
                ManaMax: 90)
        );

        public static HeroDefinition VoidOracle { get; } = new(
            Id: "void_oracle",
            Name: "Оракул Пустоты",
            Rarity: HeroRarity.Epic,
            Cost: 3,
            Faction: "Бездонные",
            Class: "Целитель",
            RuneAffinity: RuneType.Green,
            Role: HeroRole.Support,
            AttackType: "ranged",
            Targeting: "lowest_health_ally",
            Stars: 1,
            Ability: "Лечит союзника и проклинает атакующего врага.",
            Passive: "Эхо бездны: исцеление накладывает слабый дебафф на врага рядом.",
            BaseStats: new HeroStats(
                BaseHealth: 600,
                Attack: 40,
                Armor: 3,
                MagicResist: 6,
                BaseAttackSpeed: 0.85,
                ManaMax: 80)
        );

        public static HeroDefinition DroneMarshal { get; } = new(
            Id: "drone_marshal",
            Name: "Маршал Дронов",
            Rarity: HeroRarity.Epic,
            Cost: 3,
            Faction: "Механисты",
            Class: "Призыватель",
            RuneAffinity: RuneType.Blue,
            Role: HeroRole.Summoner,
            AttackType: "ranged",
            Targeting: "summon_slot",
            Stars: 1,
            Ability: "Призывает двух атакующих дронов.",
            Passive: "Серийное производство: дроны живут немного дольше.",
            BaseStats: new HeroStats(
                BaseHealth: 580,
                Attack: 46,
                Armor: 4,
                MagicResist: 4,
                BaseAttackSpeed: 0.8,
                ManaMax: 85)
        );

        public static IReadOnlyList<HeroDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            IronGuard,
            OathArcher,
            FieldMedic,
            WildClaw,
            ThornShaman,
            MistCutthroat,
            RuneApprentice,
            GearSquire,
            SparkTinker,
            AbyssAcolyte,
            SpiritDuelist,
            DuskRanger,
            BulwarkCaptain,
            VoidOracle,
            DroneMarshal
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
