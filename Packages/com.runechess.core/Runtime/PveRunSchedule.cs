using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    public static class PveRunSchedule
    {
        public const int FirstRound = 1;
        public const int FinalRound = 10;

        public static IReadOnlyList<PveRoundDefinition> Rounds { get; } = new List<PveRoundDefinition>
        {
            new(1, "round_01_training_band", 1337, 2),
            new(2, "round_02_scouts", 1438, 2),
            new(3, "round_03_shield_line", 1539, 3),
            new(4, "round_04_rune_poachers", 1640, 3),
            new(5, "round_05_elite_guard", 1741, 4),
            new(6, "round_06_backline_raiders", 1842, 4),
            new(7, "round_07_hex_mages", 1943, 5),
            new(8, "round_08_twin_captains", 2044, 5),
            new(9, "round_09_boss_vanguard", 2145, 6),
            new(10, "round_10_final_warlord", 2246, 8)
        };

        public static PveRoundDefinition GetRound(int round)
        {
            if (round is < FirstRound or > FinalRound)
            {
                throw new ArgumentOutOfRangeException(nameof(round), "MVP PvE run has exactly 10 rounds.");
            }

            return Rounds[round - 1];
        }
    }
}
