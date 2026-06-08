using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The set of onboarding gates the player has already performed (GDD "Обучение должно быть
    /// интерактивным"). It is the persisted, immutable record of how far the interactive tutorial
    /// has advanced: a step is only cleared once its <see cref="OnboardingGate"/> has been done,
    /// so the tutorial progresses by playing the mechanic rather than by reading text. Pure data,
    /// serializable by gate id, and driven by <see cref="OnboardingFlowModel"/>.
    /// </summary>
    public sealed record OnboardingProgress
    {
        private readonly HashSet<OnboardingGate> _completed;

        private OnboardingProgress(HashSet<OnboardingGate> completed)
        {
            _completed = completed;
        }

        /// <summary>No gates performed yet — the tutorial has not started.</summary>
        public static OnboardingProgress Empty { get; } = new(new HashSet<OnboardingGate>());

        /// <summary>The completed gates, in reveal order.</summary>
        public IReadOnlyList<OnboardingGate> CompletedGates =>
            OnboardingScript.AllGates.Where(_completed.Contains).ToList();

        /// <summary>Stable string ids of the completed gates (for persistence), in reveal order.</summary>
        public IReadOnlyList<string> CompletedGateIds =>
            CompletedGates.Select(OnboardingGates.GetId).ToList();

        /// <summary>How many tutorial steps the player has cleared.</summary>
        public int CompletedCount => _completed.Count(OnboardingScript.AllGates.Contains);

        /// <summary>True once every tutorial gate has been performed.</summary>
        public bool IsTutorialComplete => OnboardingScript.AllGates.All(_completed.Contains);

        /// <summary>True when the given gate has already been performed.</summary>
        public bool IsCompleted(OnboardingGate gate) => _completed.Contains(gate);

        /// <summary>
        /// Record that the player performed a gate. Idempotent: completing an already-cleared gate
        /// returns the same progress. Returns a new value; the original is unchanged.
        /// </summary>
        public OnboardingProgress Complete(OnboardingGate gate)
        {
            if (_completed.Contains(gate))
            {
                return this;
            }

            var next = new HashSet<OnboardingGate>(_completed) { gate };
            return new OnboardingProgress(next);
        }

        /// <summary>
        /// Record that the player performed the action the given round teaches. Only tutorial
        /// rounds carry a gate; advancing a non-tutorial round throws so callers cannot complete a
        /// step that does not exist.
        /// </summary>
        public OnboardingProgress CompleteRound(int round)
        {
            var gate = OnboardingScript.GateForRound(round)
                ?? throw new InvalidOperationException($"Round {round} has no onboarding gate to complete.");
            return Complete(gate);
        }

        /// <summary>Rebuild progress from persisted gate ids, ignoring unknown ids defensively.</summary>
        public static OnboardingProgress FromGateIds(IEnumerable<string> gateIds)
        {
            if (gateIds is null)
            {
                throw new ArgumentNullException(nameof(gateIds));
            }

            var set = new HashSet<OnboardingGate>();
            foreach (var id in gateIds)
            {
                if (OnboardingGates.TryParseId(id, out var gate))
                {
                    set.Add(gate);
                }
            }

            return new OnboardingProgress(set);
        }

        public bool Equals(OnboardingProgress? other)
        {
            return other is not null && _completed.SetEquals(other._completed);
        }

        public override int GetHashCode()
        {
            var hash = 0;
            foreach (var gate in _completed)
            {
                hash ^= gate.GetHashCode();
            }

            return hash;
        }
    }
}
