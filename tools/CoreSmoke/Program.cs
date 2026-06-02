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

var mergeBench = state with
{
    Bench = new List<HeroInstance>
    {
        new("merge_ig_1", "iron_guard", 1),
        new("merge_ig_2", "iron_guard", 1),
        new("merge_ig_3", "iron_guard", 1)
    }
};
var mergedBench = mergeBench.MergeOneStarHeroes("iron_guard");
Require(mergedBench.Bench.Count == 1, "merging three one-star bench copies leaves one hero");
Require(mergedBench.Bench[0].InstanceId == "merge_ig_1", "bench merge preserves the survivor instance id");
Require(mergedBench.Bench[0].HeroId == "iron_guard" && mergedBench.Bench[0].Stars == 2, "bench merge creates a two-star hero");
RequireThrows(() => mergeBench.MergeOneStarHeroes("oath_archer"), "one-star merge requires three matching copies");
RequireThrows(() => (mergeBench with { Phase = RunPhase.Combat }).MergeOneStarHeroes("iron_guard"), "hero merging is blocked outside preparation");

var earlyMergeBench = state with
{
    Bench = new List<HeroInstance>
    {
        new("early_merge_ig_1", "iron_guard", 1),
        new("early_merge_ig_2", "iron_guard", 1)
    }
};
var earlyMergedBench = earlyMergeBench.MergeOneStarHeroes("iron_guard", copiesRequired: 2);
Require(earlyMergedBench.Bench.Count == 1, "early test merge can use two one-star copies");
Require(earlyMergedBench.Bench[0].InstanceId == "early_merge_ig_1" && earlyMergedBench.Bench[0].Stars == 2, "early test merge creates a two-star survivor");
RequireThrows(() => earlyMergeBench.MergeOneStarHeroes("iron_guard"), "default one-star merge still requires three copies");
RequireThrows(() => earlyMergeBench.MergeOneStarHeroes("iron_guard", copiesRequired: 1), "early merge copy count validates its lower bound");

var mergeWithField = state with
{
    Team = new List<BoardHero>
    {
        new(new HeroInstance("field_ig_1", "iron_guard", 1), new TacticalPosition(2, 0))
    },
    Bench = new List<HeroInstance>
    {
        new("field_merge_ig_2", "iron_guard", 1),
        new("field_merge_ig_3", "iron_guard", 1)
    }
};
var mergedField = mergeWithField.MergeOneStarHeroes("iron_guard");
Require(mergedField.Team.Count == 1 && mergedField.Bench.Count == 0, "field merge keeps the upgraded hero on the board");
Require(mergedField.Team[0].Hero.InstanceId == "field_ig_1" && mergedField.Team[0].Hero.Stars == 2, "field merge upgrades the placed survivor");

var twoStarMergeBench = state with
{
    Bench = new List<HeroInstance>
    {
        new("merge_2s_ig_1", "iron_guard", 2),
        new("merge_2s_ig_2", "iron_guard", 2),
        new("merge_2s_ig_3", "iron_guard", 2)
    }
};
var mergedThreeStar = twoStarMergeBench.MergeTwoStarHeroes("iron_guard");
Require(mergedThreeStar.Bench.Count == 1, "merging three two-star bench copies leaves one hero");
Require(mergedThreeStar.Bench[0].InstanceId == "merge_2s_ig_1", "two-star merge preserves the survivor instance id");
Require(mergedThreeStar.Bench[0].HeroId == "iron_guard" && mergedThreeStar.Bench[0].Stars == 3, "two-star merge creates a three-star hero");
RequireThrows(() => twoStarMergeBench.MergeTwoStarHeroes("oath_archer"), "two-star merge requires three matching copies");

var sellBenchState = state with
{
    Gold = 2,
    Bench = new List<HeroInstance>
    {
        new("sell_bench_ig", "iron_guard", 1)
    }
};
var soldBenchHero = sellBenchState.SellHero("sell_bench_ig", baseCost: 1);
Require(soldBenchHero.Gold == 3 && soldBenchHero.Bench.Count == 0, "selling a one-star bench hero refunds its base cost");
RequireThrows(() => sellBenchState.SellHero("missing_hero", baseCost: 1), "selling requires an existing hero instance");
RequireThrows(() => sellBenchState.SellHero("sell_bench_ig", baseCost: 0), "selling validates the hero base cost");
RequireThrows(() => (sellBenchState with { Phase = RunPhase.Combat }).SellHero("sell_bench_ig", baseCost: 1), "selling is blocked outside preparation");

var sellFieldState = state with
{
    Gold = 4,
    Team = new List<BoardHero>
    {
        new(new HeroInstance("sell_field_ig", "iron_guard", 2), new TacticalPosition(2, 0))
    }
};
var soldFieldHero = sellFieldState.SellHero("sell_field_ig", baseCost: 2);
Require(soldFieldHero.Gold == 10 && soldFieldHero.Team.Count == 0, "selling a two-star field hero refunds three base copies");
Require(HeroEconomy.CalculateSellValue(baseCost: 1, stars: 3) == 9, "three-star sell value counts nine base copies");

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
Require(combat.GlobalCooldownMillisecondsRemaining == 0, "new combat starts without match-3 cooldown");
Require(!combat.IsSwapOnCooldown, "new combat allows an immediate rune swap");
Require(combat.SecondsSinceLastRuneSwap == 0, "new combat starts with a fresh rune-swap idle timer");
Require(combat.SlowdownMillisecondsRemaining == 0, "new combat starts without slowdown");
Require(!combat.EarnedChainFourGoldBonus, "new combat starts without a chain 4+ gold bonus");
Require(!combat.IsCombatSlowed, "new combat runs at normal speed");
Require(combat.CombatSpeedPercent == CombatState.NormalCombatSpeedPercent, "normal combat speed is 100 percent");
Require(!combat.ShouldShowMatchHint, "new combat does not show a match hint immediately");
Require(combat.CurrentMatchHint is null, "match hint is hidden before the idle delay");
var hintReadyCombat = combat.AdvanceTimer(CombatState.MatchHintDelaySeconds);
Require(hintReadyCombat.SecondsSinceLastRuneSwap == CombatState.MatchHintDelaySeconds, "combat tracks rune-swap idle seconds");
Require(hintReadyCombat.ShouldShowMatchHint, "combat shows a match hint after eight idle seconds");
var idleHint = hintReadyCombat.CurrentMatchHint ?? throw new InvalidOperationException("Smoke check failed: idle hint missing");
Require(combat.RuneBoard.TryCreateMoveHint(idleHint.From, idleHint.To, out _), "idle hint points to a legal match move");

var timedCombat = afterXp.StartCombat(1337, 45);
Require(timedCombat.Combat?.DurationSeconds == 45, "combat can start with a custom timer");
var tickedCombat = timedCombat.ResolveCombatTick(10);
Require(tickedCombat.Phase == RunPhase.Combat, "non-terminal combat tick stays in combat");
Require(tickedCombat.Combat?.ElapsedSeconds == 10, "combat tick advances elapsed time");
Require(tickedCombat.Combat?.RemainingSeconds == 35, "combat tick updates remaining time");
RequireThrows(() => timedCombat.ResolveCombatTick(-1), "combat tick rejects negative elapsed time");
RequireThrows(() => combat.AdvanceCooldownMilliseconds(-1), "cooldown tick rejects negative elapsed milliseconds");
RequireThrows(() => afterXp.StartCombat(1337, 0), "combat timer must be positive");

var legalSwap = FindFirstLegalSwap(combat.RuneBoard);
var afterRuneSwap = inCombat.SwapRunes(legalSwap.From, legalSwap.To);
Require(afterRuneSwap.Phase == RunPhase.Combat, "rune swaps keep the run in combat");
Require(afterRuneSwap.Combat is not null, "rune swaps keep combat state");
var swappedCombat = afterRuneSwap.Combat ?? throw new InvalidOperationException("Smoke check failed: swapped combat state missing");
Require(swappedCombat.Match3MovesUsed == 1, "rune swap counts as a match-3 combat move");
Require(swappedCombat.LastMatchedRunesCount >= 3, "rune swap records matched runes");
Require(swappedCombat.LastMatchPower >= swappedCombat.LastMatchedRunesCount, "rune swap records matchPower from matched runes and combo depth");
Require(swappedCombat.RuneBoard.FindMatches().Count == 0, "rune swap resolves matches and chains before combat continues");
Require(swappedCombat.GlobalCooldownMillisecondsRemaining == CombatState.SwapGlobalCooldownMilliseconds, "rune swap starts the global cooldown");
Require(swappedCombat.IsSwapOnCooldown, "rune swap blocks immediate follow-up swaps");
Require(swappedCombat.SecondsSinceLastRuneSwap == 0, "rune swap resets the match hint idle timer");
Require(!swappedCombat.ShouldShowMatchHint, "rune swap hides the idle match hint");
RequireThrows(() => swappedCombat.SwapRunes(legalSwap.From, legalSwap.To), "global cooldown blocks immediate rune swaps");
var almostReadyCombat = swappedCombat.AdvanceCooldownMilliseconds(CombatState.SwapGlobalCooldownMilliseconds - 1);
Require(almostReadyCombat.IsSwapOnCooldown, "global cooldown remains active before 0.25 seconds pass");
var readyCombat = almostReadyCombat.AdvanceCooldownMilliseconds(1);
Require(!readyCombat.IsSwapOnCooldown, "global cooldown expires after 0.25 seconds");

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
Require(restoredCombat.GlobalCooldownMillisecondsRemaining == savedCombat.GlobalCooldownMillisecondsRemaining, "restored progress preserves match-3 cooldown");
Require(restoredCombat.SecondsSinceLastRuneSwap == savedCombat.SecondsSinceLastRuneSwap, "restored progress preserves match hint idle timer");
Require(restoredCombat.SlowdownMillisecondsRemaining == savedCombat.SlowdownMillisecondsRemaining, "restored progress preserves combat slowdown");
Require(restoredCombat.EarnedChainFourGoldBonus == savedCombat.EarnedChainFourGoldBonus, "restored progress preserves chain 4+ gold bonus state");
Require(restoredCombat.RuneBoard[0, 0] == savedCombat.RuneBoard[0, 0], "restored progress preserves rune board");
var greatSnapshotPoint = new BoardPoint(2, 3);
var greatSnapshotBoard = new Match3Board(Match3Board.CreateCells()
    .Select(point => new RuneCell(savedCombat.RuneBoard[point], point == greatSnapshotPoint))
    .ToList());
