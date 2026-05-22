using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core;

public readonly record struct BoardPoint(int Row, int Column);

public sealed class Match3Board
{
    public const int Rows = 7;
    public const int Columns = 7;

    private readonly RuneType[] cells;

    public Match3Board(IReadOnlyList<RuneType> runes)
    {
        if (runes.Count != Rows * Columns)
        {
            throw new ArgumentException($"A match-3 board needs {Rows * Columns} runes.", nameof(runes));
        }

        cells = runes.ToArray();
    }

    public RuneType this[int row, int column] => cells[Index(row, column)];

    public static Match3Board CreateDeterministic(int seed)
    {
        var random = new Random(seed);
        var values = Enum.GetValues<RuneType>();
        var runes = new RuneType[Rows * Columns];

        for (var index = 0; index < runes.Length; index += 1)
        {
            runes[index] = values[random.Next(values.Length)];
        }

        return new Match3Board(runes);
    }

    public static bool AreAdjacent(BoardPoint a, BoardPoint b)
    {
        var rowDistance = Math.Abs(a.Row - b.Row);
        var columnDistance = Math.Abs(a.Column - b.Column);

        return rowDistance + columnDistance == 1;
    }

    public IReadOnlySet<BoardPoint> FindMatches()
    {
        var matches = new HashSet<BoardPoint>();

        for (var row = 0; row < Rows; row += 1)
        {
            AddLineMatches(matches, Enumerable.Range(0, Columns).Select(column => new BoardPoint(row, column)));
        }

        for (var column = 0; column < Columns; column += 1)
        {
            AddLineMatches(matches, Enumerable.Range(0, Rows).Select(row => new BoardPoint(row, column)));
        }

        return matches;
    }

    public Match3Board Swap(BoardPoint a, BoardPoint b)
    {
        if (!AreAdjacent(a, b))
        {
            throw new InvalidOperationException("Only adjacent runes can be swapped.");
        }

        var swapped = cells.ToArray();
        (swapped[Index(a.Row, a.Column)], swapped[Index(b.Row, b.Column)]) =
            (swapped[Index(b.Row, b.Column)], swapped[Index(a.Row, a.Column)]);

        return new Match3Board(swapped);
    }

    public bool IsLegalSwap(BoardPoint a, BoardPoint b)
    {
        return AreAdjacent(a, b) && Swap(a, b).FindMatches().Count > 0;
    }

    private void AddLineMatches(HashSet<BoardPoint> matches, IEnumerable<BoardPoint> line)
    {
        var run = new List<BoardPoint>();
        RuneType? currentRune = null;

        foreach (var point in line)
        {
            var rune = this[point.Row, point.Column];
            if (currentRune == rune)
            {
                run.Add(point);
                continue;
            }

            FlushRun(matches, run);
            run.Clear();
            run.Add(point);
            currentRune = rune;
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

    private static int Index(int row, int column)
    {
        if (row is < 0 or >= Rows || column is < 0 or >= Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Board coordinates are outside the 7x7 board.");
        }

        return row * Columns + column;
    }
}
