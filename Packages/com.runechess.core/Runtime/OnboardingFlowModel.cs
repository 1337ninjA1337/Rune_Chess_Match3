using System;

namespace RuneChess.Core
{
    /// <summary>
    /// View-model that drives the interactive first-run tutorial (GDD "Обучение должно быть
    /// интерактивным, а не длинным текстовым объяснением"). Given the run's current round and the
    /// player's <see cref="OnboardingProgress"/>, it decides whether a tutorial prompt is on screen,
    /// what action the player must perform to clear it, and how far the tutorial has advanced. The
    /// prompt blocks on the player doing the gated action, not on tapping through text, so the Unity
    /// layer only has to render <see cref="ActionPrompt"/> and report the action via
    /// <see cref="OnboardingProgress.CompleteRound"/>. Deterministic and smoke-testable without Unity.
    /// </summary>
    public sealed record OnboardingFlowModel(
        int Round,
        OnboardingStep? Step,
        bool IsActive,
        OnboardingGate? PendingGate,
        string? Title,
        string? ActionPrompt,
        int CompletedSteps,
        int TotalSteps,
        bool IsTutorialComplete)
    {
        /// <summary>
        /// True while the tutorial is waiting for the player to perform the gated action. Same as
        /// <see cref="IsActive"/>; named for the presentation, which gates input on it rather than
        /// on a "tap to continue" button.
        /// </summary>
        public bool RequiresPlayerAction => IsActive;

        /// <summary>True when the current round is a tutorial round (whether or not it is cleared).</summary>
        public bool IsTutorialRound => Step is not null;

        /// <summary>Build the interactive flow state for a run's current round.</summary>
        public static OnboardingFlowModel Build(RunState run, OnboardingProgress progress)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return Build(run.Round, progress);
        }

        /// <summary>Build the interactive flow state for a round and the player's progress.</summary>
        public static OnboardingFlowModel Build(int round, OnboardingProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var step = OnboardingScript.ForRound(round);
            var total = OnboardingScript.Steps.Count;

            if (step is null)
            {
                return new OnboardingFlowModel(
                    Round: round,
                    Step: null,
                    IsActive: false,
                    PendingGate: null,
                    Title: null,
                    ActionPrompt: null,
                    CompletedSteps: progress.CompletedCount,
                    TotalSteps: total,
                    IsTutorialComplete: progress.IsTutorialComplete);
            }

            var cleared = progress.IsCompleted(step.Gate);

            return new OnboardingFlowModel(
                Round: round,
                Step: step,
                IsActive: !cleared,
                PendingGate: cleared ? null : step.Gate,
                Title: step.Title,
                ActionPrompt: step.Hint,
                CompletedSteps: progress.CompletedCount,
                TotalSteps: total,
                IsTutorialComplete: progress.IsTutorialComplete);
        }
    }
}
