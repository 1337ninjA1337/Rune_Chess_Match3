using System;

namespace RuneChess.Core
{
    /// <summary>
    /// The result of one player's fight in a parallel Live PvP round (GDD "Будущие режимы":
    /// "Игроки параллельно проходят раунды и теряют здоровье после поражений"). Every alive
    /// participant fights its own battle at the same time; the combat module decides who won
    /// and, for a loser, how much health the defeat costs (e.g. scaled by the surviving enemy
    /// units). The lobby state machine stays agnostic of those combat numbers and just applies
    /// the reported <see cref="Damage"/>.
    ///
    /// Invariant: a winner takes no health damage, so <see cref="Damage"/> must be zero when
    /// <see cref="Won"/> is true. A defeat costs at least one health, so losers carry a
    /// positive <see cref="Damage"/>.
    /// </summary>
    public sealed record LivePvpRoundOutcome(string ParticipantId, bool Won, int Damage)
    {
        public string ParticipantId { get; init; } = string.IsNullOrWhiteSpace(ParticipantId)
            ? throw new ArgumentException("Live PvP outcome participant id cannot be blank.", nameof(ParticipantId))
            : ParticipantId.Trim();

        public int Damage { get; init; } = ValidateDamage(Won, Damage);

        private static int ValidateDamage(bool won, int damage)
        {
            if (won)
            {
                return damage == 0
                    ? 0
                    : throw new ArgumentOutOfRangeException(
                        nameof(damage), "A Live PvP winner cannot take health damage.");
            }

            return damage > 0
                ? damage
                : throw new ArgumentOutOfRangeException(
                    nameof(damage), "A Live PvP defeat must cost at least one health.");
        }

        /// <summary>A win: the player keeps all of their health this round.</summary>
        public static LivePvpRoundOutcome Win(string participantId) =>
            new(participantId, Won: true, Damage: 0);

        /// <summary>A defeat: the player loses <paramref name="damage"/> health this round.</summary>
        public static LivePvpRoundOutcome Loss(string participantId, int damage) =>
            new(participantId, Won: false, Damage: damage);
    }
}
