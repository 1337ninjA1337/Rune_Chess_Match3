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
Require(TacticalField.Mvp.Columns == 6, "MVP tactical field has six columns");
Require(TacticalField.Mvp.Rows == 4, "MVP tactical field has four rows");
Require(TacticalField.Mvp.CellCount == 24, "MVP tactical field has 24 cells");
Require(TacticalField.Mvp.Contains(new TacticalPosition(3, 5)), "MVP tactical field includes its last cell");
Require(!TacticalField.Mvp.Contains(new TacticalPosition(4, 0)), "MVP tactical field rejects rows outside the board");
Require(TacticalField.Mvp.CreateCells().Count == TacticalField.Mvp.CellCount, "MVP tactical field enumerates every cell");
Require(TacticalField.Mvp.GetSide(new TacticalPosition(0, 0)) == TacticalSide.Enemy, "top tactical rows belong to the enemy side");
Require(TacticalField.Mvp.GetSide(new TacticalPosition(2, 0)) == TacticalSide.Player, "bottom tactical rows belong to the player side");
Require(new TacticalPosition(1, 5).IsEnemySide, "tactical position exposes enemy side");
Require(new TacticalPosition(3, 5).IsPlayerSide, "tactical position exposes player side");
Require(TacticalField.Mvp.CreateCells(TacticalSide.Enemy).Count == 12, "enemy side contains half the MVP field");
Require(TacticalField.Mvp.CreateCells(TacticalSide.Player).Count == 12, "player side contains half the MVP field");
RequireThrows(() => TacticalField.Mvp.GetSide(new TacticalPosition(-1, 0)), "tactical side rejects positions outside the field");
Require(TacticalField.Mvp.GetLine(new TacticalPosition(1, 0)) == TacticalLine.Frontline, "enemy row closest to player is frontline");
Require(TacticalField.Mvp.GetLine(new TacticalPosition(0, 0)) == TacticalLine.Backline, "enemy row farthest from player is backline");
Require(TacticalField.Mvp.GetLine(new TacticalPosition(2, 0)) == TacticalLine.Frontline, "player row closest to enemy is frontline");
Require(TacticalField.Mvp.GetLine(new TacticalPosition(3, 0)) == TacticalLine.Backline, "player row farthest from enemy is backline");
Require(new TacticalPosition(2, 5).IsFrontline, "tactical position exposes frontline");
Require(new TacticalPosition(3, 5).IsBackline, "tactical position exposes backline");
RequireThrows(() => TacticalField.Mvp.GetLine(new TacticalPosition(4, 0)), "tactical line rejects positions outside the field");
Require(Enum.IsDefined(typeof(TacticalCellState), TacticalCellState.Free), "tactical cells support a free state");
Require(Enum.IsDefined(typeof(TacticalCellState), TacticalCellState.OccupiedAlly), "tactical cells support an ally-occupied state");
Require(Enum.IsDefined(typeof(TacticalCellState), TacticalCellState.OccupiedEnemy), "tactical cells support an enemy-occupied state");
Require(Enum.IsDefined(typeof(TacticalCellState), TacticalCellState.AvailableForPlacement), "tactical cells support placement availability");
Require(Enum.IsDefined(typeof(TacticalCellState), TacticalCellState.Unavailable), "tactical cells support unavailable state");

