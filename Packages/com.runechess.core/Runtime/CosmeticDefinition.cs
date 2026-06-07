using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Data model for a metaprogression cosmetic reward (GDD "Метапрогрессия": косметику,
    /// визуальные эффекты рун). It carries identity, the visual surface it reskins
    /// (<see cref="CosmeticKind"/>) and a short Russian description. A cosmetic is purely
    /// visual by construction: it has no stat, gold or combat field, so unlocking one can
    /// never grant a pay-to-win advantage. Pure data so the catalog can be smoke-tested
    /// without Unity.
    /// </summary>
    public sealed record CosmeticDefinition(
        string Id,
        string Name,
        CosmeticKind Kind,
        string Description)
    {
        public string Id { get; init; } = string.IsNullOrWhiteSpace(Id)
            ? throw new ArgumentException("Cosmetic id cannot be blank.", nameof(Id))
            : Id;

        public string Name { get; init; } = string.IsNullOrWhiteSpace(Name)
            ? throw new ArgumentException("Cosmetic name cannot be blank.", nameof(Name))
            : Name;

        public string Description { get; init; } = string.IsNullOrWhiteSpace(Description)
            ? throw new ArgumentException("Cosmetic description cannot be blank.", nameof(Description))
            : Description;

        /// <summary>True for a rune visual-effect cosmetic (GDD "визуальные эффекты рун").</summary>
        public bool IsRuneEffect => Kind == CosmeticKind.RuneEffect;
    }
}
