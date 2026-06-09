using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// An alternate enemy composition for a regular combat round, giving the run replay variety
    /// without changing the round's teaching goal or its difficulty. A variant is pure data: a name,
    /// the combat round it can stand in for, and a roster of <see cref="PveEnemyUnit"/>s placed on
    /// the enemy half of the field. Variants are designed to match the canonical round's total enemy
    /// stars so swapping one in keeps the fight as hard as the GDD-tuned default, only with different
    /// faces. The Unity/run layer can pick a variant for a fresh run; the canonical
    /// <see cref="PveRunSchedule"/> stays the authoritative default.
    /// </summary>
    public sealed record EnemyVariant(
        string Id,
        int Round,
        string Name,
        IReadOnlyList<PveEnemyUnit> Composition)
    {
        /// <summary>Total enemy stars in the variant — matched to the canonical round for parity.</summary>
        public int StarTotal => Composition.Sum(unit => unit.Stars);
    }

    /// <summary>
    /// Extra enemy compositions for the run's regular combat rounds (GDD/codex P2 "Добавить
    /// дополнительные варианты врагов для обычных боев"). For each <see cref="PveRoundType.Combat"/>
    /// round (2, 3, 6, 7) it offers an alternate roster that preserves the round's design goal —
    /// teaching red/blue runes, tank positioning, the backline threat, magic damage — while changing
    /// the specific units, so repeat runs feel fresh without re-tuning difficulty (each variant
    /// matches the canonical round's total stars). Pure data, anchored to <see cref="HeroCatalog"/>
    /// and <see cref="PveRunSchedule"/>; the run layer chooses a variant, and the canonical schedule
    /// remains the default. Statically verified against the schedule; the live "does the variant feel
    /// different yet fair" playtest is the documented gap (no Unity runtime here).
    /// </summary>
    public static class EnemyVariantCatalog
    {
        // Enemy half of the MVP 6x4 field, mirroring PveRunSchedule's convention.
        private const int EnemyFrontRow = TacticalField.MvpRows / 2 - 1; // row 1
        private const int EnemyBackRow = 0;

        private static TacticalPosition Front(int column) => new(EnemyFrontRow, column);
        private static TacticalPosition Back(int column) => new(EnemyBackRow, column);

        private static IReadOnlyList<PveEnemyUnit> Roster(params PveEnemyUnit[] units) => units;

        /// <summary>The alternate combat-round compositions, in round order.</summary>
        public static IReadOnlyList<EnemyVariant> All { get; } = Array.AsReadOnly(new[]
        {
            // Round 2 (teach red/blue runes): a light ranged band instead of the rogue bruisers.
            new EnemyVariant("round_02_ranger_scouts", 2, "Следопыты-разведчики",
                Roster(
                    new PveEnemyUnit("dusk_ranger", 1, Front(2)),
                    new PveEnemyUnit("wild_claw", 1, Front(3)),
                    new PveEnemyUnit("spark_tinker", 1, Back(2)))),

            // Round 3 (tank and positioning): a heavier wall plus a back-line caster and archer.
            new EnemyVariant("round_03_shield_wall", 3, "Стена щитов и стрелок",
                Roster(
                    new PveEnemyUnit("bulwark_captain", 1, Front(2)),
                    new PveEnemyUnit("iron_guard", 1, Front(3)),
                    new PveEnemyUnit("oath_archer", 1, Back(2)),
                    new PveEnemyUnit("rune_apprentice", 1, Back(3)))),

            // Round 6 (backline threat): an assassin-led dive that still rushes the back row.
            new EnemyVariant("round_06_shadow_ambush", 6, "Теневая засада",
                Roster(
                    new PveEnemyUnit("phase_assassin", 2, Front(1)),
                    new PveEnemyUnit("mist_cutthroat", 2, Front(2)),
                    new PveEnemyUnit("spirit_duelist", 2, Front(3)),
                    new PveEnemyUnit("dusk_ranger", 1, Back(2)))),

            // Round 7 (magic damage): a curse-heavy back line shielded by a single tank.
            new EnemyVariant("round_07_curse_cabal", 7, "Ковен проклятий",
                Roster(
                    new PveEnemyUnit("iron_guard", 2, Front(2)),
                    new PveEnemyUnit("abyss_acolyte", 2, Back(1)),
                    new PveEnemyUnit("curse_weaver", 2, Back(2)),
                    new PveEnemyUnit("spark_tinker", 2, Back(3))))
        });

        private static readonly IReadOnlyDictionary<string, EnemyVariant> ById =
            All.ToDictionary(variant => variant.Id, StringComparer.OrdinalIgnoreCase);

        private static readonly ILookup<int, EnemyVariant> ByRound =
            All.ToLookup(variant => variant.Round);

        /// <summary>The combat rounds that have at least one alternate composition, in order.</summary>
        public static IReadOnlyList<int> VariantRounds { get; } =
            Array.AsReadOnly(All.Select(variant => variant.Round).Distinct().OrderBy(round => round).ToArray());

        /// <summary>The alternate compositions available for a round (empty when none exist).</summary>
        public static IReadOnlyList<EnemyVariant> ForRound(int round) => ByRound[round].ToList();

        /// <summary>True when the round has at least one alternate composition.</summary>
        public static bool HasVariants(int round) => ByRound.Contains(round);

        /// <summary>Look up a variant by id (case-insensitive).</summary>
        public static EnemyVariant Get(string id)
        {
            if (TryGet(id, out var variant))
            {
                return variant;
            }

            throw new ArgumentException($"Unknown enemy variant id '{id}'.", nameof(id));
        }

        /// <summary>Try to look up a variant by id (case-insensitive).</summary>
        public static bool TryGet(string? id, out EnemyVariant variant)
        {
            if (id is not null && ById.TryGetValue(id, out var found))
            {
                variant = found;
                return true;
            }

            variant = null!;
            return false;
        }
    }
}
