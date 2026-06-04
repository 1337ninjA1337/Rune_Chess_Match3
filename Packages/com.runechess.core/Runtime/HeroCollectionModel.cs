using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One hero entry for the collection / hero-details screen (GDD UI screen 7
    /// "Экран деталей героя"). It exposes every field the detail view lists: identity,
    /// rarity, cost, faction, class, preferred rune, current stars, base stats, and the
    /// ability and passive descriptions. Pure data so the Unity layer only renders it.
    /// </summary>
    public sealed record HeroCollectionEntry(
        string HeroId,
        string Name,
        HeroRarity Rarity,
        int Cost,
        string Faction,
        string Class,
        RuneType RuneAffinity,
        HeroRole Role,
        int Stars,
        HeroStats Stats,
        string Ability,
        string Passive)
    {
        /// <summary>Russian label for the hero's preferred rune colour.</summary>
        public string RuneAffinityLabel => RuneAffinity switch
        {
            RuneType.Red => "Красная руна",
            RuneType.Blue => "Синяя руна",
            RuneType.Green => "Зелёная руна",
            RuneType.Yellow => "Жёлтая руна",
            RuneType.Purple => "Фиолетовая руна",
            RuneType.White => "Белая руна",
            _ => throw new ArgumentOutOfRangeException(nameof(RuneAffinity), RuneAffinity, "Unknown rune type.")
        };

        /// <summary>Compact one-line stat summary for the detail card.</summary>
        public string StatsLabel =>
            $"HP {Stats.BaseHealth:0}  ATK {Stats.Attack:0}  AS {Stats.BaseAttackSpeed:0.00}  "
            + $"ARM {Stats.Armor:0}  MR {Stats.MagicResist:0}  MP {Stats.ManaMax:0}";
    }

    /// <summary>
    /// View-model for the hero collection screen reachable from the main menu (GDD UI
    /// screen 1 "доступ к коллекции героев") and the per-hero detail view (screen 7).
    /// It lists the whole MVP roster sorted by rarity, then cost, then name so the grid
    /// reads predictably. Sorting and counts live in core so they can be smoke-tested.
    /// </summary>
    public sealed record HeroCollectionModel(IReadOnlyList<HeroCollectionEntry> Heroes)
    {
        /// <summary>Number of heroes in the roster.</summary>
        public int Count => Heroes.Count;

        /// <summary>Build the collection from the hero catalog, using base (one-star) stats.</summary>
        public static HeroCollectionModel Build()
        {
            var heroes = HeroCatalog.All
                .OrderBy(hero => hero.Rarity)
                .ThenBy(hero => hero.Cost)
                .ThenBy(hero => hero.Name, StringComparer.Ordinal)
                .Select(hero => new HeroCollectionEntry(
                    HeroId: hero.Id,
                    Name: hero.Name,
                    Rarity: hero.Rarity,
                    Cost: hero.Cost,
                    Faction: hero.Faction,
                    Class: hero.Class,
                    RuneAffinity: hero.RuneAffinity,
                    Role: hero.Role,
                    Stars: hero.Stars,
                    Stats: hero.StatsForStars(hero.Stars),
                    Ability: hero.Ability,
                    Passive: hero.Passive))
                .ToList();

            return new HeroCollectionModel(heroes);
        }
    }
}
