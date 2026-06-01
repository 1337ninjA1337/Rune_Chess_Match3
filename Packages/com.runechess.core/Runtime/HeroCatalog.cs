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
            Passive: "Скрытый клинок: повышенный урон базовыми атаками.",
            BaseStats: new HeroStats(
                BaseHealth: 560,
                Attack: 85,
                Armor: 3,
                MagicResist: 3,
                BaseAttackSpeed: 1.1,
                ManaMax: 70)
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
            Targeting: "two_enemies",
            Stars: 1,
            Ability: "Бьет магическим снарядом по двум целям.",
            Passive: "Резонанс рун: начинает бой с частью маны.",
            BaseStats: new HeroStats(
                BaseHealth: 540,
                Attack: 55,
                Armor: 3,
                MagicResist: 6,
                BaseAttackSpeed: 0.8,
                ManaMax: 90)
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
            Passive: "Бронепластины: прочнее в передней линии.",
            BaseStats: new HeroStats(
                BaseHealth: 820,
                Attack: 48,
                Armor: 10,
                MagicResist: 5,
                BaseAttackSpeed: 0.75,
                ManaMax: 85)
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
            Ability: "Выпускает электрическую дугу.",
            Passive: "Заряд конденсатора: начинает бой с частью маны.",
            BaseStats: new HeroStats(
                BaseHealth: 540,
                Attack: 58,
                Armor: 3,
                MagicResist: 6,
                BaseAttackSpeed: 0.82,
                ManaMax: 95)
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
            Targeting: "nearest",
            Stars: 1,
            Ability: "Накладывает слабость и наносит урон.",
            Passive: "Печать бездны: начинает бой с частью маны.",
            BaseStats: new HeroStats(
                BaseHealth: 560,
                Attack: 60,
                Armor: 3,
                MagicResist: 6,
                BaseAttackSpeed: 0.8,
                ManaMax: 90)
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
            Ability: "Создает иллюзию на короткое время.",
            Passive: "Дух поединка: получает дополнительное здоровье.",
            BaseStats: new HeroStats(
                BaseHealth: 760,
                Attack: 62,
                Armor: 6,
                MagicResist: 5,
                BaseAttackSpeed: 0.92,
                ManaMax: 70)
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
            Ability: "Метит цель, повышая урон по ней.",
            Passive: "Охотничий взгляд: усиленные базовые атаки.",
            BaseStats: new HeroStats(
                BaseHealth: 650,
                Attack: 90,
                Armor: 4,
                MagicResist: 4,
                BaseAttackSpeed: 1.1,
                ManaMax: 75)
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
            Ability: "Поднимает общий щит передней линии.",
            Passive: "Знамя бастиона: прочнее в передней линии.",
            BaseStats: new HeroStats(
                BaseHealth: 950,
                Attack: 55,
                Armor: 12,
                MagicResist: 8,
                BaseAttackSpeed: 0.75,
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
            Passive: "Покров пустоты: лучше сопротивляется магическому урону.",
            BaseStats: new HeroStats(
                BaseHealth: 680,
                Attack: 50,
                Armor: 4,
                MagicResist: 8,
                BaseAttackSpeed: 0.85,
                ManaMax: 95)
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
            Passive: "Протокол развертывания: начинает бой с частью маны.",
            BaseStats: new HeroStats(
                BaseHealth: 680,
                Attack: 55,
                Armor: 5,
                MagicResist: 6,
                BaseAttackSpeed: 0.82,
                ManaMax: 95)
        );

        public static HeroDefinition PhaseAssassin { get; } = new(
            Id: "phase_assassin",
            Name: "Фазовый Убийца",
            Rarity: HeroRarity.Epic,
            Cost: 3,
            Faction: "Духи",
            Class: "Убийца",
            RuneAffinity: RuneType.White,
            Role: HeroRole.Assassin,
            AttackType: "melee",
            Targeting: "farthest_enemy",
            Stars: 1,
            Ability: "Становится неуязвимым и бьет заднюю линию.",
            Passive: "Фазовый шаг: повышенный урон базовыми атаками.",
            BaseStats: new HeroStats(
                BaseHealth: 650,
                Attack: 105,
                Armor: 4,
                MagicResist: 5,
                BaseAttackSpeed: 1.15,
                ManaMax: 80)
        );

        public static HeroDefinition MagmaBrute { get; } = new(
            Id: "magma_brute",
            Name: "Магмовый Громила",
            Rarity: HeroRarity.Epic,
            Cost: 4,
            Faction: "Дикие",
            Class: "Берсерк",
            RuneAffinity: RuneType.Red,
            Role: HeroRole.Bruiser,
            AttackType: "melee",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Наносит урон вокруг себя и получает ярость.",
            Passive: "Магмовая кожа: получает дополнительное здоровье.",
            BaseStats: new HeroStats(
                BaseHealth: 1050,
                Attack: 80,
                Armor: 9,
                MagicResist: 6,
                BaseAttackSpeed: 0.9,
                ManaMax: 80)
        );

        public static HeroDefinition CurseWeaver { get; } = new(
            Id: "curse_weaver",
            Name: "Ткач Проклятий",
            Rarity: HeroRarity.Epic,
            Cost: 4,
            Faction: "Бездонные",
            Class: "Маг",
            RuneAffinity: RuneType.Purple,
            Role: HeroRole.Caster,
            AttackType: "ranged",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Связывает врагов, разделяя получаемый урон.",
            Passive: "Нити проклятия: начинает бой с частью маны.",
            BaseStats: new HeroStats(
                BaseHealth: 720,
                Attack: 72,
                Armor: 4,
                MagicResist: 9,
                BaseAttackSpeed: 0.82,
                ManaMax: 100)
        );

        public static HeroDefinition ClockworkSaint { get; } = new(
            Id: "clockwork_saint",
            Name: "Заводной Святой",
            Rarity: HeroRarity.Epic,
            Cost: 4,
            Faction: "Механисты",
            Class: "Целитель",
            RuneAffinity: RuneType.Green,
            Role: HeroRole.Healer,
            AttackType: "ranged",
            Targeting: "lowest_health_ally",
            Stars: 1,
            Ability: "Лечит команду импульсами.",
            Passive: "Священный механизм: лучше сопротивляется магическому урону.",
            BaseStats: new HeroStats(
                BaseHealth: 780,
                Attack: 52,
                Armor: 5,
                MagicResist: 9,
                BaseAttackSpeed: 0.85,
                ManaMax: 105)
        );

        public static HeroDefinition AstralRegent { get; } = new(
            Id: "astral_regent",
            Name: "Астральный Регент",
            Rarity: HeroRarity.Legendary,
            Cost: 5,
            Faction: "Духи",
            Class: "Маг",
            RuneAffinity: RuneType.White,
            Role: HeroRole.Caster,
            AttackType: "ranged",
            Targeting: "nearest",
            Stars: 1,
            Ability: "Останавливает время боя и усиливает все руны.",
            Passive: "Астральная корона: начинает бой с частью маны.",
            BaseStats: new HeroStats(
                BaseHealth: 900,
                Attack: 95,
                Armor: 6,
                MagicResist: 10,
                BaseAttackSpeed: 0.9,
                ManaMax: 120)
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
            DroneMarshal,
            PhaseAssassin,
            MagmaBrute,
            CurseWeaver,
            ClockworkSaint,
            AstralRegent
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
