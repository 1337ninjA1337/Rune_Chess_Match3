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

RequireThrows(() => state.StartCombat(), "combat cannot start before placement");

var afterXp = afterPlace.BuyXp();
Require(afterXp.Gold == 1, "buying XP spends configured gold");
Require(afterXp.Xp == 4, "buying XP adds configured XP");

var inCombat = afterXp.StartCombat(1337);
Require(inCombat.Phase == RunPhase.Combat, "start combat changes phase");
Require(inCombat.Combat is not null, "start combat creates a combat state");
var combat = inCombat.Combat ?? throw new InvalidOperationException("Smoke check failed: combat state missing");
Require(combat.RuneBoard is not null, "start combat creates a match-3 board");

var legalSwap = FindFirstLegalSwap(combat.RuneBoard);
var afterRuneSwap = inCombat.SwapRunes(legalSwap.From, legalSwap.To);
Require(afterRuneSwap.Phase == RunPhase.Combat, "rune swaps keep the run in combat");
Require(afterRuneSwap.Combat is not null, "rune swaps keep combat state");
var swappedCombat = afterRuneSwap.Combat ?? throw new InvalidOperationException("Smoke check failed: swapped combat state missing");
Require(swappedCombat.Match3MovesUsed == 1, "rune swap counts as a match-3 combat move");
Require(swappedCombat.LastMatchedRunesCount >= 3, "rune swap records matched runes");
Require(swappedCombat.LastMatchPower == swappedCombat.LastMatchedRunesCount, "first swap uses matchPower = matchedRunesCount + comboDepth");

var reward = afterRuneSwap.ClaimReward(2);
Require(reward.Phase == RunPhase.Reward, "claiming reward exits combat into reward phase");
Require(reward.Combat is null, "claiming reward clears combat state");
Require(reward.Gold == 3, "claiming reward adds gold");

var nextRound = reward.AdvanceRound("round_02_scouts");
Require(nextRound.Round == 2, "advancing reward starts the next round");
Require(nextRound.Phase == RunPhase.Preparation, "advancing reward returns to preparation");
Require(nextRound.Shop.Offers.Count == 3, "next round refreshes the shop");
Require(nextRound.NextEnemyId == "round_02_scouts", "next round updates the enemy preview");

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

static (BoardPoint From, BoardPoint To) FindFirstLegalSwap(Match3Board board)
{
    for (var row = 0; row < Match3Board.Rows; row += 1)
    {
        for (var column = 0; column < Match3Board.Columns; column += 1)
        {
            var current = new BoardPoint(row, column);
            if (column + 1 < Match3Board.Columns)
            {
                var right = new BoardPoint(row, column + 1);
                if (board.IsLegalSwap(current, right))
                {
                    return (current, right);
                }
            }

            if (row + 1 < Match3Board.Rows)
            {
                var down = new BoardPoint(row + 1, column);
                if (board.IsLegalSwap(current, down))
                {
                    return (current, down);
                }
            }
        }
    }

    throw new InvalidOperationException("Smoke board does not contain a legal swap.");
}

static void RequireThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException($"Smoke check failed: {message}");
}
