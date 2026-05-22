using RuneChess.Core;

var state = RunState.NewRun();
Require(state.Round == 1, "new run starts at round 1");
Require(state.Phase == RunPhase.Preparation, "new run starts in preparation");
Require(state.RunHealth == 100, "new run starts with full run health");
Require(state.Gold == 6, "new run starts with configured gold");
Require(state.Xp == 0, "new run starts with zero XP");
Require(state.PlayerLevel == 1, "new run starts at player level 1");
Require(state.Commander.Id == "stone_oath", "new run has a selected commander");
Require(state.Team.Count == 0, "new run starts with empty team");
Require(state.Bench.Count == 0, "new run starts with empty bench");
Require(state.Shop.Offers.Count == 3, "new run starts with a shop");
Require(state.Artifacts.Count == 0, "new run starts without artifacts");

var afterBuy = state.BuyHero(0);
Require(afterBuy.Gold == 5, "buying a common hero spends gold");
Require(afterBuy.Bench.Count == 1, "bought hero goes to bench");
Require(afterBuy.Shop.Offers.Count == 2, "bought shop offer is removed");

var boughtHeroId = afterBuy.Bench[0].InstanceId;
var afterPlace = afterBuy.PlaceHeroFromBench(boughtHeroId, new TacticalPosition(2, 1));
Require(afterPlace.Bench.Count == 0, "placing removes hero from bench");
Require(afterPlace.Team.Count == 1, "placing adds hero to team");

var afterXp = afterPlace.BuyXp();
Require(afterXp.Gold == 1, "buying XP spends configured gold");
Require(afterXp.Xp == 4, "buying XP adds configured XP");
Require(afterXp.StartCombat().Phase == RunPhase.Combat, "start combat changes phase");

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
