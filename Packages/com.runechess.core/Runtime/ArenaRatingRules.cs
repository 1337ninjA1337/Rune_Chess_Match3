using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Moves a player's Async PvP Arena rating after a recorded match (GDD "Будущие режимы":
    /// "Реализовать подбор записанных составов для async PvP"). <see cref="ArenaMatchmaker"/>
    /// pairs opponents by <see cref="ArenaCompositionSnapshot.Rating"/>; this is the missing
    /// step that updates that number so wins raise it and losses lower it, keeping the matchmaking
    /// pool self-correcting.
    ///
    /// It is a standard Elo step: the player gains more for beating a stronger opponent and less
    /// for beating a weaker one, and the swing is capped by the K-factor so a single match cannot
    /// move the rating wildly. Matching stays purely skill-based — rating never reads account or
    /// spend state — so the arena keeps the GDD's non-pay-to-win guarantee ("Монетизация не
    /// должна давать прямое преимущество в PvP"). Pure math with no I/O, so it stays Unity-free
    /// and smoke-testable on Windows.
    /// </summary>
    public static class ArenaRatingRules
    {
        /// <summary>Default rating swing cap: a single match moves the rating by at most this much.</summary>
        public const int DefaultKFactor = 32;

        /// <summary>Largest K-factor the step accepts, bounding how volatile a season can be.</summary>
        public const int MaxKFactor = 64;

        /// <summary>The Elo logistic spread; a 400-point gap means a 10:1 expected win ratio.</summary>
        public const double RatingSpread = 400.0;

        /// <summary>
        /// Returns <paramref name="playerRating"/> updated after a match against
        /// <paramref name="opponentRating"/>. The change is <c>kFactor * (actual - expected)</c>
        /// where <paramref name="won"/> sets the actual score (1 or 0) and the expected score is
        /// the standard Elo logistic of the rating gap. The result is clamped at zero so a rating
        /// can never go negative, and the absolute change never exceeds <paramref name="kFactor"/>.
        /// </summary>
        public static int Update(int playerRating, int opponentRating, bool won, int kFactor = DefaultKFactor)
        {
            if (playerRating < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerRating), "Player rating cannot be negative.");
            }

            if (opponentRating < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opponentRating), "Opponent rating cannot be negative.");
            }

            if (kFactor < 1 || kFactor > MaxKFactor)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kFactor), $"K-factor must be between 1 and {MaxKFactor}.");
            }

            var expected = ExpectedScore(playerRating, opponentRating);
            var actual = won ? 1.0 : 0.0;
            var delta = kFactor * (actual - expected);
            var updated = (int)Math.Round(playerRating + delta, MidpointRounding.AwayFromZero);

            return Math.Max(0, updated);
        }

        /// <summary>
        /// The Elo expected score for <paramref name="playerRating"/> against
        /// <paramref name="opponentRating"/>: a value in (0, 1) that is 0.5 for equal ratings and
        /// rises as the player out-rates the opponent.
        /// </summary>
        public static double ExpectedScore(int playerRating, int opponentRating)
        {
            return 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - playerRating) / RatingSpread));
        }
    }
}
