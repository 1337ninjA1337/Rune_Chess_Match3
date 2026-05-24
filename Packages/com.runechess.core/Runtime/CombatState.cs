using System;

namespace RuneChess.Core;

public sealed record CombatState(
    Match3Board RuneBoard,
    int Match3MovesUsed,
    int LastMatchedRunesCount,
    int LastComboDepth,
    int LastMatchPower,
    int DurationSeconds,
    int ElapsedSeconds
)
{
    public const int DefaultDurationSeconds = 60;

    public int RemainingSeconds => Math.Max(0, DurationSeconds - ElapsedSeconds);
    public bool IsTimerExpired => RemainingSeconds == 0;

    public static CombatState Start(int runeSeed, int durationSeconds = DefaultDurationSeconds)
    {
        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Combat duration must be positive.");
        }

        return new CombatState(
            RuneBoard: Match3Board.CreateDeterministic(runeSeed),
            Match3MovesUsed: 0,
            LastMatchedRunesCount: 0,
            LastComboDepth: 0,
            LastMatchPower: 0,
            DurationSeconds: durationSeconds,
            ElapsedSeconds: 0
        );
    }

    public CombatState AdvanceTimer(int elapsedSeconds)
    {
        if (elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed combat time cannot be negative.");
        }

        return this with
        {
            ElapsedSeconds = Math.Min(DurationSeconds, ElapsedSeconds + elapsedSeconds)
        };
    }

    public CombatState SwapRunes(BoardPoint a, BoardPoint b, int comboDepth = 0)
    {
        if (comboDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(comboDepth), "Combo depth cannot be negative.");
        }

        var swapped = RuneBoard.SwapIfCreatesMatch(a, b);
        var resolution = swapped.ResolveChainReactions(Match3MovesUsed + 1);
        var matchedRunesCount = resolution.TotalMatchedRunesCount;
        var resolvedComboDepth = comboDepth + resolution.MaxComboDepth;
        var matchPower = resolution.GetTotalMatchPower(comboDepth);

        return this with
        {
            RuneBoard = resolution.Board,
            Match3MovesUsed = Match3MovesUsed + 1,
            LastMatchedRunesCount = matchedRunesCount,
            LastComboDepth = resolvedComboDepth,
            LastMatchPower = matchPower
        };
    }
}
