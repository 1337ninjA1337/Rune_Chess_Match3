using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// How many distinct heroes a board contributes to one synergy, plus the tiers
    /// that the count unlocks and the next breakpoint still to reach.
    /// </summary>
    public sealed record SynergyProgress(
        SynergyDefinition Definition,
        int UnitCount,
        IReadOnlyList<SynergyTier> ActiveTiers,
        SynergyTier? NextTier)
    {
        /// <summary>True when at least one synergy tier is active.</summary>
        public bool IsActive => ActiveTiers.Count > 0;

        /// <summary>The highest active tier, or null when none is active.</summary>
        public SynergyTier? HighestActiveTier => ActiveTiers.Count > 0 ? ActiveTiers[^1] : null;
    }

    /// <summary>
    /// Pure composition calculator for faction and class synergies. A hero counts
    /// once per synergy regardless of star level, matching auto-battler trait rules,
    /// so the same id placed twice still contributes a single unit.
    /// </summary>
    public static class SynergyCalculator
    {
        /// <summary>Evaluate synergies for the placed heroes of a run.</summary>
        public static IReadOnlyList<SynergyProgress> Evaluate(IEnumerable<BoardHero> team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            return EvaluateByHeroIds(team.Select(boardHero => boardHero.Hero.HeroId));
        }

        /// <summary>Evaluate synergies for a raw set of hero ids.</summary>
        public static IReadOnlyList<SynergyProgress> EvaluateByHeroIds(IEnumerable<string> heroIds)
        {
            if (heroIds is null)
            {
                throw new ArgumentNullException(nameof(heroIds));
            }

            var uniqueHeroes = heroIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(HeroCatalog.Get)
                .ToList();

            var factionCounts = CountBy(uniqueHeroes, hero => hero.Faction);
            var classCounts = CountBy(uniqueHeroes, hero => hero.Class);

            var results = new List<SynergyProgress>();
            results.AddRange(BuildProgress(FactionCatalog.All, factionCounts));
            results.AddRange(BuildProgress(ClassCatalog.All, classCounts));
            return results;
        }

        /// <summary>Only the synergies that currently have at least one active tier.</summary>
        public static IReadOnlyList<SynergyProgress> ActiveSynergies(IEnumerable<BoardHero> team)
        {
            return Evaluate(team).Where(progress => progress.IsActive).ToList();
        }

        private static IReadOnlyDictionary<string, int> CountBy(
            IReadOnlyList<HeroDefinition> heroes,
            Func<HeroDefinition, string> selector)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var hero in heroes)
            {
                var key = selector(hero);
                counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;
            }

            return counts;
        }

        private static IEnumerable<SynergyProgress> BuildProgress(
            IReadOnlyList<SynergyDefinition> definitions,
            IReadOnlyDictionary<string, int> counts)
        {
            foreach (var definition in definitions)
            {
                if (!counts.TryGetValue(definition.Name, out var count) || count <= 0)
                {
                    continue;
                }

                yield return new SynergyProgress(
                    definition,
                    count,
                    definition.ActiveTiers(count),
                    definition.NextTier(count));
            }
        }
    }
}
