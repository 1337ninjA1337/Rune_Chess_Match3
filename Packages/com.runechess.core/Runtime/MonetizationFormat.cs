using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Data model for one acceptable monetization format (GDD "Монетизация → Подходящие
    /// форматы"). It carries identity, the <see cref="MonetizationFormatKind"/>, a short
    /// Russian description, and — when the format reskins one of the existing in-game cosmetic
    /// surfaces — the <see cref="CosmeticKind"/> it maps onto (otherwise null for formats that
    /// live outside the run cosmetic loadout, e.g. a battle pass or an emote).
    ///
    /// A monetization format is cosmetic-only by construction: the record exposes no stat,
    /// gold, or combat field, so offering one can never grant a pay-to-win advantage. Pure
    /// data so the catalog can be smoke-tested without Unity. See docs/monetization.md.
    /// </summary>
    public sealed record MonetizationFormat(
        string Id,
        string Name,
        MonetizationFormatKind Kind,
        string Description,
        CosmeticKind? AppliesTo)
    {
        public string Id { get; init; } = string.IsNullOrWhiteSpace(Id)
            ? throw new ArgumentException("Monetization format id cannot be blank.", nameof(Id))
            : Id.Trim();

        public string Name { get; init; } = string.IsNullOrWhiteSpace(Name)
            ? throw new ArgumentException("Monetization format name cannot be blank.", nameof(Name))
            : Name.Trim();

        public string Description { get; init; } = string.IsNullOrWhiteSpace(Description)
            ? throw new ArgumentException("Monetization format description cannot be blank.", nameof(Description))
            : Description.Trim();

        /// <summary>
        /// True when this format reskins an existing in-run cosmetic surface
        /// (<see cref="CosmeticKind"/>) rather than living outside the run loadout.
        /// </summary>
        public bool ReskinsRunSurface => AppliesTo is not null;
    }
}
