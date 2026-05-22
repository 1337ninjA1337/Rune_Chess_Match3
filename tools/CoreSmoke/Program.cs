using RuneChess.Core;

var state = RunState.NewRun();
Require(state.Round == 1, "new run starts at round 1");
Require(state.Phase == RunPhase.Preparation, "new run starts in preparation");
Require(state.StartCombat().Phase == RunPhase.Combat, "start combat changes phase");

var board = Match3Board.CreateDeterministic(1337);
Require(Match3Board.AreAdjacent(new BoardPoint(0, 0), new BoardPoint(0, 1)), "horizontal neighbors are adjacent");
Require(!Match3Board.AreAdjacent(new BoardPoint(0, 0), new BoardPoint(1, 1)), "diagonal cells are not adjacent");
Require(board.FindMatches() is not null, "match scan returns a set");

Console.WriteLine("Core smoke checks passed.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Smoke check failed: {message}");
    }
}
