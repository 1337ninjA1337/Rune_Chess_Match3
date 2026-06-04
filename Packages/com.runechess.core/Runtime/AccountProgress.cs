using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Account-level meta progression shown on the main screen (GDD "Метапрогрессия"
    /// and UI screen 1 "прогресс аккаунта"). It tracks the persistent account level,
    /// experience toward the next level, the soft currency earned across runs, and how
    /// much of the commander and hero rosters the account has unlocked.
    ///
    /// This is pure data so the meta-progression maths can be smoke-tested without
    /// Unity. The MVP starts every account with the full commander and hero rosters
    /// unlocked; locking content behind account level is a later monetisation-free
    /// progression task tracked in tasks/.tasks.md.
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
        /// A fresh account: level one, no XP or currency, and the full MVP rosters
        /// available so the player can use every commander and hero from the start.
        /// </summary>
        public static AccountProgress Starting { get; } = new(
            AccountLevel: 1,
            AccountXp: 0,
            SoftCurrency: 0,
            UnlockedCommanders: CommanderCatalog.All.Count,
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
            if (summary is null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            var xpGained = 50 + (summary.RoundsCleared * 25) + (summary.IsVictory ? 100 : 0);
            var currencyGained = 10 + (summary.RoundsCleared * 5) + (summary.IsVictory ? 50 : 0);
            return WithGains(xpGained, currencyGained);
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
                SoftCurrency = SoftCurrency + currencyGained
            };
        }
    }
}
