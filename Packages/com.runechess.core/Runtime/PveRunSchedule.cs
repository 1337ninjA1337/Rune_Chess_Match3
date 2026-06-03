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

        public static IReadOnlyList<PveRoundDefinition> Rounds { get; } = new List<PveRoundDefinition>
        {
            new(1, "round_01_training_dummies", 1337, 3, PreventsRunDefeat: true,
                Type: PveRoundType.Tutorial,
                EnemyName: "2 тренировочных манекена",
                DesignGoal: "Научить покупке и выставлению героя",
                Reward: new PveRoundReward(GrantsStarterHero: true),
                // Two passive 1-star tanks act as the training dummies.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("iron_guard", 1, new TacticalPosition(1, 2)),
                    new("iron_guard", 1, new TacticalPosition(1, 3))
                }),
            new(2, "round_02_rogue_band", 1438, 4,
                Type: PveRoundType.Combat,
                EnemyName: "Малый отряд разбойников",
                DesignGoal: "Научить match-3 красных и синих рун",
                // A small rogue band: two melee brawlers backed by an archer.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("wild_claw", 1, new TacticalPosition(1, 2)),
                    new("wild_claw", 1, new TacticalPosition(1, 3)),
                    new("oath_archer", 1, new TacticalPosition(0, 2))
                }),
            new(3, "round_03_frontline_archer", 1539, 4,
                Type: PveRoundType.Combat,
                EnemyName: "Передняя линия и стрелок",
                DesignGoal: "Показать важность танка и позиционирования",
                Reward: new PveRoundReward(HeroChoice: true),
                // A protected frontline plus a backline archer to punish bad positioning.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("iron_guard", 1, new TacticalPosition(1, 2)),
                    new("gear_squire", 1, new TacticalPosition(1, 3)),
                    new("oath_archer", 1, new TacticalPosition(0, 3))
                }),
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
                // An armored guardian with a supporting tank and healer to test sustain.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("bulwark_captain", 2, new TacticalPosition(1, 2)),
                    new("gear_squire", 1, new TacticalPosition(1, 3)),
                    new("field_medic", 1, new TacticalPosition(0, 3))
                }),
            new(6, "round_06_spirit_raiders", 1842, 5,
                Type: PveRoundType.Combat,
                EnemyName: "Духи-налетчики",
                DesignGoal: "Показать угрозу задней линии",
                // Spirit assassins that threaten the player's backline.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("mist_cutthroat", 1, new TacticalPosition(1, 2)),
                    new("phase_assassin", 1, new TacticalPosition(1, 3)),
                    new("spirit_duelist", 1, new TacticalPosition(0, 3))
                }),
            new(7, "round_07_abyss_mages", 1943, 5,
                Type: PveRoundType.Combat,
                EnemyName: "Маги Бездны",
                DesignGoal: "Научить работать против магического урона",
                Reward: new PveRoundReward(HeroChoice: true),
                // Backline abyssal casters behind a single front protector.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("gear_squire", 1, new TacticalPosition(1, 2)),
                    new("abyss_acolyte", 1, new TacticalPosition(0, 2)),
                    new("curse_weaver", 1, new TacticalPosition(0, 3))
                }),
            new(8, "round_08_mechanical_colossus", 2044, 7,
                Type: PveRoundType.Boss,
                EnemyName: "Механический Колосс",
                DesignGoal: "Проверить урон по одной крупной цели",
                Reward: new PveRoundReward(RareArtifact: true),
                // A single huge 3-star bruiser as the colossus, plus a small support drone.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("magma_brute", 3, new TacticalPosition(1, 2)),
                    new("drone_marshal", 1, new TacticalPosition(0, 3))
                }),
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
                // A mixed-faction council that tests the player's whole build at the finale.
                EnemyRoster: new List<PveEnemyUnit>
                {
                    new("bulwark_captain", 2, new TacticalPosition(1, 2)),
                    new("magma_brute", 2, new TacticalPosition(1, 3)),
                    new("phase_assassin", 1, new TacticalPosition(1, 4)),
                    new("abyss_acolyte", 2, new TacticalPosition(0, 1)),
                    new("clockwork_saint", 2, new TacticalPosition(0, 3)),
                    new("astral_regent", 1, new TacticalPosition(0, 4))
                })
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
