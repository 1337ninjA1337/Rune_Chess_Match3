using System;

namespace RuneChess.Core
{
    public sealed record CommanderState(
        string Id,
        string Name,
        double Energy,
        double MaxEnergy
    )
    {
        public string Id { get; init; } = string.IsNullOrWhiteSpace(Id)
            ? throw new ArgumentException("Commander id is required.", nameof(Id))
            : Id;

        public string Name { get; init; } = string.IsNullOrWhiteSpace(Name)
            ? throw new ArgumentException("Commander name is required.", nameof(Name))
            : Name;

        public double Energy { get; init; } = Energy >= 0.0 && Energy <= MaxEnergy
            ? Energy
            : throw new ArgumentOutOfRangeException(nameof(Energy), "Commander energy must stay within the energy bar.");

        public double MaxEnergy { get; init; } = MaxEnergy > 0.0
            ? MaxEnergy
            : throw new ArgumentOutOfRangeException(nameof(MaxEnergy), "Commander energy bar must be positive.");

        public double EnergyFillRatio => Math.Clamp(Energy / MaxEnergy, 0.0, 1.0);
        public bool IsEnergyFull => Energy >= MaxEnergy;

        public CommanderState GainEnergy(double amount)
        {
            if (amount < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Commander energy gain cannot be negative.");
            }

            return this with { Energy = Math.Min(MaxEnergy, Energy + amount) };
        }

        public CommanderState SpendEnergy(double amount)
        {
            if (amount < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Commander energy spend cannot be negative.");
            }

            if (amount > Energy)
            {
                throw new InvalidOperationException("Commander does not have enough energy.");
            }

            return this with { Energy = Energy - amount };
        }
    }
}