var greatSnapshotCombat = savedCombat with { RuneBoard = greatSnapshotBoard };
var greatSnapshot = CombatProgressSnapshot.Capture(greatSnapshotCombat);
Require(
    greatSnapshot.GreatRuneFlags[(greatSnapshotPoint.Row * Match3Board.Columns) + greatSnapshotPoint.Column],
    "combat snapshot records great-rune cells"
);
var restoredGreatSnapshot = greatSnapshot.Restore();
Require(restoredGreatSnapshot.RuneBoard.IsGreatRune(greatSnapshotPoint), "combat snapshot restores great-rune cells");
Require(restoredGreatSnapshot.RuneBoard[greatSnapshotPoint] == savedCombat.RuneBoard[greatSnapshotPoint], "great-rune snapshot preserves rune color");
var unsupportedSnapshot = progressStore.Snapshot ?? throw new InvalidOperationException("Smoke check failed: snapshot missing");
RequireThrows(() => (unsupportedSnapshot with { Version = 0 }).Restore(), "unsupported progress version is rejected");
progressStore.Clear();
Require(!progressStore.HasSavedRun, "progress store clears saved run");

var reward = afterRuneSwap.ClaimReward(2);
Require(reward.Phase == RunPhase.Reward, "claiming reward exits combat into reward phase");
Require(reward.Combat is null, "claiming reward clears combat state");
Require(reward.Gold == 3, "claiming reward adds gold");
var chainGoldReward = (inCombat with
{
    Combat = combat with { EarnedChainFourGoldBonus = true }
}).ClaimReward(2);
Require(chainGoldReward.Gold == inCombat.Gold + 2 + CombatState.ChainFourGoldBonus, "chain 4+ grants one bonus gold after combat");
Require(chainGoldReward.Combat is null, "claiming a chain 4+ reward still clears combat state");

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
Require(RuneTypes.All.Count == 6, "rune catalog exposes six rune types");
Require(
    RuneTypes.All.SequenceEqual(new[]
    {
        RuneType.Red,
        RuneType.Blue,
        RuneType.Green,
        RuneType.Yellow,
        RuneType.Purple,
        RuneType.White
    }),
    "rune catalog keeps the canonical MVP order"
);
Require(
    string.Join(",", RuneTypes.All.Select(RuneTypes.GetId)) == "red,blue,green,yellow,purple,white",
    "rune catalog exposes canonical lowercase ids"
);
Require(RuneTypes.ParseId("red") == RuneType.Red, "red rune id parses to red");
Require(RuneTypes.ParseId("blue") == RuneType.Blue, "blue rune id parses to blue");
Require(RuneTypes.ParseId("green") == RuneType.Green, "green rune id parses to green");
Require(RuneTypes.ParseId("yellow") == RuneType.Yellow, "yellow rune id parses to yellow");
Require(RuneTypes.ParseId("purple") == RuneType.Purple, "purple rune id parses to purple");
Require(RuneTypes.ParseId("white") == RuneType.White, "white rune id parses to white");
Require(RuneTypes.TryParseId("WHITE", out var parsedWhite) && parsedWhite == RuneType.White, "rune parsing accepts case-insensitive ids");
Require(!RuneTypes.TryParseId("orange", out _), "rune parsing rejects unknown ids");
RequireThrows(() => RuneTypes.ParseId("orange"), "rune parsing throws for unknown ids");
Require(Match3Scoring.CalculateMatchPower(3, 0) == 3, "matchPower starts from matched rune count");
Require(Match3Scoring.CalculateMatchPower(5, 2) == 7, "matchPower adds combo depth");
RequireThrows(() => Match3Scoring.CalculateMatchPower(-1, 0), "matchPower rejects negative matched rune count");
RequireThrows(() => Match3Scoring.CalculateMatchPower(3, -1), "matchPower rejects negative combo depth");
Require(Match3Board.Rows == 7, "match-3 board has seven rows");
Require(Match3Board.Columns == 7, "match-3 board has seven columns");
Require(Match3Board.CellCount == 49, "match-3 board has 49 cells");
Require(Match3Board.Contains(new BoardPoint(6, 6)), "match-3 board includes its last cell");
Require(!Match3Board.Contains(new BoardPoint(7, 0)), "match-3 board rejects rows outside the board");
Require(Match3Board.CreateCells().Count == Match3Board.CellCount, "match-3 board enumerates every cell");
Require(Enum.IsDefined(typeof(RuneType), board[6, 6]), "match-3 board stores a rune in every cell");
RequireThrows(
    () => { _ = board[7, 0]; },
    "match-3 board rejects index access outside the board"
);
Require(Match3Board.AreAdjacent(new BoardPoint(0, 0), new BoardPoint(0, 1)), "horizontal neighbors are adjacent");
Require(Match3Board.AreAdjacent(new BoardPoint(0, 0), new BoardPoint(1, 0)), "vertical neighbors are adjacent");
Require(!Match3Board.AreAdjacent(new BoardPoint(0, 0), new BoardPoint(1, 1)), "diagonal cells are not adjacent");
Require(Match3Board.CanSwap(new BoardPoint(0, 0), new BoardPoint(0, 1)), "adjacent in-board runes can be swapped");
Require(!Match3Board.CanSwap(new BoardPoint(0, 0), new BoardPoint(1, 1)), "diagonal runes cannot be swapped");
Require(!Match3Board.CanSwap(new BoardPoint(-1, 0), new BoardPoint(0, 0)), "out-of-board runes cannot be swapped");
var swapFixture = new Match3Board(Match3Board.CreateCells()
    .Select((_, index) => RuneTypes.All[index % RuneTypes.All.Count])
    .ToList());
