using System.Collections.Generic;

namespace RuneChess.Core;

public sealed record TacticalField(int Columns, int Rows)
{
    public const int MvpColumns = 6;
    public const int MvpRows = 4;

    public static TacticalField Mvp { get; } = new(MvpColumns, MvpRows);

    public int CellCount => Columns * Rows;

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
}
