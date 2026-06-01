namespace RuneChess.Core
{
    public readonly struct TacticalPosition : System.IEquatable<TacticalPosition>
    {
        public TacticalPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        public bool IsInsideMvpField => TacticalField.Mvp.Contains(this);
        public bool IsEnemySide => TacticalField.Mvp.IsEnemySide(this);
        public bool IsPlayerSide => TacticalField.Mvp.IsPlayerSide(this);
        public bool IsFrontline => TacticalField.Mvp.IsFrontline(this);
        public bool IsBackline => TacticalField.Mvp.IsBackline(this);

        public void Deconstruct(out int row, out int column)
        {
            row = Row;
            column = Column;
        }

        public bool Equals(TacticalPosition other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object? obj)
        {
            return obj is TacticalPosition other && Equals(other);
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
            return $"{nameof(TacticalPosition)} {{ {nameof(Row)} = {Row}, {nameof(Column)} = {Column} }}";
        }

        public static bool operator ==(TacticalPosition left, TacticalPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TacticalPosition left, TacticalPosition right)
        {
            return !left.Equals(right);
        }
    }
}
