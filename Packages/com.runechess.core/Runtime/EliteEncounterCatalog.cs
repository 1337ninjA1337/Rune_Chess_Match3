using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// An additional elite encounter for the run (GDD/codex P2 "Добавить дополнительные элитные
    /// бои"). An elite is a harder-than-regular set-piece with a distinct twist that pressures one
    /// part of the player's build. A definition is pure data: a name, the twist it tests, and a
    /// roster of <see cref="PveEnemyUnit"/>s on the enemy half. Elites are tuned to the canonical
    /// elite's total stars so any of them can stand in as a round-5-tier fight without re-tuning the
    /// difficulty curve. The run layer may swap an elite in for variety; the canonical
    /// <see cref="PveRunSchedule"/> elite stays the default.
    /// </summary>
    public sealed record EliteEncounter(
        string Id,
        string Name,
        string EliteTwist,
        IReadOnlyList<PveEnemyUnit> Composition)
    {
        /// <summary>Total enemy stars in the encounter — matched to the canonical elite for parity.</summary>
        public int StarTotal => Composition.Sum(unit => unit.Stars);
    }

    /// <summary>
    /// Extra elite encounters beyond the canonical round-5 Stone Guardian (GDD/codex P2 "Добавить
    /// дополнительные элитные бои"). Each one keeps the elite difficulty (same total stars as the
    /// canonical elite round) but presents a different twist — sustaining bruisers, a magic burst
    /// from the back line, a pure armour wall — so an elite slot can rotate instead of always being
    /// the same fight. Pure data, anchored to <see cref="HeroCatalog"/>; the canonical elite stays in
    /// <see cref="PveRunSchedule"/> as the default, and the run layer can insert any of these as an
    /// alternate. Statically verified against the schedule and hero catalog; the live "does the elite
    /// feel like a step up" playtest is the documented gap (no Unity runtime here).
    /// </summary>
    public static class EliteEncounterCatalog
    {
        // Enemy half of the MVP 6x4 field, mirroring PveRunSchedule's convention.
        private const int EnemyFrontRow = TacticalField.MvpRows / 2 - 1; // row 1
        private const int EnemyBackRow = 0;

        private static TacticalPosition Front(int column) => new(EnemyFrontRow, column);
        private static TacticalPosition Back(int column) => new(EnemyBackRow, column);

        private static IReadOnlyList<PveEnemyUnit> Roster(params PveEnemyUnit[] units) => units;

        /// <summary>The round whose elite difficulty the pool is tuned to (the canonical elite round).</summary>
        public const int ReferenceEliteRound = 5;

        /// <summary>The star budget every elite encounter is tuned to — the canonical elite's total stars.</summary>
        public static int EliteStarBudget => PveRunSchedule.GetRound(ReferenceEliteRound).EnemyStarTotal;

        /// <summary>The additional elite encounters.</summary>
        public static IReadOnlyList<EliteEncounter> All { get; } = Array.AsReadOnly(new[]
        {
            new EliteEncounter("elite_blood_reaver", "Кровавый Жнец",
                "Устойчивый строй с лечением: бьёт без передышки и подлечивается, " +
                "поэтому проверяет бёрст-урон против само-лечения и анти-хил.",
                Roster(
                    new PveEnemyUnit("wild_claw", 2, Front(2)),
                    new PveEnemyUnit("spirit_duelist", 2, Front(3)),
                    new PveEnemyUnit("field_medic", 1, Back(2)))),

            new EliteEncounter("elite_void_choir", "Хор Пустоты",
                "Магический бёрст из тыла за одним танком: " +
                "проверяет магическую защиту и быстрый размен по задней линии.",
                Roster(
                    new PveEnemyUnit("iron_guard", 1, Front(2)),
                    new PveEnemyUnit("abyss_acolyte", 2, Back(1)),
                    new PveEnemyUnit("void_oracle", 2, Back(2)))),

            new EliteEncounter("elite_iron_phalanx", "Железная Фаланга",
                "Тройная стена брони без тыла: " +
                "проверяет пробитие защиты, магический урон и терпение против высокой брони.",
                Roster(
                    new PveEnemyUnit("bulwark_captain", 2, Front(2)),
                    new PveEnemyUnit("iron_guard", 2, Front(3)),
                    new PveEnemyUnit("gear_squire", 1, Front(1))))
        });

        private static readonly IReadOnlyDictionary<string, EliteEncounter> ById =
            All.ToDictionary(elite => elite.Id, StringComparer.OrdinalIgnoreCase);

        /// <summary>Look up an elite encounter by id (case-insensitive).</summary>
        public static EliteEncounter Get(string id)
        {
            if (TryGet(id, out var elite))
            {
                return elite;
            }

            throw new ArgumentException($"Unknown elite encounter id '{id}'.", nameof(id));
        }

        /// <summary>Try to look up an elite encounter by id (case-insensitive).</summary>
        public static bool TryGet(string? id, out EliteEncounter elite)
        {
            if (id is not null && ById.TryGetValue(id, out var found))
            {
                elite = found;
                return true;
            }

            elite = null!;
            return false;
        }
    }
}
