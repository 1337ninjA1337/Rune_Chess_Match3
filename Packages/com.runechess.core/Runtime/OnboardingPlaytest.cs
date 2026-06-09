using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The flow state a scripted first-run tester observes on one tutorial round as they walk the
    /// onboarding: the round, the mechanic it reveals, whether a prompt is on screen, whether that
    /// prompt requires an interactive action (rather than tapping through text), how many mechanics
    /// have been revealed cumulatively by now, and the running completed-step count. One step of the
    /// reproducible onboarding playtest.
    /// </summary>
    public sealed record OnboardingPlaytestStep(
        int Round,
        OnboardingMechanic Mechanic,
        string Title,
        string Prompt,
        bool RequiresInteractiveAction,
        OnboardingGate Gate,
        int RevealedMechanicsSoFar,
        int CompletedStepsAfter);

    /// <summary>
    /// A deterministic, scripted stand-in for the "быстрые игровые тесты на сложность onboarding"
    /// playtest task (codex/GDD: "Обучение должно быть интерактивным, а не длинным текстовым
    /// объяснением"; "Первые раунды должны постепенно открывать механику"). It walks a fresh player
    /// through tutorial rounds 1-7, performing each gated action in turn via the real
    /// <see cref="OnboardingFlowModel"/>/<see cref="OnboardingProgress"/>, and asserts the difficulty
    /// curve is fair: exactly one new mechanic opens per round (a gentle, non-overwhelming slope),
    /// every step blocks on an interactive action instead of a wall of text, no round ever asks for a
    /// mechanic it has not taught yet, progress advances one step at a time, and the tutorial finishes
    /// exactly at the last tutorial round — neither early nor leaking past it. Pure data so the
    /// "playtest" runs as a regression suite; the live human onboarding-difficulty session on device
    /// remains the documented verification gap (no Unity runtime in this environment).
    /// </summary>
    public static class OnboardingPlaytest
    {
        /// <summary>
        /// The most new mechanics any single tutorial round is allowed to introduce. A gentle
        /// onboarding reveals one at a time; more than one at once is the overwhelming case the
        /// playtest guards against.
        /// </summary>
        public const int MaxNewMechanicsPerRound = 1;

        /// <summary>
        /// Walk the whole tutorial as a first-run player who performs each gated action the moment
        /// it is asked, returning the flow state observed on every tutorial round in reveal order.
        /// This is the scripted session a tester would run by hand, made reproducible.
        /// </summary>
        public static IReadOnlyList<OnboardingPlaytestStep> WalkTutorial()
        {
            var steps = new List<OnboardingPlaytestStep>();
            var progress = OnboardingProgress.Empty;

            for (var round = OnboardingScript.FirstTutorialRound; round <= OnboardingScript.LastTutorialRound; round += 1)
            {
                var flow = OnboardingFlowModel.Build(round, progress);
                var step = flow.Step
                    ?? throw new InvalidOperationException($"Tutorial round {round} unexpectedly carries no onboarding step.");

                // The player does the gated action the round asks for.
                progress = progress.CompleteRound(round);

                steps.Add(new OnboardingPlaytestStep(
                    Round: round,
                    Mechanic: step.Mechanic,
                    Title: flow.Title ?? string.Empty,
                    Prompt: flow.ActionPrompt ?? string.Empty,
                    RequiresInteractiveAction: flow.RequiresPlayerAction && flow.PendingGate.HasValue,
                    Gate: step.Gate,
                    RevealedMechanicsSoFar: OnboardingScript.RevealedBy(round).Count,
                    CompletedStepsAfter: progress.CompletedCount));
            }

            return steps;
        }

        /// <summary>
        /// The most new mechanics introduced on any single tutorial round across the walkthrough.
        /// A fair onboarding keeps this at <see cref="MaxNewMechanicsPerRound"/>.
        /// </summary>
        public static int PeakNewMechanicsPerRound()
        {
            var perRound = WalkTutorial()
                .GroupBy(step => step.Round)
                .Select(group => group.Select(step => step.Mechanic).Distinct().Count());
            return perRound.DefaultIfEmpty(0).Max();
        }

        /// <summary>
        /// True when the onboarding difficulty curve is fair across the scripted walkthrough:
        /// one new mechanic per round, every step interactive, progress monotonic by one, the
        /// reveal cumulative and in round order, and the tutorial completing exactly at the last
        /// tutorial round. The overall verdict of the playtest.
        /// </summary>
        public static bool IsDifficultyFair() => Diagnose() is null;

        /// <summary>
        /// Check the whole scripted walkthrough. Returns <c>null</c> when the onboarding curve is
        /// fair, otherwise a short reason describing the first problem a tester would flag.
        /// </summary>
        public static string? Diagnose()
        {
            var steps = WalkTutorial();

            if (steps.Count != OnboardingScript.Steps.Count)
            {
                return $"пройдено {steps.Count} шагов вместо {OnboardingScript.Steps.Count}";
            }

            // Gentle slope: no round dumps more than one new mechanic on the player.
            if (PeakNewMechanicsPerRound() > MaxNewMechanicsPerRound)
            {
                return $"раунд открывает больше {MaxNewMechanicsPerRound} механик за раз";
            }

            // Every step is distinct and revealed cumulatively, one per round, in order.
            if (steps.Select(step => step.Mechanic).Distinct().Count() != steps.Count)
            {
                return "механики обучения повторяются между раундами";
            }

            for (var index = 0; index < steps.Count; index += 1)
            {
                var step = steps[index];
                var expectedRound = OnboardingScript.FirstTutorialRound + index;

                if (step.Round != expectedRound)
                {
                    return $"шаг {index} ожидался на раунде {expectedRound}, а пришёл на {step.Round}";
                }

                // Interactive, not a wall of text: a prompt and a pending gate must be present.
                if (!step.RequiresInteractiveAction)
                {
                    return $"раунд {step.Round} не требует интерактивного действия";
                }

                if (string.IsNullOrWhiteSpace(step.Prompt) || string.IsNullOrWhiteSpace(step.Title))
                {
                    return $"раунд {step.Round} не показывает интерактивную подсказку";
                }

                // No round asks for a mechanic before it has been revealed.
                if (step.RevealedMechanicsSoFar != index + 1)
                {
                    return $"раунд {step.Round} раскрыл {step.RevealedMechanicsSoFar} механик вместо {index + 1}";
                }

                // Progress advances exactly one step at a time — no skips, no double-counting.
                if (step.CompletedStepsAfter != index + 1)
                {
                    return $"после раунда {step.Round} прогресс {step.CompletedStepsAfter} вместо {index + 1}";
                }
            }

            // The tutorial completes exactly at the last tutorial round: not before, and the final
            // step does land the player in a fully-taught state.
            var afterLast = OnboardingScript.AllGates.Aggregate(
                OnboardingProgress.Empty,
                (progress, gate) => progress.Complete(gate));
            if (!afterLast.IsTutorialComplete)
            {
                return "после последнего обучающего раунда туториал не отмечен завершённым";
            }

            return null;
        }
    }
}
