using System;

namespace RuneChess.Core
{
    /// <summary>
    /// View-model for the roguelite event screen (GDD UI screen "Экран события").
    /// It surfaces a single event encounter: the event name and design goal from the
    /// round, a headline, the offered <see cref="EventOption"/> with its accept/decline
    /// copy, and the continue routing. The four supported event archetypes live in
    /// <see cref="EventCatalog"/>; the relic-merchant event round picks one
    /// deterministically from the round seed so the screen is reproducible and
    /// smoke-testable without Unity. Applying an accepted event to the run is the
    /// separate P1 event-mechanics task.
    /// </summary>
    public sealed record EventScreenModel(
        int Round,
        PveRoundType RoundType,
        string EventName,
        string DesignGoal,
        string Headline,
        EventOption Choice,
        bool AllowsDecline,
        string DeclineLabel,
        string ContinueLabel,
        string ContinueMeta)
    {
        /// <summary>Headline label shown above the event card.</summary>
        public const string EventHeadline = "СОБЫТИЕ";

        /// <summary>Archetype of the offered event.</summary>
        public EventChoiceKind Kind => Choice.Kind;

        /// <summary>Whether the offered event matches a given archetype.</summary>
        public bool Offers(EventChoiceKind kind) => Choice.Kind == kind;

        /// <summary>
        /// Build an event screen for an explicit event choice. <paramref name="round"/>,
        /// <paramref name="eventName"/> and <paramref name="designGoal"/> describe the
        /// encounter context shown around the card.
        /// </summary>
        public static EventScreenModel ForEvent(
            EventOption choice,
            int round,
            string eventName,
            string designGoal)
        {
            if (choice is null)
            {
                throw new ArgumentNullException(nameof(choice));
            }

            return new EventScreenModel(
                Round: round,
                RoundType: PveRoundType.Event,
                EventName: eventName ?? string.Empty,
                DesignGoal: designGoal ?? string.Empty,
                Headline: EventHeadline,
                Choice: choice,
                AllowsDecline: true,
                DeclineLabel: choice.DeclineLabel,
                ContinueLabel: "Продолжить",
                ContinueMeta: "CONTINUE");
        }

        /// <summary>
        /// Build the event screen for a GDD event round. The offered archetype is
        /// chosen deterministically from the round seed so the same round always
        /// presents the same event.
        /// </summary>
        public static EventScreenModel Build(PveRoundDefinition round)
        {
            if (round is null)
            {
                throw new ArgumentNullException(nameof(round));
            }

            if (round.Type != PveRoundType.Event)
            {
                throw new InvalidOperationException("An event screen can only be built for an event round.");
            }

            var choice = PickForSeed(round.CombatRuneSeed);
            return ForEvent(choice, round.Round, round.EnemyName, round.DesignGoal);
        }

        /// <summary>Build the event screen for a run currently on an event round.</summary>
        public static EventScreenModel Build(RunState run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return Build(run.CurrentRoundDefinition);
        }

        /// <summary>Deterministically pick one of the four supported events from a seed.</summary>
        private static EventOption PickForSeed(int seed)
        {
            var index = (int)((uint)seed % (uint)EventCatalog.All.Count);
            return EventCatalog.All[index];
        }
    }
}
