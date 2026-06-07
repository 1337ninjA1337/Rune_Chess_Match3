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
    /// Meta rewards earned by the finished run, as shown on the summary screen
    /// ("Итог забега": полученный опыт и валюта, разблокировки). It carries the account
    /// XP and soft currency gained, how many account levels that pushed the player
    /// through, and the resulting unlock notices.
    /// </summary>
    public sealed record RunRewardSummary(
        int AccountXpGained,
        int SoftCurrencyGained,
        int AccountLevelsGained,
        IReadOnlyList<string> Unlocks)
    {
        /// <summary>True when the run produced any unlock notice.</summary>
        public bool HasUnlocks => Unlocks.Count > 0;
    }

    /// <summary>
    /// View-model for the end-of-run summary screen (GDD UI screens: "Итог забега").
    /// It aggregates how far the run got, whether it was won, the final team roster,
    /// the run's best hero and the accumulated rewards from <see cref="RunState"/>.
    /// When built with an <see cref="AccountProgress"/> it also previews the meta
    /// rewards and unlocks the run earns. Keeping it in core lets the ranking and
    /// counts be smoke-tested without Unity.
    /// </summary>
    public sealed record RunSummaryModel(
        int RoundsCleared,
        int FinalRound,
        bool IsVictory,
        int Gold,
        int PlayerLevel,
        int RunHealth,
        IReadOnlyList<RunSummaryHero> Team,
        RunSummaryHero? BestHero,
        RunRewardSummary? Rewards = null)
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

        /// <summary>
        /// Build the summary and preview the meta rewards the run earns against the
        /// player's current <paramref name="account"/>: account XP, soft currency,
        /// account levels gained and the resulting unlock notices ("разблокировки").
        /// </summary>
        public static RunSummaryModel Build(RunState run, AccountProgress account)
        {
            if (account is null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var summary = Build(run);
            var gains = AccountProgress.CalculateRunRewards(summary);
            var after = account.WithGains(gains.AccountXp, gains.SoftCurrency);

            var unlocks = new List<string>();
            for (var level = account.AccountLevel + 1; level <= after.AccountLevel; level += 1)
            {
                unlocks.Add($"Уровень аккаунта {level}");
            }

            var newCommanders = after.UnlockedCommanders - account.UnlockedCommanders;
            if (newCommanders > 0)
            {
                unlocks.Add($"Новых командиров: {newCommanders}");
            }

            var newStartingArtifacts = after.UnlockedStartingArtifacts - account.UnlockedStartingArtifacts;
            if (newStartingArtifacts > 0)
            {
                unlocks.Add($"Новых стартовых артефактов: {newStartingArtifacts}");
            }

            var newCosmetics = after.UnlockedCosmetics - account.UnlockedCosmetics;
            if (newCosmetics > 0)
            {
                unlocks.Add($"Новой косметики: {newCosmetics}");
            }

            var newHeroes = after.UnlockedHeroes - account.UnlockedHeroes;
            if (newHeroes > 0)
            {
                unlocks.Add($"Новых героев: {newHeroes}");
            }

            var rewards = new RunRewardSummary(
                AccountXpGained: gains.AccountXp,
                SoftCurrencyGained: gains.SoftCurrency,
                AccountLevelsGained: after.AccountLevel - account.AccountLevel,
                Unlocks: unlocks);

            return summary with { Rewards = rewards };
        }
    }
}
