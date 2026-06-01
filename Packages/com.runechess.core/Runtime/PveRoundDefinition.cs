namespace RuneChess.Core
{
    public sealed record PveRoundDefinition(
        int Round,
        string EnemyId,
        int CombatRuneSeed,
        int BaseGoldReward
    );
}
