using System;

namespace RuneChess.Core
{
    /// <summary>
    /// One seat in a <see cref="LivePvpMatch"/> (GDD "Будущие режимы": "Live PvP"). A
    /// participant is a player in the shared lobby tracked only by stable identity, the
    /// shared health pool that drains on defeats, and a final <see cref="Placement"/>.
    ///
    /// Like the rest of the core, this is a pure value with no Unity or networking
    /// dependency: a participant carries no combat board of its own, because each player's
    /// fight is resolved by the existing PvE combat module and reported back to the lobby
    /// as a <see cref="LivePvpRoundOutcome"/>. See docs/live-pvp.md.
    /// </summary>
    public sealed record LivePvpParticipant(string Id, string Name, int Health, int Placement = 0)
    {
        public string Id { get; init; } = string.IsNullOrWhiteSpace(Id)
            ? throw new ArgumentException("Live PvP participant id cannot be blank.", nameof(Id))
            : Id.Trim();

        public string Name { get; init; } = string.IsNullOrWhiteSpace(Name)
            ? throw new ArgumentException("Live PvP participant name cannot be blank.", nameof(Name))
            : Name.Trim();

        public int Health { get; init; } = Health >= 0
            ? Health
            : throw new ArgumentOutOfRangeException(nameof(Health), "Live PvP health cannot be negative.");

        public int Placement { get; init; } = Placement >= 0
            ? Placement
            : throw new ArgumentOutOfRangeException(
                nameof(Placement), "Live PvP placement cannot be negative.");

        /// <summary>True while this player still has health and stays in the match.</summary>
        public bool IsAlive => Health > 0;

        /// <summary>True once this player's health has been spent and they are knocked out.</summary>
        public bool IsEliminated => Health <= 0;

        /// <summary>True once a final standing (1 = winner) has been assigned.</summary>
        public bool HasPlacement => Placement > 0;

        /// <summary>Spend health after a defeat, never dropping below zero.</summary>
        public LivePvpParticipant TakeDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Live PvP damage cannot be negative.");
            }

            return this with { Health = Math.Max(0, Health - amount) };
        }

        /// <summary>Record this player's final standing in the match.</summary>
        public LivePvpParticipant WithPlacement(int placement) => this with { Placement = placement };
    }
}
