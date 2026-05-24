using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core;

public readonly record struct BoardPoint(int Row, int Column);

public sealed record Match3ChainStep(
    int ComboDepth,
    IReadOnlySet<BoardPoint> MatchedCells,
    Match3Board BoardAfterRemoval,
    Match3Board BoardAfterDrop
)
{
    public int ChainNumber => ComboDepth + 1;
    public int MatchedRunesCount => MatchedCells.Count;
}

public sealed record Match3ChainResolution(
    Match3Board Board,
    IReadOnlyList<Match3ChainStep> Steps
)
{
    public int ReactionCount => Math.Max(0, Steps.Count - 1);
    public int MaxComboDepth => Steps.Count == 0 ? 0 : Steps.Max(step => step.ComboDepth);
    public int TotalMatchedRunesCount => Steps.Sum(step => step.MatchedRunesCount);
}

public sealed class Match3Board
{
    public const int Rows = 7;
    public const int Columns = 7;
    public const int CellCount = Rows * Columns;

    private readonly RuneType?[] cells;

    public Match3Board(IReadOnlyList<RuneType> runes)
    {
        if (runes.Count != CellCount)
        {
            throw new ArgumentException($"A match-3 board needs {CellCount} runes.", nameof(runes));
        }

        cells = runes.Select(rune => (RuneType?)rune).ToArray();
    }

    private Match3Board(IReadOnlyList<RuneType?> runes)
    {
        if (runes.Count != CellCount)
        {
            throw new ArgumentException($"A match-3 board needs {CellCount} cells.", nameof(runes));
        }

        cells = runes.ToArray();
    }

    public RuneType this[int row, int column]
    {
        get
        {
            var rune = GetRuneOrEmpty(row, column);
            if (!rune.HasValue)
            {
                throw new InvalidOperationException("Board cell is empty.");
            }

            return rune.Value;
        }
    }

    public RuneType this[BoardPoint point] => this[point.Row, point.Column];
    public int EmptyCellCount => cells.Count(rune => !rune.HasValue);

    public static Match3Board CreateDeterministic(int seed)
    {
        var random = new Random(seed);
        var runes = new RuneType[CellCount];

        for (var index = 0; index < runes.Length; index += 1)
        {
            runes[index] = RuneTypes.All[random.Next(RuneTypes.All.Count)];
        }

        return new Match3Board(runes);
    }

    public static bool AreAdjacent(BoardPoint a, BoardPoint b)
    {
        var rowDistance = Math.Abs(a.Row - b.Row);
        var columnDistance = Math.Abs(a.Column - b.Column);

        return rowDistance + columnDistance == 1;
    }

    public static bool CanSwap(BoardPoint a, BoardPoint b)
    {
        return Contains(a) && Contains(b) && AreAdjacent(a, b);
    }

    public static bool Contains(BoardPoint point)
    {
        return point.Row >= 0 && point.Row < Rows && point.Column >= 0 && point.Column < Columns;
    }

    public static IReadOnlyList<BoardPoint> CreateCells()
    {
        var points = new List<BoardPoint>(CellCount);
        for (var row = 0; row < Rows; row += 1)
        {
            for (var column = 0; column < Columns; column += 1)
            {
                points.Add(new BoardPoint(row, column));
            }
        }

        return points;
    }

    public IReadOnlySet<BoardPoint> FindMatches()
    {
        var matches = new HashSet<BoardPoint>(FindHorizontalMatches());
        matches.UnionWith(FindVerticalMatches());
        return matches;
    }

    public IReadOnlySet<BoardPoint> FindHorizontalMatches()
    {
        var matches = new HashSet<BoardPoint>();

        for (var row = 0; row < Rows; row += 1)
        {
            AddLineMatches(matches, Enumerable.Range(0, Columns).Select(column => new BoardPoint(row, column)));
        }

        return matches;
    }

    public IReadOnlySet<BoardPoint> FindVerticalMatches()
    {
        var matches = new HashSet<BoardPoint>();

        for (var column = 0; column < Columns; column += 1)
        {
            AddLineMatches(matches, Enumerable.Range(0, Rows).Select(row => new BoardPoint(row, column)));
        }

        return matches;
    }

    public RuneType? GetRuneOrEmpty(int row, int column)
    {
        return cells[Index(row, column)];
    }

    public RuneType? GetRuneOrEmpty(BoardPoint point)
    {
        return GetRuneOrEmpty(point.Row, point.Column);
    }

    public bool IsEmpty(BoardPoint point)
    {
        return !GetRuneOrEmpty(point).HasValue;
    }

