using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// The raw account XP and soft currency a finished run earns, before they are
    /// applied to an account. Pure data so the end-of-run summary can preview the
    /// reward and the account model can apply it from the same numbers.
    /// </summary>
    public sealed record RunRewardGains(int AccountXp, int SoftCurrency);

    /// <summary>
    /// Account-level meta progression shown on the main screen (GDD "Метапрогрессия"
    /// and UI screen 1 "прогресс аккаунта"). It tracks the persistent account level,
    /// experience toward the next level, the soft currency earned across runs, and how
    /// much of the commander and hero rosters the account has unlocked.
    ///
    /// This is pure data so the meta-progression maths can be smoke-tested without
    /// Unity. Commanders unlock by account level through <see cref="CommanderUnlockSchedule"/>
    /// (GDD "Метапрогрессия": новых командиров): a fresh account has only the catalog
    /// default available and earns the rest by levelling up across runs. Heroes are in-run
    /// shop content rather than a metaprogression reward, so the whole hero roster stays
    /// unlocked. The unlock gating is non-pay-to-win: it is earned by playing and grants
    /// access only, never combat power.
    /// </summary>
    public sealed record AccountProgress(
        int AccountLevel,
        int AccountXp,
        int SoftCurrency,
        int UnlockedCommanders,
        int TotalCommanders,
        int UnlockedHeroes,
        int TotalHeroes)
    {
        public int AccountLevel { get; init; } = AccountLevel >= 1
            ? AccountLevel
            : throw new ArgumentOutOfRangeException(nameof(AccountLevel), "Account level starts at one.");

        public int AccountXp { get; init; } = AccountXp >= 0
            ? AccountXp
            : throw new ArgumentOutOfRangeException(nameof(AccountXp), "Account XP cannot be negative.");

        public int SoftCurrency { get; init; } = SoftCurrency >= 0
            ? SoftCurrency
            : throw new ArgumentOutOfRangeException(nameof(SoftCurrency), "Soft currency cannot be negative.");

        /// <summary>XP needed to advance from <paramref name="level"/> to the next level.</summary>
        public static int XpForNextLevel(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Account level starts at one.");
            }

            return 100 * level;
        }

        /// <summary>XP still required to reach the next account level.</summary>
        public int XpToNextLevel => XpForNextLevel(AccountLevel) - AccountXp;

        /// <summary>Fill ratio (0..1) of the current level's XP bar.</summary>
        public double LevelProgressRatio
        {
            get
            {
                var needed = XpForNextLevel(AccountLevel);
                return needed <= 0 ? 0.0 : Math.Clamp((double)AccountXp / needed, 0.0, 1.0);
            }
        }

        /// <summary>"unlocked / total" label for the commander roster.</summary>
        public string CommanderUnlockLabel => $"{UnlockedCommanders} / {TotalCommanders}";

        /// <summary>"unlocked / total" label for the hero roster.</summary>
        public string HeroUnlockLabel => $"{UnlockedHeroes} / {TotalHeroes}";

        /// <summary>
        /// The commander ids the account has unlocked at its current level, in catalog order
        /// (GDD "Метапрогрессия": новых командиров).
        /// </summary>
        public IReadOnlyList<string> UnlockedCommanderIds =>
            CommanderUnlockSchedule.UnlockedIdsForLevel(AccountLevel);

        /// <summary>True when the account has unlocked the given commander at its current level.</summary>
        public bool IsCommanderUnlocked(string commanderId) =>
            CommanderUnlockSchedule.IsUnlocked(commanderId, AccountLevel);

        /// <summary>
        /// The next commander unlock above the current account level, or <c>null</c> when
        /// every commander is already unlocked.
        /// </summary>
        public CommanderUnlock? NextCommanderUnlock => CommanderUnlockSchedule.NextUnlock(AccountLevel);

        /// <summary>
        /// How many starting artifacts the account has unlocked at its current level (GDD
        /// "Метапрогрессия": новые стартовые артефакты). Derived from the account level via
        /// <see cref="StartingArtifactUnlockSchedule"/> so it never needs to be persisted.
        /// </summary>
        public int UnlockedStartingArtifacts =>
            StartingArtifactUnlockSchedule.UnlockedCountForLevel(AccountLevel);

        /// <summary>The whole starting-artifact pool size, for the "unlocked / total" label.</summary>
        public int TotalStartingArtifacts => StartingArtifactUnlockSchedule.TotalCount;

        /// <summary>"unlocked / total" label for the starting-artifact roster.</summary>
        public string StartingArtifactUnlockLabel => $"{UnlockedStartingArtifacts} / {TotalStartingArtifacts}";

        /// <summary>
        /// The starting-artifact ids the account has unlocked at its current level, in unlock
        /// order (GDD "Метапрогрессия": новые стартовые артефакты).
        /// </summary>
        public IReadOnlyList<string> UnlockedStartingArtifactIds =>
            StartingArtifactUnlockSchedule.UnlockedIdsForLevel(AccountLevel);

        /// <summary>True when the account has unlocked the given starting artifact at its current level.</summary>
        public bool IsStartingArtifactUnlocked(string artifactId) =>
            StartingArtifactUnlockSchedule.IsUnlocked(artifactId, AccountLevel);

        /// <summary>
        /// The next starting-artifact unlock above the current account level, or <c>null</c>
        /// when every starting artifact is already unlocked.
        /// </summary>
        public StartingArtifactUnlock? NextStartingArtifactUnlock =>
            StartingArtifactUnlockSchedule.NextUnlock(AccountLevel);

        /// <summary>
        /// How many cosmetics the account has unlocked at its current level (GDD
        /// "Метапрогрессия": косметику, визуальные эффекты рун). Derived from the account
        /// level via <see cref="CosmeticUnlockSchedule"/> so it never needs to be persisted.
        /// </summary>
        public int UnlockedCosmetics => CosmeticUnlockSchedule.UnlockedCountForLevel(AccountLevel);

        /// <summary>The whole cosmetic pool size, for the "unlocked / total" label.</summary>
        public int TotalCosmetics => CosmeticUnlockSchedule.TotalCount;

        /// <summary>"unlocked / total" label for the cosmetic roster.</summary>
        public string CosmeticUnlockLabel => $"{UnlockedCosmetics} / {TotalCosmetics}";

        /// <summary>
        /// The cosmetic ids the account has unlocked at its current level, in unlock order
        /// (GDD "Метапрогрессия": косметику, визуальные эффекты рун).
        /// </summary>
        public IReadOnlyList<string> UnlockedCosmeticIds =>
            CosmeticUnlockSchedule.UnlockedIdsForLevel(AccountLevel);

        /// <summary>True when the account has unlocked the given cosmetic at its current level.</summary>
        public bool IsCosmeticUnlocked(string cosmeticId) =>
            CosmeticUnlockSchedule.IsUnlocked(cosmeticId, AccountLevel);

        /// <summary>
        /// The next cosmetic unlock above the current account level, or <c>null</c> when every
        /// cosmetic is already unlocked.
        /// </summary>
        public CosmeticUnlock? NextCosmeticUnlock => CosmeticUnlockSchedule.NextUnlock(AccountLevel);

        /// <summary>
        /// A fresh account: level one, no XP or currency, the full hero roster available, and
        /// only the commanders unlocked at account level one (the catalog default) per
        /// <see cref="CommanderUnlockSchedule"/>. The rest of the commander roster unlocks as
        /// the account levels up across runs.
        /// </summary>
        public static AccountProgress Starting { get; } = new(
            AccountLevel: 1,
            AccountXp: 0,
            SoftCurrency: 0,
            UnlockedCommanders: CommanderUnlockSchedule.UnlockedCountForLevel(1),
            TotalCommanders: CommanderCatalog.All.Count,
            UnlockedHeroes: HeroCatalog.All.Count,
            TotalHeroes: HeroCatalog.All.Count);

        /// <summary>
        /// Apply the meta rewards earned from a finished run (GDD: after a run the
        /// player gains account XP and soft currency). Rewards scale with how far the
        /// run got, with a bonus for clearing the whole run. Excess XP rolls into
        /// further account levels.
        /// </summary>
        public AccountProgress WithRunRewards(RunSummaryModel summary)
        {
            var gains = CalculateRunRewards(summary);
            return WithGains(gains.AccountXp, gains.SoftCurrency);
        }

        /// <summary>
        /// Compute the meta rewards a finished run earns, without applying them. Rewards
        /// scale with how far the run got, with a bonus for clearing the whole run. The
        /// end-of-run summary screen uses this to preview "полученный опыт и валюта".
        /// </summary>
        public static RunRewardGains CalculateRunRewards(RunSummaryModel summary)
        {
            if (summary is null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            var xpGained = 50 + (summary.RoundsCleared * 25) + (summary.IsVictory ? 100 : 0);
            var currencyGained = 10 + (summary.RoundsCleared * 5) + (summary.IsVictory ? 50 : 0);
            return new RunRewardGains(xpGained, currencyGained);
        }

        /// <summary>Add raw XP and currency, levelling up while the XP bar overflows.</summary>
        public AccountProgress WithGains(int xpGained, int currencyGained)
        {
            if (xpGained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(xpGained), "Cannot grant negative XP.");
            }

            if (currencyGained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currencyGained), "Cannot grant negative currency.");
            }

            var level = AccountLevel;
            var xp = AccountXp + xpGained;
            while (xp >= XpForNextLevel(level))
            {
                xp -= XpForNextLevel(level);
                level += 1;
            }

            return this with
            {
                AccountLevel = level,
                AccountXp = xp,
                SoftCurrency = SoftCurrency + currencyGained,
                UnlockedCommanders = CommanderUnlockSchedule.UnlockedCountForLevel(level)
            };
        }
    }
}
