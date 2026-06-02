using System;

namespace RuneChess.Core
{
    public sealed record CommanderStartingBonus(
        string Description,
        CommanderStartingBonusKind Kind,
        int Amount = 0,
        string? HeroId = null
    )
    {
        public string Description { get; init; } = string.IsNullOrWhiteSpace(Description)
            ? throw new ArgumentException("Commander starting bonus description is required.", nameof(Description))
            : Description;

        public int Amount { get; init; } = Kind is CommanderStartingBonusKind.CommanderEnergy or CommanderStartingBonusKind.Gold
            ? Amount > 0
                ? Amount
                : throw new ArgumentOutOfRangeException(nameof(Amount), "Amount-based commander starting bonuses must be positive.")
            : Amount;

        public string? HeroId { get; init; } = Kind == CommanderStartingBonusKind.BenchHero
            ? !string.IsNullOrWhiteSpace(HeroId)
                ? HeroId
                : throw new ArgumentException("Bench-hero commander starting bonuses require a hero id.", nameof(HeroId))
            : HeroId;
    }
}
