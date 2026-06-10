using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// A shared Live PvP lobby (GDD "Будущие режимы": "Live PvP — Матч на 4 или 8 игроков.
    /// Игроки параллельно проходят раунды и теряют здоровье после поражений."). The match is
    /// an auto-battler-style elimination lobby: 4 or 8 players each run their own board,
    /// every round is fought by all alive players at the same time, losers spend health, and
    /// the last player standing wins.
    ///
    /// This is the deterministic state machine only — a pure value with no networking. It
    /// owns the two mechanics the backlog tasks call out:
    /// <list type="bullet">
    /// <item><b>Parallel round progression</b>: <see cref="ResolveRound"/> advances every
    /// alive player by exactly one shared round at once.</item>
    /// <item><b>Health loss after defeats</b>: each loser's reported damage drains the shared
    /// health pool, eliminating players at zero and assigning final placements.</item>
    /// </list>
    /// Live transport (sockets, reconnection, lock-step synchronisation) is intentionally out
    /// of scope; the per-player fights reuse the existing combat module and report back as
    /// <see cref="LivePvpRoundOutcome"/>. See docs/live-pvp.md.
    /// </summary>
    public sealed record LivePvpMatch(
        int Round,
        LivePvpPhase Phase,
        IReadOnlyList<LivePvpParticipant> Participants,
        LivePvpConfig Config)
    {
        /// <summary>Players still in the match, in their seated order.</summary>
        public IReadOnlyList<LivePvpParticipant> AliveParticipants =>
            Participants.Where(participant => participant.IsAlive).ToList();

        /// <summary>Players who have been knocked out, in their seated order.</summary>
        public IReadOnlyList<LivePvpParticipant> EliminatedParticipants =>
            Participants.Where(participant => participant.IsEliminated).ToList();

        /// <summary>How many players are still alive.</summary>
        public int AliveCount => Participants.Count(participant => participant.IsAlive);

        /// <summary>True once the match has resolved to a winner (or a rare mutual knockout).</summary>
        public bool IsFinished => Phase == LivePvpPhase.Finished;

        /// <summary>
        /// The winner once the match is finished: the participant placed first, or null while
        /// the match is still in progress.
        /// </summary>
        public LivePvpParticipant? Winner => IsFinished
            ? Participants.FirstOrDefault(participant => participant.Placement == 1)
            : null;

        /// <summary>
        /// Final standings best-first: ranked players by placement, then any still-alive
        /// players (an unfinished match), then unranked. Deterministic for testing.
        /// </summary>
        public IReadOnlyList<LivePvpParticipant> Standings => Participants
            .OrderBy(participant => participant.HasPlacement ? participant.Placement : int.MaxValue)
            .ThenByDescending(participant => participant.Health)
            .ToList();

        /// <summary>
        /// Open a lobby for the supplied seats. The size must be exactly 4 or 8 (GDD), ids must
        /// be distinct, and every player starts on the configured health. Round 1 begins in
        /// progress.
        /// </summary>
        public static LivePvpMatch Create(
            IReadOnlyList<(string Id, string Name)> seats,
            LivePvpConfig? config = null)
        {
            if (seats is null)
            {
                throw new ArgumentNullException(nameof(seats));
            }

            if (!LivePvpConfig.IsValidLobbySize(seats.Count))
            {
                throw new ArgumentException(
                    "A Live PvP match seats exactly 4 or 8 players.", nameof(seats));
            }

            var resolvedConfig = config ?? LivePvpConfig.Default;
            var participants = new List<LivePvpParticipant>(seats.Count);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (id, name) in seats)
            {
                var participant = new LivePvpParticipant(id, name, resolvedConfig.StartingHealth);
                if (!seenIds.Add(participant.Id))
                {
                    throw new ArgumentException(
                        $"Live PvP seat id '{participant.Id}' is listed twice.", nameof(seats));
                }

                participants.Add(participant);
            }

            return new LivePvpMatch(
                Round: 1,
                Phase: LivePvpPhase.InProgress,
                Participants: participants.AsReadOnly(),
                Config: resolvedConfig);
        }

        /// <summary>
        /// Resolve one parallel round. Every alive participant must have exactly one outcome
        /// (no extras, no duplicates, none missing); eliminated players sit the round out.
        /// Each loser spends the reported health, players hitting zero are eliminated and
        /// placed just above the survivors, the shared round counter advances by one, and the
        /// match finishes once one or zero players remain.
        /// </summary>
        public LivePvpMatch ResolveRound(IReadOnlyList<LivePvpRoundOutcome> outcomes)
        {
            if (Phase == LivePvpPhase.Finished)
            {
                throw new InvalidOperationException("A finished Live PvP match cannot resolve another round.");
            }

            if (outcomes is null)
            {
                throw new ArgumentNullException(nameof(outcomes));
            }

            var byId = new Dictionary<string, LivePvpRoundOutcome>(StringComparer.Ordinal);
            foreach (var outcome in outcomes)
            {
                if (outcome is null)
                {
                    throw new ArgumentException("A Live PvP round outcome cannot be null.", nameof(outcomes));
                }

                if (!byId.TryAdd(outcome.ParticipantId, outcome))
                {
                    throw new ArgumentException(
                        $"Live PvP round has two outcomes for participant '{outcome.ParticipantId}'.",
                        nameof(outcomes));
                }
            }

            var aliveIds = new HashSet<string>(
                Participants.Where(participant => participant.IsAlive).Select(participant => participant.Id),
                StringComparer.Ordinal);

            if (byId.Count != aliveIds.Count || !byId.Keys.All(aliveIds.Contains))
            {
                throw new ArgumentException(
                    "A Live PvP round needs exactly one outcome for each alive participant.",
                    nameof(outcomes));
            }

            // Apply damage in parallel: every alive player resolves this round at once.
            var updated = Participants
                .Select(participant => participant.IsAlive
                    ? participant.TakeDamage(byId[participant.Id].Damage)
                    : participant)
                .ToList();

            var aliveAfter = updated.Count(participant => participant.IsAlive);
            var finished = aliveAfter <= 1;

            // Newly knocked-out players finish just above whoever is still alive; if everyone
            // remaining died together (aliveAfter == 0) they tie as co-winners at first place.
            var eliminatedPlacement = aliveAfter + 1;
            var placed = updated
                .Select(participant =>
                {
                    if (participant.HasPlacement)
                    {
                        return participant;
                    }

                    if (participant.IsEliminated)
                    {
                        return participant.WithPlacement(eliminatedPlacement);
                    }

                    if (finished && participant.IsAlive)
                    {
                        return participant.WithPlacement(1);
                    }

                    return participant;
                })
                .ToList();

            return this with
            {
                Round = Round + 1,
                Phase = finished ? LivePvpPhase.Finished : LivePvpPhase.InProgress,
                Participants = placed.AsReadOnly(),
            };
        }
    }
}