var swapA = new BoardPoint(0, 0);
var swapB = new BoardPoint(0, 1);
var swapC = new BoardPoint(0, 2);
var swappedBoard = swapFixture.Swap(swapA, swapB);
Require(swapFixture[swapA] == RuneType.Red, "swap fixture starts with red in the first cell");
Require(swapFixture[swapB] == RuneType.Blue, "swap fixture starts with blue in the second cell");
Require(swappedBoard[swapA] == RuneType.Blue, "swap moves the second rune into the first cell");
Require(swappedBoard[swapB] == RuneType.Red, "swap moves the first rune into the second cell");
Require(swappedBoard[swapC] == swapFixture[swapC], "swap keeps unrelated cells unchanged");
Require(swapFixture[swapA] == RuneType.Red, "swap does not mutate the original board");
RequireThrows(() => swapFixture.Swap(swapA, new BoardPoint(1, 1)), "swap rejects diagonal runes");
RequireThrows(() => swapFixture.Swap(swapA, swapA), "swap rejects the same rune");
RequireThrows(() => swapFixture.Swap(new BoardPoint(-1, 0), swapA), "swap rejects out-of-board runes");
var noMatchSwapBoard = CreatePatternBoard();
Require(noMatchSwapBoard.Swap(swapA, swapB).FindMatches().Count == 0, "raw adjacent swap can produce no matches");
Require(!noMatchSwapBoard.CreatesMatchAfterSwap(swapA, swapB), "match swap check rejects no-match swaps");
Require(!noMatchSwapBoard.IsLegalSwap(swapA, swapB), "legal swap rejects no-match swaps");
RequireThrows(() => noMatchSwapBoard.SwapIfCreatesMatch(swapA, swapB), "match swap command rejects no-match swaps");
var legalMatchA = new BoardPoint(0, 1);
var legalMatchB = new BoardPoint(1, 1);
var legalMatchBoard = CreatePatternBoard(
    (new BoardPoint(0, 0), RuneType.Red),
    (new BoardPoint(0, 1), RuneType.Blue),
    (new BoardPoint(0, 2), RuneType.Red),
    (new BoardPoint(1, 1), RuneType.Red)
);
Require(legalMatchBoard.FindMatches().Count == 0, "legal swap fixture starts without matches");
Require(legalMatchBoard.CreatesMatchAfterSwap(legalMatchA, legalMatchB), "match swap check accepts swaps that create a match");
Require(legalMatchBoard.IsLegalSwap(legalMatchA, legalMatchB), "legal swap accepts swaps that create a match");
var legalMatchCells = new[] { new BoardPoint(0, 0), legalMatchA, new BoardPoint(0, 2) };
Require(legalMatchBoard.TryCreateMoveHint(legalMatchA, legalMatchB, out var directHint) && directHint is not null, "move hint accepts legal match swaps");
Require(ContainsExactly(directHint.MatchedCells, legalMatchCells), "move hint reports the created match cells");
Require(directHint.HighlightedCells.Contains(legalMatchA), "move hint highlights the source cell");
Require(directHint.HighlightedCells.Contains(legalMatchB), "move hint highlights the swap target");
var firstHint = legalMatchBoard.FindFirstLegalMoveHint();
Require(firstHint is not null, "board finds a legal move hint");
Require(firstHint.From == legalMatchA && firstHint.To == legalMatchB, "board hint uses deterministic scan order");
Require(!noMatchSwapBoard.TryCreateMoveHint(swapA, swapB, out var missingHint) && missingHint is null, "move hint rejects no-match swaps");
var legalMatchResult = legalMatchBoard.SwapIfCreatesMatch(legalMatchA, legalMatchB);
Require(legalMatchResult.FindMatches().Contains(legalMatchA), "created match includes one of the swapped runes");
var smallScoredCombat = new CombatState(
    RuneBoard: legalMatchBoard,
    Match3MovesUsed: 0,
    LastMatchedRunesCount: 0,
    LastComboDepth: 0,
    LastMatchPower: 0,
    DurationSeconds: CombatState.DefaultDurationSeconds,
    ElapsedSeconds: 0,
    GlobalCooldownMillisecondsRemaining: 0,
    SecondsSinceLastRuneSwap: 0,
    SlowdownMillisecondsRemaining: 0
).SwapRunes(legalMatchA, legalMatchB);
Require(!smallScoredCombat.IsCombatSlowed, "base match-3 does not slow combat");
Require(smallScoredCombat.CombatSpeedPercent == CombatState.NormalCombatSpeedPercent, "base match-3 keeps normal combat speed");
var comboScoredCombat = new CombatState(
    RuneBoard: legalMatchBoard,
    Match3MovesUsed: 0,
    LastMatchedRunesCount: 0,
    LastComboDepth: 0,
    LastMatchPower: 0,
    DurationSeconds: CombatState.DefaultDurationSeconds,
    ElapsedSeconds: 0,
    GlobalCooldownMillisecondsRemaining: 0,
    SecondsSinceLastRuneSwap: 0,
    SlowdownMillisecondsRemaining: 0
).SwapRunes(legalMatchA, legalMatchB, comboDepth: 2);
Require(comboScoredCombat.LastMatchedRunesCount == 3, "scored combat records the matched rune count");
Require(comboScoredCombat.LastComboDepth == 2, "scored combat records the requested combo depth");
Require(comboScoredCombat.LastMatchPower == 5, "scored combat uses matchPower = matchedRunesCount + comboDepth");
Require(comboScoredCombat.IsCombatSlowed, "large combo starts combat slowdown");
Require(comboScoredCombat.CombatSpeedPercent == CombatState.LargeComboCombatSpeedPercent, "large combo slows combat to 70 percent");
Require(comboScoredCombat.SlowdownMillisecondsRemaining == CombatState.LargeComboSlowdownMilliseconds, "large combo slowdown lasts one second");
var almostNormalCombat = comboScoredCombat.AdvanceTimedEffectsMilliseconds(CombatState.LargeComboSlowdownMilliseconds - 1);
Require(almostNormalCombat.IsCombatSlowed, "combat remains slowed before the full slowdown second passes");
var normalSpeedCombat = almostNormalCombat.AdvanceTimedEffectsMilliseconds(1);
Require(!normalSpeedCombat.IsCombatSlowed, "combat slowdown expires after one second");
Require(normalSpeedCombat.CombatSpeedPercent == CombatState.NormalCombatSpeedPercent, "combat speed returns to normal after slowdown");
var staleMatchBoard = CreatePatternBoard(
    (new BoardPoint(6, 0), RuneType.Red),
    (new BoardPoint(6, 1), RuneType.Red),
    (new BoardPoint(6, 2), RuneType.Red)
);
Require(staleMatchBoard.FindMatches().Count >= 3, "stale-match fixture starts with an unrelated match");
Require(!staleMatchBoard.IsLegalSwap(swapA, swapB), "legal swap ignores pre-existing unrelated matches");
RequireThrows(() => staleMatchBoard.SwapIfCreatesMatch(swapA, swapB), "match swap command rejects unrelated pre-existing matches");
var horizontalMatchBoard = CreatePatternBoard(
    (new BoardPoint(0, 1), RuneType.Red),
    (new BoardPoint(0, 2), RuneType.Red)
);
var horizontalMatchCells = new[] { new BoardPoint(0, 0), new BoardPoint(0, 1), new BoardPoint(0, 2) };
Require(ContainsExactly(horizontalMatchBoard.FindHorizontalMatches(), horizontalMatchCells), "horizontal match scan finds row matches");
Require(horizontalMatchBoard.FindVerticalMatches().Count == 0, "horizontal match scan does not leak into vertical matches");
Require(ContainsExactly(horizontalMatchBoard.FindMatches(), horizontalMatchCells), "combined match scan includes horizontal matches");
var verticalMatchBoard = CreatePatternBoard(
    (new BoardPoint(1, 0), RuneType.Red),
    (new BoardPoint(2, 0), RuneType.Red)
);
var verticalMatchCells = new[] { new BoardPoint(0, 0), new BoardPoint(1, 0), new BoardPoint(2, 0) };
Require(verticalMatchBoard.FindHorizontalMatches().Count == 0, "vertical match scan does not leak into horizontal matches");
Require(ContainsExactly(verticalMatchBoard.FindVerticalMatches(), verticalMatchCells), "vertical match scan finds column matches");
Require(ContainsExactly(verticalMatchBoard.FindMatches(), verticalMatchCells), "combined match scan includes vertical matches");
var crossMatchBoard = CreatePatternBoard(
    (new BoardPoint(0, 1), RuneType.Red),
    (new BoardPoint(0, 2), RuneType.Red),
    (new BoardPoint(1, 0), RuneType.Red),
    (new BoardPoint(2, 0), RuneType.Red)
);
var crossMatchCells = new[]
{
    new BoardPoint(0, 0),
    new BoardPoint(0, 1),
    new BoardPoint(0, 2),
    new BoardPoint(1, 0),
    new BoardPoint(2, 0)
};
Require(ContainsExactly(crossMatchBoard.FindMatches(), crossMatchCells), "combined match scan unions horizontal and vertical matches");
Require(horizontalMatchBoard.EmptyCellCount == 0, "filled board starts without empty cells");
var noRemovalBoard = noMatchSwapBoard.RemoveMatches();
Require(noRemovalBoard.EmptyCellCount == 0, "removing matches from a no-match board leaves it filled");
var removedHorizontalBoard = horizontalMatchBoard.RemoveMatches();
Require(removedHorizontalBoard.EmptyCellCount == horizontalMatchCells.Length, "removing a horizontal match empties matched cells");
Require(removedHorizontalBoard.IsEmpty(new BoardPoint(0, 0)), "removed match leaves an empty first matched cell");
Require(removedHorizontalBoard.IsEmpty(new BoardPoint(0, 1)), "removed match leaves an empty middle matched cell");
Require(removedHorizontalBoard.IsEmpty(new BoardPoint(0, 2)), "removed match leaves an empty last matched cell");
Require(removedHorizontalBoard.GetRuneOrEmpty(new BoardPoint(0, 3)) == horizontalMatchBoard[new BoardPoint(0, 3)], "removing matches preserves unrelated cells");
Require(horizontalMatchBoard.EmptyCellCount == 0, "removing matches does not mutate the original board");
Require(removedHorizontalBoard.FindMatches().Count == 0, "empty cells do not count as rune matches");
RequireThrows(() => { _ = removedHorizontalBoard[new BoardPoint(0, 0)]; }, "indexing a removed cell rejects empty cells");
RequireThrows(() => removedHorizontalBoard.Swap(new BoardPoint(0, 0), new BoardPoint(0, 1)), "swap rejects empty cells");
var removedCrossBoard = crossMatchBoard.RemoveMatches();
Require(removedCrossBoard.EmptyCellCount == crossMatchCells.Length, "removing cross matches empties the union of matched cells once");
Require(removedCrossBoard.GetRuneOrEmpty(new BoardPoint(0, 0)) is null, "removed cells expose no rune value");
var droppedFilledBoard = noMatchSwapBoard.DropRunesFromTop(101);
Require(droppedFilledBoard.EmptyCellCount == 0, "dropping a filled board keeps it filled");
Require(droppedFilledBoard[0, 0] == noMatchSwapBoard[0, 0], "dropping a filled board preserves existing cells");
Require(droppedFilledBoard[6, 6] == noMatchSwapBoard[6, 6], "dropping a filled board preserves the last cell");
var fallSourceBoard = CreatePatternBoard();
var fallBoardWithGaps = fallSourceBoard.RemoveRunes(new HashSet<BoardPoint>
{
    new(3, 0),
    new(5, 0),
    new(6, 1)
});
var droppedFallBoard = fallBoardWithGaps.DropRunesFromTop(2024);
Require(droppedFallBoard.EmptyCellCount == 0, "dropping runes refills every empty cell");
Require(droppedFallBoard[6, 0] == fallSourceBoard[6, 0], "bottom rune stays at the bottom after falling");
Require(droppedFallBoard[5, 0] == fallSourceBoard[4, 0], "runes above a gap fall downward in order");
Require(droppedFallBoard[4, 0] == fallSourceBoard[2, 0], "runes preserve vertical order while falling");
Require(droppedFallBoard[3, 0] == fallSourceBoard[1, 0], "upper runes fall into lower empty cells");
Require(droppedFallBoard[2, 0] == fallSourceBoard[0, 0], "top original rune falls below newly spawned runes");
Require(droppedFallBoard.GetRuneOrEmpty(new BoardPoint(0, 0)).HasValue, "new runes spawn into top empty cells");
Require(droppedFallBoard.GetRuneOrEmpty(new BoardPoint(1, 0)).HasValue, "new runes fill every top gap");
Require(droppedFallBoard[6, 1] == fallSourceBoard[5, 1], "bottom-column gaps pull runes down");
Require(fallBoardWithGaps.EmptyCellCount == 3, "dropping runes does not mutate the gapped source board");
var noChainResolution = noMatchSwapBoard.ResolveChainReactions(0);
Require(noChainResolution.Steps.Count == 0, "chain resolution leaves a stable board untouched");
Require(noChainResolution.ReactionCount == 0, "stable boards have no chain reactions");
Require(noChainResolution.Board[0, 0] == noMatchSwapBoard[0, 0], "stable chain resolution preserves board contents");
var chainResolution = horizontalMatchBoard.ResolveChainReactions(0);
Require(chainResolution.Steps.Count == 2, "chain resolution continues after refill-created matches");
Require(chainResolution.ReactionCount == 1, "second resolved match is counted as one chain reaction");
Require(chainResolution.MaxComboDepth == 1, "chain resolution records the deepest combo depth");
Require(chainResolution.TotalMatchedRunesCount == 6, "chain resolution totals matched runes across steps");
Require(chainResolution.Steps[0].ComboDepth == 0, "initial match starts at combo depth zero");
Require(chainResolution.Steps[0].ChainNumber == 1, "initial match is chain number one for effect bonuses");
Require(chainResolution.Steps[0].MatchPower == 3, "initial chain step calculates base matchPower");
Require(chainResolution.Steps[1].ComboDepth == 1, "refill-created match increments combo depth");
Require(chainResolution.Steps[1].ChainNumber == 2, "first chain reaction is chain number two for bonuses");
Require(chainResolution.Steps[1].MatchPower == 4, "chain reaction matchPower includes combo depth");
Require(chainResolution.TotalMatchPower == 7, "chain resolution totals per-step matchPower");
Require(chainResolution.GetTotalMatchPower(2) == 11, "chain resolution applies external combo depth offsets");
Require(ContainsExactly(chainResolution.Steps[0].MatchedCells, horizontalMatchCells), "chain step records its matched cells");
Require(chainResolution.Steps[0].BoardAfterRemoval.EmptyCellCount == horizontalMatchCells.Length, "chain step exposes the post-removal board");
Require(chainResolution.Steps[0].BoardAfterDrop.EmptyCellCount == 0, "chain step exposes the post-drop board");
Require(chainResolution.Steps[1].MatchedRunesCount == 3, "chain reaction records refill-created matches");
Require(chainResolution.Board.EmptyCellCount == 0, "chain resolution ends on a filled board");
Require(chainResolution.Board.FindMatches().Count == 0, "chain resolution ends on a stable board");
RequireThrows(() => horizontalMatchBoard.ResolveChainReactions(0, 0), "chain resolution rejects a non-positive depth limit");
RequireThrows(() => horizontalMatchBoard.ResolveChainReactions(0, 1), "chain resolution enforces the maximum chain depth");
RequireThrows(() => chainResolution.Steps[0].GetMatchPower(-1), "chain step rejects negative combo depth offsets");
RequireThrows(() => chainResolution.GetTotalMatchPower(-1), "chain resolution rejects negative combo depth offsets");
Require(board.FindMatches() is not null, "match scan returns a set");

