using System;

namespace RuneChess.Core
{
    public static class HeroEconomy
    {
        public const int CopiesPerStarUpgrade = 3;
        public const int MinStars = 1;
        public const int MaxStars = 3;

        public static int CalculateSellValue(int baseCost, int stars)
        {
            if (baseCost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCost), "Hero base cost must be positive.");
            }

            if (stars is < MinStars or > MaxStars)
            {
                throw new ArgumentOutOfRangeException(nameof(stars), "Hero stars must be between one and three.");
            }

            var investedCopies = 1;
            for (var star = MinStars; star < stars; star += 1)
            {
                investedCopies *= CopiesPerStarUpgrade;
            }

            return baseCost * investedCopies;
        }
    }
}
