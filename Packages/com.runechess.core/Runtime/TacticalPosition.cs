namespace RuneChess.Core;

public readonly record struct TacticalPosition(int Row, int Column)
{
    public bool IsInsideMvpField => TacticalField.Mvp.Contains(this);
    public bool IsEnemySide => TacticalField.Mvp.IsEnemySide(this);
    public bool IsPlayerSide => TacticalField.Mvp.IsPlayerSide(this);
    public bool IsFrontline => TacticalField.Mvp.IsFrontline(this);
    public bool IsBackline => TacticalField.Mvp.IsBackline(this);
}