// Rune effects (GDD "Эффекты по цветам", "Великие руны", "Цепные реакции").
Require(RuneEffects.GetEffectKind(RuneType.Red) == RuneEffectKind.PhysicalDamage, "red runes deal physical damage");
Require(RuneEffects.GetEffectKind(RuneType.Blue) == RuneEffectKind.Mana, "blue runes grant mana");
Require(RuneEffects.GetEffectKind(RuneType.Green) == RuneEffectKind.Healing, "green runes heal");
Require(RuneEffects.GetEffectKind(RuneType.Yellow) == RuneEffectKind.Shield, "yellow runes grant shields");
Require(RuneEffects.GetEffectKind(RuneType.Purple) == RuneEffectKind.MagicDamage, "purple runes deal magic damage");
Require(RuneEffects.GetEffectKind(RuneType.White) == RuneEffectKind.CommanderEnergy, "white runes charge the commander");
Require(RuneEffects.GetTier(3) == RuneMatchTier.Match3, "three runes form a match-3 tier");
Require(RuneEffects.GetTier(4) == RuneMatchTier.Match4, "four runes form a match-4 tier");
Require(RuneEffects.GetTier(5) == RuneMatchTier.Match5, "five runes form a match-5 tier");
Require(RuneEffects.GetTier(6) == RuneMatchTier.Match5, "six runes still form the match-5 tier");
RequireThrows(() => RuneEffects.GetTier(2), "rune tier rejects sub-match groups");
Require(Math.Abs(RuneEffects.GetChainMultiplier(1) - 1.0) < 1e-9, "chain 1 keeps base effect strength");
Require(Math.Abs(RuneEffects.GetChainMultiplier(2) - 1.25) < 1e-9, "chain 2 adds 25 percent");
Require(Math.Abs(RuneEffects.GetChainMultiplier(3) - 1.5) < 1e-9, "chain 3 adds 50 percent");
Require(Math.Abs(RuneEffects.GetChainMultiplier(4) - 2.0) < 1e-9, "chain 4+ doubles effect strength");
Require(Math.Abs(RuneEffects.GetChainMultiplier(7) - 2.0) < 1e-9, "deep chains stay at the chain 4+ bonus");
RequireThrows(() => RuneEffects.GetChainMultiplier(0), "chain multiplier rejects chain numbers below one");

var redMatch3Groups = horizontalMatchBoard.FindMatchGroups();
Require(redMatch3Groups.Count == 1, "a single horizontal match yields one rune group");
var redMatch3Group = redMatch3Groups[0];
Require(redMatch3Group.Rune == RuneType.Red, "horizontal red match group reports its color");
Require(redMatch3Group.Size == 3, "horizontal red match group has three runes");
Require(!redMatch3Group.IsTOrLShaped, "a straight match is not T/L shaped");
Require(!redMatch3Group.ContainsGreatRune, "a normal match group does not contain a great rune");
Require(!redMatch3Group.ActivatesGreatRune, "a normal match group does not activate a great rune");
Require(redMatch3Group.Tier == RuneMatchTier.Match3, "three matched runes are a match-3 group");
Require(ContainsExactly(redMatch3Group.Cells, horizontalMatchCells), "match group reports its matched cells");

var match3Effect = RuneEffectResolver.Resolve(redMatch3Group, 1);
Require(match3Effect.Kind == RuneEffectKind.PhysicalDamage, "red match-3 resolves into physical damage");
Require(match3Effect.Tier == RuneMatchTier.Match3, "red match-3 effect keeps the match-3 tier");
Require(!match3Effect.ChargesHero, "match-3 does not charge a hero");
Require(!match3Effect.CreatesGreatRune, "match-3 does not create a great rune");
Require(!match3Effect.IsMassEffect, "a straight match-3 is not a mass effect");
Require(match3Effect.CommanderEnergy == 0, "a straight match-3 grants no commander energy");
Require(Math.Abs(match3Effect.Power - 3.0) < 1e-9, "match-3 base power equals its matchPower");

var redMatch4Board = CreatePatternBoard(
    (new BoardPoint(0, 1), RuneType.Red),
    (new BoardPoint(0, 2), RuneType.Red),
    (new BoardPoint(0, 3), RuneType.Red)
);
var redMatch4Group = redMatch4Board.FindMatchGroups().Single();
Require(redMatch4Group.Size == 4, "four in a row is a single four-rune group");
Require(redMatch4Group.Tier == RuneMatchTier.Match4, "four matched runes are a match-4 group");
var match4Effect = RuneEffectResolver.Resolve(redMatch4Group, 1);
Require(match4Effect.ChargesHero, "match-4 charges a suitable hero");
Require(!match4Effect.CreatesGreatRune, "match-4 does not create a great rune");
Require(Math.Abs(match4Effect.Power - 4.0) < 1e-9, "match-4 base power equals its matchPower");

var redTLGroups = crossMatchBoard.FindMatchGroups();
Require(redTLGroups.Count == 1, "a crossing same-color match is one connected group");
var redTLGroup = redTLGroups[0];
Require(redTLGroup.Size == 5, "the L-shaped red match has five runes");
Require(redTLGroup.IsTOrLShaped, "a bent match is detected as T/L shaped");
Require(redTLGroup.Tier == RuneMatchTier.Match5, "five matched runes are a match-5 group");
var tlEffect = RuneEffectResolver.Resolve(redTLGroup, 1);
Require(tlEffect.IsMassEffect, "T/L combos resolve into mass effects");
Require(tlEffect.CreatesGreatRune, "match-5 creates a great rune");
Require(tlEffect.CommanderEnergy == RuneEffects.TShapeCommanderEnergy, "T/L combos grant commander energy");
Require(Math.Abs(tlEffect.Power - 7.0) < 1e-9, "T/L combo adds its matchPower bonus before chain scaling");
var match5CreationResolution = crossMatchBoard.ResolveChainReactions(99);
var greatRuneAnchor = new BoardPoint(0, 0);
Require(match5CreationResolution.Steps[0].CreatedGreatRunes.Contains(greatRuneAnchor), "match-5 chain step records the created great rune anchor");
Require(match5CreationResolution.Steps[0].BoardAfterDrop.IsGreatRune(greatRuneAnchor), "match-5 creates a great rune on the board");
Require(match5CreationResolution.Steps[0].BoardAfterDrop[greatRuneAnchor] == RuneType.Red, "created great rune keeps the matched color");
var greatRuneMatchBoard = new Match3Board(Match3Board.CreateCells()
    .Select(point => new RuneCell(horizontalMatchBoard[point], point == horizontalMatchCells[1]))
    .ToList());
var greatRuneMatchGroup = greatRuneMatchBoard.FindMatchGroups().Single();
Require(greatRuneMatchGroup.ContainsGreatRune, "a match group detects a stored great-rune cell");
Require(greatRuneMatchGroup.ActivatesGreatRune, "matching a stored great-rune cell activates it");
var activatedGreatRuneEffect = RuneEffectResolver.ResolveStep(greatRuneMatchBoard, 1).Single();
Require(activatedGreatRuneEffect.IsGreatRuneActivation, "resolving a match with a stored great rune flags the activation");
Require(Math.Abs(activatedGreatRuneEffect.Power - 7.5) < 1e-9, "stored great-rune activation applies the x2.5 multiplier");

var chain2Effect = RuneEffectResolver.Resolve(redMatch3Group, 2);
Require(chain2Effect.ChainNumber == 2, "chain effect records its chain number");
Require(Math.Abs(chain2Effect.Power - 5.0) < 1e-9, "chain 2 adds combo depth and a 25 percent bonus");
var chain3Effect = RuneEffectResolver.Resolve(redMatch3Group, 3);
Require(Math.Abs(chain3Effect.Power - 7.5) < 1e-9, "chain 3 applies the 50 percent bonus");
var chain4Effect = RuneEffectResolver.Resolve(redMatch3Group, 4);
Require(Math.Abs(chain4Effect.Power - 12.0) < 1e-9, "chain 4+ doubles the chain matchPower");

var greatRuneEffect = RuneEffectResolver.Resolve(redMatch3Group, 1, greatRuneActivated: true);
Require(greatRuneEffect.IsGreatRuneActivation, "great rune activation is flagged on the effect");
Require(Math.Abs(greatRuneEffect.Power - 7.5) < 1e-9, "great rune activation multiplies effect power by 2.5");

Require(RuneEffectResolver.ResolveStep(crossMatchBoard, 1).Count == redTLGroups.Count, "resolving a step covers every match group");
RequireThrows(() => RuneEffectResolver.Resolve(redMatch3Group, 0), "resolving rejects chain numbers below one");

