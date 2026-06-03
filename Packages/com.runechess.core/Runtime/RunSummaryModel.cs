using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One hero shown in the end-of-run roster: identity plus the data the summary
    /// screen needs to render and rank it. Pure data so the Unity layer only draws it.
    /// </summary>
    public sealed record RunSummaryHero(
        string HeroId,
        string Name,
        int Stars,
        int Cost,
        HeroRarity Rarity,
        string Faction,
        string Class);

    /// <summary>
    /// View-model for the end-of-run summary screen (GDD UI screens: "Итог забега").
    /// It aggregates how far the run got, whether it was won, the final team roster,
    /// the run's best hero and the accumulated rewards from <see cref="RunState"/>.
    /// Keeping it in core lets the ranking and counts be smoke-tested without Unity.
    /// </summary>
    public sealed record RunSummaryModel(
        int RoundsCleared,
        int FinalRound,
        bool IsVictory,
        int Gold,
        int PlayerLevel,
        int RunHealth,
        IReadOnlyList<RunSummaryHero> Team,
        RunSummaryHero? BestHero)
    {
        /// <summary>Headline label for the run result.</summary>
        public string ResultLabel => IsVictory ? "ЗАБЕГ ПРОЙДЕН" : "ЗАБЕГ ОКОНЧЕН";

        /// <summary>"cleared / total" progress label, e.g. "7 / 10".</summary>
        public string ProgressLabel => $"{RoundsCleared} / {FinalRound}";

        /// <summary>
        /// Build the summary from the live run. The best hero is a deterministic MVP
        /// heuristic (most stars, then highest cost, then name) until per-unit combat
        /// contribution is tracked; see tasks/.tasks.md.
        /// </summary>
        public static RunSummaryModel Build(RunState run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var isVictory = run.Phase == RunPhase.Victory
                || (run.IsFinalRound && run.Phase != RunPhase.Defeat);
            var roundsCleared = isVictory
                ? PveRunSchedule.FinalRound
                : Math.Max(0, run.Round - 1);

            var team = run.Team
                .Select(boardHero =>
                {
                    var definition = HeroCatalog.Get(boardHero.Hero.HeroId);
                    return new RunSummaryHero(
                        HeroId: boardHero.Hero.HeroId,
                        Name: definition.Name,
                        Stars: boardHero.Hero.Stars,
                        Cost: definition.Cost,
                        Rarity: definition.Rarity,
                        Faction: definition.Faction,
                        Class: definition.Class);
                })
                .ToList();

            var bestHero = team
                .OrderByDescending(hero => hero.Stars)
                .ThenByDescending(hero => hero.Cost)
                .ThenBy(hero => hero.Name, StringComparer.Ordinal)
                .FirstOrDefault();

            return new RunSummaryModel(
                RoundsCleared: roundsCleared,
                FinalRound: PveRunSchedule.FinalRound,
                IsVictory: isVictory,
                Gold: run.Gold,
                PlayerLevel: run.PlayerLevel,
                RunHealth: run.RunHealth,
                Team: team,
                BestHero: bestHero);
        }
    }
}
