using System;

namespace RuneChess.Core
{
    public static class Match3Scoring
    {
        public static int CalculateMatchPower(int matchedRunesCount, int comboDepth)
        {
            if (matchedRunesCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchedRunesCount), "Matched rune count cannot be negative.");
            }

            if (comboDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comboDepth), "Combo depth cannot be negative.");
            }

            return matchedRunesCount + comboDepth;
        }
    }
}