// Combat formulas (GDD "Формулы боя").
Require(Math.Abs(CombatFormulas.GetStarMultiplier(1) - 1.0) < 1e-9, "one star keeps base stats");
Require(Math.Abs(CombatFormulas.GetStarMultiplier(2) - 2.0) < 1e-9, "two stars double stats");
Require(Math.Abs(CombatFormulas.GetStarMultiplier(3) - 4.0) < 1e-9, "three stars quadruple stats");
RequireThrows(() => CombatFormulas.GetStarMultiplier(0), "star multiplier rejects zero stars");
RequireThrows(() => CombatFormulas.GetStarMultiplier(4), "star multiplier rejects four stars");
Require(Math.Abs(CombatFormulas.CalculateFinalHealth(100, CombatFormulas.GetStarMultiplier(2)) - 200.0) < 1e-9, "final health scales base health by star multiplier");
Require(Math.Abs(CombatFormulas.CalculateFinalHealth(100, 2.0, 1.5, 2.0) - 600.0) < 1e-9, "final health multiplies star, synergy, and artifact bonuses");
RequireThrows(() => CombatFormulas.CalculateFinalHealth(-1, 1.0), "final health rejects negative base health");
Require(Math.Abs(CombatFormulas.DamageReduction(25) - 0.2) < 1e-9, "armor 25 reduces damage by 20 percent");
Require(Math.Abs(CombatFormulas.DamageReduction(0) - 0.0) < 1e-9, "zero defense gives no reduction");
RequireThrows(() => CombatFormulas.DamageReduction(-1), "damage reduction rejects negative defense");
Require(Math.Abs(CombatFormulas.CalculatePhysicalDamage(100, 25) - 80.0) < 1e-9, "physical damage applies armor reduction");
Require(Math.Abs(CombatFormulas.CalculateMagicDamage(100, 25) - 80.0) < 1e-9, "magic damage applies resist reduction");
Require(Math.Abs(CombatFormulas.CalculateAttacksPerSecond(1.0, 1.0) - 1.0) < 1e-9, "attack speed multiplies base by bonus");
Require(Math.Abs(CombatFormulas.CalculateAttacksPerSecond(1.0, 1.5) - 1.5) < 1e-9, "attack speed bonus increases attacks per second");
Require(Math.Abs(CombatFormulas.CalculateAttacksPerSecond(0.1, 1.0) - 0.4) < 1e-9, "attack speed clamps to the 0.4 minimum");
Require(Math.Abs(CombatFormulas.CalculateAttacksPerSecond(2.0, 2.0) - 3.0) < 1e-9, "attack speed clamps to the 3.0 maximum");
Require(Math.Abs(CombatFormulas.CalculateAttackInterval(2.0) - 0.5) < 1e-9, "attack interval is the inverse of attacks per second");
RequireThrows(() => CombatFormulas.CalculateAttackInterval(0), "attack interval rejects non-positive attack speed");
Require(Math.Abs(CombatFormulas.ManaFromAttack - 10.0) < 1e-9, "an attack grants ten mana");
Require(Math.Abs(CombatFormulas.CalculateManaFromDamageTaken(50, 100) - 20.0) < 1e-9, "mana from damage taken caps at twenty");
Require(Math.Abs(CombatFormulas.CalculateManaFromDamageTaken(10, 100) - 5.0) < 1e-9, "mana from damage taken scales with damage fraction");
RequireThrows(() => CombatFormulas.CalculateManaFromDamageTaken(10, 0), "mana from damage taken rejects non-positive max health");
Require(Math.Abs(CombatFormulas.CalculateManaFromBlueRunes(3) - 24.0) < 1e-9, "blue runes grant eight mana each");
RequireThrows(() => CombatFormulas.CalculateManaFromBlueRunes(-1), "blue rune mana rejects negative counts");
Require(CombatFormulas.IsAbilityReady(100, 100), "ability casts when mana reaches the maximum");
Require(!CombatFormulas.IsAbilityReady(99, 100), "ability waits below the mana maximum");
Require(Math.Abs(CombatFormulas.BaseCritChance - 0.05) < 1e-9, "base crit chance is five percent");
Require(Math.Abs(CombatFormulas.BaseCritMultiplier - 1.5) < 1e-9, "base crit multiplier is 1.5x");
Require(CombatFormulas.WouldCrit(0.04), "a roll below the crit chance crits");
Require(!CombatFormulas.WouldCrit(0.05), "a roll at the crit chance does not crit");
RequireThrows(() => CombatFormulas.WouldCrit(1.0), "crit roll rejects values outside [0, 1)");
Require(Math.Abs(CombatFormulas.ApplyCrit(80) - 120.0) < 1e-9, "crit applies the base 1.5x multiplier");
RequireThrows(() => CombatFormulas.ApplyCrit(80, 0.9), "crit multiplier cannot reduce damage");
Require(Math.Abs(CombatFormulas.DamageAfterShield(30, 50) - 0.0) < 1e-9, "a shield fully absorbs smaller hits");
Require(Math.Abs(CombatFormulas.DamageAfterShield(60, 50) - 10.0) < 1e-9, "damage past the shield reaches health");
Require(Math.Abs(CombatFormulas.ShieldAfterDamage(50, 30) - 20.0) < 1e-9, "a shield is reduced by absorbed damage");
Require(Math.Abs(CombatFormulas.ShieldAfterDamage(50, 60) - 0.0) < 1e-9, "an overwhelmed shield drops to zero");
Require(Math.Abs(CombatFormulas.CapShield(100, 100) - 60.0) < 1e-9, "total shield caps at 60 percent of max health");
Require(Math.Abs(CombatFormulas.CapShield(40, 100) - 40.0) < 1e-9, "shields below the cap are unchanged");
Require(Math.Abs(CombatFormulas.CalculateFinalHealing(50, 2.0) - 100.0) < 1e-9, "healing scales with the healing multiplier");
Require(Math.Abs(CombatFormulas.CalculateFinalHealing(50, 1.0, 0.5) - 25.0) < 1e-9, "anti-healing reduces final healing");
RequireThrows(() => CombatFormulas.CalculateFinalHealing(50, 1.0, 1.5), "anti-healing rejects values above one");
Require(Math.Abs(CombatFormulas.ApplyHealing(80, 50, 100) - 100.0) < 1e-9, "healing never overfills max health");
Require(Math.Abs(CombatFormulas.ApplyHealing(40, 30, 100) - 70.0) < 1e-9, "healing adds to current health below the cap");
RequireThrows(() => CombatFormulas.ApplyHealing(40, 30, 0), "healing rejects non-positive max health");

// Hero data model (GDD "Структура героя", редкость и звезды).
Require(HeroRarities.All.Count == 4, "rarity catalog exposes four rarities");
Require(string.Join(",", HeroRarities.All.Select(HeroRarities.GetId)) == "common,rare,epic,legendary", "rarity catalog keeps canonical ids");
Require(HeroRarities.GetCost(HeroRarity.Common) == 1, "common heroes cost one gold");
Require(HeroRarities.GetCost(HeroRarity.Rare) == 2, "rare heroes cost two gold");
Require(HeroRarities.GetCostRange(HeroRarity.Epic).Min == 3, "epic heroes start at three gold");
Require(HeroRarities.GetCostRange(HeroRarity.Epic).Max == 4, "epic heroes cap at four gold");
Require(HeroRarities.GetCost(HeroRarity.Legendary) == 5, "legendary heroes cost five gold");
Require(HeroRarities.ParseId("legendary") == HeroRarity.Legendary, "rarity ids parse back to rarities");
Require(HeroRarities.TryParseId("EPIC", out var parsedEpic) && parsedEpic == HeroRarity.Epic, "rarity parsing is case-insensitive");
Require(!HeroRarities.TryParseId("mythic", out _), "rarity parsing rejects unknown ids");
RequireThrows(() => HeroRarities.ParseId("mythic"), "rarity parsing throws for unknown ids");

