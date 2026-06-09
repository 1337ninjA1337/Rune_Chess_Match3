using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One row in the main-screen cosmetics shop: a cosmetic with the unlock and equipped state
    /// the card renders. Pure data so the shop projection can be smoke-tested without Unity.
    /// </summary>
    public sealed record CosmeticShopEntry(
        string Id,
        string Name,
        CosmeticKind Kind,
        string Description,
        int RequiredAccountLevel,
        bool IsUnlocked,
        bool IsEquipped)
    {
        /// <summary>Player-facing status for the cosmetic card.</summary>
        public string StatusLabel => IsEquipped
            ? "Применено"
            : IsUnlocked
                ? "Открыто"
                : $"Откроется на уровне {RequiredAccountLevel}";

        /// <summary>True when the player can apply this cosmetic right now (unlocked and not already on).</summary>
        public bool CanEquip => IsUnlocked && !IsEquipped;
    }

    /// <summary>
    /// View-model for the cosmetics shop reached from the main screen (GDD UI screen 1
    /// "Главный экран"; monetization "магазин косметики"). It projects the account's cosmetic
    /// unlocks and the player's equipped <see cref="CosmeticLoadout"/> into a browsable, grouped
    /// list: each cosmetic shows whether it is unlocked, the level that unlocks it, and whether it
    /// is currently applied. The shop only equips cosmetics the account has already earned, so it
    /// is non-pay-to-win by construction — soft currency is surfaced for display only and buys no
    /// power. Keeping it in core lets the labels, grouping and counts be smoke-tested without
    /// Unity; the presentation layer just draws the rows and reads these fields.
    /// </summary>
    public sealed record CosmeticShopModel(
        IReadOnlyList<CosmeticShopEntry> Entries,
        int UnlockedCount,
        int TotalCount,
        CosmeticUnlock? NextUnlock,
        int SoftCurrency)
    {
        /// <summary>Headline shown above the cosmetics shop.</summary>
        public const string ShopHeadline = "КОСМЕТИКА";

        /// <summary>Headline label shown above the cosmetics shop.</summary>
        public string Headline => ShopHeadline;

        /// <summary>"unlocked / total" label for the cosmetic roster.</summary>
        public string UnlockLabel => $"{UnlockedCount} / {TotalCount}";

        /// <summary>The cosmetic kinds the shop has rows for, in display order.</summary>
        public IReadOnlyList<CosmeticKind> Kinds => Entries
            .Select(entry => entry.Kind)
            .Distinct()
            .ToList();

        /// <summary>The rows for a given cosmetic kind (one shop section), in display order.</summary>
        public IReadOnlyList<CosmeticShopEntry> ForKind(CosmeticKind kind) => Entries
            .Where(entry => entry.Kind == kind)
            .ToList();

        /// <summary>The cosmetic currently applied for a kind, or <c>null</c> when nothing is equipped for it.</summary>
        public CosmeticShopEntry? EquippedFor(CosmeticKind kind) => Entries
            .FirstOrDefault(entry => entry.Kind == kind && entry.IsEquipped);

        /// <summary>
        /// Build the cosmetics-shop model from the account's unlocks and the player's equipped
        /// loadout. Rows are ordered by kind (catalog enum order) then by the account level that
        /// unlocks them, so the same shop lists the same way every time.
        /// </summary>
        public static CosmeticShopModel Build(AccountProgress account, CosmeticLoadout loadout)
        {
            if (account is null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (loadout is null)
            {
                throw new ArgumentNullException(nameof(loadout));
            }

            var entries = CosmeticCatalog.All
                .Select(cosmetic => new CosmeticShopEntry(
                    Id: cosmetic.Id,
                    Name: cosmetic.Name,
                    Kind: cosmetic.Kind,
                    Description: cosmetic.Description,
                    RequiredAccountLevel: CosmeticUnlockSchedule.RequiredLevel(cosmetic.Id),
                    IsUnlocked: account.IsCosmeticUnlocked(cosmetic.Id),
                    IsEquipped: loadout.IsEquipped(cosmetic.Id)))
                .OrderBy(entry => (int)entry.Kind)
                .ThenBy(entry => entry.RequiredAccountLevel)
                .ToList();

            return new CosmeticShopModel(
                Entries: Array.AsReadOnly(entries.ToArray()),
                UnlockedCount: account.UnlockedCosmetics,
                TotalCount: account.TotalCosmetics,
                NextUnlock: account.NextCosmeticUnlock,
                SoftCurrency: account.SoftCurrency);
        }
    }
}
