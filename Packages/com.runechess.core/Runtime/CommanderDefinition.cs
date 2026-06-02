using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Static design data for a commander - the player's avatar from the GDD
    /// "Командиры" section. Each commander has a unique passive, its own energy bar,
    /// and a small concrete starting bonus for the MVP run setup. The runtime energy
    /// lives in <see cref="CommanderState"/>; this record holds the immutable definition
    /// used for commander selection and display.
    /// </summary>
    public sealed record CommanderDefinition(
        string Id,
        string Name,
        string Passive,
        int MaxEnergy,
        CommanderStartingBonus StartingBonus,
        IReadOnlyList<string> RecommendedStyles
    )
    {
        public string Id { get; init; } = string.IsNullOrWhiteSpace(Id)
            ? throw new ArgumentException("Commander id is required.", nameof(Id))
            : Id;

        public string Name { get; init; } = string.IsNullOrWhiteSpace(Name)
            ? throw new ArgumentException("Commander name is required.", nameof(Name))
            : Name;

        public int MaxEnergy { get; init; } = MaxEnergy > 0
            ? MaxEnergy
            : throw new ArgumentOutOfRangeException(nameof(MaxEnergy), "Commander energy bar must be positive.");

        public CommanderStartingBonus StartingBonus { get; init; } = StartingBonus
            ?? throw new ArgumentNullException(nameof(StartingBonus));

        public IReadOnlyList<string> RecommendedStyles { get; init; } = RecommendedStyles ?? Array.Empty<string>();

        /// <summary>Builds the runtime energy state for this commander at the start of a run (empty bar).</summary>
        public CommanderState CreateInitialState()
        {
            return new CommanderState(Id, Name, Energy: 0, MaxEnergy: MaxEnergy);
        }
    }
}
