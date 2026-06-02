using System;

namespace RuneChess.Core
{
    public sealed record CommanderState(
        string Id,
        string Name,
        double Energy,
        double MaxEnergy,
        int Match4CombosTowardPassive = 0
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

        public int Match4CombosTowardPassive { get; init; } = Match4CombosTowardPassive >= 0
            ? Match4CombosTowardPassive
            : throw new ArgumentOutOfRangeException(nameof(Match4CombosTowardPassive), "Commander passive progress cannot be negative.");

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

        public CommanderState AddMatch4Combos(int count, int triggerEvery, out int triggerCount)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Match-4 combo count cannot be negative.");
            }

            if (triggerEvery <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(triggerEvery), "Commander passive trigger threshold must be positive.");
            }

            var total = Match4CombosTowardPassive + count;
            triggerCount = total / triggerEvery;
            return this with { Match4CombosTowardPassive = total % triggerEvery };
        }
    }
}
