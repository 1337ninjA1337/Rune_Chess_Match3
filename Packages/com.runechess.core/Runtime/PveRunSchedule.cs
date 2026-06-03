using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// The fixed 10-round MVP PvE run. Enemy identities, types, design goals and
    /// rewards mirror the GDD "Первые 10 раундов" and "Темп сложности" tables, which
    /// are the source of truth for the run structure.
    /// </summary>
    public static class PveRunSchedule
    {
        public const int FirstRound = 1;
        public const int FinalRound = 10;

        // Enemy half of the MVP 6x4 field: front row faces the player, back row sits behind.
        private const int EnemyFrontRow = TacticalField.MvpRows / 2 - 1; // row 1
        private const int EnemyBackRow = 0;

        private static TacticalPosition Front(int column) => new(EnemyFrontRow, column);
        private static TacticalPosition Back(int column) => new(EnemyBackRow, column);

        private static IReadOnlyList<PveEnemyUnit> Roster(params PveEnemyUnit[] units) => units;

        public static IReadOnlyList<PveRoundDefinition> Rounds { get; } = new List<PveRoundDefinition>
        {
            new(1, "round_01_training_dummies", 1337, 3, PreventsRunDefeat: true,
                Type: PveRoundType.Tutorial,
                EnemyName: "2 тренировочных манекена",
                DesignGoal: "Научить покупке и выставлению героя",
                Reward: new PveRoundReward(GrantsStarterHero: true),
                EnemyComposition: Roster(
                    new PveEnemyUnit("iron_guard", 1, Front(2)),
                    new PveEnemyUnit("iron_guard", 1, Front(3)))),
            new(2, "round_02_rogue_band", 1438, 4,
                Type: PveRoundType.Combat,
                EnemyName: "Малый отряд разбойников",
                DesignGoal: "Научить match-3 красных и синих рун",
                EnemyComposition: Roster(
                    new PveEnemyUnit("wild_claw", 1, Front(2)),
                    new PveEnemyUnit("wild_claw", 1, Front(3)),
                    new PveEnemyUnit("oath_archer", 1, Back(2)))),
            new(3, "round_03_frontline_archer", 1539, 4,
                Type: PveRoundType.Combat,
                EnemyName: "Передняя линия и стрелок",
                DesignGoal: "Показать важность танка и позиционирования",
                Reward: new PveRoundReward(HeroChoice: true),
                EnemyComposition: Roster(
                    new PveEnemyUnit("iron_guard", 1, Front(2)),
                    new PveEnemyUnit("gear_squire", 1, Front(3)),
                    new PveEnemyUnit("oath_archer", 1, Back(2)),
                    new PveEnemyUnit("dusk_ranger", 1, Back(3)))),
            new(4, "round_04_relic_merchant", 1640, 4,
                Type: PveRoundType.Event,
                EnemyName: "Торговец реликвиями",
                DesignGoal: "Дать первый выбор риска и награды",
                HasCombat: false,
                Reward: new PveRoundReward(ArtifactOrGold: true)),
            new(5, "round_05_stone_guardian", 1741, 5,
                Type: PveRoundType.Elite,
                EnemyName: "Каменный страж",
                DesignGoal: "Проверить щиты и лечение",
                Reward: new PveRoundReward(Artifact: true),
                EnemyComposition: Roster(
                    new PveEnemyUnit("bulwark_captain", 2, Front(2)),
                    new PveEnemyUnit("gear_squire", 2, Front(3)),
                    new PveEnemyUnit("field_medic", 1, Back(2)))),
            new(6, "round_06_spirit_raiders", 1842, 5,
                Type: PveRoundType.Combat,
                EnemyName: "Духи-налетчики",
                DesignGoal: "Показать угрозу задней линии",
                EnemyComposition: Roster(
                    new PveEnemyUnit("spirit_duelist", 2, Front(1)),
                    new PveEnemyUnit("mist_cutthroat", 2, Front(2)),
                    new PveEnemyUnit("phase_assassin", 2, Front(3)),
                    new PveEnemyUnit("oath_archer", 1, Back(2)))),
            new(7, "round_07_abyss_mages", 1943, 5,
                Type: PveRoundType.Combat,
                EnemyName: "Маги Бездны",
                DesignGoal: "Научить работать против магического урона",
                Reward: new PveRoundReward(HeroChoice: true),
                EnemyComposition: Roster(
                    new PveEnemyUnit("gear_squire", 2, Front(2)),
                    new PveEnemyUnit("void_oracle", 2, Back(1)),
                    new PveEnemyUnit("abyss_acolyte", 2, Back(2)),
                    new PveEnemyUnit("curse_weaver", 2, Back(3)))),
            new(8, "round_08_mechanical_colossus", 2044, 7,
                Type: PveRoundType.Boss,
                EnemyName: "Механический Колосс",
                DesignGoal: "Проверить урон по одной крупной цели",
                Reward: new PveRoundReward(RareArtifact: true),
                EnemyComposition: Roster(
                    new PveEnemyUnit("magma_brute", 3, Front(2)),
                    new PveEnemyUnit("drone_marshal", 2, Back(2)))),
            new(9, "round_09_enhanced_shop", 2145, 4,
                Type: PveRoundType.EnhancedShop,
                EnemyName: "Усиленный магазин (без боя)",
                DesignGoal: "Дать перестроить состав перед финалом",
                HasCombat: false,
                Reward: new PveRoundReward(FreeReroll: true)),
            new(10, "round_10_council_three_factions", 2246, 0,
                Type: PveRoundType.FinalBoss,
                EnemyName: "Совет Трех Фракций",
                DesignGoal: "Проверить состав, синергии и match-3 навыки",
                Reward: new PveRoundReward(RunVictory: true),
                EnemyComposition: Roster(
                    new PveEnemyUnit("bulwark_captain", 2, Front(1)),
                    new PveEnemyUnit("magma_brute", 2, Front(2)),
                    new PveEnemyUnit("phase_assassin", 2, Front(3)),
                    new PveEnemyUnit("clockwork_saint", 2, Back(1)),
                    new PveEnemyUnit("astral_regent", 2, Back(2)),
                    new PveEnemyUnit("abyss_acolyte", 2, Back(3))))
        };

        public static PveRoundDefinition GetRound(int round)
        {
            if (round is < FirstRound or > FinalRound)
            {
                throw new ArgumentOutOfRangeException(nameof(round), "MVP PvE run has exactly 10 rounds.");
            }

            return Rounds[round - 1];
        }

        /// <summary>
        /// Difficulty pacing tier for a round, per the GDD "Темп сложности" section.
        /// </summary>
        public static PveDifficultyTier GetDifficultyTier(int round)
        {
            return GetRound(round).DifficultyTier;
        }
    }
}
