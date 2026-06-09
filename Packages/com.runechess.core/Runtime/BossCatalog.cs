using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// A single MVP boss encounter (GDD "Контент MVP": "3 босса"). A boss is the named, set-piece
    /// fight on one of the run's three escalating boss-tier rounds — the elite mini-boss, the boss,
    /// and the final boss. Each definition gives the encounter an identity (name and epithet), the
    /// round it appears on, its tier, the GDD design goal it must exercise, the signature mechanic
    /// that makes it a boss rather than a regular fight, and the lead unit that anchors its
    /// composition. Pure data, anchored to <see cref="PveRunSchedule"/> so the boss identity and the
    /// round it fights on can never drift apart; the Unity layer renders the boss banner and the
    /// signature-mechanic telegraph from this contract.
    /// </summary>
    public sealed record BossDefinition(
        string Id,
        string Name,
        string Title,
        int Round,
        PveRoundType Tier,
        string DesignGoal,
        string SignatureMechanic,
        string LeadUnitHeroId)
    {
        /// <summary>True for the run-ending final boss (the only encounter that wins the run).</summary>
        public bool IsFinalBoss => Tier == PveRoundType.FinalBoss;

        /// <summary>True for the early elite mini-boss that opens the boss-tier difficulty.</summary>
        public bool IsElite => Tier == PveRoundType.Elite;

        /// <summary>The PvE round this boss is fought on, from the run schedule.</summary>
        public PveRoundDefinition RoundDefinition => PveRunSchedule.GetRound(Round);
    }

    /// <summary>
    /// The MVP boss roster (GDD "Контент MVP": "3 босса"). The three bosses are the run's set-piece
    /// encounters — the <see cref="PveRoundType.Elite"/> mini-boss (round 5), the
    /// <see cref="PveRoundType.Boss"/> (round 8) and the <see cref="PveRoundType.FinalBoss"/>
    /// (round 10) — each designed around the GDD design goal its round must teach: shields/healing,
    /// single-target focus damage, and a full composition/synergy/match-3 check. The catalog is the
    /// single source of truth the presentation uses to frame these fights as bosses; the actual
    /// enemy units stay on <see cref="PveRunSchedule"/> so balance and identity live in one place.
    /// Designed and validated statically against the schedule and <see cref="HeroCatalog"/>; the live
    /// "does the boss read as a boss" playtest remains the documented gap (no Unity runtime here).
    /// </summary>
    public static class BossCatalog
    {
        /// <summary>The three MVP bosses, in the order the run faces them.</summary>
        public static IReadOnlyList<BossDefinition> All { get; } = Array.AsReadOnly(new[]
        {
            new BossDefinition(
                Id: "stone_guardian",
                Name: "Каменный страж",
                Title: "Несокрушимая стена",
                Round: 5,
                Tier: PveRoundType.Elite,
                DesignGoal: PveRunSchedule.GetRound(5).DesignGoal,
                SignatureMechanic: "Стена брони: высокое здоровье и щиты держат передний строй, " +
                    "поэтому без устойчивого урона, пробития защиты или лечения бой не выиграть.",
                LeadUnitHeroId: "bulwark_captain"),
            new BossDefinition(
                Id: "mechanical_colossus",
                Name: "Механический Колосс",
                Title: "Одна огромная цель",
                Round: 8,
                Tier: PveRoundType.Boss,
                DesignGoal: PveRunSchedule.GetRound(8).DesignGoal,
                SignatureMechanic: "Одна крупная цель: почти весь запас здоровья сосредоточен в одном " +
                    "живучем юните, поэтому решает фокус-урон и своевременный заряд способностей.",
                LeadUnitHeroId: "magma_brute"),
            new BossDefinition(
                Id: "council_three_factions",
                Name: "Совет Трех Фракций",
                Title: "Финальное испытание состава",
                Round: 10,
                Tier: PveRoundType.FinalBoss,
                DesignGoal: PveRunSchedule.GetRound(10).DesignGoal,
                SignatureMechanic: "Смешанный совет: широкий состав с физическим и магическим уроном и " +
                    "встречными синергиями, поэтому решают ширина состава, позиционирование и навык match-3.",
                LeadUnitHeroId: "astral_regent")
        });

        private static readonly IReadOnlyDictionary<string, BossDefinition> ById =
            All.ToDictionary(boss => boss.Id, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<int, BossDefinition> ByRound =
            All.ToDictionary(boss => boss.Round);

        /// <summary>The round numbers that field a boss, in run order.</summary>
        public static IReadOnlyList<int> BossRounds { get; } =
            Array.AsReadOnly(All.Select(boss => boss.Round).ToArray());

        /// <summary>The PvE round types the MVP treats as boss-tier encounters.</summary>
        public static IReadOnlyList<PveRoundType> BossTiers { get; } = Array.AsReadOnly(new[]
        {
            PveRoundType.Elite,
            PveRoundType.Boss,
            PveRoundType.FinalBoss
        });

        /// <summary>The boss fought on a round, or <c>null</c> when the round is not a boss fight.</summary>
        public static BossDefinition? ForRound(int round) =>
            ByRound.TryGetValue(round, out var boss) ? boss : null;

        /// <summary>True when the given round is one of the run's boss encounters.</summary>
        public static bool IsBossRound(int round) => ByRound.ContainsKey(round);

        /// <summary>Look up a boss by id (case-insensitive).</summary>
        public static BossDefinition Get(string id)
        {
            if (TryGet(id, out var boss))
            {
                return boss;
            }

            throw new ArgumentException($"Unknown boss id '{id}'.", nameof(id));
        }

        /// <summary>Try to look up a boss by id (case-insensitive).</summary>
        public static bool TryGet(string? id, out BossDefinition boss)
        {
            if (id is not null && ById.TryGetValue(id, out var found))
            {
                boss = found;
                return true;
            }

            boss = null!;
            return false;
        }

        /// <summary>The final boss — the encounter that ends the run on victory.</summary>
        public static BossDefinition FinalBoss => All.Single(boss => boss.IsFinalBoss);
    }
}