Require(HeroCatalog.All.Count == 18, "hero catalog starts with the first eighteen MVP heroes");
var catalogIronGuard = HeroCatalog.Get("iron_guard");
Require(HeroCatalog.TryGet("IRON_GUARD", out var parsedIronGuard) && parsedIronGuard.Id == "iron_guard", "hero catalog lookup is case-insensitive");
Require(catalogIronGuard.Name == "Железный Страж", "iron guard uses the GDD display name");
Require(catalogIronGuard.Rarity == HeroRarity.Common && catalogIronGuard.Cost == 1, "iron guard is a one-cost common hero");
Require(catalogIronGuard.Faction == "Империя" && catalogIronGuard.Class == "Защитник", "iron guard belongs to the Empire Defender line");
Require(catalogIronGuard.RuneAffinity == RuneType.Yellow && catalogIronGuard.Role == HeroRole.Tank, "iron guard is a yellow-rune tank");
Require(catalogIronGuard.AttackType == "melee" && catalogIronGuard.Targeting == "nearest", "iron guard uses melee nearest targeting");
Require(Math.Abs(catalogIronGuard.BaseStats.BaseHealth - 750.0) < 1e-9, "iron guard base health comes from the GDD");
Require(Math.Abs(catalogIronGuard.BaseStats.Attack - 45.0) < 1e-9, "iron guard base attack comes from the GDD");
Require(Math.Abs(catalogIronGuard.BaseStats.Armor - 8.0) < 1e-9, "iron guard base armor comes from the GDD");
Require(Math.Abs(catalogIronGuard.BaseStats.MagicResist - 3.0) < 1e-9, "iron guard magic resist comes from the GDD");
Require(Math.Abs(catalogIronGuard.BaseStats.BaseAttackSpeed - 0.75) < 1e-9, "iron guard attack speed comes from the GDD");
Require(Math.Abs(catalogIronGuard.BaseStats.ManaMax - 80.0) < 1e-9, "iron guard mana max comes from the GDD");
var catalogOathArcher = HeroCatalog.Get("oath_archer");
Require(catalogOathArcher.Name == "Лучница Присяги", "oath archer uses the GDD display name");
Require(catalogOathArcher.Rarity == HeroRarity.Common && catalogOathArcher.Cost == 1, "oath archer is a one-cost common hero");
Require(catalogOathArcher.Faction == "Империя" && catalogOathArcher.Class == "Стрелок", "oath archer belongs to the Empire Marksman line");
Require(catalogOathArcher.RuneAffinity == RuneType.Red && catalogOathArcher.Role == HeroRole.Carry, "oath archer is a red-rune carry");
Require(catalogOathArcher.AttackType == "ranged" && catalogOathArcher.Targeting == "current", "oath archer uses ranged current-target rules");
Require(Math.Abs(catalogOathArcher.BaseStats.Attack - 65.0) < 1e-9, "oath archer has carry attack stats");
var catalogFieldMedic = HeroCatalog.Get("field_medic");
Require(catalogFieldMedic.Name == "Полевой Медик", "field medic uses the GDD display name");
Require(catalogFieldMedic.Rarity == HeroRarity.Common && catalogFieldMedic.Cost == 1, "field medic is a one-cost common hero");
Require(catalogFieldMedic.Faction == "Империя" && catalogFieldMedic.Class == "Целитель", "field medic belongs to the Empire Healer line");
Require(catalogFieldMedic.RuneAffinity == RuneType.Green && catalogFieldMedic.Role == HeroRole.Healer, "field medic is a green-rune healer");
Require(catalogFieldMedic.AttackType == "ranged" && catalogFieldMedic.Targeting == "lowest_health_ally", "field medic targets the lowest-health ally");
Require(Math.Abs(catalogFieldMedic.BaseStats.ManaMax - 70.0) < 1e-9, "field medic has healer mana stats");
Require(ShopState.StartingShop.Offers.All(offer => HeroCatalog.TryGet(offer.HeroId, out _)), "starting shop offers reference catalog heroes");
var catalogWildClaw = HeroCatalog.Get("wild_claw");
Require(catalogWildClaw.Name == "Дикий Коготь", "wild claw uses the GDD display name");
Require(catalogWildClaw.Rarity == HeroRarity.Common && catalogWildClaw.Cost == 1, "wild claw is a one-cost common hero");
Require(catalogWildClaw.Faction == "Дикие" && catalogWildClaw.Class == "Берсерк", "wild claw belongs to the Wild Berserker line");
Require(catalogWildClaw.RuneAffinity == RuneType.Red && catalogWildClaw.Role == HeroRole.Bruiser, "wild claw is a red-rune bruiser");
Require(catalogWildClaw.AttackType == "melee" && catalogWildClaw.Targeting == "nearest", "wild claw uses melee nearest targeting");
Require(Math.Abs(catalogWildClaw.BaseStats.BaseHealth - 700.0) < 1e-9, "wild claw has bruiser health stats");
var catalogThornShaman = HeroCatalog.Get("thorn_shaman");
Require(catalogThornShaman.Name == "Терновый Шаман", "thorn shaman uses the GDD display name");
Require(catalogThornShaman.Rarity == HeroRarity.Common && catalogThornShaman.Cost == 1, "thorn shaman is a one-cost common hero");
Require(catalogThornShaman.Faction == "Дикие" && catalogThornShaman.Class == "Призыватель", "thorn shaman belongs to the Wild Summoner line");
Require(catalogThornShaman.RuneAffinity == RuneType.Green && catalogThornShaman.Role == HeroRole.Summoner, "thorn shaman is a green-rune summoner");
Require(catalogThornShaman.AttackType == "ranged" && catalogThornShaman.Targeting == "summon_slot", "thorn shaman uses summon-slot targeting");
Require(Math.Abs(catalogThornShaman.BaseStats.ManaMax - 75.0) < 1e-9, "thorn shaman has summoner mana stats");
var catalogMistCutthroat = HeroCatalog.Get("mist_cutthroat");
Require(catalogMistCutthroat.Name == "Туманный Резчик", "mist cutthroat uses the GDD display name");
Require(catalogMistCutthroat.Rarity == HeroRarity.Rare && catalogMistCutthroat.Cost == 2, "mist cutthroat is a two-cost rare hero");
Require(catalogMistCutthroat.Faction == "Духи" && catalogMistCutthroat.Class == "Убийца", "mist cutthroat belongs to the Spirit Assassin line");
Require(catalogMistCutthroat.RuneAffinity == RuneType.Purple && catalogMistCutthroat.Role == HeroRole.Assassin, "mist cutthroat is a purple-rune assassin");
Require(catalogMistCutthroat.AttackType == "melee" && catalogMistCutthroat.Targeting == "farthest_enemy", "mist cutthroat jumps to the farthest enemy");
Require(catalogMistCutthroat.PreferredEffectKind == RuneEffectKind.MagicDamage, "a purple hero prefers magic-damage rune effects");
Require(catalogMistCutthroat.AbilityForStars(1).Kind == HeroAbilityKind.PhysicalDamage, "assassin heroes get an active physical-damage ability");
Require(Math.Abs(catalogMistCutthroat.BaseStats.Attack - 78.0) < 1e-9, "mist cutthroat has assassin attack stats");
var catalogRuneApprentice = HeroCatalog.Get("rune_apprentice");
Require(catalogRuneApprentice.Name == "Ученик Рун", "rune apprentice uses the GDD display name");
Require(catalogRuneApprentice.Rarity == HeroRarity.Rare && catalogRuneApprentice.Cost == 2, "rune apprentice is a two-cost rare hero");
Require(catalogRuneApprentice.Faction == "Империя" && catalogRuneApprentice.Class == "Маг", "rune apprentice belongs to the Empire Mage line");
Require(catalogRuneApprentice.RuneAffinity == RuneType.Blue && catalogRuneApprentice.Role == HeroRole.Caster, "rune apprentice is a blue-rune caster");
Require(catalogRuneApprentice.AttackType == "ranged" && catalogRuneApprentice.Targeting == "two_nearest_enemies", "rune apprentice strikes two enemies at range");
Require(catalogRuneApprentice.PreferredEffectKind == RuneEffectKind.Mana, "a blue hero prefers mana rune effects");
Require(catalogRuneApprentice.AbilityForStars(1).Kind == HeroAbilityKind.MagicDamage, "caster heroes get an active magic-damage ability");
var catalogGearSquire = HeroCatalog.Get("gear_squire");
Require(catalogGearSquire.Name == "Шестеренный Сквайр", "gear squire uses the GDD display name");
Require(catalogGearSquire.Rarity == HeroRarity.Rare && catalogGearSquire.Cost == 2, "gear squire is a two-cost rare hero");
Require(catalogGearSquire.Faction == "Механисты" && catalogGearSquire.Class == "Защитник", "gear squire belongs to the Mechanist Defender line");
Require(catalogGearSquire.RuneAffinity == RuneType.Yellow && catalogGearSquire.Role == HeroRole.Tank, "gear squire is a yellow-rune tank");
Require(catalogGearSquire.AttackType == "melee" && catalogGearSquire.Targeting == "nearest", "gear squire uses melee nearest targeting");
Require(catalogGearSquire.PreferredEffectKind == RuneEffectKind.Shield, "a yellow tank prefers shield rune effects");
Require(catalogGearSquire.AbilityForStars(1).Kind == HeroAbilityKind.Shield, "tank heroes get an active shield ability");
var catalogSparkTinker = HeroCatalog.Get("spark_tinker");
Require(catalogSparkTinker.Name == "Искровой Мастер", "spark tinker uses the GDD display name");
Require(catalogSparkTinker.Rarity == HeroRarity.Rare && catalogSparkTinker.Cost == 2, "spark tinker is a two-cost rare hero");
Require(catalogSparkTinker.Faction == "Механисты" && catalogSparkTinker.Class == "Маг", "spark tinker belongs to the Mechanist Mage line");
Require(catalogSparkTinker.RuneAffinity == RuneType.Blue && catalogSparkTinker.Role == HeroRole.Caster, "spark tinker is a blue-rune caster");
Require(catalogSparkTinker.AttackType == "ranged" && catalogSparkTinker.Targeting == "nearest", "spark tinker uses ranged nearest targeting");
Require(catalogSparkTinker.AbilityForStars(1).Kind == HeroAbilityKind.MagicDamage, "caster heroes get an active magic-damage ability");
var catalogAbyssAcolyte = HeroCatalog.Get("abyss_acolyte");
Require(catalogAbyssAcolyte.Name == "Послушник Бездны", "abyss acolyte uses the GDD display name");
Require(catalogAbyssAcolyte.Rarity == HeroRarity.Rare && catalogAbyssAcolyte.Cost == 2, "abyss acolyte is a two-cost rare hero");
Require(catalogAbyssAcolyte.Faction == "Бездонные" && catalogAbyssAcolyte.Class == "Маг", "abyss acolyte belongs to the Abyss Mage line");
Require(catalogAbyssAcolyte.RuneAffinity == RuneType.Purple && catalogAbyssAcolyte.Role == HeroRole.Caster, "abyss acolyte is a purple-rune caster");
Require(catalogAbyssAcolyte.PreferredEffectKind == RuneEffectKind.MagicDamage, "a purple caster prefers magic-damage rune effects");
Require(catalogAbyssAcolyte.AbilityForStars(1).Kind == HeroAbilityKind.MagicDamage, "caster heroes get an active magic-damage ability");
var catalogSpiritDuelist = HeroCatalog.Get("spirit_duelist");
Require(catalogSpiritDuelist.Name == "Духовный Дуэлянт", "spirit duelist uses the GDD display name");
Require(catalogSpiritDuelist.Rarity == HeroRarity.Rare && catalogSpiritDuelist.Cost == 2, "spirit duelist is a two-cost rare hero");
Require(catalogSpiritDuelist.Faction == "Духи" && catalogSpiritDuelist.Class == "Берсерк", "spirit duelist belongs to the Spirit Berserker line");
Require(catalogSpiritDuelist.RuneAffinity == RuneType.White && catalogSpiritDuelist.Role == HeroRole.Bruiser, "spirit duelist is a white-rune bruiser");
Require(catalogSpiritDuelist.PreferredEffectKind == RuneEffectKind.CommanderEnergy, "a white hero prefers commander-energy rune effects");
Require(catalogSpiritDuelist.AbilityForStars(1).Kind == HeroAbilityKind.PhysicalDamage, "bruiser heroes get an active physical-damage ability");
var catalogDuskRanger = HeroCatalog.Get("dusk_ranger");
Require(catalogDuskRanger.Name == "Сумеречный Егерь", "dusk ranger uses the GDD display name");
Require(catalogDuskRanger.Rarity == HeroRarity.Epic && catalogDuskRanger.Cost == 3, "dusk ranger is a three-cost epic hero");
Require(catalogDuskRanger.Faction == "Дикие" && catalogDuskRanger.Class == "Стрелок", "dusk ranger belongs to the Wild Marksman line");
Require(catalogDuskRanger.RuneAffinity == RuneType.Red && catalogDuskRanger.Role == HeroRole.Carry, "dusk ranger is a red-rune carry");
Require(catalogDuskRanger.AttackType == "ranged" && catalogDuskRanger.Targeting == "current", "dusk ranger uses ranged current-target attacks");
Require(catalogDuskRanger.AbilityForStars(1).Kind == HeroAbilityKind.PhysicalDamage, "carry heroes get an active physical-damage ability");
var catalogBulwarkCaptain = HeroCatalog.Get("bulwark_captain");
Require(catalogBulwarkCaptain.Name == "Капитан Бастиона", "bulwark captain uses the GDD display name");
Require(catalogBulwarkCaptain.Rarity == HeroRarity.Epic && catalogBulwarkCaptain.Cost == 3, "bulwark captain is a three-cost epic hero");
Require(catalogBulwarkCaptain.Faction == "Империя" && catalogBulwarkCaptain.Class == "Защитник", "bulwark captain belongs to the Empire Defender line");
Require(catalogBulwarkCaptain.RuneAffinity == RuneType.Yellow && catalogBulwarkCaptain.Role == HeroRole.Tank, "bulwark captain is a yellow-rune tank");
Require(catalogBulwarkCaptain.PreferredEffectKind == RuneEffectKind.Shield, "a yellow tank prefers shield rune effects");
Require(catalogBulwarkCaptain.AbilityForStars(1).Kind == HeroAbilityKind.Shield, "tank heroes get an active shield ability");
var catalogVoidOracle = HeroCatalog.Get("void_oracle");
Require(catalogVoidOracle.Name == "Оракул Пустоты", "void oracle uses the GDD display name");
Require(catalogVoidOracle.Rarity == HeroRarity.Epic && catalogVoidOracle.Cost == 3, "void oracle is a three-cost epic hero");
Require(catalogVoidOracle.Faction == "Бездонные" && catalogVoidOracle.Class == "Целитель", "void oracle belongs to the Abyss Healer line");
Require(catalogVoidOracle.RuneAffinity == RuneType.Green && catalogVoidOracle.Role == HeroRole.Support, "void oracle is a green-rune support");
Require(catalogVoidOracle.PreferredEffectKind == RuneEffectKind.Healing, "a green hero prefers healing rune effects");
Require(catalogVoidOracle.AbilityForStars(1).Kind == HeroAbilityKind.Healing, "support heroes get an active healing ability");
var catalogDroneMarshal = HeroCatalog.Get("drone_marshal");
Require(catalogDroneMarshal.Name == "Маршал Дронов", "drone marshal uses the GDD display name");
Require(catalogDroneMarshal.Rarity == HeroRarity.Epic && catalogDroneMarshal.Cost == 3, "drone marshal is a three-cost epic hero");
Require(catalogDroneMarshal.Faction == "Механисты" && catalogDroneMarshal.Class == "Призыватель", "drone marshal belongs to the Mechanist Summoner line");
Require(catalogDroneMarshal.RuneAffinity == RuneType.Blue && catalogDroneMarshal.Role == HeroRole.Summoner, "drone marshal is a blue-rune summoner");
Require(catalogDroneMarshal.AttackType == "ranged" && catalogDroneMarshal.Targeting == "summon_slot", "drone marshal uses summon-slot targeting");
Require(catalogDroneMarshal.AbilityForStars(1).Kind == HeroAbilityKind.MagicDamage, "summoner heroes get an active magic-damage ability");
var catalogPhaseAssassin = HeroCatalog.Get("phase_assassin");
Require(catalogPhaseAssassin.Name == "Фазовый Убийца", "phase assassin uses the GDD display name");
Require(catalogPhaseAssassin.Rarity == HeroRarity.Epic && catalogPhaseAssassin.Cost == 3, "phase assassin is a three-cost epic hero");
Require(catalogPhaseAssassin.Faction == "Духи" && catalogPhaseAssassin.Class == "Убийца", "phase assassin belongs to the Spirit Assassin line");
Require(catalogPhaseAssassin.RuneAffinity == RuneType.White && catalogPhaseAssassin.Role == HeroRole.Assassin, "phase assassin is a white-rune assassin");
Require(catalogPhaseAssassin.AttackType == "melee" && catalogPhaseAssassin.Targeting == "farthest_enemy", "phase assassin jumps to the farthest enemy");
Require(catalogPhaseAssassin.PreferredEffectKind == RuneEffectKind.CommanderEnergy, "a white assassin prefers commander-energy rune effects");
Require(catalogPhaseAssassin.AbilityForStars(1).Kind == HeroAbilityKind.PhysicalDamage, "assassin heroes get an active physical-damage ability");
var catalogMagmaBrute = HeroCatalog.Get("magma_brute");
Require(catalogMagmaBrute.Name == "Магмовый Громила", "magma brute uses the GDD display name");
Require(catalogMagmaBrute.Rarity == HeroRarity.Epic && catalogMagmaBrute.Cost == 4, "magma brute is a four-cost epic hero");
Require(catalogMagmaBrute.Faction == "Дикие" && catalogMagmaBrute.Class == "Берсерк", "magma brute belongs to the Wild Berserker line");
Require(catalogMagmaBrute.RuneAffinity == RuneType.Red && catalogMagmaBrute.Role == HeroRole.Bruiser, "magma brute is a red-rune bruiser");
Require(catalogMagmaBrute.AttackType == "melee" && catalogMagmaBrute.Targeting == "nearest", "magma brute uses melee nearest targeting");
Require(catalogMagmaBrute.AbilityForStars(1).Kind == HeroAbilityKind.PhysicalDamage, "bruiser heroes get an active physical-damage ability");
var catalogCurseWeaver = HeroCatalog.Get("curse_weaver");
Require(catalogCurseWeaver.Name == "Ткач Проклятий", "curse weaver uses the GDD display name");
Require(catalogCurseWeaver.Rarity == HeroRarity.Epic && catalogCurseWeaver.Cost == 4, "curse weaver is a four-cost epic hero");
Require(catalogCurseWeaver.Faction == "Бездонные" && catalogCurseWeaver.Class == "Маг", "curse weaver belongs to the Abyss Mage line");
Require(catalogCurseWeaver.RuneAffinity == RuneType.Purple && catalogCurseWeaver.Role == HeroRole.Caster, "curse weaver is a purple-rune caster");
Require(catalogCurseWeaver.PreferredEffectKind == RuneEffectKind.MagicDamage, "a purple caster prefers magic-damage rune effects");
Require(catalogCurseWeaver.AbilityForStars(1).Kind == HeroAbilityKind.MagicDamage, "caster heroes get an active magic-damage ability");
RequireThrows(() => HeroCatalog.Get("missing_hero"), "hero catalog rejects unknown ids");