var boughtHeroId = afterBuy.Bench[0].InstanceId;
var afterPlace = afterBuy.PlaceHeroFromBench(boughtHeroId, new TacticalPosition(2, 1));
Require(afterPlace.Bench.Count == 0, "placing removes hero from bench");
Require(afterPlace.Team.Count == 1, "placing adds hero to team");
var afterMoveToBench = afterPlace.MoveHeroToBench(boughtHeroId);
Require(afterMoveToBench.Team.Count == 0, "moving to bench removes hero from team");
Require(afterMoveToBench.Bench.Count == 1, "moving to bench restores hero to bench");
var afterRePlace = afterMoveToBench.PlaceHeroFromBench(boughtHeroId, new TacticalPosition(3, 1));
Require(afterRePlace.Bench.Count == 0, "re-placing removes hero from bench again");
Require(afterRePlace.Team.Count == 1, "re-placing returns hero to field");
RequireThrows(
    () => afterBuy.PlaceHeroFromBench(boughtHeroId, new TacticalPosition(4, 0)),
    "placing rejects positions outside the MVP tactical field"
);
RequireThrows(
    () => afterBuy.PlaceHeroFromBench(boughtHeroId, new TacticalPosition(1, 0)),
    "placing rejects the enemy side during preparation"
);
RequireThrows(() => afterPlace.StartCombat().MoveHeroToBench(boughtHeroId), "moving to bench is blocked during combat");

var twoHeroBench = state.BuyHero(0).BuyHero(0);
var firstLimitedHeroId = twoHeroBench.Bench[0].InstanceId;
var secondLimitedHeroId = twoHeroBench.Bench[1].InstanceId;
var fieldAtLevelCap = twoHeroBench.PlaceHeroFromBench(firstLimitedHeroId, new TacticalPosition(2, 2));
RequireThrows(
    () => fieldAtLevelCap.PlaceHeroFromBench(secondLimitedHeroId, new TacticalPosition(3, 2)),
    "player level limits the number of fielded heroes"
);

RequireThrows(() => state.StartCombat(), "combat cannot start before placement");

var afterXp = afterPlace.BuyXp();
Require(afterXp.Gold == 1, "buying XP spends configured gold");
Require(afterXp.Xp == 4, "buying XP adds configured XP");

var inCombat = afterXp.StartCombat(1337);
Require(inCombat.Phase == RunPhase.Combat, "start combat changes phase");
Require(inCombat.Combat is not null, "start combat creates a combat state");
var combat = inCombat.Combat ?? throw new InvalidOperationException("Smoke check failed: combat state missing");
Require(combat.RuneBoard is not null, "start combat creates a match-3 board");
Require(combat.DurationSeconds == CombatState.DefaultDurationSeconds, "start combat creates the default combat timer");
Require(combat.RemainingSeconds == CombatState.DefaultDurationSeconds, "new combat starts with full timer remaining");

var timedCombat = afterXp.StartCombat(1337, 45);
Require(timedCombat.Combat?.DurationSeconds == 45, "combat can start with a custom timer");
var tickedCombat = timedCombat.ResolveCombatTick(10);
Require(tickedCombat.Phase == RunPhase.Combat, "non-terminal combat tick stays in combat");
Require(tickedCombat.Combat?.ElapsedSeconds == 10, "combat tick advances elapsed time");
Require(tickedCombat.Combat?.RemainingSeconds == 35, "combat tick updates remaining time");
RequireThrows(() => timedCombat.ResolveCombatTick(-1), "combat tick rejects negative elapsed time");
RequireThrows(() => afterXp.StartCombat(1337, 0), "combat timer must be positive");

var legalSwap = FindFirstLegalSwap(combat.RuneBoard);
var afterRuneSwap = inCombat.SwapRunes(legalSwap.From, legalSwap.To);
Require(afterRuneSwap.Phase == RunPhase.Combat, "rune swaps keep the run in combat");
Require(afterRuneSwap.Combat is not null, "rune swaps keep combat state");
var swappedCombat = afterRuneSwap.Combat ?? throw new InvalidOperationException("Smoke check failed: swapped combat state missing");
Require(swappedCombat.Match3MovesUsed == 1, "rune swap counts as a match-3 combat move");
Require(swappedCombat.LastMatchedRunesCount >= 3, "rune swap records matched runes");
Require(swappedCombat.LastMatchPower == swappedCombat.LastMatchedRunesCount, "first swap uses matchPower = matchedRunesCount + comboDepth");

