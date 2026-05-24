namespace RuneChess.Core;

public readonly record struct TacticalPosition(int Row, int Column)
{
    public bool IsInsideMvpField => TacticalField.Mvp.Contains(this);
    public bool IsPlayerSide => IsInsideMvpField && Row >= TacticalField.Mvp.Rows / 2;
}
