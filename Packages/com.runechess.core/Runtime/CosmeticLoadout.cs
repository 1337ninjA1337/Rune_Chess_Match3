using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The cosmetics the player currently has applied — at most one per
    /// <see cref="CosmeticKind"/> (one board skin, one rune effect, one hero skin). This is the
    /// equipped selection the main-screen cosmetics shop (GDD "магазин косметики") edits. It is
    /// account-independent player choice, kept separate from <see cref="AccountProgress"/> so the
    /// persistence layer can store the loadout on its own. Equipping is purely visual and gated
    /// only by what the account has unlocked, so the loadout can never carry combat power. Pure
    /// data so the selection logic can be smoke-tested without Unity.
    /// </summary>
    public sealed record CosmeticLoadout(IReadOnlyList<string> EquippedIds)
    {
        public IReadOnlyList<string> EquippedIds { get; init; } = Normalize(EquippedIds);

        /// <summary>
        /// The starting loadout: the always-available default board skin (the first cosmetic,
        /// unlocked at account level one) applied, with the other kinds left unequipped until the
        /// account unlocks a cosmetic for them.
        /// </summary>
        public static CosmeticLoadout Default { get; } =
            new(new[] { CosmeticUnlockSchedule.Entries[0].CosmeticId });

        /// <summary>The equipped cosmetic id for a kind, or <c>null</c> when nothing is applied for it.</summary>
        public string? EquippedFor(CosmeticKind kind) => EquippedIds
            .Select(id => CosmeticCatalog.Get(id))
            .Where(cosmetic => cosmetic.Kind == kind)
            .Select(cosmetic => cosmetic.Id)
            .FirstOrDefault();

        /// <summary>True when the given cosmetic is the one currently applied for its kind.</summary>
        public bool IsEquipped(string cosmeticId) => cosmeticId is not null
            && EquippedIds.Any(id => string.Equals(id, cosmeticId.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Apply a cosmetic for its kind, replacing whatever was equipped for that kind. The
        /// cosmetic must exist and be unlocked on the given account — locked cosmetics cannot be
        /// equipped, so the shop can only surface earned looks.
        /// </summary>
        public CosmeticLoadout Equip(string cosmeticId, AccountProgress account)
        {
            if (account is null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var cosmetic = CosmeticCatalog.Get(cosmeticId);
            if (!account.IsCosmeticUnlocked(cosmetic.Id))
            {
                throw new InvalidOperationException(
                    $"Cosmetic '{cosmetic.Id}' is not unlocked on this account yet.");
            }

            var kept = EquippedIds
                .Where(id => CosmeticCatalog.Get(id).Kind != cosmetic.Kind)
                .Append(cosmetic.Id);
            return this with { EquippedIds = kept.ToList() };
        }

        /// <summary>
        /// Validate and de-duplicate the equipped ids: every id must resolve to a catalog cosmetic
        /// and no two ids may target the same kind (at most one applied look per surface).
        /// </summary>
        private static IReadOnlyList<string> Normalize(IReadOnlyList<string> equippedIds)
        {
            if (equippedIds is null)
            {
                throw new ArgumentNullException(nameof(equippedIds));
            }

            var resolved = equippedIds.Select(CosmeticCatalog.Get).ToList();
            if (resolved.Select(cosmetic => cosmetic.Kind).Distinct().Count() != resolved.Count)
            {
                throw new ArgumentException("A loadout cannot equip two cosmetics of the same kind.", nameof(equippedIds));
            }

            return Array.AsReadOnly(resolved.Select(cosmetic => cosmetic.Id).ToArray());
        }
    }
}