var progressStore = new RunProgressStore();
Require(!progressStore.HasSavedRun, "new progress store starts empty");
Require(!progressStore.TryLoad(out var emptyProgress), "empty progress store reports no saved run");
Require(emptyProgress.Round == 1, "empty progress store provides a safe new run fallback");

var combatProgress = afterRuneSwap.ResolveCombatTick(12);
progressStore.Save(combatProgress);
Require(progressStore.HasSavedRun, "progress store reports saved run after save");
Require(progressStore.Snapshot?.Version == RunProgressSnapshot.CurrentVersion, "progress snapshot records its version");
Require(progressStore.TryLoad(out var restoredProgress), "progress store restores saved run");
Require(restoredProgress.Phase == RunPhase.Combat, "restored progress preserves combat phase");
Require(restoredProgress.Gold == combatProgress.Gold, "restored progress preserves economy");
Require(restoredProgress.Team.Count == combatProgress.Team.Count, "restored progress preserves team");
var restoredCombat = restoredProgress.Combat ?? throw new InvalidOperationException("Smoke check failed: restored combat missing");
var savedCombat = combatProgress.Combat ?? throw new InvalidOperationException("Smoke check failed: saved combat missing");
Require(restoredCombat.ElapsedSeconds == savedCombat.ElapsedSeconds, "restored progress preserves combat timer");
Require(restoredCombat.Match3MovesUsed == savedCombat.Match3MovesUsed, "restored progress preserves match-3 move count");
Require(restoredCombat.RuneBoard[0, 0] == savedCombat.RuneBoard[0, 0], "restored progress preserves rune board");
var unsupportedSnapshot = progressStore.Snapshot ?? throw new InvalidOperationException("Smoke check failed: snapshot missing");
RequireThrows(() => (unsupportedSnapshot with { Version = 0 }).Restore(), "unsupported progress version is rejected");
progressStore.Clear();
Require(!progressStore.HasSavedRun, "progress store clears saved run");

var reward = afterRuneSwap.ClaimReward(2);
Require(reward.Phase == RunPhase.Reward, "claiming reward exits combat into reward phase");
Require(reward.Combat is null, "claiming reward clears combat state");
Require(reward.Gold == 3, "claiming reward adds gold");

var nextRound = reward.AdvanceRound("round_02_scouts");
Require(nextRound.Round == 2, "advancing reward starts the next round");
Require(nextRound.Phase == RunPhase.Preparation, "advancing reward returns to preparation");
Require(nextRound.Shop.Offers.Count == 3, "next round refreshes the shop");
Require(nextRound.NextEnemyId == "round_02_scouts", "next round updates the enemy preview");
Require(PveRunSchedule.Rounds.Count == 10, "MVP PvE schedule has 10 rounds");
Require(PveRunSchedule.GetRound(1).EnemyId == state.NextEnemyId, "new run uses the first scheduled enemy");
RequireThrows(() => PveRunSchedule.GetRound(11), "round 11 is outside the MVP schedule");

var scheduledRun = RunState.NewRun()
    .BuyHero(0)
    .PlaceHeroFromBench("iron_guard_1_1", new TacticalPosition(2, 1));

for (var expectedRound = 1; expectedRound < PveRunSchedule.FinalRound; expectedRound += 1)
{
    var definition = PveRunSchedule.GetRound(expectedRound);
    Require(scheduledRun.Round == expectedRound, "scheduled run is on the expected round");
    Require(scheduledRun.NextEnemyId == definition.EnemyId, "scheduled run exposes the current enemy preview");

    var combatRound = scheduledRun.StartCombat();
    Require(combatRound.Combat is not null, "scheduled round starts combat");

    var rewardedRound = combatRound.ClaimReward();
    Require(rewardedRound.Phase == RunPhase.Reward, "non-final scheduled rounds enter reward phase");
    Require(rewardedRound.Gold == scheduledRun.Gold + definition.BaseGoldReward, "scheduled reward grants round gold");

    scheduledRun = rewardedRound.AdvanceRound();
    var nextDefinition = PveRunSchedule.GetRound(expectedRound + 1);
    Require(scheduledRun.Round == expectedRound + 1, "scheduled run advances by one round");
    Require(scheduledRun.Phase == RunPhase.Preparation, "scheduled run returns to preparation between rounds");
    Require(scheduledRun.NextEnemyId == nextDefinition.EnemyId, "scheduled run previews the next enemy");
}