    public Match3Board Swap(BoardPoint a, BoardPoint b)
    {
        if (!CanSwap(a, b))
        {
            throw new InvalidOperationException("Only adjacent in-board runes can be swapped.");
        }

        var aIndex = Index(a.Row, a.Column);
        var bIndex = Index(b.Row, b.Column);
        if (!cells[aIndex].HasValue || !cells[bIndex].HasValue)
        {
            throw new InvalidOperationException("Only filled rune cells can be swapped.");
        }

        var swapped = cells.ToArray();
        (swapped[aIndex], swapped[bIndex]) = (swapped[bIndex], swapped[aIndex]);

        return new Match3Board(swapped);
    }

    public Match3Board RemoveMatches()
    {
        return RemoveRunes(FindMatches());
    }

    public Match3Board RemoveRunes(IReadOnlySet<BoardPoint> points)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        var removed = cells.ToArray();
        foreach (var point in points)
        {
            removed[Index(point.Row, point.Column)] = null;
        }

        return new Match3Board(removed);
    }

    public Match3Board DropRunesFromTop(int seed)
    {
        var random = new Random(seed);
        var dropped = new RuneType?[CellCount];

        for (var column = 0; column < Columns; column += 1)
        {
            var writeRow = Rows - 1;
            for (var readRow = Rows - 1; readRow >= 0; readRow -= 1)
            {
                var rune = GetRuneOrEmpty(readRow, column);
                if (!rune.HasValue)
                {
                    continue;
                }

                dropped[Index(writeRow, column)] = rune.Value;
                writeRow -= 1;
            }

            for (; writeRow >= 0; writeRow -= 1)
            {
                dropped[Index(writeRow, column)] = RuneTypes.All[random.Next(RuneTypes.All.Count)];
            }
        }

        return new Match3Board(dropped);
    }

    public Match3ChainResolution ResolveChainReactions(int seed, int maxChainSteps = 8)
    {
        if (maxChainSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChainSteps), "Chain resolution must allow at least one step.");
        }

        var current = this;
        var steps = new List<Match3ChainStep>();

        while (steps.Count < maxChainSteps)
        {
            var matches = current.FindMatches();
            if (matches.Count == 0)
            {
                return new Match3ChainResolution(current, steps.ToList());
            }

            var matchedCells = matches.ToHashSet();
            var removed = current.RemoveRunes(matchedCells);
            var dropped = removed.DropRunesFromTop(unchecked(seed + steps.Count));

            steps.Add(new Match3ChainStep(
                ComboDepth: steps.Count,
                MatchedCells: matchedCells,
                BoardAfterRemoval: removed,
                BoardAfterDrop: dropped
            ));

            current = dropped;
        }

        if (current.FindMatches().Count > 0)
        {
            throw new InvalidOperationException("Match-3 chain resolution exceeded the maximum chain depth.");
        }

        return new Match3ChainResolution(current, steps.ToList());
    }

    public Match3Board SwapIfCreatesMatch(BoardPoint a, BoardPoint b)
    {
        var swapped = Swap(a, b);
        var matches = swapped.FindMatches();
        if (!ContainsSwappedRuneMatch(matches, a, b))
        {
            throw new InvalidOperationException("Rune swap must create a match-3 or higher.");
        }

        return swapped;
    }

    public bool CreatesMatchAfterSwap(BoardPoint a, BoardPoint b)
    {
        if (!CanSwap(a, b))
        {
            return false;
        }

        var swapped = Swap(a, b);
        var matches = swapped.FindMatches();
        return ContainsSwappedRuneMatch(matches, a, b);
    }

    public bool IsLegalSwap(BoardPoint a, BoardPoint b)
    {
        return CreatesMatchAfterSwap(a, b);
    }

    private void AddLineMatches(HashSet<BoardPoint> matches, IEnumerable<BoardPoint> line)
    {
        var run = new List<BoardPoint>();
        RuneType? currentRune = null;

        foreach (var point in line)
        {
            var rune = GetRuneOrEmpty(point);
            if (!rune.HasValue)
            {
                FlushRun(matches, run);
                run.Clear();
                currentRune = null;
                continue;
            }

            if (currentRune == rune.Value)
            {
                run.Add(point);
                continue;
            }

            FlushRun(matches, run);
            run.Clear();
            run.Add(point);
            currentRune = rune.Value;
        }

        FlushRun(matches, run);
    }

    private static void FlushRun(HashSet<BoardPoint> matches, IReadOnlyList<BoardPoint> run)
    {
        if (run.Count < 3)
        {
            return;
        }

        foreach (var point in run)
        {
            matches.Add(point);
        }
    }

    private static bool ContainsSwappedRuneMatch(IReadOnlySet<BoardPoint> matches, BoardPoint a, BoardPoint b)
    {
        return matches.Contains(a) || matches.Contains(b);
    }

    private static int Index(int row, int column)
    {
        if (!Contains(new BoardPoint(row, column)))
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Board coordinates are outside the 7x7 board.");
        }

        return row * Columns + column;
    }
}
