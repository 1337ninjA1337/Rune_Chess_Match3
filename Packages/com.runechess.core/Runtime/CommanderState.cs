namespace RuneChess.Core
{
    public sealed record CommanderState(
        string Id,
        string Name,
        int Energy,
        int MaxEnergy
    )
    {
        public static CommanderState StoneOath { get; } = new(
            Id: "stone_oath",
            Name: "Stone Oath",
            Energy: 0,
            MaxEnergy: 100
        );
    }
}