var finalCombat = scheduledRun.StartCombat();
var finalReward = finalCombat.ClaimReward();
Require(finalReward.Round == PveRunSchedule.FinalRound, "final reward stays on round 10");
Require(finalReward.Phase == RunPhase.Victory, "final scheduled reward ends the MVP run in victory");
Require(finalReward.IsFinalRound, "final reward is marked as the final round");
Require(finalReward.IsRunWon, "final reward exposes a run win flag");
Require(finalReward.IsRunComplete, "victory marks the run complete");

var healthDefeat = RunState.NewRun().ApplyRunDamage(100);
Require(healthDefeat.Phase == RunPhase.Defeat, "run health depletion causes defeat");
Require(healthDefeat.IsRunLost, "health defeat exposes a run loss flag");
Require(healthDefeat.IsRunComplete, "health defeat marks the run complete");
Require(healthDefeat.DefeatReason == "run_health_depleted", "health defeat records a reason");

var combatDefeat = afterPlace.StartCombat().ResolveCombatDefeat("all_allies_defeated");
Require(combatDefeat.Phase == RunPhase.Defeat, "combat condition failure causes defeat");
Require(combatDefeat.Combat is null, "combat defeat clears combat state");
Require(combatDefeat.IsRunLost, "combat defeat exposes a run loss flag");
Require(combatDefeat.DefeatReason == "all_allies_defeated", "combat defeat records a reason");
RequireThrows(() => afterPlace.ResolveCombatDefeat(), "combat defeat cannot be resolved outside combat");
RequireThrows(() => afterPlace.StartCombat().ResolveCombatDefeat(" "), "combat defeat requires a reason");

var defeatedByAllies = afterPlace.StartCombat(1337).ResolveCombatTick(1, allAlliesDefeated: true);
Require(defeatedByAllies.Phase == RunPhase.Defeat, "all allies defeated resolves combat defeat");
Require(defeatedByAllies.DefeatReason == "all_allies_defeated", "allies defeat records a reason");

var rewardedByEnemies = afterPlace.StartCombat(1337).ResolveCombatTick(1, allEnemiesDefeated: true, goldReward: 1);
Require(rewardedByEnemies.Phase == RunPhase.Reward, "all enemies defeated resolves combat victory");
Require(rewardedByEnemies.Gold == afterPlace.Gold + 1, "combat victory tick grants reward");

var timerDefeat = afterPlace.StartCombat(1337, 5)
    .ResolveCombatTick(5, playerHealthPercent: 40, enemyHealthPercent: 60);
Require(timerDefeat.Phase == RunPhase.Defeat, "expired timer with enemy health advantage causes defeat");
Require(timerDefeat.DefeatReason == "timer_enemy_health_advantage", "timer defeat records a reason");

var timerVictory = afterPlace.StartCombat(1337, 5)
    .ResolveCombatTick(5, playerHealthPercent: 60, enemyHealthPercent: 40, goldReward: 2);
Require(timerVictory.Phase == RunPhase.Reward, "expired timer without enemy health advantage grants victory");
Require(timerVictory.Gold == afterPlace.Gold + 2, "timer victory grants reward");
RequireThrows(() => afterPlace.StartCombat(1337).ResolveCombatTick(1, playerHealthPercent: 101), "combat tick validates health percent");

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
    catch (ArgumentException)
    {
        return;
    }

    throw new InvalidOperationException($"Smoke check failed: {message}");
}
