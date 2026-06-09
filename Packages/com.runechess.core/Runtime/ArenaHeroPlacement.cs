using System;

namespace RuneChess.Core
{
    /// <summary>
    /// One recorded hero in an async PvP composition snapshot (GDD "Будущие режимы":
    /// Async PvP Arena против записанных составов). It stores the stable, serializable
    /// fields needed to rebuild the hero on a board later: which hero it is, its star
    /// level, and where the owner placed it on their own player-side cell.
    ///
    /// This is intentionally a flat value (ids + grid coordinates) rather than a live
    /// <see cref="BoardHero"/>: a snapshot must survive being saved, shipped to another
    /// player's device, and replayed long after the original run ended, so it cannot
    /// hold runtime instance ids or object references. Pure data so the arena schema
    /// can be smoke-tested without Unity.
    /// </summary>
    public sealed record ArenaHeroPlacement(
        string HeroId,
        int Stars,
        int Row,
        int Column)
    {
        public string HeroId { get; init; } = string.IsNullOrWhiteSpace(HeroId)
            ? throw new ArgumentException("Arena hero id cannot be blank.", nameof(HeroId))
            : HeroId.Trim();

        public int Stars { get; init; } = Stars is >= 1 and <= 3
            ? Stars
            : throw new ArgumentOutOfRangeException(nameof(Stars), "Arena hero stars must be between one and three.");

        public int Row { get; init; } = Row;

        public int Column { get; init; } = Column;

        /// <summary>The grid cell this hero occupies, in the owner's own player-side frame.</summary>
        public TacticalPosition Position => new(Row, Column);

        public static ArenaHeroPlacement From(BoardHero boardHero)
        {
            if (boardHero is null)
            {
                throw new ArgumentNullException(nameof(boardHero));
            }

            return new ArenaHeroPlacement(
                boardHero.Hero.HeroId,
                boardHero.Hero.Stars,
                boardHero.Position.Row,
                boardHero.Position.Column);
        }
    }
}
