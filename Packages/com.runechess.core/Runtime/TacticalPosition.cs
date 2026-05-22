namespace RuneChess.Core;

public readonly record struct TacticalPosition(int Row, int Column)
{
    public bool IsPlayerSide => Row >= 2;
}