var ironGuardDefinition = new HeroDefinition(
    Id: "iron_guard",
    Name: "Iron Guard",
    Rarity: HeroRarity.Common,
    Cost: HeroRarities.GetCost(HeroRarity.Common),
    Faction: "Empire",
    Class: "Defender",
    RuneAffinity: RuneType.Yellow,
    Role: HeroRole.Tank,
    AttackType: "melee",
    Targeting: "nearest",
    Stars: 1,
    Ability: "Shields itself and the nearest ally",
    Passive: "Takes less damage on the front line",
    BaseStats: new HeroStats(BaseHealth: 100, Attack: 10, Armor: 25, MagicResist: 15, BaseAttackSpeed: 0.8, ManaMax: 60)
);
Require(ironGuardDefinition.PreferredEffectKind == RuneEffectKind.Shield, "a yellow hero prefers shield rune effects");
Require(Enum.IsDefined(typeof(HeroRole), ironGuardDefinition.Role), "hero role uses the role enum");
Require(ironGuardDefinition.Cost == 1, "common hero cost matches the rarity price");
var ironGuardAbility = ironGuardDefinition.AbilityForStars(1);
Require(ironGuardAbility.Kind == HeroAbilityKind.Shield, "tank heroes get an active shield ability");
Require(Math.Abs(ironGuardAbility.Power - 20.0) < 1e-9, "tank shield ability scales from max health");
var ironGuardPassive = ironGuardDefinition.PassiveForStars(1);
Require(ironGuardPassive.Kind == HeroPassiveKind.FrontlineGuard, "tank heroes get a frontline passive");
Require(Math.Abs(ironGuardPassive.Power - HeroPassives.FrontlineGuardBonus) < 1e-9, "tank passive uses the frontline guard bonus");
var twoStarStats = ironGuardDefinition.StatsForStars(2);
Require(Math.Abs(twoStarStats.BaseHealth - 200.0) < 1e-9, "two-star hero doubles base health");
Require(Math.Abs(twoStarStats.Attack - 20.0) < 1e-9, "two-star hero doubles attack");
Require(Math.Abs(twoStarStats.Armor - ironGuardDefinition.BaseStats.Armor) < 1e-9, "star scaling leaves armor unchanged in the MVP");
RequireThrows(() => ironGuardDefinition.StatsForStars(0), "star scaling rejects invalid star counts");
RequireThrows(() => new HeroAbility(HeroAbilityKind.Healing, -1.0), "hero ability rejects negative power");
RequireThrows(() => new HeroPassive(HeroPassiveKind.BonusAttack, -1.0), "hero passive rejects negative power");

// Autobattle (GDD "Автобой").
var ironGuardUnit = BattleUnit.FromHero(ironGuardDefinition, 2, "ig_1", TacticalSide.Player, new TacticalPosition(2, 1));
Require(Math.Abs(ironGuardUnit.MaxHealth - 200.0) < 1e-9, "a two-star hero unit uses scaled health");
Require(Math.Abs(ironGuardUnit.Attack - 20.0) < 1e-9, "a two-star hero unit uses scaled attack");
Require(Math.Abs(ironGuardUnit.Armor - 31.25) < 1e-9, "a frontline tank passive increases armor");
Require(Math.Abs(ironGuardUnit.MagicResist - 18.75) < 1e-9, "a frontline tank passive increases magic resist");
Require(ironGuardUnit.AttackType == BattleAttackType.Melee && !ironGuardUnit.IsRanged, "a melee hero builds a melee unit");
Require(Math.Abs(ironGuardUnit.AttackInterval - 1.25) < 1e-9, "attack interval matches the hero attack speed");
Require(ironGuardUnit.ActiveAbility.Kind == HeroAbilityKind.Shield, "battle units carry their hero active ability");
Require(ironGuardUnit.PassiveEffect.Kind == HeroPassiveKind.FrontlineGuard, "battle units carry their hero passive effect");
Require(ironGuardUnit.CurrentHealth == ironGuardUnit.MaxHealth && ironGuardUnit.AbilitiesCast == 0, "a new hero unit starts at full health");
var backlineGuardUnit = BattleUnit.FromHero(ironGuardDefinition, 1, "ig_back", TacticalSide.Player, new TacticalPosition(3, 1));
Require(Math.Abs(backlineGuardUnit.Armor - 25.0) < 1e-9, "frontline guard does not modify backline armor");

var battleVictory = BattleState.Create(new[]
{
    MakeUnit("ally_a", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 100, 1.0, 0.0),
    MakeUnit("enemy_a", TacticalSide.Enemy, new TacticalPosition(1, 0), 50, 50, 10, 1.0, 5.0)
});
Require(battleVictory.Outcome == BattleOutcome.Ongoing, "a fresh battle is ongoing");
Require(Math.Abs(battleVictory.RemainingSeconds - BattleState.DefaultDurationSeconds) < 1e-9, "a fresh battle has the full timer");
var afterVictory = battleVictory.Tick(0.5);
Require(afterVictory.Outcome == BattleOutcome.PlayerVictory, "killing the last enemy wins the battle");
Require(!afterVictory.AliveEnemies.Any(), "the defeated enemy leaves the fight");
var attackingAlly = afterVictory.Units.First(unit => unit.UnitId == "ally_a");
Require(Math.Abs(attackingAlly.CurrentHealth - 100.0) < 1e-9, "the unharmed ally keeps full health");
Require(Math.Abs(attackingAlly.CurrentMana - CombatFormulas.ManaFromAttack) < 1e-9, "an attacking unit gains attack mana");
Require(attackingAlly.AbilitiesCast == 0, "a unit below max mana does not cast");

var battleDefeat = BattleState.Create(new[]
{
    MakeUnit("ally_b", TacticalSide.Player, new TacticalPosition(2, 0), 10, 10, 0, 1.0, 5.0),
    MakeUnit("enemy_b", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 100, 1.0, 0.0)
});
var afterDefeat = battleDefeat.Tick(0.5);
Require(afterDefeat.Outcome == BattleOutcome.PlayerDefeat, "losing every ally loses the battle");
Require(!afterDefeat.AliveAllies.Any(), "defeated allies leave the field");

var timerWin = BattleState.Create(new[]
{
    MakeUnit("ally_c", TacticalSide.Player, new TacticalPosition(2, 0), 100, 80, 0, 1.0, 100.0),
    MakeUnit("enemy_c", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 50, 0, 1.0, 100.0)
}, 1.0);
Require(timerWin.Tick(1.0).Outcome == BattleOutcome.PlayerVictory, "on timeout the healthier side wins");
var timerLoss = BattleState.Create(new[]
{
    MakeUnit("ally_d", TacticalSide.Player, new TacticalPosition(2, 0), 100, 40, 0, 1.0, 100.0),
    MakeUnit("enemy_d", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 60, 0, 1.0, 100.0)
}, 1.0);
Require(timerLoss.Tick(1.0).Outcome == BattleOutcome.PlayerDefeat, "on timeout the less healthy side loses");

