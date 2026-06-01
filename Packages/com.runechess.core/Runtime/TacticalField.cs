using System.Collections.Generic;

namespace RuneChess.Core
{
    public sealed record TacticalField(int Columns, int Rows)
    {
        public const int MvpColumns = 6;
        public const int MvpRows = 4;

        public static TacticalField Mvp { get; } = new(MvpColumns, MvpRows);

        public int CellCount => Columns * Rows;
        public int HalfRows => Rows / 2;

        public bool Contains(TacticalPosition position)
        {
            return position.Row >= 0
                && position.Row < Rows
                && position.Column >= 0
                && position.Column < Columns;
        }

        public IReadOnlyList<TacticalPosition> CreateCells()
        {
            var cells = new List<TacticalPosition>(CellCount);
            for (var row = 0; row < Rows; row += 1)
            {
                for (var column = 0; column < Columns; column += 1)
                {
                    cells.Add(new TacticalPosition(row, column));
                }
            }

            return cells;
        }

        public IReadOnlyList<TacticalPosition> CreateCells(TacticalSide side)
        {
            var cells = new List<TacticalPosition>(CellCount / 2);
            foreach (var position in CreateCells())
            {
                if (GetSide(position) == side)
                {
                    cells.Add(position);
                }
            }

            return cells;
        }

        public TacticalSide GetSide(TacticalPosition position)
        {
            if (!Contains(position))
            {
                throw new System.ArgumentOutOfRangeException(nameof(position), "Tactical position is outside the field.");
            }

            return position.Row < HalfRows ? TacticalSide.Enemy : TacticalSide.Player;
        }

        public bool IsEnemySide(TacticalPosition position)
        {
            return Contains(position) && GetSide(position) == TacticalSide.Enemy;
        }

        public bool IsPlayerSide(TacticalPosition position)
        {
            return Contains(position) && GetSide(position) == TacticalSide.Player;
        }

        public TacticalLine GetLine(TacticalPosition position)
        {
            var side = GetSide(position);
            var frontRow = side == TacticalSide.Enemy ? HalfRows - 1 : HalfRows;

            return position.Row == frontRow ? TacticalLine.Frontline : TacticalLine.Backline;
        }

        public bool IsFrontline(TacticalPosition position)
        {
            return Contains(position) && GetLine(position) == TacticalLine.Frontline;
        }

        public bool IsBackline(TacticalPosition position)
        {
            return Contains(position) && GetLine(position) == TacticalLine.Backline;
        }
    }
}
