using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    public readonly struct BoardPoint : IEquatable<BoardPoint>
    {
        public BoardPoint(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        public void Deconstruct(out int row, out int column)
        {
            row = Row;
            column = Column;
        }

        public bool Equals(BoardPoint other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object? obj)
        {
            return obj is BoardPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Column;
            }
        }

        public override string ToString()
        {
            return $"{nameof(BoardPoint)} {{ {nameof(Row)} = {Row}, {nameof(Column)} = {Column} }}";
        }

        public static bool operator ==(BoardPoint left, BoardPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPoint left, BoardPoint right)
        {
            return !left.Equals(right);
        }
    }

    public sealed record Match3MoveHint(
        BoardPoint From,
        BoardPoint To,
        IReadOnlyCollection<BoardPoint> MatchedCells
    )
    {
        public IReadOnlyCollection<BoardPoint> HighlightedCells
        {
            get
            {
                var highlighted = MatchedCells.ToHashSet();
                highlighted.Add(From);
                highlighted.Add(To);
                return highlighted;
            }
        }
    }

    public sealed record Match3ChainStep(
        int ComboDepth,
        IReadOnlyCollection<BoardPoint> MatchedCells,
        IReadOnlyCollection<BoardPoint> CreatedGreatRunes,
        Match3Board BoardAfterRemoval,
        Match3Board BoardAfterDrop
    )
    {
        public int ChainNumber => ComboDepth + 1;
        public int MatchedRunesCount => MatchedCells.Count;
        public int MatchPower => GetMatchPower();

        public int GetMatchPower(int comboDepthOffset = 0)
        {
            if (comboDepthOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comboDepthOffset), "Combo depth offset cannot be negative.");
            }

            return Match3Scoring.CalculateMatchPower(MatchedRunesCount, ComboDepth + comboDepthOffset);
        }
    }

    public sealed record Match3ChainResolution(
        Match3Board Board,
        IReadOnlyList<Match3ChainStep> Steps
    )
    {
        public int ReactionCount => Math.Max(0, Steps.Count - 1);
        public int MaxComboDepth => Steps.Count == 0 ? 0 : Steps.Max(step => step.ComboDepth);
        public int TotalMatchedRunesCount => Steps.Sum(step => step.MatchedRunesCount);
        public int TotalMatchPower => GetTotalMatchPower();

        public int GetTotalMatchPower(int comboDepthOffset = 0)
        {
            if (comboDepthOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comboDepthOffset), "Combo depth offset cannot be negative.");
            }

            return Steps.Sum(step => step.GetMatchPower(comboDepthOffset));
        }
    }

    public sealed class Match3Board
    {
        public const int Rows = 7;
        public const int Columns = 7;
        public const int CellCount = Rows * Columns;

        private readonly RuneCell?[] cells;

        public Match3Board(IReadOnlyList<RuneType> runes)
        {
            if (runes.Count != CellCount)
            {
                throw new ArgumentException($"A match-3 board needs {CellCount} runes.", nameof(runes));
            }

            cells = runes.Select(rune => (RuneCell?)new RuneCell(rune)).ToArray();
        }

        public Match3Board(IReadOnlyList<RuneCell> runes)
        {
            if (runes.Count != CellCount)
            {
                throw new ArgumentException($"A match-3 board needs {CellCount} cells.", nameof(runes));
            }

            cells = runes.Select(rune => (RuneCell?)rune).ToArray();
        }

        private Match3Board(IReadOnlyList<RuneCell?> cells)
        {
            if (cells.Count != CellCount)
            {
                throw new ArgumentException($"A match-3 board needs {CellCount} cells.", nameof(cells));
            }

            this.cells = cells.ToArray();
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

        public IReadOnlyCollection<BoardPoint> FindMatches()
        {
            var matches = new HashSet<BoardPoint>(FindHorizontalMatches());
            matches.UnionWith(FindVerticalMatches());
            return matches;
        }

        public IReadOnlyCollection<BoardPoint> FindHorizontalMatches()
        {
            var matches = new HashSet<BoardPoint>();

            for (var row = 0; row < Rows; row += 1)
            {
                AddLineMatches(matches, Enumerable.Range(0, Columns).Select(column => new BoardPoint(row, column)));
            }

            return matches;
        }

        public IReadOnlyCollection<BoardPoint> FindVerticalMatches()
        {
            var matches = new HashSet<BoardPoint>();

            for (var column = 0; column < Columns; column += 1)
            {
                AddLineMatches(matches, Enumerable.Range(0, Rows).Select(row => new BoardPoint(row, column)));
            }

            return matches;
        }

        /// <summary>
        /// Groups matched cells into discrete rune matches. Each group is a connected,
        /// same-color component, so crossing matches of different colors stay separate and
        /// a single bent T/L match is reported as one group. Groups are ordered deterministically
        /// by their top-left cell so callers and tests see a stable sequence.
        /// </summary>
        public IReadOnlyList<RuneMatchGroup> FindMatchGroups()
        {
            var matched = FindMatches();
            var visited = new HashSet<BoardPoint>();
            var groups = new List<RuneMatchGroup>();

            var orderedMatched = matched
                .OrderBy(point => point.Row)
                .ThenBy(point => point.Column);

            foreach (var start in orderedMatched)
            {
                if (visited.Contains(start))
                {
                    continue;
                }

                var rune = this[start];
                var component = new HashSet<BoardPoint>();
                var queue = new Queue<BoardPoint>();
                queue.Enqueue(start);
                visited.Add(start);

                while (queue.Count > 0)
                {
                    var point = queue.Dequeue();
                    component.Add(point);

                    foreach (var neighbor in OrthogonalNeighbors(point))
                    {
                        if (visited.Contains(neighbor) || !matched.Contains(neighbor))
                        {
                            continue;
                        }

                        if (GetRuneOrEmpty(neighbor) != rune)
                        {
                            continue;
                        }

                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }

                groups.Add(new RuneMatchGroup(rune, component, IsBentShape(component)));
            }

            return groups;
        }

        public RuneCell? GetCellOrEmpty(int row, int column)
        {
            return cells[Index(row, column)];
        }

        public RuneCell? GetCellOrEmpty(BoardPoint point)
        {
            return GetCellOrEmpty(point.Row, point.Column);
        }

        public RuneType? GetRuneOrEmpty(int row, int column)
        {
            return GetCellOrEmpty(row, column)?.Rune;
        }

        public RuneType? GetRuneOrEmpty(BoardPoint point)
        {
            return GetRuneOrEmpty(point.Row, point.Column);
        }

        public bool IsGreatRune(BoardPoint point)
        {
            return GetCellOrEmpty(point)?.IsGreatRune ?? false;
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

        public Match3Board RemoveRunes(IReadOnlyCollection<BoardPoint> points)
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
            var dropped = new RuneCell?[CellCount];

            for (var column = 0; column < Columns; column += 1)
            {
                var writeRow = Rows - 1;
                for (var readRow = Rows - 1; readRow >= 0; readRow -= 1)
                {
                    var cell = GetCellOrEmpty(readRow, column);
                    if (!cell.HasValue)
                    {
                        continue;
                    }

                    dropped[Index(writeRow, column)] = cell.Value;
                    writeRow -= 1;
                }

                for (; writeRow >= 0; writeRow -= 1)
                {
                    dropped[Index(writeRow, column)] = new RuneCell(RuneTypes.All[random.Next(RuneTypes.All.Count)]);
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
                var matchGroups = current.FindMatchGroups();
                if (matchGroups.Count == 0)
                {
                    return new Match3ChainResolution(current, steps.ToList());
                }

                var matchedCells = matchGroups.SelectMany(group => group.Cells).ToHashSet();
                var greatRunes = GetGreatRuneCreationAnchors(matchGroups);
                var removed = current.RemoveRunes(matchedCells);
                var dropped = removed
                    .DropRunesFromTop(unchecked(seed + steps.Count))
                    .PlaceGreatRunes(greatRunes);

                steps.Add(new Match3ChainStep(
                    ComboDepth: steps.Count,
                    MatchedCells: matchedCells,
                    CreatedGreatRunes: greatRunes.Keys.ToHashSet(),
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

        public Match3MoveHint? FindFirstLegalMoveHint()
        {
            for (var row = 0; row < Rows; row += 1)
            {
                for (var column = 0; column < Columns; column += 1)
                {
                    var current = new BoardPoint(row, column);
                    if (column + 1 < Columns)
                    {
                        var right = new BoardPoint(row, column + 1);
                        if (TryCreateMoveHint(current, right, out var horizontalHint))
                        {
                            return horizontalHint;
                        }
                    }

                    if (row + 1 < Rows)
                    {
                        var down = new BoardPoint(row + 1, column);
                        if (TryCreateMoveHint(current, down, out var verticalHint))
                        {
                            return verticalHint;
                        }
                    }
                }
            }

            return null;
        }

        public bool TryCreateMoveHint(BoardPoint a, BoardPoint b, out Match3MoveHint? hint)
        {
            hint = null;
            if (!CanSwap(a, b) || IsEmpty(a) || IsEmpty(b))
            {
                return false;
            }

            var swapped = Swap(a, b);
            var matches = swapped.FindMatches();
            if (!ContainsSwappedRuneMatch(matches, a, b))
            {
                return false;
            }

            hint = new Match3MoveHint(a, b, matches.ToHashSet());
            return true;
        }

        public bool CreatesMatchAfterSwap(BoardPoint a, BoardPoint b)
        {
            return TryCreateMoveHint(a, b, out _);
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

        private static bool ContainsSwappedRuneMatch(IReadOnlyCollection<BoardPoint> matches, BoardPoint a, BoardPoint b)
        {
            return matches.Contains(a) || matches.Contains(b);
        }

        private Match3Board PlaceGreatRunes(IReadOnlyDictionary<BoardPoint, RuneType> greatRunes)
        {
            if (greatRunes.Count == 0)
            {
                return this;
            }

            var placed = cells.ToArray();
            foreach (var (point, rune) in greatRunes)
            {
                placed[Index(point.Row, point.Column)] = new RuneCell(rune, isGreatRune: true);
            }

            return new Match3Board(placed);
        }

        private static IReadOnlyDictionary<BoardPoint, RuneType> GetGreatRuneCreationAnchors(
            IReadOnlyList<RuneMatchGroup> groups)
        {
            var anchors = new Dictionary<BoardPoint, RuneType>();
            foreach (var group in groups)
            {
                if (group.Tier != RuneMatchTier.Match5)
                {
                    continue;
                }

                var anchor = group.Cells
                    .OrderBy(point => point.Row)
                    .ThenBy(point => point.Column)
                    .First();
                anchors[anchor] = group.Rune;
            }

            return anchors;
        }

        private static IEnumerable<BoardPoint> OrthogonalNeighbors(BoardPoint point)
        {
            yield return new BoardPoint(point.Row - 1, point.Column);
            yield return new BoardPoint(point.Row + 1, point.Column);
            yield return new BoardPoint(point.Row, point.Column - 1);
            yield return new BoardPoint(point.Row, point.Column + 1);
        }

        private static bool IsBentShape(IReadOnlyCollection<BoardPoint> cells)
        {
            // A straight horizontal match spans a single row; a straight vertical match spans a
            // single column. A bent T/L match spans more than one row and more than one column.
            var distinctRows = cells.Select(point => point.Row).Distinct().Count();
            var distinctColumns = cells.Select(point => point.Column).Distinct().Count();
            return distinctRows > 1 && distinctColumns > 1;
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

    public readonly struct RuneCell : IEquatable<RuneCell>
    {
        public RuneCell(RuneType rune, bool isGreatRune = false)
        {
            Rune = rune;
            IsGreatRune = isGreatRune;
        }

        public RuneType Rune { get; }
        public bool IsGreatRune { get; }

        public bool Equals(RuneCell other)
        {
            return Rune == other.Rune && IsGreatRune == other.IsGreatRune;
        }

        public override bool Equals(object? obj)
        {
            return obj is RuneCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Rune * 397) ^ IsGreatRune.GetHashCode();
            }
        }

        public static bool operator ==(RuneCell left, RuneCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuneCell left, RuneCell right)
        {
            return !left.Equals(right);
        }
    }
}