var battleMove = BattleState.Create(new[]
{
    MakeUnit("mover", TacticalSide.Player, new TacticalPosition(3, 0), 100, 100, 50, 1.0, 0.0),
    MakeUnit("enemy_e", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var movedUnit = battleMove.Tick(0.5).Units.First(unit => unit.UnitId == "mover");
Require(movedUnit.Position.Equals(new TacticalPosition(2, 0)), "a melee unit steps toward a distant target");
Require(Math.Abs(movedUnit.CurrentMana) < 1e-9, "moving instead of attacking grants no mana");

var castBattle = BattleState.Create(new[]
{
    MakeUnit("caster", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 50, 1.0, 0.0, manaMax: 10),
    MakeUnit("enemy_f", TacticalSide.Enemy, new TacticalPosition(1, 0), 1000, 1000, 0, 1.0, 100.0)
});
var afterCast = castBattle.Tick(0.5);
var caster = afterCast.Units.First(unit => unit.UnitId == "caster");
Require(caster.AbilitiesCast == 1, "a unit casts when mana reaches the maximum");
Require(Math.Abs(caster.CurrentMana) < 1e-9, "casting resets mana to zero");
Require(afterCast.Outcome == BattleOutcome.Ongoing, "the battle continues while both sides survive");

var runeManaBattle = BattleState.Create(new[]
{
    MakeUnit("ally_g", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("enemy_g", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var afterRuneMana = runeManaBattle.AddManaFromBlueRunes(TacticalSide.Player, 3);
var fedAlly = afterRuneMana.Units.First(unit => unit.UnitId == "ally_g");
Require(Math.Abs(fedAlly.CurrentMana - 24.0) < 1e-9, "blue runes grant eight mana each to a living ally");
RequireThrows(() => runeManaBattle.AddManaFromBlueRunes(TacticalSide.Player, -1), "blue rune mana rejects negative counts");

var runeCastBattle = BattleState.Create(new[]
{
    MakeUnit("ally_h", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0, manaMax: 20),
    MakeUnit("enemy_h", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var runeCaster = runeCastBattle.AddManaFromBlueRunes(TacticalSide.Player, 3).Units.First(unit => unit.UnitId == "ally_h");
Require(runeCaster.AbilitiesCast == 1 && Math.Abs(runeCaster.CurrentMana) < 1e-9, "blue-rune mana that fills the bar triggers a cast");

var mageDefinition = ironGuardDefinition with
{
    Id = "rune_apprentice",
    Name = "Rune Apprentice",
    Role = HeroRole.Caster,
    RuneAffinity = RuneType.Blue,
    AttackType = "ranged",
    BaseStats = new HeroStats(BaseHealth: 80, Attack: 20, Armor: 0, MagicResist: 0, BaseAttackSpeed: 1.0, ManaMax: 10)
};
var mageUnit = BattleUnit.FromHero(mageDefinition, 1, "mage_active", TacticalSide.Player, new TacticalPosition(2, 0))
    with { CurrentMana = 8.0 };
Require(mageUnit.ActiveAbility.Kind == HeroAbilityKind.MagicDamage, "caster heroes get an active magic-damage ability");
Require(Math.Abs(mageUnit.ActiveAbility.Power - 40.0) < 1e-9, "caster ability scales from attack");
var openingManaMage = BattleUnit.FromHero(mageDefinition, 1, "mage_passive", TacticalSide.Player, new TacticalPosition(2, 1));
Require(openingManaMage.PassiveEffect.Kind == HeroPassiveKind.OpeningMana, "caster heroes get an opening mana passive");
Require(Math.Abs(openingManaMage.CurrentMana - 2.0) < 1e-9, "caster opening mana scales from mana max");
var activeDamageBattle = BattleState.Create(new[]
{
    mageUnit,
    MakeUnit("ability_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var afterActiveDamage = activeDamageBattle.AddManaFromBlueRunes(TacticalSide.Player, 1);
var activeMage = afterActiveDamage.Units.First(unit => unit.UnitId == "mage_active");
var activeEnemy = afterActiveDamage.Units.First(unit => unit.UnitId == "ability_enemy");
Require(activeMage.AbilitiesCast == 1 && Math.Abs(activeMage.CurrentMana) < 1e-9, "active ability casts when blue mana fills the bar");
Require(Math.Abs(activeEnemy.CurrentHealth - 60.0) < 1e-9, "active caster ability deals magic damage to an enemy");

var shieldCaster = BattleUnit.FromHero(ironGuardDefinition, 1, "shield_active", TacticalSide.Player, new TacticalPosition(2, 0))
    with { CurrentMana = 56.0 };
var activeShieldBattle = BattleState.Create(new[]
{
    shieldCaster,
    MakeUnit("shield_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var afterActiveShield = activeShieldBattle.AddManaFromBlueRunes(TacticalSide.Player, 1);
var shieldedCaster = afterActiveShield.Units.First(unit => unit.UnitId == "shield_active");
Require(shieldedCaster.AbilitiesCast == 1, "shield active ability casts when mana is full");
Require(Math.Abs(shieldedCaster.Shield - 20.0) < 1e-9, "tank active ability applies a shield");

var carryDefinition = ironGuardDefinition with
{
    Id = "oath_archer",
    Name = "Oath Archer",
    Role = HeroRole.Carry,
    AttackType = "ranged",
    BaseStats = new HeroStats(BaseHealth: 70, Attack: 20, Armor: 0, MagicResist: 0, BaseAttackSpeed: 1.0, ManaMax: 40)
};
var carryUnit = BattleUnit.FromHero(carryDefinition, 1, "carry_passive", TacticalSide.Player, new TacticalPosition(3, 0));
Require(carryUnit.PassiveEffect.Kind == HeroPassiveKind.BonusAttack, "carry heroes get an attack passive");
Require(Math.Abs(carryUnit.Attack - 23.0) < 1e-9, "carry attack passive increases attack");

var rangedBattle = BattleState.Create(new[]
{
    MakeUnit("archer", TacticalSide.Player, new TacticalPosition(3, 0), 100, 100, 30, 1.0, 0.0, attackType: BattleAttackType.Ranged),
    MakeUnit("enemy_r", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var afterRanged = rangedBattle.Tick(0.5);
var archer = afterRanged.Units.First(unit => unit.UnitId == "archer");
var rangedTarget = afterRanged.Units.First(unit => unit.UnitId == "enemy_r");
Require(archer.Position.Equals(new TacticalPosition(3, 0)), "a ranged unit attacks without moving");
Require(Math.Abs(rangedTarget.CurrentHealth - 70.0) < 1e-9, "a ranged unit hits a distant target");
Require(Math.Abs(archer.CurrentMana - CombatFormulas.ManaFromAttack) < 1e-9, "a ranged attack also grants mana");

RequireThrows(() => BattleState.Create(System.Array.Empty<BattleUnit>(), 0.0), "battle creation rejects a non-positive duration");
RequireThrows(() => battleVictory.Tick(0.0), "battle tick rejects a non-positive delta");

// Rune effects applied to the autobattle (match-3 ↔ combat link).
var fxBattle = BattleState.Create(new[]
{
    MakeUnit("fx_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 50, 0, 1.0, 100.0),
    MakeUnit("fx_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 40)).Units.First(u => u.UnitId == "fx_enemy").CurrentHealth - 60.0) < 1e-9, "a red rune deals physical damage to an enemy");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.Healing, 30)).Units.First(u => u.UnitId == "fx_ally").CurrentHealth - 80.0) < 1e-9, "a green rune heals the wounded ally");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 25)).Units.First(u => u.UnitId == "fx_ally").Shield - 25.0) < 1e-9, "a yellow rune shields the front ally");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 100)).Units.First(u => u.UnitId == "fx_ally").Shield - 60.0) < 1e-9, "rune shields respect the 60 percent cap");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.Mana, 24)).Units.First(u => u.UnitId == "fx_ally").CurrentMana - 24.0) < 1e-9, "a blue rune grants mana to an ally");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.CommanderEnergy, 5)).CommanderEnergy - 5.0) < 1e-9, "a white rune accrues commander energy");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 40, mass: true, commanderEnergy: 10)).CommanderEnergy - 10.0) < 1e-9, "T/L combos accrue commander energy alongside their effect");

var massHealBattle = BattleState.Create(new[]
{
    MakeUnit("m1", TacticalSide.Player, new TacticalPosition(2, 0), 100, 40, 0, 1.0, 100.0),
    MakeUnit("m2", TacticalSide.Player, new TacticalPosition(3, 0), 100, 40, 0, 1.0, 100.0),
    MakeUnit("me", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
Require(massHealBattle.ApplyRuneEffect(Effect(RuneEffectKind.Healing, 20, mass: true)).AliveAllies.All(u => Math.Abs(u.CurrentHealth - 60.0) < 1e-9), "a mass heal restores every ally");

var lethalRuneBattle = BattleState.Create(new[]
{
    MakeUnit("lb_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("lb_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 30, 30, 0, 1.0, 100.0)
});
Require(lethalRuneBattle.ApplyRuneEffects(new[] { Effect(RuneEffectKind.PhysicalDamage, 50) }).Outcome == BattleOutcome.PlayerVictory, "rune damage that kills the last enemy wins the battle");
RequireThrows(() => fxBattle.ApplyRuneEffect(null!), "applying a rune effect rejects null");

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

static bool ContainsExactly(IReadOnlyCollection<BoardPoint> actual, IReadOnlyList<BoardPoint> expected)
{
    return actual.Count == expected.Count && expected.All(actual.Contains);
}

static RuneEffect Effect(RuneEffectKind kind, double power, bool mass = false, int commanderEnergy = 0)
{
    return new RuneEffect(
        Rune: RuneType.Red,
        Kind: kind,
        Tier: RuneMatchTier.Match3,
        MatchedRunesCount: 3,
        ChainNumber: 1,
        IsMassEffect: mass,
        ChargesHero: false,
        CreatesGreatRune: false,
        IsGreatRuneActivation: false,
        CommanderEnergy: commanderEnergy,
        Power: power);
}

static BattleUnit MakeUnit(
    string id,
    TacticalSide side,
    TacticalPosition position,
    double maxHealth,
    double currentHealth,
    double attack,
    double attacksPerSecond,
    double cooldown,
    double manaMax = 100.0,
    double armor = 0.0,
    BattleAttackType attackType = BattleAttackType.Melee)
{
    return new BattleUnit(
        UnitId: id,
        Side: side,
        Position: position,
        MaxHealth: maxHealth,
        CurrentHealth: currentHealth,
        Attack: attack,
        Armor: armor,
        MagicResist: 0.0,
        AttacksPerSecond: attacksPerSecond,
        CurrentMana: 0.0,
        ManaMax: manaMax,
        Shield: 0.0,
        AttackType: attackType,
        AttackCooldownRemaining: cooldown,
        AbilitiesCast: 0);
}

static Match3Board CreatePatternBoard(params (BoardPoint Point, RuneType Rune)[] overrides)
{
    var runes = Match3Board.CreateCells()
        .Select(point => RuneTypes.All[((point.Row * 3) + point.Column) % RuneTypes.All.Count])
        .ToList();

    foreach (var (point, rune) in overrides)
    {
        if (!Match3Board.Contains(point))
        {
            throw new ArgumentOutOfRangeException(nameof(overrides), "Board override point is outside the board.");
        }

        runes[(point.Row * Match3Board.Columns) + point.Column] = rune;
    }

    return new Match3Board(runes);
}
