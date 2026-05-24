namespace RuneChess.Core;

public readonly record struct TacticalPosition(int Row, int Column)
{
    public bool IsInsideMvpField => TacticalField.Mvp.Contains(this);
    public bool IsEnemySide => TacticalField.Mvp.IsEnemySide(this);
    public bool IsPlayerSide => TacticalField.Mvp.IsPlayerSide(this);
}
