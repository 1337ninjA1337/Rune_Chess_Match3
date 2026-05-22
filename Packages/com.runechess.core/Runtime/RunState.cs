using System.Collections.Generic;

namespace RuneChess.Core;

public sealed record RunState(
    int Round,
    int RunHealth,
    int Gold,
    int Xp,
    int PlayerLevel,
    string CommanderId,
    IReadOnlyList<string> Team,
    IReadOnlyList<string> Bench,
    IReadOnlyList<string> Shop,
    IReadOnlyList<string> Artifacts,
    RunPhase Phase
)
{
    public static RunState NewRun(string commanderId = "stone_oath")
    {
        return new RunState(
            Round: 1,
            RunHealth: 100,
            Gold: 6,
            Xp: 0,
            PlayerLevel: 1,
            CommanderId: commanderId,
            Team: new List<string>(),
            Bench: new List<string>(),
            Shop: new List<string> { "iron_guard", "oath_archer", "field_medic" },
            Artifacts: new List<string>(),
            Phase: RunPhase.Preparation
        );
    }

    public RunState StartCombat()
    {
        return this with { Phase = RunPhase.Combat };
    }

    public RunState ClaimReward(int goldReward)
    {
        return this with
        {
            Gold = Gold + goldReward,
            Phase = Round >= 10 ? RunPhase.Victory : RunPhase.Reward
        };
    }

    public RunState AdvanceRound()
    {
        return this with
        {
            Round = Round + 1,
            Phase = RunPhase.Preparation
        };
    }
}
