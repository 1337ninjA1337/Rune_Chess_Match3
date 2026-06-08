using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One step of the first-run onboarding: the round it fires on, the single mechanic that
    /// round reveals, the round's GDD design goal, the short interactive hint the tutorial layer
    /// shows, and the <see cref="OnboardingGate"/> the player must actually perform to clear the
    /// step. Pure data so the progressive reveal is authored in one place and the Unity
    /// presentation only has to render the prompt and report when the gated action happened.
    /// </summary>
    public sealed record OnboardingStep(
        int Round,
        OnboardingMechanic Mechanic,
        string Title,
        string Hint,
        string DesignGoal,
        OnboardingGate Gate);

    /// <summary>
    /// The first-run onboarding script (GDD "Обучение и onboarding"): a deterministic,
    /// data-driven schedule that opens one mechanic per round across the tutorial rounds 1-7
    /// so the player learns buying/placement, runes, positioning, risk/reward, shields/healing,
    /// backline threats and magic damage in turn. Each step's <see cref="OnboardingStep.DesignGoal"/>
    /// mirrors the matching <see cref="PveRoundDefinition.DesignGoal"/>, keeping the teaching
    /// schedule in sync with the round table. Each step also carries the <see cref="OnboardingGate"/>
    /// the player must perform to clear it, so the tutorial advances by doing the mechanic
    /// (GDD "обучение должно быть интерактивным, а не текстовым") rather than by reading. The
    /// interactive presentation of these prompts is the Unity tutorial layer's job; this type is
    /// the single source of truth it drives, and <see cref="OnboardingFlowModel"/> decides when a
    /// prompt is satisfied.
    /// </summary>
    public static class OnboardingScript
    {
        /// <summary>First and last rounds that carry an onboarding step.</summary>
        public const int FirstTutorialRound = 1;
        public const int LastTutorialRound = 7;

        /// <summary>The onboarding steps, one per tutorial round, in reveal order.</summary>
        public static IReadOnlyList<OnboardingStep> Steps { get; } = Array.AsReadOnly(new[]
        {
            Step(1, OnboardingMechanic.BuyAndPlaceHero,
                "Покупка и расстановка",
                "Купи героя в магазине и перетащи его на свою сторону поля."),
            Step(2, OnboardingMechanic.RedAndBlueRunes,
                "Красные и синие руны",
                "Собирай красные руны для урона и синие для маны способностей."),
            Step(3, OnboardingMechanic.TankAndPositioning,
                "Танк и позиционирование",
                "Ставь танка в передний ряд, чтобы прикрыть заднюю линию."),
            Step(4, OnboardingMechanic.RiskAndReward,
                "Риск и награда",
                "Реши, стоит ли рискнуть здоровьем забега ради награды события."),
            Step(5, OnboardingMechanic.ShieldsAndHealing,
                "Щиты и лечение",
                "Жёлтые руны дают щиты, зелёные лечат — удержи строй живым."),
            Step(6, OnboardingMechanic.BacklineThreat,
                "Угроза задней линии",
                "Враги рвутся в тыл — прикрой своих стрелков и магов."),
            Step(7, OnboardingMechanic.MagicDamage,
                "Магический урон",
                "Против магов нужна защита от магии и быстрый размен по тылу.")
        });

        private static readonly IReadOnlyDictionary<int, OnboardingStep> ByRound =
            Steps.ToDictionary(step => step.Round);

        /// <summary>True when the given round opens a new mechanic in the onboarding script.</summary>
        public static bool IsTutorialRound(int round) => ByRound.ContainsKey(round);

        /// <summary>The onboarding step for a round, or <c>null</c> when the round teaches nothing new.</summary>
        public static OnboardingStep? ForRound(int round) =>
            ByRound.TryGetValue(round, out var step) ? step : null;

        /// <summary>Try to get the onboarding step for a round.</summary>
        public static bool TryGetForRound(int round, out OnboardingStep step)
        {
            return ByRound.TryGetValue(round, out step!);
        }

        /// <summary>The onboarding step for the run's current round, or <c>null</c> when none applies.</summary>
        public static OnboardingStep? ForRun(RunState run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return ForRound(run.Round);
        }

        /// <summary>The mechanics revealed up to and including the given round, in reveal order.</summary>
        public static IReadOnlyList<OnboardingMechanic> RevealedBy(int round) =>
            Steps.Where(step => step.Round <= round).Select(step => step.Mechanic).ToList();

        /// <summary>The interactive gate the given round asks the player to perform, or <c>null</c> when the round teaches nothing new.</summary>
        public static OnboardingGate? GateForRound(int round) =>
            ForRound(round)?.Gate;

        /// <summary>The interactive gates for every tutorial step, in reveal order.</summary>
        public static IReadOnlyList<OnboardingGate> AllGates { get; } =
            Array.AsReadOnly(Steps.Select(step => step.Gate).ToArray());

        private static OnboardingStep Step(int round, OnboardingMechanic mechanic, string title, string hint) =>
            new(round, mechanic, title, hint, PveRunSchedule.GetRound(round).DesignGoal, OnboardingGates.ForMechanic(mechanic));
    }
}
