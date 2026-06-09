using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// A recorded player composition for the Async PvP Arena (GDD "Будущие режимы":
    /// "Игрок сражается против записанных составов других игроков"). Instead of a live
    /// opponent, the arena stores each player's finished board as a snapshot and later
    /// replays it as an AI-controlled enemy. This is the schema for one such record.
    ///
    /// The snapshot keeps only stable, serializable identity:
    /// <list type="bullet">
    /// <item>who recorded it (<see cref="OwnerName"/>) and a stable <see cref="SnapshotId"/>;</item>
    /// <item>the matchmaking <see cref="Rating"/> used to pair fair opponents;</item>
    /// <item>the chosen <see cref="CommanderId"/>;</item>
    /// <item>the placed <see cref="Heroes"/> with star level and grid cell;</item>
    /// <item>the owned <see cref="ArtifactIds"/> that shaped the run.</item>
    /// </list>
    ///
    /// It deliberately holds no soft currency, purchase, or other account state: an arena
    /// opponent is reconstructed purely from gameplay choices, so a snapshot can never carry
    /// a paid advantage into PvP (GDD: "Монетизация не должна давать прямое преимущество в
    /// PvP"). Placements are recorded in the owner's own player-side frame; converting them
    /// to the enemy half for a battle is a separate replay concern. Pure data so the schema
    /// and its validation can be smoke-tested without Unity. See docs/async-pvp-arena.md for
    /// the full design.
    /// </summary>
    public sealed record ArenaCompositionSnapshot(
        string SnapshotId,
        string OwnerName,
        int Rating,
        string CommanderId,
        IReadOnlyList<ArenaHeroPlacement> Heroes,
        IReadOnlyList<string> ArtifactIds)
    {
        /// <summary>The most heroes an owner can place: every cell on the player half.</summary>
        public static int MaxHeroes => TacticalField.Mvp.CellCount / 2;

        public string SnapshotId { get; init; } = string.IsNullOrWhiteSpace(SnapshotId)
            ? throw new ArgumentException("Arena snapshot id cannot be blank.", nameof(SnapshotId))
            : SnapshotId.Trim();

        public string OwnerName { get; init; } = string.IsNullOrWhiteSpace(OwnerName)
            ? throw new ArgumentException("Arena snapshot owner name cannot be blank.", nameof(OwnerName))
            : OwnerName.Trim();

        public int Rating { get; init; } = Rating >= 0
            ? Rating
            : throw new ArgumentOutOfRangeException(nameof(Rating), "Arena rating cannot be negative.");

        public string CommanderId { get; init; } = string.IsNullOrWhiteSpace(CommanderId)
            ? throw new ArgumentException("Arena snapshot commander id cannot be blank.", nameof(CommanderId))
            : CommanderId.Trim();

        public IReadOnlyList<ArenaHeroPlacement> Heroes { get; init; } = ValidateHeroes(Heroes);

        public IReadOnlyList<string> ArtifactIds { get; init; } = ValidateArtifacts(ArtifactIds);

        /// <summary>Number of heroes in this composition.</summary>
        public int HeroCount => Heroes.Count;

        private static IReadOnlyList<ArenaHeroPlacement> ValidateHeroes(IReadOnlyList<ArenaHeroPlacement> heroes)
        {
            if (heroes is null)
            {
                throw new ArgumentNullException(nameof(heroes));
            }

            if (heroes.Count == 0)
            {
                throw new ArgumentException("An arena composition must record at least one hero.", nameof(heroes));
            }

            if (heroes.Count > MaxHeroes)
            {
                throw new ArgumentException(
                    $"An arena composition cannot record more than {MaxHeroes} heroes.", nameof(heroes));
            }

            var seen = new HashSet<TacticalPosition>();
            foreach (var hero in heroes)
            {
                if (hero is null)
                {
                    throw new ArgumentException("An arena composition cannot record a null hero.", nameof(heroes));
                }

                if (!hero.Position.IsPlayerSide)
                {
                    throw new ArgumentException(
                        $"Arena hero '{hero.HeroId}' is not on a player-side cell.", nameof(heroes));
                }

                if (!seen.Add(hero.Position))
                {
                    throw new ArgumentException(
                        $"Two arena heroes share cell {hero.Position}.", nameof(heroes));
                }
            }

            return heroes.ToList().AsReadOnly();
        }

        private static IReadOnlyList<string> ValidateArtifacts(IReadOnlyList<string> artifactIds)
        {
            if (artifactIds is null)
            {
                throw new ArgumentNullException(nameof(artifactIds));
            }

            var cleaned = new List<string>(artifactIds.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in artifactIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("An arena artifact id cannot be blank.", nameof(artifactIds));
                }

                var trimmed = id.Trim();
                if (!seen.Add(trimmed))
                {
                    throw new ArgumentException($"Arena artifact '{trimmed}' is listed twice.", nameof(artifactIds));
                }

                cleaned.Add(trimmed);
            }

            return cleaned.AsReadOnly();
        }

        /// <summary>Absolute rating distance to another rating, used by matchmaking.</summary>
        public int RatingDistanceTo(int otherRating) => Math.Abs(Rating - otherRating);
    }
}
