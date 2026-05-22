using System;

namespace RuneChess.Core;

public sealed record CombatState(
    Match3Board RuneBoard,
    int Match3MovesUsed,
    int LastMatchedRunesCount,
    int LastComboDepth,
    int LastMatchPower
)
{
    public static CombatState Start(int runeSeed)
    {
        return new CombatState(
            RuneBoard: Match3Board.CreateDeterministic(runeSeed),
            Match3MovesUsed: 0,
            LastMatchedRunesCount: 0,
            LastComboDepth: 0,
            LastMatchPower: 0
        );
    }

    public CombatState SwapRunes(BoardPoint a, BoardPoint b, int comboDepth = 0)
    {
        if (comboDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(comboDepth), "Combo depth cannot be negative.");
        }

        if (!RuneBoard.IsLegalSwap(a, b))
        {
            throw new InvalidOperationException("Rune swap must create a match during combat.");
        }

        var swapped = RuneBoard.Swap(a, b);
        var matches = swapped.FindMatches();
        var matchedRunesCount = matches.Count;

        return this with
        {
            RuneBoard = swapped,
            Match3MovesUsed = Match3MovesUsed + 1,
            LastMatchedRunesCount = matchedRunesCount,
            LastComboDepth = comboDepth,
            LastMatchPower = matchedRunesCount + comboDepth
        };
    }
}
