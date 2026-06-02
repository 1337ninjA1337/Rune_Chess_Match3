namespace RuneChess.Core
{
    public sealed record CommanderState(
        string Id,
        string Name,
        int Energy,
        int MaxEnergy
    );
}
