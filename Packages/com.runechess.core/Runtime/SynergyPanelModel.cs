using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One row on the synergy panel: a represented faction or class, the heroes it
    /// contributes, its active tiers, the next breakpoint, how many more distinct
    /// heroes reach it, the catalog heroes that would close that breakpoint, and the
    /// colour-coded strength. Pure data so the Unity panel renders without re-deriving
    /// the synergy maths.
    /// </summary>
    public sealed record SynergyPanelEntry(
        SynergyKind Kind,
        string Id,
        string Name,
        string Focus,
        int UnitCount,
        IReadOnlyList<SynergyTier> ActiveTiers,
        SynergyTier? NextTier,
        int HeroesToNextTier,
        IReadOnlyList<string> NextTierHeroes,
        SynergyStrength Strength)
    {
        /// <summary>True when at least one synergy tier is active.</summary>
        public bool IsActive => ActiveTiers.Count > 0;

        /// <summary>True when there is a further breakpoint still to reach.</summary>
        public bool HasNextTier => NextTier is not null;
    }

    /// <summary>
    /// View-model for the synergy panel (GDD UI screen "Панель синергий"). It surfaces
    /// the five panel elements: active factions, active classes, the nearest upcoming
    /// breakpoints, the heroes that would close the next breakpoint, and a colour-coded
    /// strength per synergy. It builds on <see cref="SynergyCalculator"/> (distinct
    /// heroes per faction/class) and <see cref="HeroCatalog"/> for the candidate heroes,
    /// so it is deterministic and smoke-testable without Unity.
    /// </summary>
    public sealed record SynergyPanelModel(
        IReadOnlyList<SynergyPanelEntry> Entries,
        IReadOnlyList<SynergyPanelEntry> ActiveFactions,
        IReadOnlyList<SynergyPanelEntry> ActiveClasses,
        IReadOnlyList<SynergyPanelEntry> UpcomingThresholds)
    {
        /// <summary>True when any faction or class synergy currently has an active tier.</summary>
        public bool HasActiveSynergies => ActiveFactions.Count > 0 || ActiveClasses.Count > 0;

        /// <summary>
        /// The single synergy a new player should aim for first (GDD "Слишком сложный onboarding":
        /// "первые синергии должны быть очевидными"). It is the started synergy (at least one hero
        /// placed) that is closest to its next breakpoint, preferring the one needing the fewest
        /// heroes, then the most heroes already placed, then name. When no started synergy still has
        /// a breakpoint left, it falls back to the largest active synergy. Null when the team has not
        /// started any synergy yet. The UI can spotlight this so the first synergy is obvious.
        /// </summary>
        public SynergyPanelEntry? BeginnerHighlight =>
            Entries
                .Where(entry => entry.UnitCount > 0 && entry.HasNextTier)
                .OrderBy(entry => entry.HeroesToNextTier)
                .ThenByDescending(entry => entry.UnitCount)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .FirstOrDefault()
            ?? Entries
                .Where(entry => entry.UnitCount > 0 && entry.IsActive)
                .OrderByDescending(entry => entry.UnitCount)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .FirstOrDefault();

        /// <summary>
        /// A short, player-facing hint for <see cref="BeginnerHighlight"/> so onboarding can make the
        /// first synergy obvious without re-deriving the maths. Null when no synergy has been started.
        /// </summary>
        public string? BeginnerHint
        {
            get
            {
                var highlight = BeginnerHighlight;
                if (highlight is null)
                {
                    return null;
                }

                if (highlight.HasNextTier)
                {
                    return $"Соберите ещё {highlight.HeroesToNextTier} для синергии «{highlight.Name}» ({highlight.NextTier!.RequiredCount}).";
                }

                return $"Синергия «{highlight.Name}» активна.";
            }
        }

        /// <summary>Build the synergy panel from the placed heroes of a run.</summary>
        public static SynergyPanelModel Build(RunState run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return Build(run.Team);
        }

        /// <summary>Build the synergy panel from a placed team.</summary>
        public static SynergyPanelModel Build(IReadOnlyList<BoardHero> team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var teamHeroIds = new HashSet<string>(
                team.Select(boardHero => boardHero.Hero.HeroId),
                StringComparer.OrdinalIgnoreCase);

            var entries = SynergyCalculator.Evaluate(team)
                .Select(progress => BuildEntry(progress, teamHeroIds))
                .ToList();

            var activeFactions = entries
                .Where(entry => entry.Kind == SynergyKind.Faction && entry.IsActive)
                .ToList();
            var activeClasses = entries
                .Where(entry => entry.Kind == SynergyKind.Class && entry.IsActive)
                .ToList();
            var upcoming = entries
                .Where(entry => entry.HasNextTier)
                .OrderBy(entry => entry.HeroesToNextTier)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .ToList();

            return new SynergyPanelModel(entries, activeFactions, activeClasses, upcoming);
        }

        private static SynergyPanelEntry BuildEntry(SynergyProgress progress, ISet<string> teamHeroIds)
        {
            var definition = progress.Definition;
            var nextTier = progress.NextTier;
            var heroesToNext = nextTier is null
                ? 0
                : Math.Max(0, nextTier.RequiredCount - progress.UnitCount);

            var candidateHeroes = nextTier is null
                ? (IReadOnlyList<string>)Array.Empty<string>()
                : HeroCatalog.All
                    .Where(hero => MatchesSynergy(hero, definition) && !teamHeroIds.Contains(hero.Id))
                    .OrderBy(hero => hero.Name, StringComparer.Ordinal)
                    .Select(hero => hero.Name)
                    .ToList();

            return new SynergyPanelEntry(
                Kind: definition.Kind,
                Id: definition.Id,
                Name: definition.Name,
                Focus: definition.Focus,
                UnitCount: progress.UnitCount,
                ActiveTiers: progress.ActiveTiers,
                NextTier: nextTier,
                HeroesToNextTier: heroesToNext,
                NextTierHeroes: candidateHeroes,
                Strength: DetermineStrength(progress));
        }

        private static bool MatchesSynergy(HeroDefinition hero, SynergyDefinition definition)
        {
            var trait = definition.Kind == SynergyKind.Faction ? hero.Faction : hero.Class;
            return string.Equals(trait, definition.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static SynergyStrength DetermineStrength(SynergyProgress progress)
        {
            if (progress.ActiveTiers.Count == 0)
            {
                return SynergyStrength.Building;
            }

            return progress.NextTier is null ? SynergyStrength.Maxed : SynergyStrength.Active;
        }
    }
}
