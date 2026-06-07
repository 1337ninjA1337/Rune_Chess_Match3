using System;

namespace RuneChess.Core
{
    /// <summary>
    /// A serialisable capture of <see cref="AccountProgress"/> so the meta progression a
    /// player earns "после забега" (account XP, soft currency and roster unlocks) survives
    /// between runs instead of resetting to <see cref="AccountProgress.Starting"/> every
    /// time. Mirrors <see cref="RunProgressSnapshot"/>: pure data with a version stamp so a
    /// future schema change can reject an incompatible save. Kept in core so the round-trip
    /// can be smoke-tested without Unity.
    /// </summary>
    public sealed record AccountProgressSnapshot(
        int Version,
        int AccountLevel,
        int AccountXp,
        int SoftCurrency,
        int UnlockedCommanders,
        int TotalCommanders,
        int UnlockedHeroes,
        int TotalHeroes)
    {
        public const int CurrentVersion = 1;

        public static AccountProgressSnapshot Capture(AccountProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return new AccountProgressSnapshot(
                Version: CurrentVersion,
                AccountLevel: progress.AccountLevel,
                AccountXp: progress.AccountXp,
                SoftCurrency: progress.SoftCurrency,
                UnlockedCommanders: progress.UnlockedCommanders,
                TotalCommanders: progress.TotalCommanders,
                UnlockedHeroes: progress.UnlockedHeroes,
                TotalHeroes: progress.TotalHeroes);
        }

        public AccountProgress Restore()
        {
            if (Version != CurrentVersion)
            {
                throw new InvalidOperationException("Account progress snapshot version is not supported.");
            }

            return new AccountProgress(
                AccountLevel: AccountLevel,
                AccountXp: AccountXp,
                SoftCurrency: SoftCurrency,
                UnlockedCommanders: UnlockedCommanders,
                TotalCommanders: TotalCommanders,
                UnlockedHeroes: UnlockedHeroes,
                TotalHeroes: TotalHeroes);
        }
    }

    /// <summary>
    /// Holds the persisted account progress between runs. The presentation layer applies a
    /// finished run's rewards to its <see cref="AccountProgress"/> and saves it here so the
    /// next run starts from the carried-forward account. Mirrors <see cref="RunProgressStore"/>:
    /// an in-memory store keeps the meta loop working today while leaving a single seam for a
    /// later cross-session (disk/PlayerPrefs) backend.
    /// </summary>
    public sealed class AccountProgressStore
    {
        private AccountProgressSnapshot? snapshot;

        public bool HasSavedProgress => snapshot is not null;
        public AccountProgressSnapshot? Snapshot => snapshot;

        public void Save(AccountProgress progress)
        {
            snapshot = AccountProgressSnapshot.Capture(progress);
        }

        /// <summary>Return the saved progress, or a fresh starting account when nothing is saved.</summary>
        public AccountProgress Load()
        {
            return snapshot?.Restore() ?? AccountProgress.Starting;
        }

        public bool TryLoad(out AccountProgress progress)
        {
            if (snapshot is null)
            {
                progress = AccountProgress.Starting;
                return false;
            }

            progress = snapshot.Restore();
            return true;
        }

        public void Clear()
        {
            snapshot = null;
        }
    }
}
