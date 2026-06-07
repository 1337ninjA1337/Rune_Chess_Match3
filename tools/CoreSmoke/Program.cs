using RuneChess.Core;

var state = RunState.NewRun();
Require(state.Round == 1, "new run starts at round 1");
Require(state.Phase == RunPhase.Preparation, "new run starts in preparation");
Require(state.RunHealth == 20, "new run starts with full run health");
Require(EconomyConfig.Default.StartingRunHealth == 20, "default economy config starts run health at 20");
Require(state.Gold == 5, "new run starts with configured gold");
Require(state.Xp == 0, "new run starts with zero XP");
Require(state.PlayerLevel == 1, "new run starts at player level 1");
Require(EconomyConfig.Default.StartingPlayerLevel == 1, "default economy config starts the player at level 1");
Require(state.Commander.Id == CommanderCatalog.Default.Id, "new run starts with the catalog default commander");
Require(state.Commander.Energy == 20, "the default commander starting bonus grants initial commander energy");
Require(state.Team.Count == 0, "new run starts with empty team");
Require(state.Bench.Count == 0, "new run starts with empty bench");
Require(EconomyConfig.Default.StartingBenchSize == 6, "default economy config uses a six-slot bench");
Require(state.Shop.Offers.Count == 3, "new run starts with a shop");
Require(EconomyConfig.Default.GetShopSizeForLevel(1) == 3, "level 1 shop has three offers");
Require(EconomyConfig.Default.GetShopSizeForLevel(2) == 3, "level 2 shop has three offers");
Require(EconomyConfig.Default.GetShopSizeForLevel(3) == 4, "level 3 shop has four offers");
Require(ShopState.ForPlayerLevel(3).Offers.Count == 4, "shop state can build a four-offer shop for level 3+");
RequireThrows(() => EconomyConfig.Default.GetShopSizeForLevel(0), "shop size rejects player levels below one");
Require(state.Artifacts.Count == 0, "new run starts without artifacts");

// Commander model (GDD "Командиры").
Require(CommanderCatalog.All.Count == 3, "the MVP ships three commanders");
var runeArchon = CommanderCatalog.Get("rune_archon");
Require(runeArchon.Name == "Архонт Рун", "rune archon commander has its GDD name");
Require(CommanderCatalog.Default.Id == "rune_archon", "the catalog default commander matches the GDD commander list");
Require(runeArchon.Passive == "Каждое третье match-4 комбо создает дополнительную синюю руну.", "rune archon passive matches the GDD");
Require(runeArchon.MaxEnergy == 100 && runeArchon.RecommendedStyles.Count > 0, "a commander defines an energy bar and recommended styles");
Require(runeArchon.StartingBonus.Kind == CommanderStartingBonusKind.CommanderEnergy && runeArchon.StartingBonus.Amount == 20, "rune archon starts with commander energy");
Require(CommanderCatalog.Get("warlord").StartingBonus.HeroId == "iron_guard", "warlord starts with a frontline defender");
var warlordRun = RunState.NewRun("warlord");
Require(warlordRun.Commander.Id == "warlord" && warlordRun.Bench.Single().HeroId == "iron_guard", "a new run can select the warlord commander before start");
var alchemistRun = RunState.NewRun("alchemist");
Require(alchemistRun.Commander.Id == "alchemist" && alchemistRun.Gold == 7, "a new run can select the alchemist commander before start");
Require(CommanderCatalog.Get("warlord").Passive.Contains("+20%"), "warlord commander buffs the first defender's health");
Require(CommanderCatalog.Get("alchemist").Passive.Contains("золото"), "alchemist commander rewards gold for chain reactions");
var runeArchonState = runeArchon.CreateInitialState();
Require(runeArchonState.Id == "rune_archon" && runeArchonState.Energy == 0 && runeArchonState.MaxEnergy == 100, "a commander builds an empty runtime energy bar");
Require(Math.Abs(state.Commander.EnergyFillRatio - 0.2) < 1e-9, "commander state exposes an energy bar fill ratio");
Require(!state.Commander.IsEnergyFull, "commander energy bar reports when it is not full");
Require(new CommanderState("test_commander", "Test", 95, 100).GainEnergy(10).IsEnergyFull, "commander energy gain clamps at the energy bar maximum");
Require(Math.Abs(new CommanderState("test_commander", "Test", 50, 100).SpendEnergy(25).Energy - 25.0) < 1e-9, "commander energy can be spent from the bar");
var commanderAfterMatch4s = new CommanderState("test_commander", "Test", 0, 100, Match4CombosTowardPassive: 2)
    .AddMatch4Combos(1, 3, out var commanderPassiveTriggers);
Require(commanderPassiveTriggers == 1 && commanderAfterMatch4s.Match4CombosTowardPassive == 0, "commander passive progress triggers every configured match-4 count");
Require(CommanderCatalog.TryGet("RUNE_ARCHON", out _), "commander lookup is case-insensitive");
Require(!CommanderCatalog.TryGet("unknown_commander", out _), "commander lookup rejects unknown ids");
RequireThrows(() => CommanderCatalog.Get("unknown_commander"), "commander get throws on unknown ids");
RequireThrows(() => RunState.NewRun("unknown_commander"), "run setup rejects an unknown selected commander");
RequireThrows(() => new CommanderDefinition("", "Name", "Passive", 100, new CommanderStartingBonus("Bonus", CommanderStartingBonusKind.Gold, Amount: 1), Array.Empty<string>()), "commander definition rejects a blank id");
RequireThrows(() => new CommanderDefinition("id", "Name", "Passive", 0, new CommanderStartingBonus("Bonus", CommanderStartingBonusKind.Gold, Amount: 1), Array.Empty<string>()), "commander definition rejects a non-positive energy bar");
RequireThrows(() => new CommanderState("", "Name", 0, 100), "commander state rejects a blank id");
RequireThrows(() => new CommanderState("id", "Name", 101, 100), "commander state rejects energy above max");
RequireThrows(() => new CommanderState("id", "Name", 0, 100).SpendEnergy(1), "commander energy spend rejects insufficient energy");
RequireThrows(() => new CommanderStartingBonus("", CommanderStartingBonusKind.Gold, Amount: 1), "commander starting bonus rejects a blank description");
RequireThrows(() => new CommanderStartingBonus("Bad", CommanderStartingBonusKind.Gold, Amount: 0), "commander starting bonus rejects non-positive gold");
RequireThrows(() => new CommanderStartingBonus("Bad", CommanderStartingBonusKind.BenchHero), "commander starting bonus rejects missing bench hero id");

var afterBuy = state.BuyHero(0);
Require(afterBuy.Gold == 4, "buying a common hero spends gold");
Require(afterBuy.Bench.Count == 1, "bought hero goes to bench");
Require(afterBuy.Shop.Offers.Count == 2, "bought shop offer is removed");
RequireThrows(() => state.BuyHero(-1), "buying rejects an invalid shop offer index");
RequireThrows(() => (state with { Gold = 0 }).BuyHero(0), "buying rejects offers the player cannot afford");
Require(EconomyConfig.Default.RerollCost == 2, "default economy config rerolls the shop for two gold");
var levelOneReroll = state.RerollShop(ShopState.StartingShop.Offers);
Require(levelOneReroll.Gold == 3 && levelOneReroll.Shop.RerollsThisRound == 1, "level 1 reroll costs two gold and tracks reroll count");
RequireThrows(() => (state with { Gold = 1 }).RerollShop(ShopState.StartingShop.Offers), "reroll rejects insufficient gold");
var fullBenchState = state with
{
    Gold = 10,
    Bench = Enumerable.Range(0, EconomyConfig.Default.StartingBenchSize)
        .Select(index => new HeroInstance($"bench_full_{index}", "iron_guard", 1))
        .ToList()
};
RequireThrows(() => fullBenchState.BuyHero(0), "buying a hero is blocked when the six-slot bench is full");
var levelThreeRun = state with { PlayerLevel = 3 };
var levelThreeReroll = levelThreeRun.RerollShop(ShopState.ForPlayerLevel(3).Offers);
Require(levelThreeReroll.Shop.Offers.Count == 4 && levelThreeReroll.Gold == 3, "level 3 reroll uses a four-offer shop");
RequireThrows(() => levelThreeRun.RerollShop(ShopState.StartingShop.Offers), "level 3 reroll rejects three-offer shops");
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

var threeHeroBench = state.BuyHero(0).BuyHero(0).BuyHero(0);
var firstLimitedHeroId = threeHeroBench.Bench[0].InstanceId;
var secondLimitedHeroId = threeHeroBench.Bench[1].InstanceId;
var thirdLimitedHeroId = threeHeroBench.Bench[2].InstanceId;
var fieldAtLevelCap = threeHeroBench
    .PlaceHeroFromBench(firstLimitedHeroId, new TacticalPosition(2, 2))
    .PlaceHeroFromBench(secondLimitedHeroId, new TacticalPosition(3, 2));
Require(fieldAtLevelCap.Team.Count == 2, "level 1 allows two heroes on the tactical field");
RequireThrows(
    () => fieldAtLevelCap.PlaceHeroFromBench(thirdLimitedHeroId, new TacticalPosition(2, 3)),
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
Require(EconomyConfig.Default.BuyXpCost == 4 && EconomyConfig.Default.XpPerPurchase == 4, "default economy config buys 4 XP for 4 gold");
Require(afterXp.Gold == 0, "buying XP spends configured gold");
Require(afterXp.Xp == 4, "buying XP adds configured XP");
RequireThrows(() => (afterPlace with { Gold = 3 }).BuyXp(), "buying XP rejects insufficient gold");
Require(EconomyConfig.Default.MaxPlayerLevel == 5, "default economy config supports five player levels");
Require(string.Join(",", Enumerable.Range(1, 5).Select(EconomyConfig.Default.GetXpThresholdForLevel)) == "0,4,8,12,16", "player levels use the configured XP thresholds");
Require(string.Join(",", Enumerable.Range(1, 5).Select(EconomyConfig.Default.GetHeroLimitForLevel)) == "2,3,4,5,6", "player levels use the configured field hero limits");
var levelOneOdds = EconomyConfig.Default.GetShopRarityOddsForLevel(1);
Require(levelOneOdds.GetChance(HeroRarity.Common) == 80 && levelOneOdds.GetChance(HeroRarity.Rare) == 20, "level 1 shop odds match the GDD common/rare split");
Require(levelOneOdds.TotalChance == 100, "shop rarity odds sum to 100 percent");
var levelFiveOdds = EconomyConfig.Default.GetShopRarityOddsForLevel(5);
Require(levelFiveOdds.GetChance(HeroRarity.Common) == 20 && levelFiveOdds.GetChance(HeroRarity.Rare) == 35 && levelFiveOdds.GetChance(HeroRarity.Epic) == 35 && levelFiveOdds.GetChance(HeroRarity.Legendary) == 10, "level 5 shop odds match the GDD rarity table");
RequireThrows(() => EconomyConfig.Default.GetShopRarityOddsForLevel(0), "shop rarity odds reject levels below one");
RequireThrows(() => EconomyConfig.Default.GetShopRarityOddsForLevel(6), "shop rarity odds reject levels above five");
RequireThrows(() => new ShopRarityOdds(Common: -1, Rare: 101, Epic: 0, Legendary: 0), "shop rarity odds reject negative chances");
Require(EconomyConfig.Default.GetXpCostForNextLevel(1) == 4, "level 1 advances at four banked XP");
var leveledByConfig = afterXp.LevelUp();
Require(leveledByConfig.PlayerLevel == 2 && leveledByConfig.Xp == 0, "level-up uses the configured XP threshold for the next level");
RequireThrows(() => state.LevelUp(), "configured level-up rejects insufficient XP");
RequireThrows(() => (state with { PlayerLevel = 5, Xp = 100 }).LevelUp(), "configured level-up rejects the max player level");
RequireThrows(() => EconomyConfig.Default.GetXpThresholdForLevel(0), "XP thresholds reject levels below one");
RequireThrows(() => EconomyConfig.Default.GetXpThresholdForLevel(6), "XP thresholds reject levels above five");
Require(EconomyConfig.Default.CalculateGoldIncome(wonCombat: true, winStreak: 5, currentGold: 30, eventBonus: 2) == 11, "gold income sums base, win, streak, interest, and event bonuses");
Require(EconomyConfig.Default.BaseIncome == 3, "base income is three gold after combat");
Require(EconomyConfig.Default.WinBonus == 1 && EconomyConfig.Default.CalculateGoldIncome(wonCombat: true, winStreak: 0, currentGold: 0) == 4, "win bonus adds one gold for victory");
Require(EconomyConfig.Default.CalculateStreakBonus(2) == 0, "streak bonus is zero before three wins");
Require(EconomyConfig.Default.CalculateStreakBonus(3) == 1, "streak bonus adds one gold at three wins");
Require(EconomyConfig.Default.CalculateStreakBonus(5) == 2, "streak bonus adds two gold at five wins");
Require(EconomyConfig.Default.CalculateInterestBonus(0) == 0, "interest bonus is zero below ten gold");
Require(EconomyConfig.Default.CalculateInterestBonus(10) == 1, "interest bonus adds one gold per ten saved gold");
Require(EconomyConfig.Default.CalculateInterestBonus(29) == 2, "interest bonus floors partial ten-gold steps");
Require(EconomyConfig.Default.CalculateInterestBonus(30) == 3 && EconomyConfig.Default.CalculateInterestBonus(99) == 3, "interest bonus caps at three gold");
Require(EconomyConfig.Default.CalculateGoldIncome(wonCombat: false, winStreak: 0, currentGold: 0, eventBonus: 4) == 7, "event bonus can add incoming event gold");
Require(EconomyConfig.Default.CalculateGoldIncome(wonCombat: false, winStreak: 0, currentGold: 0, eventBonus: -1) == 2, "event bonus can represent an event penalty");
Require(EconomyConfig.Default.CalculateGoldIncome(wonCombat: false, winStreak: 0, currentGold: 0) == 3, "gold income works without optional bonuses");
Require(EconomyConfig.Default.MinRunHealthDamage == 2 && EconomyConfig.Default.MaxRunHealthDamage == 8, "run health damage clamp is configured to 2-8");
Require(EconomyConfig.Default.CalculateRunHealthDamage(roundNumber: 1, survivingEnemyStars: 0) == 2, "run health damage starts at two on round 1");
Require(EconomyConfig.Default.CalculateRunHealthDamage(roundNumber: 2, survivingEnemyStars: 0) == 2, "run health damage floors round scaling below round 3");
Require(EconomyConfig.Default.CalculateRunHealthDamage(roundNumber: 3, survivingEnemyStars: 2) == 5, "run health damage includes round scaling and surviving enemy stars");
Require(EconomyConfig.Default.CalculateRunHealthDamage(roundNumber: 5, survivingEnemyStars: 0) == 3, "run health damage floors fractional round scaling");
Require(EconomyConfig.Default.CalculateRunHealthDamage(roundNumber: 10, survivingEnemyStars: 20) == 8, "run health damage is clamped to its maximum");
RequireThrows(() => EconomyConfig.Default.CalculateRunHealthDamage(roundNumber: 0, survivingEnemyStars: 0), "run health damage rejects invalid rounds");
RequireThrows(() => EconomyConfig.Default.CalculateRunHealthDamage(roundNumber: 1, survivingEnemyStars: -1), "run health damage rejects negative surviving stars");
RequireThrows(() => EconomyConfig.Default.CalculateGoldIncome(wonCombat: true, winStreak: -1, currentGold: 0), "gold income rejects negative streaks");
RequireThrows(() => EconomyConfig.Default.CalculateGoldIncome(wonCombat: true, winStreak: 0, currentGold: -1), "gold income rejects negative current gold");

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
Require(!combat.HadChainReaction, "new combat starts without a recorded chain reaction");
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
Require(afterRuneSwap.Commander.Energy >= inCombat.Commander.Energy, "rune swaps preserve or increase the commander energy bar");
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
Require(Math.Abs(restoredCombat.LastCommanderEnergyGain - savedCombat.LastCommanderEnergyGain) < 1e-9, "restored progress preserves last commander energy gain");
Require(restoredCombat.LastMatch4ComboCount == savedCombat.LastMatch4ComboCount, "restored progress preserves last match-4 combo count");
Require(restoredCombat.LastBonusBlueRunesCreated == savedCombat.LastBonusBlueRunesCreated, "restored progress preserves last bonus blue rune count");
Require(restoredCombat.HadChainReaction == savedCombat.HadChainReaction, "restored progress preserves chain reaction state");
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
Require(reward.Gold == 2, "claiming reward adds gold");
var chainGoldReward = (inCombat with
{
    Combat = combat with { EarnedChainFourGoldBonus = true }
}).ClaimReward(2);
Require(chainGoldReward.Gold == inCombat.Gold + 2 + CombatState.ChainFourGoldBonus, "chain 4+ grants one bonus gold after combat");
Require(chainGoldReward.Combat is null, "claiming a chain 4+ reward still clears combat state");
var alchemistChainReward = (RunState.NewRun("alchemist") with
{
    Phase = RunPhase.Combat,
    Combat = combat with { HadChainReaction = true }
}).ClaimReward(2);
Require(alchemistChainReward.Gold == alchemistRun.Gold + 2 + 1, "Alchemist gains one gold after a round with any chain reaction");
var alchemistNoChainReward = (RunState.NewRun("alchemist") with
{
    Phase = RunPhase.Combat,
    Combat = combat
}).ClaimReward(2);
Require(alchemistNoChainReward.Gold == alchemistRun.Gold + 2, "Alchemist gains no passive gold without a chain reaction");
var nonAlchemistChainReward = (inCombat with
{
    Combat = combat with { HadChainReaction = true }
}).ClaimReward(2);
Require(nonAlchemistChainReward.Gold == inCombat.Gold + 2, "non-Alchemist commanders do not gain chain-reaction gold");

// Aggregate round-reward calculation (GDD "итоговый расчёт наград за раунд"): one breakdown
// is the single source of truth, and ClaimReward credits exactly its total.
var plainReward = afterRuneSwap.RoundReward(2);
Require(plainReward.BaseGold == 2 && plainReward.BonusGold == 0 && plainReward.TotalGold == 2, "the round-reward breakdown reports the base payout with no bonus");
var chainBreakdownRun = inCombat with { Combat = combat with { EarnedChainFourGoldBonus = true } };
var chainBreakdown = chainBreakdownRun.RoundReward(2);
Require(chainBreakdown.ChainBonusGold == CombatState.ChainFourGoldBonus && chainBreakdown.TotalGold == 2 + CombatState.ChainFourGoldBonus, "the breakdown isolates the chain 4+ bonus");
Require(chainBreakdownRun.ClaimReward(2).Gold == chainBreakdownRun.Gold + chainBreakdown.TotalGold, "ClaimReward credits exactly the breakdown total");
var alchemistBreakdown = (RunState.NewRun("alchemist") with
{
    Phase = RunPhase.Combat,
    Combat = combat with { HadChainReaction = true }
}).RoundReward(2);
Require(alchemistBreakdown.AlchemistBonusGold == 1 && alchemistBreakdown.ChainBonusGold == 0, "the breakdown isolates the Alchemist chain-reaction bonus");
var defaultGoldBreakdown = afterRuneSwap.RoundReward();
Require(defaultGoldBreakdown.BaseGold == afterRuneSwap.CurrentRoundDefinition.BaseGoldReward, "the breakdown defaults to the round's base gold payout");
var artifactRoundBreakdown = (RunState.NewRun() with { Round = 5, Phase = RunPhase.Combat, Combat = combat }).RoundReward(3);
Require(artifactRoundBreakdown.OffersArtifactChoice, "the breakdown reports an artifact-reward round offers a choice");
RequireThrows(() => afterRuneSwap.RoundReward(-1), "the round-reward breakdown rejects negative gold");
RequireThrows(() => RoundRewardBreakdown.ForCombatResolution(null!), "the round-reward breakdown rejects a null run");

var nextRound = reward.AdvanceRound("round_02_rogue_band");
Require(nextRound.Round == 2, "advancing reward starts the next round");
Require(nextRound.Phase == RunPhase.Preparation, "advancing reward returns to preparation");
Require(nextRound.Shop.Offers.Count == 3, "next round refreshes the shop");
Require(nextRound.NextEnemyId == "round_02_rogue_band", "next round updates the enemy preview");
var levelThreeNextRound = (reward with { PlayerLevel = 3 }).AdvanceRound("round_02_rogue_band");
Require(levelThreeNextRound.Shop.Offers.Count == 4, "level 3 next-round shop refresh uses four offers");
Require(PveRunSchedule.Rounds.Count == 10, "MVP PvE schedule has 10 rounds");
Require(PveRunSchedule.FirstRound == 1 && PveRunSchedule.FinalRound == 10, "MVP PvE run spans rounds 1 through 10");
Require(PveRunSchedule.GetRound(1).EnemyId == state.NextEnemyId, "new run uses the first scheduled enemy");
Require(PveRunSchedule.GetRound(1).PreventsRunDefeat, "tutorial round prevents full run defeat");
RequireThrows(() => PveRunSchedule.GetRound(11), "round 11 is outside the MVP schedule");

// Round identities, types, goals and rewards mirror the GDD "Первые 10 раундов" table.
Require(PveRunSchedule.GetRound(1).Type == PveRoundType.Tutorial, "round 1 is the tutorial round");
Require(PveRunSchedule.GetRound(1).RoundReward.GrantsStarterHero, "round 1 rewards a one-cost starter hero");
Require(PveRunSchedule.GetRound(1).EnemyName.Contains("манекен"), "round 1 fights the training dummies");
Require(PveRunSchedule.GetRound(2).Type == PveRoundType.Combat && PveRunSchedule.GetRound(2).BaseGoldReward == 4, "round 2 is a four-gold combat round");
Require(PveRunSchedule.GetRound(3).RoundReward.HeroChoice, "round 3 rewards a hero choice");
Require(PveRunSchedule.GetRound(4).Type == PveRoundType.Event && !PveRunSchedule.GetRound(4).HasCombat, "round 4 is a no-combat event");
Require(PveRunSchedule.GetRound(4).RoundReward.ArtifactOrGold, "round 4 offers an artifact-or-gold choice");
Require(PveRunSchedule.GetRound(5).Type == PveRoundType.Elite && PveRunSchedule.GetRound(5).RoundReward.Artifact, "round 5 is an elite fight rewarding an artifact");
Require(PveRunSchedule.GetRound(7).RoundReward.HeroChoice, "round 7 rewards a hero choice");
Require(PveRunSchedule.GetRound(8).Type == PveRoundType.Boss && PveRunSchedule.GetRound(8).RoundReward.RareArtifact, "round 8 is the boss rewarding a rare artifact");
Require(PveRunSchedule.GetRound(8).BaseGoldReward == 7, "round 8 boss pays seven gold");
Require(PveRunSchedule.GetRound(9).Type == PveRoundType.EnhancedShop && !PveRunSchedule.GetRound(9).HasCombat, "round 9 is a no-combat enhanced shop");
Require(PveRunSchedule.GetRound(9).RoundReward.FreeReroll && PveRunSchedule.GetRound(9).BaseGoldReward == 4, "round 9 grants a free reroll and four gold");
Require(PveRunSchedule.GetRound(10).Type == PveRoundType.FinalBoss && PveRunSchedule.GetRound(10).RoundReward.RunVictory, "round 10 is the final boss that wins the run");
Require(PveRunSchedule.GetRound(10).EnemyName.Contains("Совет"), "round 10 fights the Council of Three Factions");

// Difficulty pacing tiers mirror the GDD "Темп сложности" section.
Require(PveRunSchedule.GetDifficultyTier(1) == PveDifficultyTier.Fundamentals, "rounds 1-3 teach the fundamentals");
Require(PveRunSchedule.GetDifficultyTier(3) == PveDifficultyTier.Fundamentals, "round 3 still teaches fundamentals");
Require(PveRunSchedule.GetDifficultyTier(4) == PveDifficultyTier.ChoicesAndCounters, "rounds 4-6 introduce choices and counters");
Require(PveRunSchedule.GetDifficultyTier(6) == PveDifficultyTier.ChoicesAndCounters, "round 6 still tests choices and counters");
Require(PveRunSchedule.GetDifficultyTier(7) == PveDifficultyTier.SynergyCheck, "rounds 7-8 check synergies");
Require(PveRunSchedule.GetDifficultyTier(8) == PveDifficultyTier.SynergyCheck, "round 8 checks synergies");
Require(PveRunSchedule.GetDifficultyTier(9) == PveDifficultyTier.FullBuildCheck, "rounds 9-10 check the whole build");
Require(PveRunSchedule.GetDifficultyTier(10) == PveDifficultyTier.FullBuildCheck, "round 10 checks the whole build");

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

var tutorialDamageProtection = RunState.NewRun().ApplyRunDamage(20);
Require(tutorialDamageProtection.RunHealth == 1 && tutorialDamageProtection.Phase == RunPhase.Preparation, "tutorial round run damage cannot defeat the run");
var tutorialCombatProtection = inCombat.ResolveCombatDefeat("tutorial_loss");
Require(tutorialCombatProtection.Phase == RunPhase.Reward && tutorialCombatProtection.DefeatReason is null, "tutorial combat defeat advances without ending the run");
var healthDefeat = (RunState.NewRun() with { Round = 2 }).ApplyRunDamage(20);
Require(healthDefeat.Phase == RunPhase.Defeat, "run health depletion causes defeat");
Require(healthDefeat.IsRunLost, "health defeat exposes a run loss flag");
Require(healthDefeat.IsRunComplete, "health defeat marks the run complete");
Require(healthDefeat.DefeatReason == "run_health_depleted", "health defeat records a reason");

var combatDefeat = (afterPlace with { Round = 2 }).StartCombat().ResolveCombatDefeat("all_allies_defeated");
Require(combatDefeat.Phase == RunPhase.Defeat, "combat condition failure causes defeat");
Require(combatDefeat.Combat is null, "combat defeat clears combat state");
Require(combatDefeat.IsRunLost, "combat defeat exposes a run loss flag");
Require(combatDefeat.DefeatReason == "all_allies_defeated", "combat defeat records a reason");
RequireThrows(() => afterPlace.ResolveCombatDefeat(), "combat defeat cannot be resolved outside combat");
RequireThrows(() => (afterPlace with { Round = 2 }).StartCombat().ResolveCombatDefeat(" "), "combat defeat requires a reason");

var defeatedByAllies = (afterPlace with { Round = 2 }).StartCombat(1337).ResolveCombatTick(1, allAlliesDefeated: true);
Require(defeatedByAllies.Phase == RunPhase.Defeat, "all allies defeated resolves combat defeat");
Require(defeatedByAllies.DefeatReason == "all_allies_defeated", "allies defeat records a reason");

var rewardedByEnemies = afterPlace.StartCombat(1337).ResolveCombatTick(1, allEnemiesDefeated: true, goldReward: 1);
Require(rewardedByEnemies.Phase == RunPhase.Reward, "all enemies defeated resolves combat victory");
Require(rewardedByEnemies.Gold == afterPlace.Gold + 1, "combat victory tick grants reward");

var timerDefeat = (afterPlace with { Round = 2 }).StartCombat(1337, 5)
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
var replacedRuneBoard = legalMatchBoard.ReplaceRune(new BoardPoint(0, 3), RuneType.Blue);
Require(replacedRuneBoard[new BoardPoint(0, 3)] == RuneType.Blue, "board replacement can place a specific rune");
Require(legalMatchBoard[new BoardPoint(0, 3)] != RuneType.Blue, "board replacement does not mutate the source board");
RequireThrows(() => legalMatchBoard.ReplaceRune(new BoardPoint(9, 9), RuneType.Blue), "board replacement rejects out-of-board cells");
var whiteEnergyBoard = CreatePatternBoard(
    (new BoardPoint(0, 0), RuneType.White),
    (new BoardPoint(0, 1), RuneType.Blue),
    (new BoardPoint(0, 2), RuneType.White),
    (new BoardPoint(1, 1), RuneType.White)
);
var whiteEnergyRun = state with
{
    Phase = RunPhase.Combat,
    Combat = new CombatState(
        RuneBoard: whiteEnergyBoard,
        Match3MovesUsed: 0,
        LastMatchedRunesCount: 0,
        LastComboDepth: 0,
        LastMatchPower: 0,
        DurationSeconds: CombatState.DefaultDurationSeconds,
        ElapsedSeconds: 0,
        GlobalCooldownMillisecondsRemaining: 0,
        SecondsSinceLastRuneSwap: 0,
        SlowdownMillisecondsRemaining: 0)
};
var afterWhiteEnergySwap = whiteEnergyRun.SwapRunes(legalMatchA, legalMatchB);
Require(afterWhiteEnergySwap.Combat?.LastCommanderEnergyGain >= 3.0, "white rune swaps record commander energy gained from the match");
Require(Math.Abs(afterWhiteEnergySwap.Commander.Energy - (whiteEnergyRun.Commander.Energy + (afterWhiteEnergySwap.Combat?.LastCommanderEnergyGain ?? 0.0))) < 1e-9, "white rune swaps fill the commander energy bar");
var match4SwapBoard = CreatePatternBoard(
    (new BoardPoint(0, 0), RuneType.Red),
    (new BoardPoint(0, 1), RuneType.Blue),
    (new BoardPoint(0, 2), RuneType.Red),
    (new BoardPoint(0, 3), RuneType.Red),
    (new BoardPoint(1, 1), RuneType.Red)
);
var runeArchonReadyRun = state with
{
    Commander = state.Commander with { Match4CombosTowardPassive = 2 },
    Phase = RunPhase.Combat,
    Combat = new CombatState(
        RuneBoard: match4SwapBoard,
        Match3MovesUsed: 0,
        LastMatchedRunesCount: 0,
        LastComboDepth: 0,
        LastMatchPower: 0,
        DurationSeconds: CombatState.DefaultDurationSeconds,
        ElapsedSeconds: 0,
        GlobalCooldownMillisecondsRemaining: 0,
        SecondsSinceLastRuneSwap: 0,
        SlowdownMillisecondsRemaining: 0)
};
var afterRuneArchonMatch4 = runeArchonReadyRun.SwapRunes(legalMatchA, legalMatchB);
var archonCombat = afterRuneArchonMatch4.Combat ?? throw new InvalidOperationException("Smoke check failed: rune archon combat missing");
Require(archonCombat.LastMatch4ComboCount >= 1, "match-4 swaps record match-4 combo count");
Require(archonCombat.LastBonusBlueRunesCreated >= 1, "Rune Archon creates a bonus blue rune on every third match-4 combo");
Require(afterRuneArchonMatch4.Commander.Match4CombosTowardPassive == (2 + archonCombat.LastMatch4ComboCount) % 3, "Rune Archon match-4 passive progress carries remainder forward");
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
Require(chainResolution.Steps[0].Effects.Count == 1, "chain step stores resolved rune effects");
Require(chainResolution.Steps[0].Match4ComboCount == 0, "chain step reports zero match-4 combos for match-3 effects");
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
Require(redMatch4Board.ResolveChainReactions(99).Steps[0].Match4ComboCount == 1, "chain steps count match-4 effects");
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
Require(Math.Abs(match5CreationResolution.Steps[0].CommanderEnergyGain - RuneEffects.TShapeCommanderEnergy) < 1e-9, "T/L chain steps expose commander energy gain for the run bar");
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
Require(CombatFormulas.WouldCritByCadence(19), "the twentieth landed hit crits on the 5 percent cadence");
Require(!CombatFormulas.WouldCritByCadence(18) && !CombatFormulas.WouldCritByCadence(0), "earlier landed hits do not crit on the cadence");
RequireThrows(() => CombatFormulas.WouldCritByCadence(-1), "crit cadence rejects a negative landed count");
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

Require(HeroCatalog.All.Count == 20, "hero catalog holds the 20 MVP heroes");
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
RequireThrows(() => HeroCatalog.Get("missing_hero"), "hero catalog rejects unknown ids");

Require(HeroCatalog.All.Select(hero => hero.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == HeroCatalog.All.Count, "catalog hero ids are unique");
Require(HeroCatalog.All.All(hero => HeroRarities.GetCostRange(hero.Rarity) is var range && hero.Cost >= range.Min && hero.Cost <= range.Max), "every catalog hero cost fits its rarity range");

var catalogMistCutthroat = HeroCatalog.Get("mist_cutthroat");
Require(catalogMistCutthroat.Name == "Туманный Резчик", "mist cutthroat uses the GDD display name");
Require(catalogMistCutthroat.Rarity == HeroRarity.Rare && catalogMistCutthroat.Cost == 2, "mist cutthroat is a two-cost rare hero");
Require(catalogMistCutthroat.Faction == "Духи" && catalogMistCutthroat.Class == "Убийца", "mist cutthroat belongs to the Spirit Assassin line");
Require(catalogMistCutthroat.RuneAffinity == RuneType.Purple && catalogMistCutthroat.Role == HeroRole.Assassin, "mist cutthroat is a purple-rune assassin");
Require(catalogMistCutthroat.AttackType == "melee" && catalogMistCutthroat.Targeting == "farthest_enemy", "mist cutthroat jumps to the farthest enemy");

var catalogRuneApprentice = HeroCatalog.Get("rune_apprentice");
Require(catalogRuneApprentice.Faction == "Империя" && catalogRuneApprentice.Class == "Маг", "rune apprentice belongs to the Empire Mage line");
Require(catalogRuneApprentice.RuneAffinity == RuneType.Blue && catalogRuneApprentice.Role == HeroRole.Caster, "rune apprentice is a blue-rune caster");
Require(catalogRuneApprentice.AttackType == "ranged" && catalogRuneApprentice.Targeting == "two_targets", "rune apprentice strikes two targets");

var catalogGearSquire = HeroCatalog.Get("gear_squire");
Require(catalogGearSquire.Faction == "Механисты" && catalogGearSquire.Role == HeroRole.Tank, "gear squire is a Mechanist tank");
Require(catalogGearSquire.RuneAffinity == RuneType.Yellow && catalogGearSquire.Rarity == HeroRarity.Rare, "gear squire is a yellow-rune rare hero");

var catalogSparkTinker = HeroCatalog.Get("spark_tinker");
Require(catalogSparkTinker.Faction == "Механисты" && catalogSparkTinker.Role == HeroRole.Caster, "spark tinker is a Mechanist caster");
Require(catalogSparkTinker.RuneAffinity == RuneType.Blue, "spark tinker channels blue runes");

var catalogAbyssAcolyte = HeroCatalog.Get("abyss_acolyte");
Require(catalogAbyssAcolyte.Faction == "Бездонные" && catalogAbyssAcolyte.Role == HeroRole.Caster, "abyss acolyte is an Abyssal caster");
Require(catalogAbyssAcolyte.RuneAffinity == RuneType.Purple, "abyss acolyte channels purple runes");

var catalogSpiritDuelist = HeroCatalog.Get("spirit_duelist");
Require(catalogSpiritDuelist.Faction == "Духи" && catalogSpiritDuelist.Role == HeroRole.Bruiser, "spirit duelist is a Spirit bruiser");
Require(catalogSpiritDuelist.RuneAffinity == RuneType.White, "spirit duelist channels white runes");

var catalogDuskRanger = HeroCatalog.Get("dusk_ranger");
Require(catalogDuskRanger.Rarity == HeroRarity.Epic && catalogDuskRanger.Cost == 3, "dusk ranger is a three-cost epic hero");
Require(catalogDuskRanger.Faction == "Дикие" && catalogDuskRanger.Role == HeroRole.Carry, "dusk ranger is a Wild carry");

var catalogBulwarkCaptain = HeroCatalog.Get("bulwark_captain");
Require(catalogBulwarkCaptain.Faction == "Империя" && catalogBulwarkCaptain.Role == HeroRole.Tank, "bulwark captain is an Empire tank");
Require(catalogBulwarkCaptain.Cost == 3 && catalogBulwarkCaptain.Rarity == HeroRarity.Epic, "bulwark captain is a three-cost epic hero");

var catalogVoidOracle = HeroCatalog.Get("void_oracle");
Require(catalogVoidOracle.Faction == "Бездонные" && catalogVoidOracle.Role == HeroRole.Support, "void oracle is an Abyssal support");
Require(catalogVoidOracle.RuneAffinity == RuneType.Green, "void oracle channels green runes");

var catalogDroneMarshal = HeroCatalog.Get("drone_marshal");
Require(catalogDroneMarshal.Faction == "Механисты" && catalogDroneMarshal.Role == HeroRole.Summoner, "drone marshal is a Mechanist summoner");
Require(catalogDroneMarshal.Targeting == "summon_slot", "drone marshal uses summon-slot targeting");

var catalogPhaseAssassin = HeroCatalog.Get("phase_assassin");
Require(catalogPhaseAssassin.Faction == "Духи" && catalogPhaseAssassin.Role == HeroRole.Assassin, "phase assassin is a Spirit assassin");
Require(catalogPhaseAssassin.Targeting == "backline_enemy", "phase assassin strikes the enemy backline");

var catalogMagmaBrute = HeroCatalog.Get("magma_brute");
Require(catalogMagmaBrute.Cost == 4 && catalogMagmaBrute.Rarity == HeroRarity.Epic, "magma brute is a four-cost epic hero");
Require(catalogMagmaBrute.Faction == "Дикие" && catalogMagmaBrute.Role == HeroRole.Bruiser, "magma brute is a Wild bruiser");

var catalogCurseWeaver = HeroCatalog.Get("curse_weaver");
Require(catalogCurseWeaver.Cost == 4 && catalogCurseWeaver.Faction == "Бездонные", "curse weaver is a four-cost Abyssal hero");
Require(catalogCurseWeaver.RuneAffinity == RuneType.Purple && catalogCurseWeaver.Role == HeroRole.Caster, "curse weaver is a purple-rune caster");

var catalogClockworkSaint = HeroCatalog.Get("clockwork_saint");
Require(catalogClockworkSaint.Cost == 4 && catalogClockworkSaint.Faction == "Механисты", "clockwork saint is a four-cost Mechanist hero");
Require(catalogClockworkSaint.Role == HeroRole.Healer && catalogClockworkSaint.RuneAffinity == RuneType.Green, "clockwork saint is a green-rune healer");

var catalogAstralRegent = HeroCatalog.Get("astral_regent");
Require(catalogAstralRegent.Rarity == HeroRarity.Legendary && catalogAstralRegent.Cost == 5, "astral regent is a five-cost legendary hero");
Require(catalogAstralRegent.Faction == "Духи" && catalogAstralRegent.RuneAffinity == RuneType.White, "astral regent is a white-rune Spirit legend");

// Onboarding ability complexity (GDD "Слишком сложный onboarding": "стартовые герои должны
// иметь простые способности"). Every Common starter hero must be simple, and the round-1
// starter reward draws only from the Common pool, so a new player never meets a complex ability.
Require(HeroCatalog.All.All(hero => hero.Rarity != HeroRarity.Common || hero.HasSimpleAbility), "every Common starter hero has a simple ability");
Require(catalogIronGuard.HasSimpleAbility && catalogOathArcher.HasSimpleAbility && catalogFieldMedic.HasSimpleAbility && catalogWildClaw.HasSimpleAbility && catalogThornShaman.HasSimpleAbility, "the five Common starter heroes all read as simple");
Require(catalogRuneApprentice.HasSimpleAbility && catalogGearSquire.HasSimpleAbility && catalogClockworkSaint.HasSimpleAbility && catalogDroneMarshal.HasSimpleAbility, "direct-effect heroes keep their simple ability classification");
Require(catalogMistCutthroat.AbilityComplexity == AbilityComplexity.Advanced && catalogAstralRegent.AbilityComplexity == AbilityComplexity.Advanced, "positioning and board-wide abilities are flagged advanced");
Require(!catalogPhaseAssassin.HasSimpleAbility && !catalogCurseWeaver.HasSimpleAbility && !catalogSpiritDuelist.HasSimpleAbility && !catalogVoidOracle.HasSimpleAbility, "control, illusion and conditional abilities are flagged advanced");
Require(HeroCatalog.SimpleAbilityHeroes.All(hero => hero.HasSimpleAbility) && HeroCatalog.SimpleAbilityHeroes.Count == HeroCatalog.All.Count(hero => hero.HasSimpleAbility), "the simple-ability hero set matches the heroes flagged simple");
Require(HeroCatalog.All.Where(hero => hero.Rarity == HeroRarity.Common).All(hero => HeroCatalog.SimpleAbilityHeroes.Contains(hero)), "the simple-ability set contains every Common starter hero");
var starterRewardRun = RunState.NewRun() with { Phase = RunPhase.Reward };
Require(starterRewardRun.RewardHeroOptions().Count > 0, "the round-1 starter reward offers heroes");
Require(starterRewardRun.RewardHeroOptions().All(option => HeroCatalog.Get(option.Id).HasSimpleAbility), "the round-1 starter reward only offers simple-ability heroes");

Require(HeroCatalog.All.Count(hero => hero.Faction == "Империя") == 5, "MVP roster has five Empire heroes");
Require(HeroCatalog.All.Count(hero => hero.Faction == "Дикие") == 4, "MVP roster has four Wild heroes");
Require(HeroCatalog.All.Count(hero => hero.Faction == "Бездонные") == 3, "MVP roster has three Abyssal heroes");
Require(HeroCatalog.All.Count(hero => hero.Faction == "Механисты") == 4, "MVP roster has four Mechanist heroes");
Require(HeroCatalog.All.Count(hero => hero.Faction == "Духи") == 4, "MVP roster has four Spirit heroes");

Require(FactionCatalog.All.Count == 5, "MVP defines five factions");
Require(ClassCatalog.All.Count == 7, "MVP class list resolves to seven classes");
Require(FactionCatalog.All.All(faction => HeroCatalog.All.Any(hero => hero.Faction == faction.Name)), "every faction has at least one catalog hero");
Require(HeroCatalog.All.All(hero => FactionCatalog.TryGet(hero.Faction, out _)), "every catalog hero maps to a known faction");
Require(HeroCatalog.All.All(hero => ClassCatalog.TryGet(hero.Class, out _)), "every catalog hero maps to a known class");
Require(FactionCatalog.Get("Империя").Tiers.Count == 2, "Empire faction has two synergy tiers");
Require(ClassCatalog.Get("Маг").Tiers[0].RequiredCount == 3, "Mage synergy starts at three mages");
Require(ClassCatalog.Get("Убийца").Tiers[1].RequiredCount == 6, "Assassin synergy peaks at six assassins");
Require(ClassCatalog.Get("Стрелок").Tiers.Count == 0, "Marksman carries no MVP class synergy tier");
RequireThrows(() => FactionCatalog.Get("Эльфы"), "faction lookup rejects unknown names");

Require(SynergyCalculator.EvaluateByHeroIds(Array.Empty<string>()).Count == 0, "an empty board produces no synergies");

var empirePairSynergies = SynergyCalculator.EvaluateByHeroIds(new[] { "iron_guard", "oath_archer" });
var empireSynergy = empirePairSynergies.Single(progress => progress.Definition.Name == "Империя");
Require(empireSynergy.UnitCount == 2 && empireSynergy.ActiveTiers.Count == 1, "two Empire heroes activate the first Empire tier");
Require(empireSynergy.NextTier is not null && empireSynergy.NextTier.RequiredCount == 4, "the next Empire tier needs four heroes");

var duplicateSynergies = SynergyCalculator.EvaluateByHeroIds(new[] { "iron_guard", "iron_guard" });
Require(duplicateSynergies.Single(progress => progress.Definition.Name == "Империя").UnitCount == 1, "duplicate hero ids count once for synergies");

var mageSynergies = SynergyCalculator.EvaluateByHeroIds(new[] { "rune_apprentice", "spark_tinker", "abyss_acolyte" });
var mageSynergy = mageSynergies.Single(progress => progress.Definition.Name == "Маг");
Require(mageSynergy.IsActive && mageSynergy.ActiveTiers.Count == 1, "three mages activate the mage synergy");
var mageDamageModifiers = SynergyModifiers.FromProgress(mageSynergies);
Require(Math.Abs(mageDamageModifiers.AbilityDamageMultiplier - 1.20) < 1e-9, "three mages unlock the +20 percent ability damage modifier");
var mageFiveSynergies = SynergyCalculator.EvaluateByHeroIds(new[] { "rune_apprentice", "spark_tinker", "abyss_acolyte", "curse_weaver", "astral_regent" });
var mageFiveModifiers = SynergyModifiers.FromProgress(mageFiveSynergies);
Require(mageFiveModifiers.MageBlueMatch4BonusCharge, "five mages unlock the blue match-4 bonus charge");

var synergyBoard = new List<BoardHero>
{
    new(new HeroInstance("syn_def_1", "iron_guard", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("syn_def_2", "bulwark_captain", 2), new TacticalPosition(2, 1))
};
var defenderSynergy = SynergyCalculator.ActiveSynergies(synergyBoard).Single(progress => progress.Definition.Name == "Защитник");
Require(defenderSynergy.UnitCount == 2 && defenderSynergy.IsActive, "two defenders on the board activate the defender synergy");
var defenderHealthModifiers = SynergyModifiers.ForTeam(synergyBoard);
Require(Math.Abs(defenderHealthModifiers.FrontlineHealthMultiplier - 1.15) < 1e-9, "two defenders unlock the +15 percent frontline health modifier");
var defenderFourProgress = new[]
{
    new SynergyProgress(
        ClassCatalog.Defender,
        4,
        ClassCatalog.Defender.ActiveTiers(4),
        ClassCatalog.Defender.NextTier(4))
};
var defenderFourModifiers = SynergyModifiers.FromProgress(defenderFourProgress);
Require(defenderFourModifiers.DefenderYellowRuneArmorBoost, "four defenders unlock yellow-rune armor boosts");

var assassinThreeProgress = new[]
{
    new SynergyProgress(
        ClassCatalog.Assassin,
        3,
        ClassCatalog.Assassin.ActiveTiers(3),
        ClassCatalog.Assassin.NextTier(3))
};
var assassinThreeModifiers = SynergyModifiers.FromProgress(assassinThreeProgress);
Require(assassinThreeModifiers.AssassinBacklineStrike, "three assassins unlock the backline strike");
var assassinTwoProgress = new[]
{
    new SynergyProgress(
        ClassCatalog.Assassin,
        2,
        ClassCatalog.Assassin.ActiveTiers(2),
        ClassCatalog.Assassin.NextTier(2))
};
Require(!SynergyModifiers.FromProgress(assassinTwoProgress).AssassinBacklineStrike, "two assassins do not unlock the backline strike");
Require(!assassinThreeModifiers.AssassinCritChargesRedRunes, "three assassins do not unlock crit red-rune charging");
var assassinSixProgress = new[]
{
    new SynergyProgress(
        ClassCatalog.Assassin,
        6,
        ClassCatalog.Assassin.ActiveTiers(6),
        ClassCatalog.Assassin.NextTier(6))
};
var assassinSixModifiers = SynergyModifiers.FromProgress(assassinSixProgress);
Require(assassinSixModifiers.AssassinCritChargesRedRunes, "six assassins unlock crit red-rune charging");
Require(assassinSixModifiers.AssassinBacklineStrike, "six assassins still keep the backline strike");

var singleEmpireBoard = new List<BoardHero>
{
    new(new HeroInstance("empire_single_ig", "iron_guard", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("empire_duplicate_ig", "iron_guard", 1), new TacticalPosition(2, 1))
};
Require(Math.Abs(SynergyModifiers.ForTeam(singleEmpireBoard).ArmorMultiplier - 1.0) < 1e-9, "duplicate Empire heroes do not activate the armor synergy");

var empireArmorBoard = new List<BoardHero>
{
    new(new HeroInstance("empire_ig", "iron_guard", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("empire_oa", "oath_archer", 1), new TacticalPosition(3, 0))
};
var empireArmorModifiers = SynergyModifiers.ForTeam(empireArmorBoard);
Require(Math.Abs(empireArmorModifiers.ArmorMultiplier - 1.10) < 1e-9, "two Empire heroes unlock the +10 percent armor modifier");
var empireBuffedWildClaw = BattleUnit.FromHero(catalogWildClaw, 1, "wild_empire_ally", TacticalSide.Player, new TacticalPosition(2, 2), empireArmorModifiers);
Require(Math.Abs(empireBuffedWildClaw.Armor - 5.5) < 1e-9, "Empire 2 grants +10 percent armor to allied battle units");
var empireBuffedBoardHero = BattleUnit.FromBoardHero(empireArmorBoard[1], TacticalSide.Player, empireArmorModifiers);
Require(Math.Abs(empireBuffedBoardHero.Armor - 3.3) < 1e-9, "board heroes can be converted into synergy-buffed battle units");

var empireFourBoard = new List<BoardHero>
{
    new(new HeroInstance("empire4_ig", "iron_guard", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("empire4_oa", "oath_archer", 1), new TacticalPosition(3, 0)),
    new(new HeroInstance("empire4_fm", "field_medic", 1), new TacticalPosition(3, 1)),
    new(new HeroInstance("empire4_ra", "rune_apprentice", 1), new TacticalPosition(2, 1))
};
var empireFourModifiers = SynergyModifiers.ForTeam(empireFourBoard);
Require(empireFourModifiers.EmpireYellowRuneFrontlineShield, "four Empire heroes unlock the yellow-rune frontline shield modifier");

var wildAttackSpeedBoard = new List<BoardHero>
{
    new(new HeroInstance("wild2_wc", "wild_claw", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("wild2_ts", "thorn_shaman", 1), new TacticalPosition(3, 0))
};
var wildAttackSpeedModifiers = SynergyModifiers.ForTeam(wildAttackSpeedBoard);
Require(Math.Abs(wildAttackSpeedModifiers.AttackSpeedMultiplier - 1.10) < 1e-9, "two Wild heroes unlock the +10 percent attack-speed modifier");
var wildHastedClaw = BattleUnit.FromHero(catalogWildClaw, 1, "wild_hasted_claw", TacticalSide.Player, new TacticalPosition(2, 0), wildAttackSpeedModifiers);
Require(Math.Abs(wildHastedClaw.AttacksPerSecond - 0.99) < 1e-9, "Wild 2 increases battle-unit attack speed");

var wildFourBoard = new List<BoardHero>
{
    new(new HeroInstance("wild4_wc", "wild_claw", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("wild4_ts", "thorn_shaman", 1), new TacticalPosition(3, 0)),
    new(new HeroInstance("wild4_dr", "dusk_ranger", 1), new TacticalPosition(3, 1)),
    new(new HeroInstance("wild4_mb", "magma_brute", 1), new TacticalPosition(2, 1))
};
var wildFourModifiers = SynergyModifiers.ForTeam(wildFourBoard);
Require(wildFourModifiers.WildChainReactionLifesteal, "four Wild heroes unlock chain-reaction lifesteal");

var abyssalWeaknessBoard = new List<BoardHero>
{
    new(new HeroInstance("abyss2_aa", "abyss_acolyte", 1), new TacticalPosition(3, 0)),
    new(new HeroInstance("abyss2_vo", "void_oracle", 1), new TacticalPosition(3, 1))
};
var abyssalWeaknessModifiers = SynergyModifiers.ForTeam(abyssalWeaknessBoard);
Require(abyssalWeaknessModifiers.AbyssalAbilityWeakness, "two Abyssal heroes unlock ability-applied weakness");

var abyssalFourProgress = new[]
{
    new SynergyProgress(
        FactionCatalog.Abyssal,
        4,
        FactionCatalog.Abyssal.ActiveTiers(4),
        FactionCatalog.Abyssal.NextTier(4))
};
var abyssalFourModifiers = SynergyModifiers.FromProgress(abyssalFourProgress);
Require(abyssalFourModifiers.AbyssalPurpleRuneBonusDamage, "four Abyssal heroes unlock purple-rune bonus damage");

var mechanistOpeningDroneBoard = new List<BoardHero>
{
    new(new HeroInstance("mech2_gs", "gear_squire", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("mech2_st", "spark_tinker", 1), new TacticalPosition(3, 0))
};
var mechanistOpeningDroneModifiers = SynergyModifiers.ForTeam(mechanistOpeningDroneBoard);
Require(mechanistOpeningDroneModifiers.MechanistOpeningDrone, "two Mechanist heroes unlock the opening drone");

var mechanistFourBoard = new List<BoardHero>
{
    new(new HeroInstance("mech4_gs", "gear_squire", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("mech4_st", "spark_tinker", 1), new TacticalPosition(3, 0)),
    new(new HeroInstance("mech4_dm", "drone_marshal", 1), new TacticalPosition(3, 1)),
    new(new HeroInstance("mech4_cs", "clockwork_saint", 1), new TacticalPosition(2, 1))
};
var mechanistFourModifiers = SynergyModifiers.ForTeam(mechanistFourBoard);
Require(mechanistFourModifiers.MechanistOpeningDrone && mechanistFourModifiers.MechanistMatch4Turret, "four Mechanist heroes unlock opening drones and match-4 turrets");

var spiritDodgeBoard = new List<BoardHero>
{
    new(new HeroInstance("spirit2_mc", "mist_cutthroat", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("spirit2_sd", "spirit_duelist", 1), new TacticalPosition(3, 0))
};
var spiritDodgeModifiers = SynergyModifiers.ForTeam(spiritDodgeBoard);
Require(Math.Abs(spiritDodgeModifiers.DodgeChance - SynergyModifiers.SpiritDodgeChanceBonus) < 1e-9, "two Spirit heroes unlock ally dodge chance");

var spiritFourBoard = new List<BoardHero>
{
    new(new HeroInstance("spirit4_mc", "mist_cutthroat", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("spirit4_sd", "spirit_duelist", 1), new TacticalPosition(3, 0)),
    new(new HeroInstance("spirit4_pa", "phase_assassin", 1), new TacticalPosition(2, 1)),
    new(new HeroInstance("spirit4_ar", "astral_regent", 1), new TacticalPosition(3, 1))
};
var spiritFourModifiers = SynergyModifiers.ForTeam(spiritFourBoard);
Require(spiritFourModifiers.SpiritWhiteRuneIllusion, "four Spirit heroes unlock white-rune illusions");

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
var spiritDodgerUnit = BattleUnit.FromHero(catalogSpiritDuelist, 1, "spirit_dodger_unit", TacticalSide.Player, new TacticalPosition(3, 0), spiritDodgeModifiers);
Require(Math.Abs(spiritDodgerUnit.DodgeChance - SynergyModifiers.SpiritDodgeChanceBonus) < 1e-9, "Spirit 2 dodge chance applies when building battle units");
var backlineGuardUnit = BattleUnit.FromHero(ironGuardDefinition, 1, "ig_back", TacticalSide.Player, new TacticalPosition(3, 1));
Require(Math.Abs(backlineGuardUnit.Armor - 25.0) < 1e-9, "frontline guard does not modify backline armor");
var defenderFrontlineUnit = BattleUnit.FromHero(ironGuardDefinition, 1, "defender_front", TacticalSide.Player, new TacticalPosition(2, 0), defenderHealthModifiers);
Require(Math.Abs(defenderFrontlineUnit.MaxHealth - 115.0) < 1e-9 && Math.Abs(defenderFrontlineUnit.CurrentHealth - 115.0) < 1e-9, "Defender 2 grants +15 percent health to frontline units");
var defenderBacklineUnit = BattleUnit.FromHero(ironGuardDefinition, 1, "defender_back", TacticalSide.Player, new TacticalPosition(3, 0), defenderHealthModifiers);
Require(Math.Abs(defenderBacklineUnit.MaxHealth - 100.0) < 1e-9, "Defender 2 does not grant health to backline units");
var warlordFirstDefender = BattleUnit.FromHero(catalogIronGuard, 1, "warlord_first_defender", TacticalSide.Player, new TacticalPosition(2, 0))
    with { CurrentHealth = 300.0 };
var warlordSecondDefender = BattleUnit.FromHero(catalogGearSquire, 1, "warlord_second_defender", TacticalSide.Player, new TacticalPosition(2, 1));
var warlordEnemy = MakeUnit("warlord_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0);
var warlordBattle = BattleState.Create(
    new[] { warlordSecondDefender, warlordFirstDefender, warlordEnemy },
    playerCommander: CommanderCatalog.Warlord.CreateInitialState());
var buffedWarlordDefender = warlordBattle.Units.Single(unit => unit.UnitId == "warlord_first_defender");
var untouchedSecondDefender = warlordBattle.Units.Single(unit => unit.UnitId == "warlord_second_defender");
Require(Math.Abs(buffedWarlordDefender.MaxHealth - (warlordFirstDefender.MaxHealth * 1.2)) < 1e-9, "Warlord grants +20 percent max health to the first defender");
Require(Math.Abs(buffedWarlordDefender.HealthPercent - warlordFirstDefender.HealthPercent) < 1e-9, "Warlord preserves the first defender's current health ratio");
Require(Math.Abs(untouchedSecondDefender.MaxHealth - warlordSecondDefender.MaxHealth) < 1e-9, "Warlord does not buff additional defenders");
var nonWarlordBattle = BattleState.Create(
    new[] { warlordFirstDefender, warlordEnemy },
    playerCommander: CommanderCatalog.Alchemist.CreateInitialState());
Require(Math.Abs(nonWarlordBattle.Units.Single(unit => unit.UnitId == "warlord_first_defender").MaxHealth - warlordFirstDefender.MaxHealth) < 1e-9, "non-Warlord commanders do not apply the defender health passive");

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

var mechanistDroneBattle = BattleState.Create(
    new[]
    {
        MakeUnit("mech_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
        MakeUnit("mech_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
    },
    playerSynergyModifiers: mechanistOpeningDroneModifiers);
var playerDrone = mechanistDroneBattle.Units.Single(unit => unit.UnitId == "mechanist_drone_player");
Require(playerDrone.Side == TacticalSide.Player && playerDrone.Position.IsBackline, "Mechanist 2 spawns a player drone in a backline cell");
Require(playerDrone.IsRanged && Math.Abs(playerDrone.MaxHealth - BattleState.MechanistDroneHealth) < 1e-9, "Mechanist 2 drone uses the configured ranged drone stats");
Require(Math.Abs(playerDrone.Attack - BattleState.MechanistDroneAttack) < 1e-9, "Mechanist 2 drone has the configured attack");

var enemyMechanistDroneBattle = BattleState.Create(
    new[]
    {
        MakeUnit("enemy_mech_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
        MakeUnit("enemy_mech_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
    },
    enemySynergyModifiers: mechanistOpeningDroneModifiers);
var enemyDrone = enemyMechanistDroneBattle.Units.Single(unit => unit.UnitId == "mechanist_drone_enemy");
Require(enemyDrone.Side == TacticalSide.Enemy && enemyDrone.Position.IsBackline, "Mechanist 2 can spawn an enemy-side drone");

var occupiedBacklineBattle = BattleState.Create(
    Enumerable.Range(0, TacticalField.Mvp.Columns)
        .Select(column => MakeUnit($"occupied_backline_{column}", TacticalSide.Player, new TacticalPosition(3, column), 100, 100, 0, 1.0, 100.0))
        .Concat(new[] { MakeUnit("occupied_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0) })
        .ToArray(),
    playerSynergyModifiers: mechanistOpeningDroneModifiers);
Require(!occupiedBacklineBattle.Units.Any(unit => unit.UnitId.StartsWith("mechanist_drone_player", StringComparison.Ordinal)), "Mechanist 2 skips drone spawn when the backline is full");

var noTurretFromMatch3 = mechanistDroneBattle.ApplyRuneEffect(
    Effect(RuneEffectKind.Mana, 0, rune: RuneType.Blue),
    synergyModifiers: mechanistFourModifiers);
Require(!noTurretFromMatch3.Units.Any(unit => unit.UnitId.StartsWith("mechanist_turret_player", StringComparison.Ordinal)), "Mechanist 4 does not spawn turrets from match-3 effects");

var mechanistTurretBattle = BattleState.Create(new[]
{
    MakeUnit("turret_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("turret_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var turretSpawnedBattle = mechanistTurretBattle.ApplyRuneEffect(
    Effect(RuneEffectKind.Mana, 0, rune: RuneType.Blue, tier: RuneMatchTier.Match4, matchedRunesCount: 4),
    synergyModifiers: mechanistFourModifiers);
var playerTurret = turretSpawnedBattle.Units.Single(unit => unit.UnitId == "mechanist_turret_player");
Require(playerTurret.HasTimedSummon && playerTurret.Position.IsBackline, "Mechanist 4 match-4 spawns a temporary backline turret");
Require(Math.Abs(playerTurret.MaxHealth - BattleState.MechanistTurretHealth) < 1e-9 && Math.Abs(playerTurret.Attack - BattleState.MechanistTurretAttack) < 1e-9, "Mechanist 4 turret uses the configured turret stats");
var turretShotEnemy = turretSpawnedBattle.Tick(1.3).Units.First(unit => unit.UnitId == "turret_enemy");
Require(Math.Abs(turretShotEnemy.CurrentHealth - 86.0) < 1e-9, "Mechanist 4 turret participates in ranged autobattle attacks");
var expiredTurret = turretSpawnedBattle
    .Tick((BattleState.MechanistTurretDurationMilliseconds / 1000.0) + 0.1)
    .Units.First(unit => unit.UnitId == "mechanist_turret_player");
Require(!expiredTurret.IsAlive && expiredTurret.SummonMillisecondsRemaining == 0, "Mechanist 4 turret expires after its temporary duration");
var blockedTurretBattle = occupiedBacklineBattle.ApplyRuneEffect(
    Effect(RuneEffectKind.Mana, 0, rune: RuneType.Blue, tier: RuneMatchTier.Match4, matchedRunesCount: 4),
    synergyModifiers: mechanistFourModifiers);
Require(!blockedTurretBattle.Units.Any(unit => unit.UnitId.StartsWith("mechanist_turret_player", StringComparison.Ordinal)), "Mechanist 4 skips turret spawn when the backline is full");

var spiritBattleWithModifiers = BattleState.Create(
    new[]
    {
        MakeUnit("spirit_state_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
        MakeUnit("spirit_state_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
    },
    playerSynergyModifiers: spiritDodgeModifiers);
Require(Math.Abs(spiritBattleWithModifiers.Units.First(unit => unit.UnitId == "spirit_state_ally").DodgeChance - SynergyModifiers.SpiritDodgeChanceBonus) < 1e-9, "Spirit 2 dodge chance applies during battle creation");

var spiritHitBattle = BattleState.Create(new[]
{
    MakeUnit("spirit_hit_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0)
        with { DodgeChance = SynergyModifiers.SpiritDodgeChanceBonus },
    MakeUnit("spirit_hit_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 0.0)
});
var spiritHitAlly = spiritHitBattle.Tick(0.5).Units.First(unit => unit.UnitId == "spirit_hit_ally");
Require(Math.Abs(spiritHitAlly.CurrentHealth - 90.0) < 1e-9 && spiritHitAlly.AttacksReceived == 1, "Spirit dodge chance still lets non-dodge attacks hit");

var spiritDodgeBattle = BattleState.Create(new[]
{
    MakeUnit("spirit_dodge_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0)
        with
        {
            DodgeChance = SynergyModifiers.SpiritDodgeChanceBonus,
            AttacksReceived = 9
        },
    MakeUnit("spirit_dodge_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 0.0)
});
var spiritDodgedAlly = spiritDodgeBattle.Tick(0.5).Units.First(unit => unit.UnitId == "spirit_dodge_ally");
Require(Math.Abs(spiritDodgedAlly.CurrentHealth - 100.0) < 1e-9 && spiritDodgedAlly.AttacksReceived == 10, "Spirit 2 deterministic dodge avoids the cadence attack");

var noIllusionFromBlue = spiritBattleWithModifiers.ApplyRuneEffect(
    Effect(RuneEffectKind.Mana, 0, rune: RuneType.Blue),
    synergyModifiers: spiritFourModifiers);
Require(!noIllusionFromBlue.Units.Any(unit => unit.UnitId.StartsWith("spirit_illusion_player", StringComparison.Ordinal)), "Spirit 4 does not create illusions from non-white runes");

var spiritIllusionBattle = BattleState.Create(new[]
{
    MakeUnit("illusion_source_a", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 20, 1.0, 100.0, attackType: BattleAttackType.Ranged)
        with { DodgeChance = SynergyModifiers.SpiritDodgeChanceBonus },
    MakeUnit("illusion_source_b", TacticalSide.Player, new TacticalPosition(2, 1), 120, 120, 30, 1.0, 100.0),
    MakeUnit("illusion_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var spiritIllusionSpawned = spiritIllusionBattle.ApplyRuneEffect(
    Effect(RuneEffectKind.CommanderEnergy, 5, rune: RuneType.White),
    synergyModifiers: spiritFourModifiers);
var spiritIllusion = spiritIllusionSpawned.Units.Single(unit => unit.UnitId == "spirit_illusion_player");
Require(spiritIllusion.IsSummoned && spiritIllusion.HasTimedSummon && spiritIllusion.Position.IsBackline, "Spirit 4 white rune creates a temporary backline illusion");
Require(Math.Abs(spiritIllusion.MaxHealth - (100.0 * BattleState.SpiritIllusionStatMultiplier)) < 1e-9, "Spirit 4 illusion copies a deterministic source with reduced health");
Require(Math.Abs(spiritIllusion.Attack - (20.0 * BattleState.SpiritIllusionStatMultiplier)) < 1e-9 && spiritIllusion.IsRanged, "Spirit 4 illusion copies source attack profile at reduced strength");
Require(Math.Abs(spiritIllusion.DodgeChance - SynergyModifiers.SpiritDodgeChanceBonus) < 1e-9, "Spirit 4 illusion inherits source dodge chance");
var expiredIllusion = spiritIllusionSpawned
    .Tick((BattleState.SpiritIllusionDurationMilliseconds / 1000.0) + 0.1)
    .Units.First(unit => unit.UnitId == "spirit_illusion_player");
Require(!expiredIllusion.IsAlive && expiredIllusion.SummonMillisecondsRemaining == 0, "Spirit 4 illusion expires after its temporary duration");

// Assassin 3 synergy: assassins dive past the enemy frontline to the backline.
var assassinDiveBattle = BattleState.Create(
    new[]
    {
        MakeUnit("dive_assassin", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 50, 1.0, 0.0, 100.0, 0.0, BattleAttackType.Ranged)
            with { HeroClass = ClassCatalog.Assassin.Name },
        MakeUnit("dive_front", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0),
        MakeUnit("dive_back", TacticalSide.Enemy, new TacticalPosition(0, 0), 100, 100, 0, 1.0, 100.0)
    },
    playerSynergyModifiers: assassinThreeModifiers);
var divedState = assassinDiveBattle.Tick(0.5);
Require(Math.Abs(divedState.Units.First(unit => unit.UnitId == "dive_front").CurrentHealth - 100.0) < 1e-9, "Assassin 3 synergy makes assassins ignore the enemy frontline");
Require(divedState.Units.First(unit => unit.UnitId == "dive_back").CurrentHealth < 100.0, "Assassin 3 synergy sends assassins at the enemy backline");

var noDiveState = BattleState.Create(new[]
{
    MakeUnit("nodive_assassin", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 50, 1.0, 0.0, 100.0, 0.0, BattleAttackType.Ranged)
        with { HeroClass = ClassCatalog.Assassin.Name },
    MakeUnit("nodive_front", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("nodive_back", TacticalSide.Enemy, new TacticalPosition(0, 0), 100, 100, 0, 1.0, 100.0)
}).Tick(0.5);
Require(noDiveState.Units.First(unit => unit.UnitId == "nodive_front").CurrentHealth < 100.0, "without the synergy assassins still strike the nearest enemy");

// Assassin 6 synergy: assassin crits hit for 1.5x and bank red-rune charge.
var assassinCritBattle = BattleState.Create(
    new[]
    {
        MakeUnit("crit_assassin", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 40, 1.0, 0.0, 100.0, 0.0, BattleAttackType.Ranged)
            with { HeroClass = ClassCatalog.Assassin.Name, AttacksLanded = 19 },
        MakeUnit("crit_target", TacticalSide.Enemy, new TacticalPosition(1, 0), 1000, 1000, 0, 1.0, 100.0)
    },
    playerSynergyModifiers: assassinSixModifiers);
var afterCrit = assassinCritBattle.Tick(0.5);
Require(Math.Abs(afterCrit.PlayerRedRuneCharge - SynergyModifiers.AssassinCritRedRuneCharge) < 1e-9, "Assassin 6 crit accrues red-rune charge for the player");
var critTarget = afterCrit.Units.First(unit => unit.UnitId == "crit_target");
Require(Math.Abs((1000.0 - critTarget.CurrentHealth) - CombatFormulas.ApplyCrit(40.0)) < 1e-9, "Assassin 6 crit deals 1.5x physical damage on the crit cadence");

var afterRedSpend = afterCrit.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 10, rune: RuneType.Red));
Require(Math.Abs(afterRedSpend.PlayerRedRuneCharge) < 1e-9, "Assassin 6 red-rune charge is consumed by the next red rune effect");
var spentTarget = afterRedSpend.Units.First(unit => unit.UnitId == "crit_target");
Require(Math.Abs((critTarget.CurrentHealth - spentTarget.CurrentHealth) - 18.0) < 1e-9, "Assassin 6 charge adds bonus power to the red rune effect");
var afterBlueNoSpend = afterCrit.ApplyRuneEffect(Effect(RuneEffectKind.Mana, 0, rune: RuneType.Blue));
Require(Math.Abs(afterBlueNoSpend.PlayerRedRuneCharge - SynergyModifiers.AssassinCritRedRuneCharge) < 1e-9, "non-red rune effects do not consume assassin red-rune charge");

var noCritControl = BattleState.Create(new[]
{
    MakeUnit("nocrit_assassin", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 40, 1.0, 0.0, 100.0, 0.0, BattleAttackType.Ranged)
        with { HeroClass = ClassCatalog.Assassin.Name, AttacksLanded = 19 },
    MakeUnit("nocrit_target", TacticalSide.Enemy, new TacticalPosition(1, 0), 1000, 1000, 0, 1.0, 100.0)
}).Tick(0.5);
Require(Math.Abs(noCritControl.PlayerRedRuneCharge) < 1e-9, "assassin crits do not charge red runes without the Assassin 6 synergy");
Require(Math.Abs((1000.0 - noCritControl.Units.First(unit => unit.UnitId == "nocrit_target").CurrentHealth) - 40.0) < 1e-9, "without the synergy assassin attacks deal base physical damage");

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
    Class = "Маг",
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

var mageAbilityDamageBattle = BattleState.Create(
    new[]
    {
        mageUnit with { UnitId = "mage_damage_bonus" },
        MakeUnit("mage_damage_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
    },
    playerSynergyModifiers: mageDamageModifiers);
var mageDamageEnemy = mageAbilityDamageBattle
    .AddManaFromBlueRunes(TacticalSide.Player, 1)
    .Units.First(unit => unit.UnitId == "mage_damage_enemy");
Require(Math.Abs(mageDamageEnemy.CurrentHealth - 52.0) < 1e-9, "Mage 3 increases damaging active abilities by 20 percent");

var mageChargeUnit = BattleUnit.FromHero(mageDefinition, 1, "mage_bonus_charge", TacticalSide.Player, new TacticalPosition(2, 0))
    with { ManaMax = 100.0 };
Require(mageChargeUnit.HeroClass == "Маг", "battle units retain their hero class for class-triggered effects");
var mageChargeBattle = BattleState.Create(new[]
{
    mageChargeUnit,
    MakeUnit("mage_charge_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var mageNotChargedByMatch3 = mageChargeBattle
    .ApplyRuneEffect(Effect(RuneEffectKind.Mana, 0, rune: RuneType.Blue), synergyModifiers: mageFiveModifiers)
    .Units.First(unit => unit.UnitId == "mage_bonus_charge");
Require(Math.Abs(mageNotChargedByMatch3.CurrentMana - 2.0) < 1e-9, "Mage 5 does not add bonus charge from blue match-3");
var mageChargedByMatch4 = mageChargeBattle
    .ApplyRuneEffect(Effect(RuneEffectKind.Mana, 0, rune: RuneType.Blue, tier: RuneMatchTier.Match4, matchedRunesCount: 4), synergyModifiers: mageFiveModifiers)
    .Units.First(unit => unit.UnitId == "mage_bonus_charge");
Require(Math.Abs(mageChargedByMatch4.CurrentMana - (2.0 + SynergyModifiers.MageBlueMatch4BonusMana)) < 1e-9, "Mage 5 grants extra mana to a deterministic mage from blue match-4");

var abyssalCaster = mageUnit with { UnitId = "abyssal_caster" };
var abyssalAbilityBattle = BattleState.Create(
    new[]
    {
        abyssalCaster,
        MakeUnit("abyssal_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 50, 1.0, 100.0)
    },
    playerSynergyModifiers: abyssalWeaknessModifiers);
var abyssalAfterCast = abyssalAbilityBattle.AddManaFromBlueRunes(TacticalSide.Player, 1);
var abyssalDebuffedEnemy = abyssalAfterCast.Units.First(unit => unit.UnitId == "abyssal_enemy");
Require(abyssalDebuffedEnemy.HasActiveWeakness, "Abyssal 2 applies weakness when an allied ability hits");
Require(Math.Abs(abyssalDebuffedEnemy.WeaknessAttackPenaltyFraction - SynergyModifiers.AbyssalAbilityWeaknessAttackPenalty) < 1e-9, "Abyssal 2 weakness uses the configured attack penalty");
Require(abyssalDebuffedEnemy.WeaknessMillisecondsRemaining == SynergyModifiers.AbyssalAbilityWeaknessDurationMilliseconds, "Abyssal 2 weakness uses the configured temporary duration");

var abyssalSupportDefinition = ironGuardDefinition with
{
    Id = "void_oracle",
    Name = "Void Oracle",
    Role = HeroRole.Support,
    RuneAffinity = RuneType.Green,
    AttackType = "ranged",
    BaseStats = new HeroStats(BaseHealth: 80, Attack: 10, Armor: 0, MagicResist: 0, BaseAttackSpeed: 1.0, ManaMax: 10)
};
var abyssalSupport = BattleUnit.FromHero(abyssalSupportDefinition, 1, "abyssal_support", TacticalSide.Player, new TacticalPosition(2, 0))
    with { CurrentMana = 8.0 };
var abyssalSupportBattle = BattleState.Create(
    new[]
    {
        abyssalSupport,
        MakeUnit("abyssal_wounded", TacticalSide.Player, new TacticalPosition(3, 0), 100, 50, 0, 1.0, 100.0),
        MakeUnit("abyssal_support_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 50, 1.0, 100.0)
    },
    playerSynergyModifiers: abyssalWeaknessModifiers);
var abyssalAfterSupportCast = abyssalSupportBattle.AddManaFromBlueRunes(TacticalSide.Player, 1);
Require(abyssalAfterSupportCast.Units.First(unit => unit.UnitId == "abyssal_support_enemy").HasActiveWeakness, "Abyssal 2 applies weakness from support abilities too");

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
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.MagicDamage, 40, rune: RuneType.Purple)).Units.First(u => u.UnitId == "fx_enemy").CurrentHealth - 60.0) < 1e-9, "a purple rune uses base magic damage without Abyssal 4");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.MagicDamage, 40, rune: RuneType.Purple), synergyModifiers: abyssalFourModifiers).Units.First(u => u.UnitId == "fx_enemy").CurrentHealth - 50.0) < 1e-9, "Abyssal 4 increases purple-rune magic damage");
Require(Math.Abs(fxBattle.ApplyRuneEffect(Effect(RuneEffectKind.MagicDamage, 40, rune: RuneType.White), synergyModifiers: abyssalFourModifiers).Units.First(u => u.UnitId == "fx_enemy").CurrentHealth - 60.0) < 1e-9, "Abyssal 4 does not boost non-purple magic damage");

var empireYellowShieldBattle = BattleState.Create(new[]
{
    MakeUnit("empire_front_a", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("empire_front_b", TacticalSide.Player, new TacticalPosition(2, 1), 100, 100, 0, 1.0, 100.0),
    MakeUnit("empire_back", TacticalSide.Player, new TacticalPosition(3, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("empire_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var defaultYellowShield = empireYellowShieldBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 20, rune: RuneType.Yellow));
Require(Math.Abs(defaultYellowShield.Units.First(u => u.UnitId == "empire_front_a").Shield - 20.0) < 1e-9, "without Empire 4 a yellow rune shields only the frontmost ally");
Require(Math.Abs(defaultYellowShield.Units.First(u => u.UnitId == "empire_front_b").Shield) < 1e-9, "without Empire 4 the second frontline ally is not shielded by a single yellow match");
var defenderYellowArmor = empireYellowShieldBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 20, rune: RuneType.Yellow), synergyModifiers: defenderFourModifiers);
var defenderArmoredFront = defenderYellowArmor.Units.First(u => u.UnitId == "empire_front_a");
Require(Math.Abs(defenderArmoredFront.Armor - SynergyModifiers.DefenderYellowRuneArmorBonus) < 1e-9, "Defender 4 yellow runes add armor to shielded units");
var defenderNonYellowArmor = empireYellowShieldBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 20, rune: RuneType.White), synergyModifiers: defenderFourModifiers);
Require(Math.Abs(defenderNonYellowArmor.Units.First(u => u.UnitId == "empire_front_a").Armor) < 1e-9, "Defender 4 armor boost only applies to yellow runes");
var empireFourYellowShield = empireYellowShieldBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 20, rune: RuneType.Yellow), synergyModifiers: empireFourModifiers);
Require(empireFourYellowShield.Units.Where(u => u.UnitId.StartsWith("empire_front", StringComparison.Ordinal)).All(u => Math.Abs(u.Shield - 20.0) < 1e-9), "Empire 4 yellow runes shield the allied frontline");
Require(Math.Abs(empireFourYellowShield.Units.First(u => u.UnitId == "empire_back").Shield) < 1e-9, "Empire 4 yellow rune shield does not spill into the backline");

var wildLifestealBattle = BattleState.Create(new[]
{
    MakeUnit("wild_ls_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 50, 50, 1.0, 0.0),
    MakeUnit("wild_ls_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var wildChainBuffed = wildLifestealBattle.ApplyRuneEffect(Effect(RuneEffectKind.Mana, 0, chainNumber: 2), synergyModifiers: wildFourModifiers);
var wildBuffedAlly = wildChainBuffed.Units.First(u => u.UnitId == "wild_ls_ally");
Require(wildBuffedAlly.HasActiveLifesteal, "Wild 4 grants lifesteal after a chain reaction");
Require(wildBuffedAlly.LifestealMillisecondsRemaining == SynergyModifiers.WildChainLifestealDurationMilliseconds, "Wild 4 lifesteal uses the configured temporary duration");
var wildAfterAttack = wildChainBuffed.Tick(0.5).Units.First(u => u.UnitId == "wild_ls_ally");
Require(Math.Abs(wildAfterAttack.CurrentHealth - 57.5) < 1e-9, "Wild 4 lifesteal heals the attacker from basic attack damage");

var wildExpiryBattle = BattleState.Create(new[]
{
    MakeUnit("wild_expire_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 50, 0, 1.0, 100.0),
    MakeUnit("wild_expire_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
var wildExpiredAlly = wildExpiryBattle
    .ApplyRuneEffect(Effect(RuneEffectKind.Mana, 0, chainNumber: 2), synergyModifiers: wildFourModifiers)
    .Tick((SynergyModifiers.WildChainLifestealDurationMilliseconds / 1000.0) + 0.1)
    .Units.First(u => u.UnitId == "wild_expire_ally");
Require(!wildExpiredAlly.HasActiveLifesteal, "Wild 4 lifesteal expires after its temporary duration");

var weaknessDamageBattle = BattleState.Create(new[]
{
    MakeUnit("weakness_target_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("weakness_attacker", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 50, 1.0, 0.0)
        with
        {
            WeaknessAttackPenaltyFraction = SynergyModifiers.AbyssalAbilityWeaknessAttackPenalty,
            WeaknessMillisecondsRemaining = SynergyModifiers.AbyssalAbilityWeaknessDurationMilliseconds
        }
});
var weaknessHitAlly = weaknessDamageBattle.Tick(0.5).Units.First(u => u.UnitId == "weakness_target_ally");
Require(Math.Abs(weaknessHitAlly.CurrentHealth - 55.0) < 1e-9, "Abyssal weakness reduces basic attack damage");

var weaknessExpiryBattle = BattleState.Create(new[]
{
    MakeUnit("weakness_expiry_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 0, 1.0, 100.0),
    MakeUnit("weakness_expiring_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 50, 1.0, 100.0)
        with
        {
            WeaknessAttackPenaltyFraction = SynergyModifiers.AbyssalAbilityWeaknessAttackPenalty,
            WeaknessMillisecondsRemaining = SynergyModifiers.AbyssalAbilityWeaknessDurationMilliseconds
        }
});
var weaknessExpiredEnemy = weaknessExpiryBattle
    .Tick((SynergyModifiers.AbyssalAbilityWeaknessDurationMilliseconds / 1000.0) + 0.1)
    .Units.First(u => u.UnitId == "weakness_expiring_enemy");
Require(!weaknessExpiredEnemy.HasActiveWeakness, "Abyssal weakness expires after its temporary duration");

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

// App-level single-scene navigation model (GDD UI screen flow).
var navigation = AppNavigationState.AtMainMenu;
Require(navigation.Current == AppScreen.MainMenu && navigation.Previous is null, "a new app session starts on the main menu");
Require(navigation.CanNavigateTo(AppScreen.LevelSelect), "the main menu can open level select");
Require(navigation.CanNavigateTo(AppScreen.CommanderSelect), "the main menu can open commander select");
Require(!navigation.CanNavigateTo(AppScreen.Combat), "the main menu cannot jump straight into combat");
var atLevelSelect = navigation.NavigateTo(AppScreen.LevelSelect);
Require(atLevelSelect.Current == AppScreen.LevelSelect && atLevelSelect.Previous == AppScreen.MainMenu, "navigation records the previous screen");
Require(atLevelSelect.Back().Current == AppScreen.MainMenu, "navigation can go back to the previous screen");
var fullLoop = atLevelSelect
    .NavigateTo(AppScreen.Preparation)
    .NavigateTo(AppScreen.Combat)
    .NavigateTo(AppScreen.LevelComplete);
Require(fullLoop.Current == AppScreen.LevelComplete, "menu -> level select -> preparation -> combat -> results stays in one flow");
Require(fullLoop.CanNavigateTo(AppScreen.Preparation), "level results can advance to the next level without leaving the run");
Require(fullLoop.NavigateTo(AppScreen.RunSummary).Current == AppScreen.RunSummary, "level results can finish into the run summary");
RequireThrows(() => navigation.NavigateTo(AppScreen.Combat), "illegal screen transitions are rejected");
Require(AppNavigationState.ScreenForPhase(RunPhase.Preparation) == AppScreen.Preparation, "preparation phase maps to the preparation screen");
Require(AppNavigationState.ScreenForPhase(RunPhase.Combat) == AppScreen.Combat, "combat phase maps to the combat screen");
Require(AppNavigationState.ScreenForPhase(RunPhase.Reward) == AppScreen.LevelComplete, "reward phase maps to the level results screen");
Require(AppNavigationState.ScreenForPhase(RunPhase.Victory) == AppScreen.RunSummary, "victory maps to the run summary screen");
Require(AppNavigationState.ScreenForPhase(RunPhase.Defeat) == AppScreen.RunSummary, "defeat maps to the run summary screen");

// Level-select view-model built from the schedule and run progress (GDD level cards).
var levelCards = LevelSelectModel.Build(currentRound: 3);
Require(levelCards.Count == 10, "level select lists every scheduled round");
Require(levelCards[0].Status == LevelCardStatus.Completed, "rounds before the current one read as completed");
Require(levelCards[2].Status == LevelCardStatus.Current, "the current round is selectable");
Require(levelCards[3].Status == LevelCardStatus.Locked, "later rounds are locked");
Require(levelCards[0].Type == PveRoundType.Tutorial && levelCards[0].RewardSummary.Contains("герой 1 стоимости"), "round 1 card shows the tutorial reward");
Require(levelCards[7].RewardSummary.Contains("редкий артефакт") && levelCards[7].RewardSummary.Contains("7 золота"), "round 8 card shows the boss reward");
Require(levelCards[8].RewardSummary.Contains("бесплатный reroll"), "round 9 card shows the free reroll reward");
Require(levelCards[9].RewardSummary == "Победа в забеге", "round 10 card shows the run victory reward");
Require(LevelSelectModel.Build(currentRound: 5, runComplete: true)[9].Status == LevelCardStatus.Completed, "a completed run marks every round done");
Require(LevelSelectModel.Build(RunState.NewRun())[0].Status == LevelCardStatus.Current, "a new run highlights round one");
RequireThrows(() => LevelSelectModel.Build(currentRound: 11), "level select rejects rounds outside the schedule");

// Preparation tactical placement view-model (bench -> field with highlighted drop targets).
var placementRun = RunState.NewRun();
var halfRows = TacticalField.Mvp.HalfRows;
var idlePlacement = TacticalPlacementModel.Build(placementRun);
Require(idlePlacement.Cells.Count == TacticalField.Mvp.CellCount, "placement model covers every tactical cell");
Require(idlePlacement.Cells.Count(cell => cell.State == TacticalCellState.Unavailable) == TacticalField.Mvp.CellCount / 2, "the enemy half is unavailable during preparation");
Require(!idlePlacement.HasSelection && idlePlacement.HighlightedTargetCount == 0, "no cells highlight without a picked bench hero");
Require(idlePlacement.Cells.Where(cell => cell.Position.IsPlayerSide).All(cell => cell.State == TacticalCellState.Free), "player cells read as free until a hero is picked");

var benchRun = placementRun with { Bench = new List<HeroInstance> { new HeroInstance("ph_1", "iron_guard", 1) } };
var selectedPlacement = TacticalPlacementModel.Build(benchRun, "ph_1");
Require(selectedPlacement.HasSelection, "a valid bench id activates the selection");
Require(selectedPlacement.HighlightedTargetCount == TacticalField.Mvp.CellCount / 2, "picking a bench hero highlights every free player cell");
Require(selectedPlacement.Cells.Where(cell => cell.IsPlacementTarget).All(cell => cell.Position.IsPlayerSide), "highlighted drop targets live only on the player half");

var stalePlacement = TacticalPlacementModel.Build(benchRun, "missing_id");
Require(!stalePlacement.HasSelection && stalePlacement.HighlightedTargetCount == 0, "a bench id that is not present does not highlight the board");

var placedRun = benchRun.PlaceHeroFromBench("ph_1", new TacticalPosition(halfRows, 0));
var placedPlacement = TacticalPlacementModel.Build(placedRun, "ph_1");
var occupiedCell = placedPlacement.CellAt(new TacticalPosition(halfRows, 0));
Require(occupiedCell.State == TacticalCellState.OccupiedAlly && occupiedCell.HeroInstanceId == "ph_1", "a placed hero reads as an occupied ally cell");
Require(occupiedCell.IsOccupiedByAlly && placedPlacement.PlacedHeroCount == 1, "placed hero count tracks the team size");
Require(!placedPlacement.HasSelection, "an emptied bench clears the selection");

var fullTeam = new List<BoardHero>
{
    new BoardHero(new HeroInstance("t1", "iron_guard", 1), new TacticalPosition(halfRows, 0)),
    new BoardHero(new HeroInstance("t2", "oath_archer", 1), new TacticalPosition(halfRows, 1))
};
var fullFieldRun = placementRun with
{
    PlayerLevel = 1,
    Team = fullTeam,
    Bench = new List<HeroInstance> { new HeroInstance("ph_x", "field_medic", 1) }
};
var fullPlacement = TacticalPlacementModel.Build(fullFieldRun, "ph_x");
Require(fullPlacement.IsFieldFull && !fullPlacement.CanPlaceMore, "the field reports full at the player-level hero limit");
Require(fullPlacement.HighlightedTargetCount == 0, "no targets highlight once the field hits the player-level limit");
RequireThrows(() => idlePlacement.CellAt(new TacticalPosition(-1, 0)), "placement model rejects off-board cell queries");
RequireThrows(() => TacticalPlacementModel.Build(null!), "placement model rejects a null run");

// Preparation screen view-model: field, bench, shop, economy, synergies, enemy preview, start button.
var freshPrep = PreparationScreenModel.Build(RunState.NewRun());
Require(freshPrep.Round == 1 && freshPrep.RoundType == PveRoundType.Tutorial, "preparation screen reads the current round");
Require(freshPrep.EnemyPreview.Count == 2 && freshPrep.EnemyPreview.All(unit => unit.IsFrontline), "preparation screen previews the round-1 enemy roster");
Require(freshPrep.EnemyPreview[0].Name == "Железный Страж", "preparation enemy preview resolves catalog names");
Require(freshPrep.Shop.Count == 3 && freshPrep.Shop.All(offer => offer.CanBuy), "a fresh shop is fully affordable with starting gold and an empty bench");
Require(freshPrep.GoldLabel == "5 золота" && freshPrep.PlayerLevelLabel == "Ур. 1", "preparation screen shows gold and player level");
Require(freshPrep.XpLabel == "0 / 4 XP" && !freshPrep.IsMaxLevel, "preparation screen shows XP toward the next level");
Require(freshPrep.HeroLimit == 2 && freshPrep.FieldLimitLabel == "0 / 2 героев", "preparation screen reports the player-level field limit");
Require(freshPrep.RerollLabel == "Reroll (2)" && freshPrep.CanReroll, "preparation reroll button shows its cost and affordability");
Require(freshPrep.BuyXpLabel == "Купить опыт (4)" && freshPrep.CanBuyXp, "preparation buy-XP button shows its cost and affordability");
Require(!freshPrep.CanStartBattle, "the battle cannot start until a hero is placed");
Require(freshPrep.ActiveSynergies.Count == 0, "a fresh team has no active synergies");

var prepBench = new List<HeroInstance>
{
    new HeroInstance("pb_1", "iron_guard", 1),
    new HeroInstance("pb_2", "rune_apprentice", 2)
};
var prepTeam = new List<BoardHero>
{
    new BoardHero(new HeroInstance("pt_1", "iron_guard", 1), new TacticalPosition(halfRows, 0)),
    new BoardHero(new HeroInstance("pt_2", "oath_archer", 1), new TacticalPosition(halfRows, 1))
};
var richPrep = PreparationScreenModel.Build(placementRun with { Gold = 3, Team = prepTeam, Bench = prepBench }, "pb_1");
Require(richPrep.Bench.Count == 2 && richPrep.Bench[0].IsSelected && !richPrep.Bench[1].IsSelected, "the picked bench hero is marked selected");
Require(richPrep.Bench[0].Name == "Железный Страж" && richPrep.Bench[0].SellValue == 1, "bench rows carry catalog data and sell value");
Require(richPrep.Bench[1].SellValue == HeroEconomy.CalculateSellValue(richPrep.Bench[1].Cost, 2), "a 2-star bench hero sells for its invested copies");
Require(richPrep.CanStartBattle && richPrep.PlacedHeroCount == 2, "a placed team enables the start-battle button");
Require(richPrep.ActiveSynergies.Any(progress => progress.Definition.Name == "Империя"), "two Empire heroes light up the Empire synergy indicator");
Require(richPrep.CanReroll && !richPrep.CanBuyXp, "3 gold affords a reroll but not the 4-gold XP purchase");
Require(richPrep.Placement.HighlightedTargetCount == 0, "a full field stops highlighting drop targets");

var cappedPrep = PreparationScreenModel.Build(placementRun with { PlayerLevel = 5, Xp = 0 });
Require(cappedPrep.IsMaxLevel && cappedPrep.XpLabel == "MAX" && cappedPrep.HeroLimit == 6, "preparation screen reports the level cap and its field limit");

var stalePrep = PreparationScreenModel.Build(placementRun, "missing_id");
Require(stalePrep.Bench.Count == 0 && !stalePrep.Placement.HasSelection, "a stale bench selection leaves the screen unselected");
RequireThrows(() => PreparationScreenModel.Build(null!), "preparation screen rejects a null run");

// Combat HUD view-model (battle timer, speed indicator, key-unit bars).
var hudCombat = CombatState.Start(1337, 60).AdvanceTimer(15);
var combatHud = CombatHudModel.Build(hudCombat);
Require(combatHud.RemainingSeconds == 45 && combatHud.TimerLabel == "0:45", "combat HUD shows the remaining battle time as m:ss");
Require(Math.Abs(combatHud.TimerFraction - 0.75) < 1e-9, "combat HUD timer fraction tracks remaining/total duration");
Require(!combatHud.IsSlowed && combatHud.CombatSpeedPercent == 100 && combatHud.SpeedLabel == "NORMAL", "combat HUD reports normal speed when not slowed");
Require(combatHud.KeyUnits.Count == 0, "combat HUD defaults to no key units when none are supplied");
Require(CombatHudModel.FormatTimer(75) == "1:15" && CombatHudModel.FormatTimer(5) == "0:05", "combat HUD formats minutes and zero-padded seconds");
Require(CombatHudModel.FormatTimer(-10) == "0:00", "combat HUD clamps negative durations to zero");
var expiredHud = CombatHudModel.Build(CombatState.Start(7, 30).AdvanceTimer(30));
Require(expiredHud.IsTimerExpired && expiredHud.TimerFraction == 0.0 && expiredHud.TimerLabel == "0:00", "combat HUD reports an expired timer at zero");
var hudWithUnits = CombatHudModel.Build(hudCombat, new List<CombatHudUnit>
{
    new CombatHudUnit("Sentinel", true, 0.8, 0.4),
    new CombatHudUnit("Vanguard", false, 1.2, -0.1)
});
Require(hudWithUnits.KeyUnits.Count == 2 && hudWithUnits.KeyUnits[0].IsPlayer, "combat HUD carries the supplied key units in order");
Require(Math.Abs(hudWithUnits.KeyUnits[1].HealthBar - 1.0) < 1e-9 && Math.Abs(hudWithUnits.KeyUnits[1].ManaBar - 0.0) < 1e-9, "combat HUD clamps key-unit health/mana bars to 0..1");
RequireThrows(() => CombatHudModel.Build(null!), "combat HUD rejects a null combat state");

// Combat screen view-model (battlefield with heroes/enemies, HP/mana bars, 7x7 rune
// board, active rune effects, round timer and pause).
var screenBattle = BattleState.Create(new[]
{
    BattleUnit.FromHero(HeroCatalog.Get("iron_guard"), 1, "screen_ally_tank", TacticalSide.Player, new TacticalPosition(2, 1)),
    BattleUnit.FromHero(HeroCatalog.Get("rune_apprentice"), 1, "screen_ally_caster", TacticalSide.Player, new TacticalPosition(3, 2)),
    BattleUnit.FromHero(HeroCatalog.Get("oath_archer"), 1, "screen_enemy_archer", TacticalSide.Enemy, new TacticalPosition(1, 1))
});
var screenCombat = CombatState.Start(1337, 60).AdvanceTimer(15);
var screenRound = PveRunSchedule.GetRound(2);
var combatScreen = CombatScreenModel.Build(screenBattle, screenCombat, screenRound);
Require(combatScreen.Round == 2 && combatScreen.RoundType == PveRoundType.Combat, "combat screen carries the round header");
Require(combatScreen.FieldColumns == TacticalField.MvpColumns && combatScreen.FieldRows == TacticalField.MvpRows, "combat screen exposes the MVP tactical field size");
Require(combatScreen.Battlefield.Count == TacticalField.Mvp.CellCount, "combat screen enumerates every tactical cell");
Require(combatScreen.AlivePlayerUnits == 2 && combatScreen.AliveEnemyUnits == 1, "combat screen counts living units per side");
var screenAllyCell = combatScreen.Battlefield.Single(cell => cell.Position == new TacticalPosition(2, 1));
Require(screenAllyCell.State == TacticalCellState.OccupiedAlly && screenAllyCell.Unit!.IsPlayer, "an ally-occupied cell reports the ally state and unit");
Require(screenAllyCell.Unit!.IsFrontline && Math.Abs(screenAllyCell.Unit!.HealthBar - 1.0) < 1e-9, "a fresh field unit shows a full health bar on the frontline");
var screenCasterCell = combatScreen.Battlefield.Single(cell => cell.Position == new TacticalPosition(3, 2));
Require(screenCasterCell.Unit!.HasMana && screenCasterCell.Unit!.ManaBar >= 0.0, "a caster field unit exposes a mana bar");
var screenEnemyCell = combatScreen.Battlefield.Single(cell => cell.Position == new TacticalPosition(1, 1));
Require(screenEnemyCell.State == TacticalCellState.OccupiedEnemy && screenEnemyCell.Unit!.IsEnemy, "an enemy-occupied cell reports the enemy state and unit");
Require(combatScreen.Battlefield.Count(cell => cell.State == TacticalCellState.Free) == TacticalField.Mvp.CellCount - 3, "unoccupied tactical cells stay free");
Require(combatScreen.RuneBoard.Count == Match3Board.CellCount, "combat screen enumerates the whole 7x7 rune board");
Require(combatScreen.BoardRows == Match3Board.Rows && combatScreen.BoardColumns == Match3Board.Columns, "combat screen exposes the 7x7 board size");
Require(combatScreen.Hud.RemainingSeconds == 45 && combatScreen.Hud.TimerLabel == "0:45", "combat screen embeds the round timer HUD");
Require(combatScreen.Hud.KeyUnits.Count == 2, "combat screen highlights one key unit per side on the HUD");
Require(combatScreen.ActiveRuneEffects.Count == 0, "combat screen has no rune effects without a recent match");
Require(!combatScreen.IsPaused && combatScreen.PauseButtonLabel == "Пауза", "combat screen starts unpaused with a pause button");
Require(!combatScreen.IsResolved && combatScreen.Outcome == BattleOutcome.Ongoing, "a fresh combat screen reports an ongoing battle");
var preHintScreen = CombatScreenModel.Build(screenBattle, CombatState.Start(1337, 60).AdvanceTimer(CombatState.MatchHintDelaySeconds - 1), screenRound);
Require(!preHintScreen.ShowMatchHint, "combat screen hides the idle match hint before the delay");

var pausedScreen = CombatScreenModel.Build(screenBattle, screenCombat, screenRound, isPaused: true);
Require(pausedScreen.IsPaused && pausedScreen.PauseButtonLabel == "Продолжить", "a paused combat screen shows the resume label");

var hintScreen = CombatScreenModel.Build(screenBattle, screenCombat.AdvanceTimer(CombatState.MatchHintDelaySeconds), screenRound);
Require(hintScreen.ShowMatchHint && hintScreen.RuneBoard.Any(cell => cell.IsHintHighlighted), "combat screen highlights the idle match hint after the delay");

var screenRuneEffects = new List<RuneEffect>
{
    Effect(RuneEffectKind.PhysicalDamage, 12.4, rune: RuneType.Red),
    Effect(RuneEffectKind.Healing, 8.0, mass: true, rune: RuneType.Green, chainNumber: 2)
};
var effectScreen = CombatScreenModel.Build(screenBattle, screenCombat, screenRound, screenRuneEffects);
Require(effectScreen.ActiveRuneEffects.Count == 2, "combat screen surfaces the supplied rune effects");
Require(effectScreen.ActiveRuneEffects[0].Power == 12 && effectScreen.ActiveRuneEffects[0].Label == "Красная руна · Физ. урон", "a rune-effect chip rounds power and labels the rune effect");
Require(effectScreen.ActiveRuneEffects[1].IsMassEffect && effectScreen.ActiveRuneEffects[1].ChainNumber == 2, "a mass rune-effect chip records its chain number");

var runInCombatView = RunState.NewRun() with { Phase = RunPhase.Combat, Round = 2, Combat = screenCombat };
var runScreen = CombatScreenModel.Build(runInCombatView, screenBattle);
Require(runScreen.Round == 2 && runScreen.RuneBoard.Count == Match3Board.CellCount, "combat screen can be built straight from a run in combat");
RequireThrows(() => CombatScreenModel.Build(RunState.NewRun(), screenBattle), "combat screen rejects a run with no active combat");
RequireThrows(() => CombatScreenModel.Build((BattleState)null!, screenCombat, screenRound), "combat screen rejects a null battle");
RequireThrows(() => CombatScreenModel.Build(screenBattle, null!, screenRound), "combat screen rejects a null combat state");
RequireThrows(() => CombatScreenModel.Build(screenBattle, screenCombat, null!), "combat screen rejects a null round");

// Level-complete summary: the player-centric combat totals on BattleState, the mirror
// autobattle, and the LevelCompleteModel formatting that feeds the results screen.
var levelCompleteTeam = new List<BoardHero>
{
    new(new HeroInstance("lc_front", "iron_guard", 2), new TacticalPosition(2, 1)),
    new(new HeroInstance("lc_back", "iron_guard", 1), new TacticalPosition(3, 1))
};
var mirrorEnemies = LevelCombatSimulator.BuildMirrorEnemies(levelCompleteTeam);
Require(mirrorEnemies.Count == levelCompleteTeam.Count, "mirror enemy roster matches the team size");
Require(mirrorEnemies.All(unit => unit.Side == TacticalSide.Enemy && unit.Position.IsEnemySide), "mirror enemies sit on the enemy half");
Require(mirrorEnemies[0].Position.Row == TacticalField.MvpRows - 1 - levelCompleteTeam[0].Position.Row, "mirror enemy reflects the ally row");

var resolvedBattle = LevelCombatSimulator.ResolveMirrorMatch(levelCompleteTeam);
Require(resolvedBattle is not null, "a non-empty team produces a resolvable battle");
var resolved = resolvedBattle!;
Require(resolved.PlayerDamageDealt > 0.0, "the resolved battle accumulates player damage dealt");
Require(resolved.PlayerHealingDone >= 0.0 && resolved.PlayerShieldGranted >= 0.0, "player healing/shield totals are non-negative");
Require(LevelCombatSimulator.ResolveMirrorMatch(new List<BoardHero>()) is null, "an empty team yields no battle to resolve");
RequireThrows(() => LevelCombatSimulator.ResolveMirrorMatch(null!), "the mirror simulator rejects a null team");

// Data-driven PvE rosters: every combat round fields authored enemies on the enemy half,
// non-combat rounds field none, and the round simulator fights that roster (not a mirror).
foreach (var scheduledRound in PveRunSchedule.Rounds)
{
    if (scheduledRound.HasCombat)
    {
        Require(scheduledRound.HasEnemyComposition, $"combat round {scheduledRound.Round} has a data-driven enemy roster");
        var seenPositions = new HashSet<TacticalPosition>();
        foreach (var enemy in scheduledRound.EnemyUnits)
        {
            Require(HeroCatalog.TryGet(enemy.HeroId, out _), $"round {scheduledRound.Round} enemy '{enemy.HeroId}' is a known hero");
            Require(enemy.Stars is >= 1 and <= 3, $"round {scheduledRound.Round} enemy '{enemy.HeroId}' has a valid star level");
            Require(enemy.Position.IsEnemySide, $"round {scheduledRound.Round} enemy '{enemy.HeroId}' sits on the enemy half");
            Require(seenPositions.Add(enemy.Position), $"round {scheduledRound.Round} enemy positions are unique");
        }
    }
    else
    {
        Require(!scheduledRound.HasEnemyComposition, $"non-combat round {scheduledRound.Round} fields no enemies");
    }
}

var round2 = PveRunSchedule.GetRound(2);
var round2Enemies = LevelCombatSimulator.BuildRoundEnemies(round2);
Require(round2Enemies.Count == round2.EnemyUnits.Count, "round enemy roster matches the authored composition size");
Require(round2Enemies.All(unit => unit.Side == TacticalSide.Enemy && unit.Position.IsEnemySide), "round enemies sit on the enemy half");
Require(round2.EnemyStarTotal == round2.EnemyUnits.Sum(u => u.Stars), "round exposes its total enemy stars");

var roundBattle = LevelCombatSimulator.ResolveRoundMatch(levelCompleteTeam, round2);
Require(roundBattle is not null, "a non-empty team fights the round's data-driven roster");
Require(roundBattle!.PlayerDamageDealt > 0.0, "the data-driven round battle accumulates player damage dealt");
Require(LevelCombatSimulator.ResolveRoundMatch(levelCompleteTeam, PveRunSchedule.GetRound(4)) is null, "a non-combat round yields no battle to resolve");
Require(LevelCombatSimulator.ResolveRoundMatch(new List<BoardHero>(), round2) is null, "an empty team yields no round battle to resolve");
RequireThrows(() => LevelCombatSimulator.ResolveRoundMatch(null!, round2), "the round simulator rejects a null team");
RequireThrows(() => LevelCombatSimulator.ResolveRoundMatch(levelCompleteTeam, null!), "the round simulator rejects a null round");
RequireThrows(() => LevelCombatSimulator.BuildRoundEnemies(null!), "BuildRoundEnemies rejects a null round");

// Run modifiers (synergies, combat artifacts, commander) now feed the round autobattle
// (GDD P1: "забеговый автобой реально использует артефакты и синергии").
var enemyArmorSynergy = new SynergyModifiers(armorMultiplier: 2.0);
var neutralRoundEnemies = LevelCombatSimulator.BuildRoundEnemies(round2);
var buffedRoundEnemies = LevelCombatSimulator.BuildRoundEnemies(round2, enemyArmorSynergy);
Require(Math.Abs(buffedRoundEnemies.Sum(u => u.Armor) - (2.0 * neutralRoundEnemies.Sum(u => u.Armor))) < 1e-9, "enemy synergy scales the round roster's armor through BuildRoundEnemies");
Require(LevelCombatSimulator.BuildRoundEnemySynergies(round2).Equals(LevelCombatSimulator.BuildRoundEnemySynergies(round2)), "round enemy synergies are deterministic for a round");
RequireThrows(() => LevelCombatSimulator.BuildRoundEnemySynergies(null!), "round enemy synergies reject a null round");
var defenderRoundTeam = new List<BoardHero>
{
    new(new HeroInstance("dr_front_a", "iron_guard", 1), new TacticalPosition(2, 1)),
    new(new HeroInstance("dr_front_b", "bulwark_captain", 1), new TacticalPosition(2, 2))
};
var defenderRoundBattle = LevelCombatSimulator.ResolveRoundMatch(defenderRoundTeam, round2);
Require(defenderRoundBattle is not null && defenderRoundBattle!.PlayerSynergyModifiers.Equals(SynergyModifiers.ForTeam(defenderRoundTeam)), "the round simulator derives and carries the player's team synergies");
var roundArtifactBattle = LevelCombatSimulator.ResolveRoundMatch(
    levelCompleteTeam,
    round2,
    playerArtifactCombatModifiers: ArtifactCombatModifiers.From(new List<ArtifactState> { new("iron_banner", "Железное Знамя") }),
    playerCommander: CommanderCatalog.Get("warlord").CreateInitialState());
Require(roundArtifactBattle is not null && roundArtifactBattle!.PlayerDamageDealt > 0.0, "the round simulator accepts the run's combat artifacts and commander");
// The iron banner's +armor lands on the unit that started on the player frontline. By the
// time the autobattle resolves that unit has advanced off its starting row, so assert on the
// armored unit itself (base armor 8 + banner bonus) rather than its post-combat frontline row.
Require(roundArtifactBattle!.Units.Any(u => u.Side == TacticalSide.Player && u.Armor >= 8.0 + ArtifactCombatModifiers.IronBannerFrontlineArmorBonus), "the run's iron banner armors the player frontline in the round autobattle");

var freshBattleStats = BattleState.Create(new[]
{
    BattleUnit.FromHero(HeroCatalog.Get("iron_guard"), 1, "stat_ally", TacticalSide.Player, new TacticalPosition(2, 0)),
    BattleUnit.FromHero(HeroCatalog.Get("iron_guard"), 1, "stat_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0))
});
Require(freshBattleStats.PlayerDamageDealt == 0.0 && freshBattleStats.PlayerHealingDone == 0.0 && freshBattleStats.PlayerShieldGranted == 0.0, "a fresh battle starts with zero accumulated combat totals");

var levelComplete = LevelCompleteModel.Build(resolved, CombatState.Start(7), 12);
Require(levelComplete.GoldEarned == 12, "level-complete model carries the gold reward");
Require(levelComplete.DamageDealt > 0, "level-complete model reports rounded damage dealt");
Require(levelComplete.IsVictory == (resolved.Outcome == BattleOutcome.PlayerVictory), "level-complete victory flag matches the battle outcome");
Require(levelComplete.StatRow().Length == 6, "level-complete stat row exposes all six stats");
Require(LevelCompleteModel.FormatDuration(75) == "1:15", "level-complete duration formats as m:ss");
Require(LevelCompleteModel.DescribeOutcome(BattleOutcome.PlayerVictory) == "ПОБЕДА", "level-complete describes a victory outcome");
var clampedSummary = LevelCompleteModel.Build(BattleOutcome.PlayerDefeat, -3, -2, 9.6, 4.4, 2.5, -1);
Require(clampedSummary.DurationSeconds == 0 && clampedSummary.Match3MovesUsed == 0 && clampedSummary.GoldEarned == 0, "level-complete clamps negative inputs to zero");
Require(clampedSummary.DamageDealt == 10 && clampedSummary.HealingDone == 4 && clampedSummary.ShieldGranted == 3, "level-complete rounds combat totals away from zero");
RequireThrows(() => LevelCompleteModel.Build(null!, CombatState.Start(1), 0), "level-complete rejects a null battle");
RequireThrows(() => LevelCompleteModel.Build(resolved, null!, 0), "level-complete rejects a null combat state");

// End-of-run summary: roster aggregation, best-hero ranking and victory/progress counts.
var summaryRun = RunState.NewRun() with
{
    Gold = 18,
    PlayerLevel = 5,
    Team = new List<BoardHero>
    {
        new(new HeroInstance("sum_back", "oath_archer", 1), new TacticalPosition(3, 0)),
        new(new HeroInstance("sum_front", "iron_guard", 2), new TacticalPosition(2, 0))
    }
};
var midRunSummary = RunSummaryModel.Build(summaryRun);
Require(midRunSummary.Team.Count == 2, "run summary lists the full team roster");
Require(!midRunSummary.IsVictory && midRunSummary.RoundsCleared == 0, "an unfinished round-one run has cleared no rounds");
Require(midRunSummary.BestHero is not null && midRunSummary.BestHero!.HeroId == "iron_guard", "run summary picks the higher-star hero as best");
Require(midRunSummary.Gold == 18 && midRunSummary.PlayerLevel == 5, "run summary carries the accumulated rewards");
Require(midRunSummary.ProgressLabel == $"0 / {PveRunSchedule.FinalRound}", "run summary formats round progress");

var victorySummary = RunSummaryModel.Build(finalReward);
Require(victorySummary.IsVictory, "a final-round reward run reports victory");
Require(victorySummary.RoundsCleared == PveRunSchedule.FinalRound, "a victorious run clears every round");
Require(victorySummary.ResultLabel == "ЗАБЕГ ПРОЙДЕН", "run summary labels a cleared run");

var emptyTeamSummary = RunSummaryModel.Build(RunState.NewRun());
Require(emptyTeamSummary.Team.Count == 0 && emptyTeamSummary.BestHero is null, "an empty team yields no best hero");
RequireThrows(() => RunSummaryModel.Build(null!), "run summary rejects a null run");

// Account progress meta model (GDD "Метапрогрессия" / main screen "прогресс аккаунта").
var startingAccount = AccountProgress.Starting;
Require(startingAccount.AccountLevel == 1 && startingAccount.AccountXp == 0 && startingAccount.SoftCurrency == 0, "a fresh account starts at level one with no XP or currency");
Require(startingAccount.UnlockedHeroes == HeroCatalog.All.Count, "a fresh account unlocks the full hero roster");
Require(startingAccount.UnlockedCommanders == CommanderUnlockSchedule.UnlockedCountForLevel(1) && startingAccount.UnlockedCommanders == 1, "a fresh account only unlocks the level-one commanders (the catalog default)");
Require(startingAccount.TotalCommanders == CommanderCatalog.All.Count, "a fresh account counts the whole commander roster as the unlock total");
Require(startingAccount.CommanderUnlockLabel == $"1 / {CommanderCatalog.All.Count}", "account commander unlock label formats unlocked/total");
Require(startingAccount.IsCommanderUnlocked(CommanderCatalog.Default.Id) && !startingAccount.IsCommanderUnlocked("warlord"), "a fresh account has the default commander unlocked but not the level-gated ones");
Require(startingAccount.UnlockedCommanderIds.Count == 1 && startingAccount.UnlockedCommanderIds[0] == CommanderCatalog.Default.Id, "a fresh account lists only the default commander as unlocked");
Require(startingAccount.NextCommanderUnlock is not null && startingAccount.NextCommanderUnlock!.RequiredAccountLevel == 2, "a fresh account's next commander unlock is at account level two");
Require(startingAccount.HeroUnlockLabel == $"{HeroCatalog.All.Count} / {HeroCatalog.All.Count}", "account hero unlock label formats unlocked/total");
Require(AccountProgress.XpForNextLevel(1) == 100 && AccountProgress.XpForNextLevel(2) == 200, "account XP curve scales with level");
Require(startingAccount.XpToNextLevel == 100 && Math.Abs(startingAccount.LevelProgressRatio) < 1e-9, "a fresh account needs a full bar to reach level two");
var accountAfterGains = startingAccount.WithGains(50, 10);
Require(accountAfterGains.AccountLevel == 1 && accountAfterGains.AccountXp == 50 && accountAfterGains.SoftCurrency == 10, "account gains add XP and currency without an early level up");
var accountLevelUp = startingAccount.WithGains(150, 0);
Require(accountLevelUp.AccountLevel == 2 && accountLevelUp.AccountXp == 50, "account XP overflow rolls into the next level");
var accountFromRun = startingAccount.WithRunRewards(RunSummaryModel.Build(RunState.NewRun()));
Require(accountFromRun.AccountXp == 50 && accountFromRun.SoftCurrency == 10, "an unfinished run grants the base account reward");
var accountFromVictory = startingAccount.WithRunRewards(victorySummary);
Require(accountFromVictory.SoftCurrency > accountFromRun.SoftCurrency, "clearing the run grants more soft currency than bailing early");
RequireThrows(() => AccountProgress.XpForNextLevel(0), "account XP curve rejects levels below one");
RequireThrows(() => startingAccount.WithGains(-1, 0), "account gains reject negative XP");
RequireThrows(() => AccountProgress.CalculateRunRewards(null!), "run reward calculation rejects a null summary");

// Account progress persistence between runs (GDD "Метапрогрессия": опыт и валюта сохраняются после забега).
var accountStore = new AccountProgressStore();
Require(!accountStore.HasSavedProgress, "a new account store has no saved progress");
Require(accountStore.Load().AccountLevel == 1 && accountStore.Load().SoftCurrency == 0, "an empty account store loads a fresh starting account");
Require(!accountStore.TryLoad(out var emptyLoaded) && emptyLoaded.AccountLevel == 1, "an empty account store reports no save and yields a starting account");
var earnedAccount = startingAccount.WithGains(260, 35);
accountStore.Save(earnedAccount);
Require(accountStore.HasSavedProgress, "saving account progress marks the store as populated");
Require(accountStore.TryLoad(out var reloadedAccount), "a populated account store reports a save");
Require(reloadedAccount.AccountLevel == earnedAccount.AccountLevel
    && reloadedAccount.AccountXp == earnedAccount.AccountXp
    && reloadedAccount.SoftCurrency == earnedAccount.SoftCurrency, "account progress survives a save/load round-trip");
Require(reloadedAccount.UnlockedCommanders == earnedAccount.UnlockedCommanders
    && reloadedAccount.UnlockedHeroes == earnedAccount.UnlockedHeroes, "account roster unlocks survive a save/load round-trip");
var capturedSnapshot = AccountProgressSnapshot.Capture(earnedAccount);
Require(capturedSnapshot.Version == AccountProgressSnapshot.CurrentVersion, "an account snapshot stamps the current version");
Require(capturedSnapshot.Restore().SoftCurrency == earnedAccount.SoftCurrency, "an account snapshot restores its captured currency");
RequireThrows(() => AccountProgressSnapshot.Capture(null!), "capturing an account snapshot rejects a null account");
RequireThrows(() => (capturedSnapshot with { Version = AccountProgressSnapshot.CurrentVersion + 1 }).Restore(), "restoring rejects an unsupported snapshot version");
accountStore.Clear();
Require(!accountStore.HasSavedProgress && accountStore.Load().SoftCurrency == 0, "clearing the account store drops the save");

// Run summary reward/unlock preview (GDD UI screen "Итог забега": опыт, валюта, разблокировки).
Require(victorySummary.Rewards is null, "the run summary without an account omits the reward preview");
var summaryWithRewards = RunSummaryModel.Build(finalReward, startingAccount);
Require(summaryWithRewards.IsVictory && summaryWithRewards.RoundsCleared == PveRunSchedule.FinalRound, "the account-aware summary keeps the base summary fields");
var runRewards = summaryWithRewards.Rewards ?? throw new InvalidOperationException("Smoke check failed: run rewards missing");
Require(runRewards.AccountXpGained == 400 && runRewards.SoftCurrencyGained == 110, "the run summary previews the earned account XP and currency");
Require(runRewards.AccountLevelsGained == 2, "a cleared run previews the account levels gained");
Require(runRewards.Unlocks.Count == 5, "a cleared run from a fresh account previews two account levels plus the commander, starting-artifact and cosmetic unlocks they grant");
Require(runRewards.HasUnlocks && runRewards.Unlocks[0] == "Уровень аккаунта 2", "run summary unlock notices name each new account level");
Require(runRewards.Unlocks.Any(notice => notice == "Новых командиров: 2"), "levelling an account past commander thresholds previews the newly unlocked commanders");
Require(runRewards.Unlocks.Any(notice => notice == "Новых стартовых артефактов: 2"), "levelling an account past starting-artifact thresholds previews the newly unlocked starting artifacts");
Require(runRewards.Unlocks.Any(notice => notice == "Новой косметики: 2"), "levelling an account past cosmetic thresholds previews the newly unlocked cosmetics");
Require(startingAccount.WithRunRewards(victorySummary).SoftCurrency == startingAccount.SoftCurrency + runRewards.SoftCurrencyGained, "applying run rewards matches the previewed currency");
var freshRunRewards = RunSummaryModel.Build(RunState.NewRun(), startingAccount).Rewards
    ?? throw new InvalidOperationException("Smoke check failed: fresh run rewards missing");
Require(freshRunRewards.AccountXpGained == 50 && freshRunRewards.AccountLevelsGained == 0 && !freshRunRewards.HasUnlocks, "a short run previews a small reward and no unlocks");
RequireThrows(() => RunSummaryModel.Build(RunState.NewRun(), null!), "the account-aware run summary rejects a null account");

// Main menu view-model (GDD UI screen 1 "Главный экран").
var freshMenu = MainMenuModel.Build(RunState.NewRun(), AccountProgress.Starting);
Require(!freshMenu.RunInProgress && freshMenu.StartRunLabel == "Начать забег", "the main menu offers a new run when none is in progress");
Require(freshMenu.StartRunMeta == "ROUND 1 / 10" && freshMenu.FinalRound == PveRunSchedule.FinalRound, "the main menu start button shows the run progress");
Require(freshMenu.CommanderId == CommanderCatalog.Default.Id && freshMenu.CommanderName == CommanderCatalog.Default.Name, "the main menu surfaces the selected commander");
Require(freshMenu.CollectionLabel == $"{HeroCatalog.All.Count} / {HeroCatalog.All.Count}", "the main menu collection button shows unlocked heroes");
var ongoingRun = RunState.NewRun();
ongoingRun = ongoingRun with { Round = 3 };
Require(MainMenuModel.Build(ongoingRun, AccountProgress.Starting).StartRunLabel == "Продолжить забег", "the main menu offers to continue a run already in progress");
RequireThrows(() => MainMenuModel.Build(null!, AccountProgress.Starting), "the main menu rejects a null run");
RequireThrows(() => MainMenuModel.Build(RunState.NewRun(), null!), "the main menu rejects null account progress");

// Commander selection view-model (GDD UI screen 2 "Выбор командира").
var commanderSelect = CommanderSelectModel.Build("warlord");
Require(commanderSelect.Commanders.Count == CommanderCatalog.All.Count, "commander select lists every commander");
Require(commanderSelect.SelectedId == "warlord" && commanderSelect.Selected.Name == "Воевода", "commander select highlights the chosen commander");
Require(commanderSelect.Commanders.Count(card => card.IsSelected) == 1, "exactly one commander card is selected");
Require(commanderSelect.Selected.RecommendedStylesLabel.Contains("/"), "commander select joins recommended styles for display");
Require(commanderSelect.WithSelection("alchemist").SelectedId == "alchemist", "commander select can switch the chosen commander");
RequireThrows(() => CommanderSelectModel.Build("unknown_commander"), "commander select rejects an unknown commander id");

// Commander unlock schedule (GDD "Метапрогрессия": разблокировка новых командиров).
Require(CommanderUnlockSchedule.Entries.Count == CommanderCatalog.All.Count, "every commander has exactly one unlock entry");
Require(CommanderCatalog.All.All(commander => CommanderUnlockSchedule.RequiredLevel(commander.Id) >= 1), "every commander declares a valid unlock level");
Require(CommanderUnlockSchedule.RequiredLevel(CommanderCatalog.Default.Id) == 1, "the catalog default commander unlocks at account level one");
Require(CommanderUnlockSchedule.UnlockedCountForLevel(1) == 1, "account level one unlocks a single commander");
Require(CommanderUnlockSchedule.UnlockedCountForLevel(2) == 2, "account level two unlocks a second commander");
Require(CommanderUnlockSchedule.UnlockedCountForLevel(99) == CommanderCatalog.All.Count, "a high account level unlocks the whole commander roster");
Require(CommanderUnlockSchedule.UnlockedIdsForLevel(1).SequenceEqual(new[] { CommanderCatalog.Default.Id }), "level one unlocks only the default commander, in catalog order");
Require(!CommanderUnlockSchedule.IsUnlocked("warlord", 1) && CommanderUnlockSchedule.IsUnlocked("warlord", 2), "the warlord commander unlocks at account level two");
Require(CommanderUnlockSchedule.NextUnlock(1)!.CommanderId == "warlord" && CommanderUnlockSchedule.NextUnlock(CommanderCatalog.All.Count) is null, "next unlock walks up the schedule and ends when all commanders are unlocked");
RequireThrows(() => CommanderUnlockSchedule.RequiredLevel("unknown_commander"), "the unlock schedule rejects an unknown commander id");
RequireThrows(() => CommanderUnlockSchedule.IsUnlocked("warlord", 0), "the unlock schedule rejects account levels below one");

// Starting-artifact unlock schedule (GDD "Метапрогрессия": разблокировка новых стартовых артефактов).
Require(StartingArtifactUnlockSchedule.Entries.Count >= 3 && StartingArtifactUnlockSchedule.TotalCount == StartingArtifactUnlockSchedule.Entries.Count, "the starting-artifact schedule exposes its pool size");
Require(StartingArtifactUnlockSchedule.Entries.All(entry => ArtifactCatalog.TryGet(entry.ArtifactId, out _)), "every starting-artifact unlock resolves to a catalog artifact");
Require(StartingArtifactUnlockSchedule.Entries.All(entry => ArtifactCatalog.Get(entry.ArtifactId).Rarity == ArtifactRarity.Common), "starting artifacts stay mild Common-rarity options (non-pay-to-win)");
Require(StartingArtifactUnlockSchedule.Entries[0].RequiredAccountLevel == 1, "the first starting artifact unlocks at account level one so a fresh account can always pick one");
Require(StartingArtifactUnlockSchedule.UnlockedCountForLevel(1) == 1, "account level one unlocks a single starting artifact");
Require(StartingArtifactUnlockSchedule.UnlockedCountForLevel(2) == 2, "account level two unlocks a second starting artifact");
Require(StartingArtifactUnlockSchedule.UnlockedCountForLevel(99) == StartingArtifactUnlockSchedule.TotalCount, "a high account level unlocks the whole starting-artifact pool");
Require(StartingArtifactUnlockSchedule.UnlockedIdsForLevel(1).SequenceEqual(new[] { StartingArtifactUnlockSchedule.Entries[0].ArtifactId }), "level one unlocks only the first starting artifact, in schedule order");
Require(!StartingArtifactUnlockSchedule.IsUnlocked(StartingArtifactUnlockSchedule.Entries[1].ArtifactId, 1) && StartingArtifactUnlockSchedule.IsUnlocked(StartingArtifactUnlockSchedule.Entries[1].ArtifactId, 2), "the second starting artifact unlocks at account level two");
Require(StartingArtifactUnlockSchedule.RequiredLevel("MERCHANT_SEAL") == 1, "the starting-artifact unlock lookup is case-insensitive");
Require(StartingArtifactUnlockSchedule.NextUnlock(1)!.ArtifactId == StartingArtifactUnlockSchedule.Entries[1].ArtifactId && StartingArtifactUnlockSchedule.NextUnlock(StartingArtifactUnlockSchedule.TotalCount) is null, "next starting-artifact unlock walks the schedule and ends when all are unlocked");
RequireThrows(() => StartingArtifactUnlockSchedule.RequiredLevel("unknown_artifact"), "the starting-artifact schedule rejects an unknown artifact id");
RequireThrows(() => StartingArtifactUnlockSchedule.IsUnlocked(StartingArtifactUnlockSchedule.Entries[0].ArtifactId, 0), "the starting-artifact schedule rejects account levels below one");

// Account progress surfaces the starting-artifact unlocks (GDD "Метапрогрессия": новые стартовые артефакты).
Require(startingAccount.UnlockedStartingArtifacts == StartingArtifactUnlockSchedule.UnlockedCountForLevel(1) && startingAccount.UnlockedStartingArtifacts == 1, "a fresh account only unlocks the level-one starting artifact");
Require(startingAccount.TotalStartingArtifacts == StartingArtifactUnlockSchedule.TotalCount, "a fresh account counts the whole starting-artifact pool as the unlock total");
Require(startingAccount.StartingArtifactUnlockLabel == $"1 / {StartingArtifactUnlockSchedule.TotalCount}", "account starting-artifact label formats unlocked/total");
Require(startingAccount.UnlockedStartingArtifactIds.Count == 1 && startingAccount.IsStartingArtifactUnlocked(StartingArtifactUnlockSchedule.Entries[0].ArtifactId) && !startingAccount.IsStartingArtifactUnlocked(StartingArtifactUnlockSchedule.Entries[1].ArtifactId), "a fresh account has only the first starting artifact unlocked");
Require(startingAccount.NextStartingArtifactUnlock is not null && startingAccount.NextStartingArtifactUnlock!.RequiredAccountLevel == 2, "a fresh account's next starting-artifact unlock is at account level two");
Require(startingAccount.WithGains(AccountProgress.XpForNextLevel(1), 0).UnlockedStartingArtifacts == 2, "reaching account level two unlocks a second starting artifact on the account");

// Cosmetic catalog and unlock schedule (GDD "Метапрогрессия": косметику, визуальные эффекты рун).
Require(CosmeticCatalog.All.Count >= 3 && CosmeticCatalog.All.Select(cosmetic => cosmetic.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == CosmeticCatalog.All.Count, "the cosmetic catalog has distinct ids");
Require(CosmeticCatalog.RuneEffects.Count >= 1 && CosmeticCatalog.RuneEffects.All(cosmetic => cosmetic.Kind == CosmeticKind.RuneEffect), "the catalog exposes rune visual-effect cosmetics (GDD визуальные эффекты рун)");
Require(CosmeticCatalog.TryGet("RUNE_GLOW", out var runeGlow) && runeGlow.IsRuneEffect, "cosmetic lookup is case-insensitive and flags rune effects");
RequireThrows(() => CosmeticCatalog.Get("unknown_cosmetic"), "cosmetic get throws on unknown ids");
RequireThrows(() => new CosmeticDefinition("", "Name", CosmeticKind.BoardSkin, "desc"), "a cosmetic rejects a blank id");
Require(CosmeticUnlockSchedule.Entries.Count == CosmeticCatalog.All.Count, "every catalog cosmetic has exactly one unlock entry");
Require(CosmeticUnlockSchedule.Entries.All(entry => CosmeticCatalog.TryGet(entry.CosmeticId, out _)), "every cosmetic unlock resolves to a catalog cosmetic");
Require(CosmeticUnlockSchedule.Entries[0].RequiredAccountLevel == 1, "the first cosmetic unlocks at account level one so a fresh account always has one");
Require(CosmeticUnlockSchedule.UnlockedCountForLevel(1) == 1 && CosmeticUnlockSchedule.UnlockedCountForLevel(2) == 2, "cosmetics unlock one per account level");
Require(CosmeticUnlockSchedule.UnlockedCountForLevel(99) == CosmeticUnlockSchedule.TotalCount, "a high account level unlocks the whole cosmetic pool");
Require(CosmeticUnlockSchedule.UnlockedIdsForLevel(1).SequenceEqual(new[] { CosmeticUnlockSchedule.Entries[0].CosmeticId }), "level one unlocks only the first cosmetic, in schedule order");
Require(CosmeticUnlockSchedule.NextUnlock(1)!.CosmeticId == CosmeticUnlockSchedule.Entries[1].CosmeticId && CosmeticUnlockSchedule.NextUnlock(CosmeticUnlockSchedule.TotalCount) is null, "next cosmetic unlock walks the schedule and ends when all are unlocked");
RequireThrows(() => CosmeticUnlockSchedule.RequiredLevel("unknown_cosmetic"), "the cosmetic schedule rejects an unknown id");
RequireThrows(() => CosmeticUnlockSchedule.IsUnlocked(CosmeticUnlockSchedule.Entries[0].CosmeticId, 0), "the cosmetic schedule rejects account levels below one");

// Account surfaces cosmetic unlocks (GDD "Метапрогрессия": косметику, визуальные эффекты рун).
Require(startingAccount.UnlockedCosmetics == 1 && startingAccount.TotalCosmetics == CosmeticUnlockSchedule.TotalCount, "a fresh account only unlocks the level-one cosmetic");
Require(startingAccount.CosmeticUnlockLabel == $"1 / {CosmeticUnlockSchedule.TotalCount}", "account cosmetic label formats unlocked/total");
Require(startingAccount.UnlockedCosmeticIds.Count == 1 && startingAccount.IsCosmeticUnlocked(CosmeticUnlockSchedule.Entries[0].CosmeticId) && !startingAccount.IsCosmeticUnlocked(CosmeticUnlockSchedule.Entries[1].CosmeticId), "a fresh account has only the first cosmetic unlocked");
Require(startingAccount.NextCosmeticUnlock is not null && startingAccount.NextCosmeticUnlock!.RequiredAccountLevel == 2, "a fresh account's next cosmetic unlock is at account level two");
Require(startingAccount.WithGains(AccountProgress.XpForNextLevel(1), 0).UnlockedCosmetics == 2, "reaching account level two unlocks a second cosmetic on the account");

// Metaprogression is not pay-to-win (GDD "Метапрогрессия": баланс должен избегать pay-to-win).
// Cosmetics are purely visual (the record carries no stat/gold/combat field), and every meta
// unlock is gated by account level (earned by playing), never by soft currency: spending
// currency on WithGains does not change any unlock count.
Require(typeof(CosmeticDefinition).GetProperties().All(property => property.PropertyType == typeof(string) || property.PropertyType == typeof(CosmeticKind) || property.PropertyType == typeof(bool)), "a cosmetic exposes no numeric power field, so cosmetics cannot be pay-to-win");
var currencyOnly = startingAccount.WithGains(0, 100000);
Require(currencyOnly.AccountLevel == startingAccount.AccountLevel
    && currencyOnly.UnlockedCommanders == startingAccount.UnlockedCommanders
    && currencyOnly.UnlockedStartingArtifacts == startingAccount.UnlockedStartingArtifacts
    && currencyOnly.UnlockedCosmetics == startingAccount.UnlockedCosmetics, "soft currency alone unlocks nothing — unlocks are gated by account level, not purchase");
var xpOnly = startingAccount.WithGains(AccountProgress.XpForNextLevel(1) + AccountProgress.XpForNextLevel(2), 0);
Require(xpOnly.AccountLevel == 3 && xpOnly.UnlockedCommanders > startingAccount.UnlockedCommanders, "playing (account XP) is the only thing that advances unlocks");

// Account-aware commander selection gates locked commanders (GDD UI screen 2 + метапрогрессия).
var gatedSelect = CommanderSelectModel.Build(CommanderCatalog.Default.Id, AccountProgress.Starting);
Require(gatedSelect.Commanders.Count == CommanderCatalog.All.Count, "the account-aware commander select still lists every commander");
var defaultCard = gatedSelect.Commanders.Single(card => card.Id == CommanderCatalog.Default.Id);
Require(defaultCard.IsUnlocked && defaultCard.UnlockHint is null, "a fresh account can select the default commander");
var lockedCard = gatedSelect.Commanders.Single(card => card.Id == "warlord");
Require(!lockedCard.IsUnlocked && lockedCard.RequiredAccountLevel == 2 && lockedCard.UnlockHint == "Откроется на уровне аккаунта 2", "a fresh account sees the level-gated commander as locked with its unlock hint");
var leveledSelect = CommanderSelectModel.Build(CommanderCatalog.Default.Id, AccountProgress.Starting.WithGains(AccountProgress.XpForNextLevel(1), 0));
Require(leveledSelect.Commanders.Single(card => card.Id == "warlord").IsUnlocked, "reaching account level two unlocks the warlord on the selection screen");
Require(CommanderSelectModel.Build(CommanderCatalog.Default.Id).Commanders.All(card => card.IsUnlocked), "the account-free commander select treats every commander as unlocked");
RequireThrows(() => CommanderSelectModel.Build(CommanderCatalog.Default.Id, (AccountProgress)null!), "the account-aware commander select rejects a null account");

// Hero collection / details view-model (GDD UI screens 1 and 7).
var collection = HeroCollectionModel.Build();
Require(collection.Count == HeroCatalog.All.Count, "the collection lists the whole hero roster");
for (var i = 1; i < collection.Heroes.Count; i += 1)
{
    Require(collection.Heroes[i - 1].Rarity <= collection.Heroes[i].Rarity, "the collection is ordered by rarity");
}
var ironGuardEntry = collection.Heroes.Single(hero => hero.HeroId == "iron_guard");
Require(ironGuardEntry.RuneAffinity == RuneType.Yellow && ironGuardEntry.RuneAffinityLabel == "Жёлтая руна", "a collection entry exposes the hero's preferred rune");
Require(ironGuardEntry.Faction == "Империя" && ironGuardEntry.Cost == 1, "a collection entry carries faction and cost for the detail view");
Require(ironGuardEntry.StatsLabel.Contains("HP") && ironGuardEntry.StatsLabel.Contains("ATK"), "a collection entry summarizes hero stats");
Require(!string.IsNullOrWhiteSpace(ironGuardEntry.Ability) && !string.IsNullOrWhiteSpace(ironGuardEntry.Passive), "a collection entry carries ability and passive text");

// Settings model (GDD UI screen 10 "Настройки").
var defaultSettings = SettingsModel.Default;
Require(defaultSettings.SoundEnabled && defaultSettings.MusicEnabled && defaultSettings.VibrationEnabled, "default settings enable sound, music and vibration");
Require(defaultSettings.Language == SettingsLanguage.Russian && !defaultSettings.TutorialCompleted, "default settings start in Russian with the tutorial active");
Require(!defaultSettings.ToggleSound().SoundEnabled, "settings can toggle sound off");
Require(defaultSettings.CycleGraphicsQuality().GraphicsQuality == GraphicsQuality.High, "settings cycle graphics quality from medium to high");
Require(defaultSettings.CycleLanguage().Language == SettingsLanguage.English, "settings cycle the language");
Require(defaultSettings.CycleBattleSpeed().BattleSpeed == BattleSpeed.Fast, "settings cycle the battle speed");
Require(Math.Abs(defaultSettings.CycleBattleSpeed().BattleSpeedMultiplier - 1.5) < 1e-9, "fast battle speed applies a 1.5x multiplier");
Require(defaultSettings.CompleteTutorial().ResetTutorial().TutorialCompleted == false, "resetting the tutorial clears the completed flag");

// Collection screen navigation (GDD main-menu access to the hero collection).
Require(AppNavigationState.AtMainMenu.CanNavigateTo(AppScreen.Collection), "the main menu can open the hero collection");
Require(AppNavigationState.AtMainMenu.NavigateTo(AppScreen.Collection).CanNavigateTo(AppScreen.MainMenu), "the collection screen can return to the main menu");

// Artifact catalog (reward-screen artifact choices, GDD "Экран награды").
Require(ArtifactCatalog.All.Count >= 6, "the artifact catalog ships the MVP reward pool");
var commonOffer = ArtifactCatalog.OfferThree(1741);
Require(commonOffer.Count == ArtifactCatalog.OfferCount, "an artifact offer presents exactly three choices");
Require(commonOffer.Select(option => option.Id).Distinct().Count() == ArtifactCatalog.OfferCount, "the three artifact choices are distinct");
Require(commonOffer.All(option => !option.IsRare), "a normal round draws from the common artifact pool");
Require(ArtifactCatalog.OfferThree(1741).SequenceEqual(commonOffer), "an artifact offer is deterministic for a given seed");
var rareOffer = ArtifactCatalog.OfferThree(2044, rare: true);
Require(rareOffer.Count == ArtifactCatalog.OfferCount && rareOffer.All(option => option.IsRare), "a boss round draws three rare artifacts");
Require(ArtifactCatalog.TryGet("BLOOD_CHALICE", out var bloodChalice) && bloodChalice.Name == "Кровавый Кубок", "artifact lookup is case-insensitive");
Require(!ArtifactCatalog.TryGet("unknown_artifact", out _), "artifact lookup rejects unknown ids");
Require(commonOffer[0].ToArtifactState().Id == commonOffer[0].Id, "a chosen artifact converts into a stored artifact state");
RequireThrows(() => ArtifactCatalog.Get("unknown_artifact"), "artifact get throws on unknown ids");

// Artifact definition model (GDD P1 artifact model: id, name, rarity, effect, trigger, description).
Require(ArtifactCatalog.All.Count is >= 15 and <= 20, "the artifact catalog designs 15-20 MVP artifacts");
Require(ArtifactCatalog.All.Select(artifact => artifact.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == ArtifactCatalog.All.Count, "every artifact id is unique");
Require(ArtifactCatalog.All.All(artifact => artifact.Name.Length > 0 && artifact.Description.Length > 0), "every artifact carries a name and description");
Require(ArtifactCatalog.All.All(artifact => artifact.IsRare == (artifact.Rarity != ArtifactRarity.Common)), "artifact IsRare follows its rarity");
Require(ArtifactCatalog.All.Count(artifact => !artifact.IsRare) >= ArtifactCatalog.OfferCount, "the common pool can offer three distinct artifacts");
Require(ArtifactCatalog.All.Count(artifact => artifact.IsRare) >= ArtifactCatalog.OfferCount, "the rare pool can offer three distinct artifacts");
Require(ArtifactCatalog.All.Select(artifact => artifact.Effect).Distinct().Count() == 3, "artifacts span combat, economy and rune modifiers");
var bloodChaliceDef = ArtifactCatalog.Get("blood_chalice");
Require(bloodChaliceDef.Rarity == ArtifactRarity.Common && bloodChaliceDef.Effect == ArtifactEffectKind.Rune && bloodChaliceDef.Trigger == ArtifactTrigger.OnRuneMatch, "the blood chalice keeps its rarity, effect and trigger");
var phoenixDef = ArtifactCatalog.Get("phoenix_feather");
Require(phoenixDef.IsRare && phoenixDef.Rarity == ArtifactRarity.Legendary && phoenixDef.Trigger == ArtifactTrigger.OnAllyDeath, "the phoenix feather is a legendary on-death artifact");
Require(phoenixDef.ToRewardOption().IsRare && phoenixDef.ToRewardOption().Description == phoenixDef.Description, "an artifact definition projects onto a rare reward card");
Require(phoenixDef.ToArtifactState().Id == "phoenix_feather", "an artifact definition converts into a stored artifact state");

// Reward screen view-model (GDD UI screen "Экран награды": gold, artifacts, hero, continue).
var heroChoiceRound = PveRunSchedule.GetRound(3);
var heroReward = RewardScreenModel.Build(heroChoiceRound, isVictory: true, baseGold: heroChoiceRound.BaseGoldReward, bonusGold: 1);
Require(heroReward.TotalGold == heroChoiceRound.BaseGoldReward + 1, "the reward screen sums base and bonus gold");
Require(heroReward.GoldLines.Count == 2 && heroReward.GoldLines[1].Amount == 1, "the reward screen breaks out the combat bonus line");
Require(heroReward.ResultLabel == "НАГРАДА ЗА РАУНД", "the reward screen labels a cleared round");
Require(heroReward.OffersHeroReward && heroReward.HeroRewardLabel.Length > 0, "the hero-choice round offers a hero reward");
Require(!heroReward.OffersArtifactChoice && heroReward.ArtifactOptions.Count == 0, "the hero-choice round offers no artifact");
Require(!heroReward.IsRunVictory && heroReward.ContinueLabel == "Продолжить", "a mid-run reward screen continues to the next round");

var eliteRound = PveRunSchedule.GetRound(5);
var artifactReward = RewardScreenModel.Build(eliteRound, isVictory: true, baseGold: eliteRound.BaseGoldReward);
Require(artifactReward.OffersArtifactChoice && artifactReward.ArtifactOptions.Count == ArtifactCatalog.OfferCount, "the elite round offers three artifacts");
Require(!artifactReward.ArtifactIsRare && artifactReward.ArtifactOptions.All(option => !option.IsRare), "the elite round offers common artifacts");
Require(artifactReward.IsOfferedArtifact(artifactReward.ArtifactOptions[0].Id), "the reward screen recognizes its own offered artifacts");
Require(!artifactReward.IsOfferedArtifact("phoenix_feather"), "the reward screen rejects an artifact it did not offer");
Require(artifactReward.GoldLines.Count == 1, "a reward screen with no bonus shows only the base gold line");

var bossReward = RewardScreenModel.Build(PveRunSchedule.GetRound(8), isVictory: true, baseGold: 7);
Require(bossReward.ArtifactIsRare && bossReward.ArtifactOptions.All(option => option.IsRare), "the boss round offers rare artifacts");

var finalRoundReward = RewardScreenModel.Build(PveRunSchedule.GetRound(10), isVictory: true, baseGold: 0);
Require(finalRoundReward.IsRunVictory && finalRoundReward.ContinueLabel == "Итог забега", "the final round routes the continue button to the run summary");

var defeatReward = RewardScreenModel.Build(PveRunSchedule.GetRound(2), isVictory: false, baseGold: 0);
Require(!defeatReward.IsVictory && defeatReward.ResultLabel == "РАУНД ЗАВЕРШЁН", "the reward screen labels an unwon round");

var rewardFromRun = RewardScreenModel.Build(RunState.NewRun() with { Phase = RunPhase.Reward });
Require(rewardFromRun.Round == 1 && rewardFromRun.OffersHeroReward, "the reward screen builds from a run in the reward phase");
RequireThrows(() => RewardScreenModel.Build((PveRoundDefinition)null!, true, 0), "the reward screen rejects a null round");
RequireThrows(() => RewardScreenModel.Build(heroChoiceRound, true, -1), "the reward screen rejects negative base gold");
RequireThrows(() => RewardScreenModel.Build(heroChoiceRound, true, 0, -1), "the reward screen rejects negative bonus gold");
RequireThrows(() => RewardScreenModel.Build((RunState)null!), "the reward screen rejects a null run");

// Claiming one of the three offered artifacts (GDD "выбор одного из трёх артефактов после подходящих раундов").
var artifactRewardRun = RunState.NewRun() with { Round = 5, Phase = RunPhase.Reward };
var offeredArtifacts = artifactRewardRun.RewardArtifactOptions();
Require(offeredArtifacts.Count == ArtifactCatalog.OfferCount, "an artifact reward round offers three choices to claim");
Require(offeredArtifacts.SequenceEqual(artifactRewardRun.RewardArtifactOptions()), "the offered artifacts are deterministic for a round");
var afterArtifactPick = artifactRewardRun.ClaimRewardArtifact(offeredArtifacts[1].Id);
Require(afterArtifactPick.Artifacts.Count == 1 && afterArtifactPick.Artifacts[0].Id == offeredArtifacts[1].Id, "claiming an artifact stores the chosen one on the run");
Require(afterArtifactPick.RoundArtifactClaimed, "claiming an artifact marks the round's choice as taken");
Require(artifactRewardRun.ClaimRewardArtifact(offeredArtifacts[0].Id.ToUpperInvariant()).Artifacts.Count == 1, "the artifact claim accepts an offered id case-insensitively");
RequireThrows(() => afterArtifactPick.ClaimRewardArtifact(offeredArtifacts[0].Id), "only one artifact may be claimed per round");
RequireThrows(() => artifactRewardRun.ClaimRewardArtifact("phoenix_feather"), "claiming rejects an artifact that was not offered");
RequireThrows(() => artifactRewardRun.ClaimRewardArtifact(" "), "claiming rejects a blank artifact id");
var bossArtifactRun = RunState.NewRun() with { Round = 8, Phase = RunPhase.Reward };
Require(bossArtifactRun.RewardArtifactOptions().All(option => option.IsRare), "the boss reward round offers rare artifacts to claim");
var noArtifactRun = RunState.NewRun() with { Round = 2, Phase = RunPhase.Reward };
Require(noArtifactRun.RewardArtifactOptions().Count == 0, "a gold-only round offers no artifact to claim");
RequireThrows(() => noArtifactRun.ClaimRewardArtifact("blood_chalice"), "a gold-only round rejects an artifact claim");
RequireThrows(() => (artifactRewardRun with { Phase = RunPhase.Preparation }).ClaimRewardArtifact("blood_chalice"), "artifacts can only be claimed on the reward screen");

// Economy artifact modifiers (GDD P1 "артефакты как модификаторы ... экономики").
Require(ArtifactModifiers.None.RoundEndGoldBonus == 0 && ArtifactModifiers.None.BuyXpDiscount == 0, "a run with no artifacts has neutral economy modifiers");
Require(RunState.NewRun().Modifiers.RoundEndGoldBonus == 0, "a fresh run owns no economy artifacts");
var merchantArtifacts = new List<ArtifactState> { new("merchant_seal", "Печать Торговца") };
var merchantModifiers = ArtifactModifiers.From(merchantArtifacts);
Require(merchantModifiers.RoundEndGoldBonus == ArtifactModifiers.MerchantSealRoundEndGold && merchantModifiers.BuyXpDiscount == 0, "the merchant seal adds round-end gold and nothing else");
Require(ArtifactModifiers.From(new List<ArtifactState> { new("merchant_seal", "x"), new("merchant_seal", "x") }).RoundEndGoldBonus == 2 * ArtifactModifiers.MerchantSealRoundEndGold, "duplicate economy artifacts stack");
var tomeModifiers = ArtifactModifiers.From(new List<ArtifactState> { new("apprentice_tome", "Том Ученика") });
Require(tomeModifiers.BuyXpDiscount == ArtifactModifiers.ApprenticeTomeXpDiscount && tomeModifiers.RoundEndGoldBonus == 0, "the apprentice tome discounts XP and nothing else");
Require(ArtifactModifiers.From(new List<ArtifactState> { new("phoenix_feather", "x") }).RoundEndGoldBonus == 0, "a combat artifact contributes no economy modifier");
RequireThrows(() => ArtifactModifiers.From(null!), "the economy modifiers reject a null artifact list");
var merchantRun = RunState.NewRun() with { Artifacts = merchantArtifacts, Phase = RunPhase.Combat };
var merchantReward = merchantRun.ClaimReward(goldReward: 0);
Require(merchantReward.Gold == merchantRun.Gold + ArtifactModifiers.MerchantSealRoundEndGold, "the merchant seal grants its round-end gold when a reward is claimed");
var tomeRun = RunState.NewRun() with { Artifacts = new List<ArtifactState> { new("apprentice_tome", "Том Ученика") } };
Require(tomeRun.EffectiveBuyXpCost() == EconomyConfig.Default.BuyXpCost - ArtifactModifiers.ApprenticeTomeXpDiscount, "the apprentice tome lowers the effective XP cost");
Require(tomeRun.BuyXp().Gold == tomeRun.Gold - tomeRun.EffectiveBuyXpCost() && tomeRun.BuyXp().Xp == EconomyConfig.Default.XpPerPurchase, "buying XP with the apprentice tome spends the discounted cost");
Require(RunState.NewRun().EffectiveBuyXpCost() == EconomyConfig.Default.BuyXpCost, "without artifacts the XP cost is unchanged");
Require(PreparationScreenModel.Build(tomeRun).BuyXpCost == tomeRun.EffectiveBuyXpCost(), "the preparation screen shows the discounted XP cost");

// Rune artifacts as match-3 modifiers (GDD P1 "артефакты как модификаторы рун").
Require(ArtifactRuneModifiers.None.IsNeutral, "the empty rune-artifact set is neutral");
Require(ArtifactRuneModifiers.From(new List<ArtifactState>()).IsNeutral, "a run with no rune artifacts has neutral rune modifiers");
Require(RunState.NewRun().RuneModifiers.IsNeutral, "a fresh run owns no rune artifacts");
RequireThrows(() => ArtifactRuneModifiers.From(null!), "the rune modifiers reject a null artifact list");
RequireThrows(() => ArtifactRuneModifiers.None.Apply(null!), "applying rune modifiers rejects a null effect");
Require(ArtifactRuneModifiers.From(new List<ArtifactState> { new("phoenix_feather", "x") }).IsNeutral, "a combat artifact contributes no rune modifier");
var emberModifiers = ArtifactRuneModifiers.From(new List<ArtifactState> { new("ember_core", "Тлеющее Ядро") });
Require(Math.Abs(emberModifiers.RedPhysicalMultiplier - (1.0 + ArtifactRuneModifiers.EmberCorePhysicalBonus)) < 1e-9 && emberModifiers.GreenHealingMultiplier == 1.0, "the ember core boosts red physical power and nothing else");
Require(Math.Abs(ArtifactRuneModifiers.From(new List<ArtifactState> { new("ember_core", "x"), new("ember_core", "x") }).RedPhysicalMultiplier - (1.0 + (2 * ArtifactRuneModifiers.EmberCorePhysicalBonus))) < 1e-9, "duplicate rune artifacts stack additively");
var runeArtifactBattle = BattleState.Create(new[]
{
    MakeUnit("ra_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 50, 0, 1.0, 100.0),
    MakeUnit("ra_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 100.0)
});
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 40), runeArtifactModifiers: emberModifiers).Units.First(u => u.UnitId == "ra_enemy").CurrentHealth - 52.0) < 1e-9, "the ember core scales a red rune's physical damage");
var chaliceModifiers = ArtifactRuneModifiers.From(new List<ArtifactState> { new("blood_chalice", "Кровавый Кубок") });
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.Healing, 30, rune: RuneType.Green), runeArtifactModifiers: chaliceModifiers).Units.First(u => u.UnitId == "ra_ally").CurrentHealth - 89.0) < 1e-9, "the blood chalice scales a green rune's healing");
var totemModifiers = ArtifactRuneModifiers.From(new List<ArtifactState> { new("warding_totem", "Оберегающий Тотем") });
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 20, rune: RuneType.Yellow), runeArtifactModifiers: totemModifiers).Units.First(u => u.UnitId == "ra_ally").Shield - 26.0) < 1e-9, "the warding totem scales a yellow rune's shield");
var sparkModifiers = ArtifactRuneModifiers.From(new List<ArtifactState> { new("spark_capacitor", "Искровой Конденсатор") });
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.Mana, 24, rune: RuneType.Blue), runeArtifactModifiers: sparkModifiers).Units.First(u => u.UnitId == "ra_ally").CurrentMana - 30.0) < 1e-9, "the spark capacitor scales a blue rune's mana");
var sigilModifiers = ArtifactRuneModifiers.From(new List<ArtifactState> { new("abyssal_sigil", "Печать Бездны") });
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.MagicDamage, 40, rune: RuneType.Purple), runeArtifactModifiers: sigilModifiers).Units.First(u => u.UnitId == "ra_enemy").CurrentHealth - 48.0) < 1e-9, "the abyssal sigil scales a purple rune's magic damage");
var prismModifiers = ArtifactRuneModifiers.From(new List<ArtifactState> { new("prism_lens", "Призменная Линза") });
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.CommanderEnergy, 5, rune: RuneType.White), runeArtifactModifiers: prismModifiers).CommanderEnergy - 6.5) < 1e-9, "the prism lens scales a white rune's commander energy");
var conduitModifiers = ArtifactRuneModifiers.From(new List<ArtifactState> { new("chain_conduit", "Проводник Цепей") });
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 40, chainNumber: 2), runeArtifactModifiers: conduitModifiers).Units.First(u => u.UnitId == "ra_enemy").CurrentHealth - 50.0) < 1e-9, "the chain conduit boosts chain-reaction rune power");
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 40, chainNumber: 1), runeArtifactModifiers: conduitModifiers).Units.First(u => u.UnitId == "ra_enemy").CurrentHealth - 60.0) < 1e-9, "the chain conduit leaves the first non-chain match unchanged");
Require(Math.Abs(runeArtifactBattle.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 40)).Units.First(u => u.UnitId == "ra_enemy").CurrentHealth - 60.0) < 1e-9, "the neutral rune-modifier default leaves a rune effect untouched");

// Combat artifacts as start-of-combat stat modifiers (GDD P1 "артефакты как модификаторы боя").
Require(ArtifactCombatModifiers.None.IsNeutral, "the empty combat-artifact set is neutral");
Require(ArtifactCombatModifiers.From(new List<ArtifactState>()).IsNeutral, "a run with no combat artifacts has neutral combat modifiers");
Require(RunState.NewRun().CombatModifiers.IsNeutral, "a fresh run owns no start-of-combat artifacts");
RequireThrows(() => ArtifactCombatModifiers.From(null!), "the combat modifiers reject a null artifact list");
RequireThrows(() => ArtifactCombatModifiers.None.Apply(null!), "applying combat modifiers rejects a null unit");
Require(ArtifactCombatModifiers.From(new List<ArtifactState> { new("merchant_seal", "x") }).IsNeutral, "an economy artifact contributes no start-of-combat modifier");
var ironModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("iron_banner", "Железное Знамя") });
Require(Math.Abs(ironModifiers.FrontlineArmorBonus - ArtifactCombatModifiers.IronBannerFrontlineArmorBonus) < 1e-9 && ironModifiers.AttackSpeedMultiplier == 1.0, "the iron banner adds frontline armor and nothing else");
var swiftModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("swift_boots", "Сапоги Скорости") });
Require(Math.Abs(swiftModifiers.AttackSpeedMultiplier - (1.0 + ArtifactCombatModifiers.SwiftBootsAttackSpeedBonus)) < 1e-9 && swiftModifiers.FrontlineArmorBonus == 0.0, "the swift boots add attack speed and nothing else");
Require(Math.Abs(ArtifactCombatModifiers.From(new List<ArtifactState> { new("iron_banner", "x"), new("iron_banner", "x") }).FrontlineArmorBonus - (2 * ArtifactCombatModifiers.IronBannerFrontlineArmorBonus)) < 1e-9, "duplicate combat artifacts stack additively");
var ironBannerBattle = BattleState.Create(new[]
{
    MakeUnit("ca_front", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 100.0, 5.0),
    MakeUnit("ca_back", TacticalSide.Player, new TacticalPosition(3, 0), 100, 100, 10, 1.0, 1.0, 100.0, 5.0),
    MakeUnit("ca_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0)
}, playerArtifactCombatModifiers: ironModifiers);
Require(Math.Abs(ironBannerBattle.Units.First(u => u.UnitId == "ca_front").Armor - (5.0 + ArtifactCombatModifiers.IronBannerFrontlineArmorBonus)) < 1e-9, "the iron banner armors the allied frontline at combat start");
Require(Math.Abs(ironBannerBattle.Units.First(u => u.UnitId == "ca_back").Armor - 5.0) < 1e-9, "the iron banner does not armor the allied backline");
Require(Math.Abs(ironBannerBattle.Units.First(u => u.UnitId == "ca_enemy").Armor) < 1e-9, "the iron banner does not buff the enemy side");
var swiftBootsBattle = BattleState.Create(new[]
{
    MakeUnit("sb_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 2.0, 1.0),
    MakeUnit("sb_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 2.0, 1.0)
}, playerArtifactCombatModifiers: swiftModifiers);
Require(Math.Abs(swiftBootsBattle.Units.First(u => u.UnitId == "sb_ally").AttacksPerSecond - (2.0 * (1.0 + ArtifactCombatModifiers.SwiftBootsAttackSpeedBonus))) < 1e-9, "the swift boots speed up the allied units at combat start");
Require(Math.Abs(swiftBootsBattle.Units.First(u => u.UnitId == "sb_enemy").AttacksPerSecond - 2.0) < 1e-9, "the swift boots do not speed up the enemy side");
Require(Math.Abs(BattleState.Create(new[]
{
    MakeUnit("ca_neutral_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 100.0, 5.0),
    MakeUnit("ca_neutral_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0)
}).Units.First(u => u.UnitId == "ca_neutral_ally").Armor - 5.0) < 1e-9, "the neutral combat-modifier default leaves unit stats untouched");

// Phoenix feather revive (GDD P1 "Перо Феникса": павший герой возрождается раз за бой).
var phoenixModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("phoenix_feather", "Перо Феникса") });
Require(phoenixModifiers.PhoenixRevives == ArtifactCombatModifiers.PhoenixFeatherRevives && phoenixModifiers.FrontlineArmorBonus == 0.0 && phoenixModifiers.AttackSpeedMultiplier == 1.0, "the phoenix feather grants a revive charge and no stat tweak");
Require(!phoenixModifiers.IsNeutral, "owning a phoenix feather is not a neutral combat-modifier set");
Require(ArtifactCombatModifiers.From(new List<ArtifactState> { new("phoenix_feather", "x"), new("phoenix_feather", "x") }).PhoenixRevives == 2 * ArtifactCombatModifiers.PhoenixFeatherRevives, "duplicate phoenix feathers stack revive charges");
var phoenixBattle = BattleState.Create(new[]
{
    MakeUnit("px_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 10, 0, 1.0, 1.0, 0.0),
    MakeUnit("px_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 50, 1.0, 0.0, 0.0)
}, playerArtifactCombatModifiers: phoenixModifiers);
Require(phoenixBattle.PlayerReviveChargesRemaining == 1, "the phoenix feather seeds one revive charge into the battle");
var afterPhoenixTick = phoenixBattle.Tick(1.0);
var revivedAlly = afterPhoenixTick.Units.First(u => u.UnitId == "px_ally");
Require(revivedAlly.IsAlive && Math.Abs(revivedAlly.CurrentHealth - (100.0 * ArtifactCombatModifiers.PhoenixReviveHealthFraction)) < 1e-9, "a fallen ally is revived to half health by the phoenix feather");
Require(afterPhoenixTick.PlayerReviveChargesRemaining == 0 && afterPhoenixTick.Outcome == BattleOutcome.Ongoing, "reviving an ally spends the charge and keeps the battle going");
var afterSecondPhoenixTick = afterPhoenixTick.Tick(1.0);
Require(!afterSecondPhoenixTick.Units.First(u => u.UnitId == "px_ally").IsAlive && afterSecondPhoenixTick.Outcome == BattleOutcome.PlayerDefeat, "with no charge left the ally stays dead on its next death");
Require(BattleState.Create(new[]
{
    MakeUnit("px_neutral_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 10, 0, 1.0, 1.0, 0.0),
    MakeUnit("px_neutral_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 50, 1.0, 0.0, 0.0)
}).Tick(1.0) is { Outcome: BattleOutcome.PlayerDefeat } neutralPhoenix && !neutralPhoenix.Units.First(u => u.UnitId == "px_neutral_ally").IsAlive, "without a phoenix feather a fallen ally is not revived");

// Soul harvest on-kill heal (GDD P1 "Жатва Душ": убийства врагов лечат союзников).
var soulModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("soul_harvest", "Жатва Душ") });
Require(Math.Abs(soulModifiers.SoulHarvestHealPerKillTotal - ArtifactCombatModifiers.SoulHarvestHealPerKill) < 1e-9 && soulModifiers.FrontlineArmorBonus == 0.0 && soulModifiers.AttackSpeedMultiplier == 1.0 && soulModifiers.PhoenixRevives == 0, "the soul harvest grants per-kill healing and no other modifier");
Require(!soulModifiers.IsNeutral, "owning a soul harvest is not a neutral combat-modifier set");
Require(Math.Abs(ArtifactCombatModifiers.From(new List<ArtifactState> { new("soul_harvest", "x"), new("soul_harvest", "x") }).SoulHarvestHealPerKillTotal - (2 * ArtifactCombatModifiers.SoulHarvestHealPerKill)) < 1e-9, "duplicate soul harvests stack per-kill healing");
var soulBattle = BattleState.Create(new[]
{
    MakeUnit("sh_killer", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 50, 1.0, 0.0, 0.0),
    MakeUnit("sh_wounded", TacticalSide.Player, new TacticalPosition(3, 0), 100, 50, 0, 1.0, 5.0, 0.0),
    MakeUnit("sh_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 5, 0, 1.0, 5.0, 0.0)
}, playerArtifactCombatModifiers: soulModifiers);
Require(Math.Abs(soulBattle.PlayerSoulHarvestHealPerKill - ArtifactCombatModifiers.SoulHarvestHealPerKill) < 1e-9, "the soul harvest seeds the per-kill heal into the battle");
var afterSoulTick = soulBattle.Tick(1.0);
Require(!afterSoulTick.Units.First(u => u.UnitId == "sh_enemy").IsAlive, "the killer slays the low-health enemy");
Require(Math.Abs(afterSoulTick.Units.First(u => u.UnitId == "sh_wounded").CurrentHealth - (50.0 + ArtifactCombatModifiers.SoulHarvestHealPerKill)) < 1e-9, "an enemy kill heals the wounded ally through the soul harvest");
var noSoulTick = BattleState.Create(new[]
{
    MakeUnit("ns_killer", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 50, 1.0, 0.0, 0.0),
    MakeUnit("ns_wounded", TacticalSide.Player, new TacticalPosition(3, 0), 100, 50, 0, 1.0, 5.0, 0.0),
    MakeUnit("ns_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 5, 0, 1.0, 5.0, 0.0)
}).Tick(1.0);
Require(Math.Abs(noSoulTick.Units.First(u => u.UnitId == "ns_wounded").CurrentHealth - 50.0) < 1e-9, "without a soul harvest an enemy kill does not heal allies");
var soulRuneBattle = BattleState.Create(new[]
{
    MakeUnit("shr_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 40, 0, 1.0, 5.0, 0.0),
    MakeUnit("shr_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 10, 0, 1.0, 5.0, 0.0)
}, playerArtifactCombatModifiers: soulModifiers);
var afterSoulRune = soulRuneBattle.ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 40));
Require(!afterSoulRune.Units.First(u => u.UnitId == "shr_enemy").IsAlive && Math.Abs(afterSoulRune.Units.First(u => u.UnitId == "shr_ally").CurrentHealth - (40.0 + ArtifactCombatModifiers.SoulHarvestHealPerKill)) < 1e-9, "a rune-effect kill also triggers the soul-harvest heal");

// Hunter's mark: player ranged auto-attacks hit the enemy backline harder (GDD P1 "Метка Охотника").
var huntersModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("hunters_mark", "Метка Охотника") });
Require(Math.Abs(huntersModifiers.RangedBacklineDamageMultiplier - (1.0 + ArtifactCombatModifiers.HuntersMarkRangedBacklineBonus)) < 1e-9 && huntersModifiers.SummonDurationMultiplier == 1.0 && huntersModifiers.CommanderEnergyMultiplier == 1.0 && huntersModifiers.ShieldStrengthMultiplier == 1.0, "the hunter's mark boosts only ranged backline damage");
Require(!huntersModifiers.IsNeutral, "owning a hunter's mark is not a neutral combat-modifier set");
Require(Math.Abs(ArtifactCombatModifiers.From(new List<ArtifactState> { new("hunters_mark", "x"), new("hunters_mark", "x") }).RangedBacklineDamageMultiplier - (1.0 + (2 * ArtifactCombatModifiers.HuntersMarkRangedBacklineBonus))) < 1e-9, "duplicate hunter's marks stack the backline bonus");
var huntersBacklineHit = BattleState.Create(new[]
{
    MakeUnit("hm_archer", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 20, 1.0, 0.0, 0.0, 0.0, BattleAttackType.Ranged),
    MakeUnit("hm_back", TacticalSide.Enemy, new TacticalPosition(0, 0), 100, 100, 0, 1.0, 5.0, 0.0)
}, playerArtifactCombatModifiers: huntersModifiers).Tick(1.0);
Require(Math.Abs(huntersBacklineHit.Units.First(u => u.UnitId == "hm_back").CurrentHealth - (100.0 - (20.0 * (1.0 + ArtifactCombatModifiers.HuntersMarkRangedBacklineBonus)))) < 1e-9, "a ranged hit on the enemy backline is boosted by the hunter's mark");
var huntersFrontlineHit = BattleState.Create(new[]
{
    MakeUnit("hmf_archer", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 20, 1.0, 0.0, 0.0, 0.0, BattleAttackType.Ranged),
    MakeUnit("hmf_front", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 0, 1.0, 5.0, 0.0)
}, playerArtifactCombatModifiers: huntersModifiers).Tick(1.0);
Require(Math.Abs(huntersFrontlineHit.Units.First(u => u.UnitId == "hmf_front").CurrentHealth - 80.0) < 1e-9, "the hunter's mark leaves ranged damage to the enemy frontline unchanged");
var huntersMeleeHit = BattleState.Create(new[]
{
    MakeUnit("hmm_melee", TacticalSide.Player, new TacticalPosition(1, 0), 100, 100, 20, 1.0, 0.0, 0.0, 0.0, BattleAttackType.Melee),
    MakeUnit("hmm_back", TacticalSide.Enemy, new TacticalPosition(0, 0), 100, 100, 0, 1.0, 5.0, 0.0)
}, playerArtifactCombatModifiers: huntersModifiers).Tick(1.0);
Require(Math.Abs(huntersMeleeHit.Units.First(u => u.UnitId == "hmm_back").CurrentHealth - 80.0) < 1e-9, "the hunter's mark does not boost melee attacks on the backline");

// Clockwork heart: player timed summons live longer (GDD P1 "Заводное Сердце").
var clockworkModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("clockwork_heart", "Заводное Сердце") });
Require(Math.Abs(clockworkModifiers.SummonDurationMultiplier - (1.0 + ArtifactCombatModifiers.ClockworkHeartSummonDurationBonus)) < 1e-9 && clockworkModifiers.RangedBacklineDamageMultiplier == 1.0 && clockworkModifiers.CommanderEnergyMultiplier == 1.0 && clockworkModifiers.ShieldStrengthMultiplier == 1.0, "the clockwork heart extends only summon duration");
Require(!clockworkModifiers.IsNeutral, "owning a clockwork heart is not a neutral combat-modifier set");
Require(Math.Abs(ArtifactCombatModifiers.From(new List<ArtifactState> { new("clockwork_heart", "x"), new("clockwork_heart", "x") }).SummonDurationMultiplier - (1.0 + (2 * ArtifactCombatModifiers.ClockworkHeartSummonDurationBonus))) < 1e-9, "duplicate clockwork hearts stack summon duration");
var clockworkTurret = BattleState.Create(new[]
{
    MakeUnit("ch_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 0.0),
    MakeUnit("ch_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0, 0.0)
}, playerSynergyModifiers: mechanistFourModifiers, playerArtifactCombatModifiers: clockworkModifiers)
    .ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 1, tier: RuneMatchTier.Match4), TacticalSide.Player, mechanistFourModifiers)
    .Units.First(u => u.UnitId.Contains("mechanist_turret"));
Require(clockworkTurret.SummonMillisecondsRemaining == (int)Math.Round(BattleState.MechanistTurretDurationMilliseconds * (1.0 + ArtifactCombatModifiers.ClockworkHeartSummonDurationBonus), MidpointRounding.AwayFromZero), "the clockwork heart extends the player's match-4 turret lifetime");
var neutralTurret = BattleState.Create(new[]
{
    MakeUnit("nch_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 0.0),
    MakeUnit("nch_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0, 0.0)
}, playerSynergyModifiers: mechanistFourModifiers)
    .ApplyRuneEffect(Effect(RuneEffectKind.PhysicalDamage, 1, tier: RuneMatchTier.Match4), TacticalSide.Player, mechanistFourModifiers)
    .Units.First(u => u.UnitId.Contains("mechanist_turret"));
Require(neutralTurret.SummonMillisecondsRemaining == BattleState.MechanistTurretDurationMilliseconds, "without a clockwork heart the turret keeps its base lifetime");

// Crown of command: player rune-driven commander energy is amplified (GDD P1 "Венец Командования").
var crownModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("crown_of_command", "Венец Командования") });
Require(Math.Abs(crownModifiers.CommanderEnergyMultiplier - (1.0 + ArtifactCombatModifiers.CrownOfCommandEnergyBonus)) < 1e-9 && crownModifiers.RangedBacklineDamageMultiplier == 1.0 && crownModifiers.SummonDurationMultiplier == 1.0 && crownModifiers.ShieldStrengthMultiplier == 1.0, "the crown of command boosts only commander energy");
Require(!crownModifiers.IsNeutral, "owning a crown of command is not a neutral combat-modifier set");
Require(Math.Abs(ArtifactCombatModifiers.From(new List<ArtifactState> { new("crown_of_command", "x"), new("crown_of_command", "x") }).CommanderEnergyMultiplier - (1.0 + (2 * ArtifactCombatModifiers.CrownOfCommandEnergyBonus))) < 1e-9, "duplicate crowns stack commander energy");
var crownBattle = BattleState.Create(new[]
{
    MakeUnit("cc_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 0.0),
    MakeUnit("cc_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0, 0.0)
}, playerArtifactCombatModifiers: crownModifiers);
Require(Math.Abs(crownBattle.ApplyRuneEffect(Effect(RuneEffectKind.CommanderEnergy, 10)).CommanderEnergy - (10.0 * (1.0 + ArtifactCombatModifiers.CrownOfCommandEnergyBonus))) < 1e-9, "the crown of command amplifies player rune commander energy");
Require(Math.Abs(crownBattle.ApplyRuneEffect(Effect(RuneEffectKind.CommanderEnergy, 10), TacticalSide.Enemy).CommanderEnergy - 10.0) < 1e-9, "the crown of command does not amplify enemy commander energy");
Require(Math.Abs(BattleState.Create(new[]
{
    MakeUnit("ncc_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 0.0),
    MakeUnit("ncc_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0, 0.0)
}).ApplyRuneEffect(Effect(RuneEffectKind.CommanderEnergy, 10)).CommanderEnergy - 10.0) < 1e-9, "without a crown the commander energy gain is unscaled");

// Guardian aegis: player shields are granted stronger, still capped at 60% max health (GDD P1 "Эгида Стража").
var aegisModifiers = ArtifactCombatModifiers.From(new List<ArtifactState> { new("guardian_aegis", "Эгида Стража") });
Require(Math.Abs(aegisModifiers.ShieldStrengthMultiplier - (1.0 + ArtifactCombatModifiers.GuardianAegisShieldBonus)) < 1e-9 && aegisModifiers.RangedBacklineDamageMultiplier == 1.0 && aegisModifiers.SummonDurationMultiplier == 1.0 && aegisModifiers.CommanderEnergyMultiplier == 1.0, "the guardian aegis boosts only shield strength");
Require(!aegisModifiers.IsNeutral, "owning a guardian aegis is not a neutral combat-modifier set");
Require(Math.Abs(ArtifactCombatModifiers.From(new List<ArtifactState> { new("guardian_aegis", "x"), new("guardian_aegis", "x") }).ShieldStrengthMultiplier - (1.0 + (2 * ArtifactCombatModifiers.GuardianAegisShieldBonus))) < 1e-9, "duplicate guardian aegises stack shield strength");
var aegisBattle = BattleState.Create(new[]
{
    MakeUnit("ga_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 0.0),
    MakeUnit("ga_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0, 0.0)
}, playerArtifactCombatModifiers: aegisModifiers);
Require(Math.Abs(aegisBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 20, rune: RuneType.Yellow)).Units.First(u => u.UnitId == "ga_ally").Shield - (20.0 * (1.0 + ArtifactCombatModifiers.GuardianAegisShieldBonus))) < 1e-9, "the guardian aegis strengthens player shields");
Require(Math.Abs(aegisBattle.ApplyRuneEffect(Effect(RuneEffectKind.Shield, 50, rune: RuneType.Yellow)).Units.First(u => u.UnitId == "ga_ally").Shield - 60.0) < 1e-9, "the guardian aegis shield is still capped at 60 percent of max health");
Require(Math.Abs(BattleState.Create(new[]
{
    MakeUnit("nga_ally", TacticalSide.Player, new TacticalPosition(2, 0), 100, 100, 10, 1.0, 1.0, 0.0),
    MakeUnit("nga_enemy", TacticalSide.Enemy, new TacticalPosition(1, 0), 100, 100, 10, 1.0, 1.0, 0.0)
}).ApplyRuneEffect(Effect(RuneEffectKind.Shield, 20, rune: RuneType.Yellow)).Units.First(u => u.UnitId == "nga_ally").Shield - 20.0) < 1e-9, "without a guardian aegis the shield keeps its base strength");

// Claiming the hero reward (GDD "награды героем после выбранных раундов").
var starterRewardRun = RunState.NewRun() with { Round = 1, Phase = RunPhase.Reward };
var starterHeroOptions = starterRewardRun.RewardHeroOptions();
Require(starterHeroOptions.Count == HeroCatalog.RewardOfferCount, "the starter-hero round offers three heroes to claim");
Require(starterHeroOptions.All(option => option.Rarity == HeroRarity.Common && option.Cost == 1), "the starter-hero round offers 1-cost common heroes");
Require(starterHeroOptions.Select(option => option.Id).Distinct().Count() == HeroCatalog.RewardOfferCount, "the offered reward heroes are distinct");
Require(starterRewardRun.RewardHeroOptions().SequenceEqual(starterHeroOptions), "the reward hero offer is deterministic for a round");
var afterHeroPick = starterRewardRun.ClaimRewardHero(starterHeroOptions[2].Id);
Require(afterHeroPick.Bench.Count == 1 && afterHeroPick.Bench[0].HeroId == starterHeroOptions[2].Id && afterHeroPick.Bench[0].Stars == 1, "claiming a hero reward adds the chosen 1-star hero to the bench");
Require(afterHeroPick.RoundHeroClaimed, "claiming a hero reward marks the round's hero choice as taken");
Require(starterRewardRun.ClaimRewardHero(starterHeroOptions[0].Id.ToUpperInvariant()).Bench.Count == 1, "the hero claim accepts an offered id case-insensitively");
RequireThrows(() => afterHeroPick.ClaimRewardHero(starterHeroOptions[0].Id), "only one hero reward may be claimed per round");
RequireThrows(() => starterRewardRun.ClaimRewardHero("astral_regent"), "claiming rejects a hero that was not offered");
RequireThrows(() => starterRewardRun.ClaimRewardHero(" "), "the hero claim rejects a blank id");
var heroChoiceRewardRun = RunState.NewRun() with { Round = 3, Phase = RunPhase.Reward };
Require(heroChoiceRewardRun.RewardHeroOptions().Count == HeroCatalog.RewardOfferCount && heroChoiceRewardRun.RewardHeroOptions().All(option => option.Rarity <= HeroRarity.Rare), "the hero-choice round offers common and rare heroes");
var noHeroRewardRun = RunState.NewRun() with { Round = 2, Phase = RunPhase.Reward };
Require(noHeroRewardRun.RewardHeroOptions().Count == 0, "a gold-only round offers no hero reward");
RequireThrows(() => noHeroRewardRun.ClaimRewardHero("iron_guard"), "a gold-only round rejects a hero claim");
RequireThrows(() => (starterRewardRun with { Phase = RunPhase.Preparation }).ClaimRewardHero(starterHeroOptions[0].Id), "hero rewards can only be claimed on the reward screen");
var fullBenchRewardRun = starterRewardRun with
{
    Bench = Enumerable.Range(0, EconomyConfig.Default.StartingBenchSize)
        .Select(index => new HeroInstance($"reward_full_{index}", "iron_guard", 1))
        .ToList()
};
RequireThrows(() => fullBenchRewardRun.ClaimRewardHero(starterHeroOptions[0].Id), "a hero reward cannot be claimed when the bench is full");

// Event catalog and event screen view-model (GDD UI screen "Экран события").
Require(EventCatalog.All.Count == 4, "the event catalog ships the four MVP event archetypes");
Require(
    EventCatalog.All.Select(option => option.Kind).Distinct().Count() == 4,
    "every event archetype is represented exactly once"
);
foreach (EventChoiceKind kind in Enum.GetValues(typeof(EventChoiceKind)))
{
    var option = EventCatalog.Get(kind);
    Require(option.Kind == kind, "the event catalog returns the requested archetype");
    Require(option.Title.Length > 0 && option.Description.Length > 0, "every event option carries player-facing copy");
    Require(option.AcceptLabel.Length > 0 && option.DeclineLabel.Length > 0, "every event option offers accept and decline labels");
}
var tradeEvent = EventCatalog.TradeHealthForGold;
Require(tradeEvent.CostsHealth && tradeEvent.HealthCost == EventCatalog.TradeHealthCost, "the merchant event trades run health");
Require(tradeEvent.GrantsGold && tradeEvent.GoldReward == EventCatalog.TradeGoldReward, "the merchant event grants gold");
Require(EventCatalog.CursedFreeHero.GrantsHero && EventCatalog.CursedFreeHero.AppliesCurse, "the cursed-gift event grants a hero with a curse");
Require(EventCatalog.FactionBoost.BuffsFaction, "the blessing event buffs a faction next battle");
Require(EventCatalog.SacrificeHeroForArtifact.RemovesHero && EventCatalog.SacrificeHeroForArtifact.GrantsArtifact, "the sacrifice event swaps a hero for an artifact");
Require(EventCatalog.TryGet("EVENT_TRADE_HEALTH_FOR_GOLD", out var fetchedEvent) && fetchedEvent.Kind == EventChoiceKind.TradeHealthForGold, "event lookup is case-insensitive");
Require(!EventCatalog.TryGet("unknown_event", out _), "event lookup rejects unknown ids");
RequireThrows(() => EventCatalog.Get((EventChoiceKind)999), "event catalog rejects unknown archetypes");

var eventRound = PveRunSchedule.GetRound(4);
var eventScreen = EventScreenModel.Build(eventRound);
Require(eventScreen.Round == 4 && eventScreen.RoundType == PveRoundType.Event, "the event screen builds from the GDD event round");
Require(eventScreen.EventName == eventRound.EnemyName && eventScreen.DesignGoal == eventRound.DesignGoal, "the event screen carries the round context");
Require(eventScreen.Headline == EventScreenModel.EventHeadline, "the event screen shows the event headline");
Require(eventScreen.AllowsDecline && eventScreen.ContinueLabel == "Продолжить", "the event screen can be declined and continued");
Require(EventScreenModel.Build(eventRound).Kind == eventScreen.Kind, "the event screen picks a deterministic archetype for a round");
foreach (EventChoiceKind kind in Enum.GetValues(typeof(EventChoiceKind)))
{
    var screen = EventScreenModel.ForEvent(EventCatalog.Get(kind), round: 4, eventName: "Тест", designGoal: "Цель");
    Require(screen.Offers(kind) && screen.Choice.Kind == kind, "the event screen renders every supported archetype");
}
Require(EventScreenModel.Build(RunState.NewRun() with { Round = 4 }).RoundType == PveRoundType.Event, "the event screen builds from a run on an event round");
RequireThrows(() => EventScreenModel.Build(PveRunSchedule.GetRound(2)), "the event screen rejects a non-event round");
RequireThrows(() => EventScreenModel.Build((PveRoundDefinition)null!), "the event screen rejects a null round");
RequireThrows(() => EventScreenModel.Build((RunState)null!), "the event screen rejects a null run");
RequireThrows(() => EventScreenModel.ForEvent(null!, 4, "Тест", "Цель"), "the event screen rejects a null choice");

// Event resolution: entering an event round and applying/declining the offered outcome.
var eventRunBase = RunState.NewRun() with { Round = 4 };
RequireThrows(() => (RunState.NewRun() with { Round = 2 }).EnterEvent(), "only event rounds offer an event encounter");
var enteredEvent = eventRunBase.EnterEvent();
Require(enteredEvent.Phase == RunPhase.Event && !enteredEvent.RoundEventResolved, "entering an event round opens an unresolved event encounter");
Require(enteredEvent.OfferedEvent.Kind == EventScreenModel.Build(enteredEvent).Kind, "the run exposes the round's deterministically offered event");
RequireThrows(() => enteredEvent.AdvanceRound(), "an unresolved event blocks advancing the round");

// Trade run health for gold (GDD "обмен здоровья на золото").
var beforeTrade = enteredEvent;
var afterTrade = beforeTrade.AcceptTradeHealthForGold();
Require(afterTrade.RunHealth == beforeTrade.RunHealth - EventCatalog.TradeHealthCost, "accepting the merchant trade spends run health");
Require(afterTrade.Gold == beforeTrade.Gold + EventCatalog.TradeGoldReward, "accepting the merchant trade grants gold");
Require(afterTrade.RoundEventResolved, "accepting the merchant trade resolves the event");
RequireThrows(() => afterTrade.AcceptTradeHealthForGold(), "a resolved event cannot be accepted again");
var advancedFromTrade = afterTrade.AdvanceRound();
Require(advancedFromTrade.Round == 5 && advancedFromTrade.Phase == RunPhase.Preparation && !advancedFromTrade.RoundEventResolved, "a resolved event advances into the next round's preparation");

// Declining leaves the run unchanged but resolved, and unsafe trades are rejected.
var declined = enteredEvent.DeclineEvent();
Require(declined.RoundEventResolved && declined.Gold == enteredEvent.Gold && declined.RunHealth == enteredEvent.RunHealth, "declining an event applies no outcome but resolves it");
RequireThrows(() => declined.DeclineEvent(), "a resolved event cannot be declined again");
var lowHealthEvent = (RunState.NewRun() with { Round = 4, RunHealth = EventCatalog.TradeHealthCost }).EnterEvent();
RequireThrows(() => lowHealthEvent.AcceptTradeHealthForGold(), "the merchant trade is refused when it would end the run");
RequireThrows(() => eventRunBase.AcceptTradeHealthForGold(), "events cannot be resolved before entering the encounter");

// Free hero with a curse (GDD "бесплатный герой с проклятием").
var cursedAccept = enteredEvent.AcceptCursedFreeHero();
Require(cursedAccept.Bench.Count == enteredEvent.Bench.Count + 1, "accepting the cursed gift adds a hero to the bench");
var cursedHero = cursedAccept.Bench[^1];
Require(cursedHero.Cursed && cursedHero.Stars == 1, "the gifted hero joins cursed at one star");
Require(cursedAccept.RoundEventResolved, "accepting the cursed gift resolves the event");
RequireThrows(() => cursedAccept.AcceptCursedFreeHero(), "the cursed gift cannot be taken twice");
var fullBenchEvent = (RunState.NewRun() with
{
    Round = 4,
    Bench = Enumerable.Range(0, EconomyConfig.Default.StartingBenchSize)
        .Select(i => new HeroInstance($"fill_{i}", "iron_guard", 1))
        .ToList()
}).EnterEvent();
RequireThrows(() => fullBenchEvent.AcceptCursedFreeHero(), "the cursed gift is refused when the bench is full");
// The curse weakens the hero's combat stats versus an identical uncursed hero.
var cursedPos = new TacticalPosition(2, 0);
var healthyUnit = BattleUnit.FromBoardHero(new BoardHero(new HeroInstance("h", cursedHero.HeroId, 1), cursedPos), TacticalSide.Player);
var cursedUnit = BattleUnit.FromBoardHero(new BoardHero(new HeroInstance("c", cursedHero.HeroId, 1, Cursed: true), cursedPos), TacticalSide.Player);
Require(Math.Abs(cursedUnit.MaxHealth - healthyUnit.MaxHealth * EventCatalog.CursedHeroStatMultiplier) < 1e-9, "a cursed hero enters combat with reduced health");
Require(Math.Abs(cursedUnit.Attack - healthyUnit.Attack * EventCatalog.CursedHeroStatMultiplier) < 1e-9, "a cursed hero enters combat with reduced attack");

// Empower one faction for the next battle (GDD "усиление одной фракции на следующий бой").
var factionEventRun = (RunState.NewRun() with
{
    Round = 4,
    Team = new List<BoardHero> { new(new HeroInstance("fb_ig", "iron_guard", 1), new TacticalPosition(2, 0)) }
}).EnterEvent();
var boosted = factionEventRun.AcceptFactionBoost("empire");
Require(boosted.PendingFactionBoost.IsActive && boosted.PendingFactionBoost.FactionName == FactionCatalog.Empire.Name, "accepting the blessing queues the chosen faction boost");
Require(boosted.PendingFactionBoost.FactionName == factionEventRun.AcceptFactionBoost("Империя").PendingFactionBoost.FactionName, "the blessing accepts the faction id or its display name");
Require(boosted.RoundEventResolved, "accepting the blessing resolves the event");
RequireThrows(() => factionEventRun.AcceptFactionBoost("spirit"), "the blessing rejects a faction the player does not field");
RequireThrows(() => factionEventRun.AcceptFactionBoost("unknown_faction"), "the blessing rejects an unknown faction");
Require(!RunState.NewRun().PendingFactionBoost.IsActive, "a run with no pending blessing carries the neutral boost");
// The pending boost feeds the next round autobattle and is consumed when it resolves.
var boostTeam = new List<BoardHero>
{
    new(new HeroInstance("bt_front", "iron_guard", 1), new TacticalPosition(2, 1)),
    new(new HeroInstance("bt_back", "oath_archer", 1), new TacticalPosition(3, 1))
};
var neutralRoundBattle = LevelCombatSimulator.ResolveRoundMatch(boostTeam, round2)!;
var blessedRoundBattle = LevelCombatSimulator.ResolveRoundMatch(boostTeam, round2, playerFactionBoost: new FactionBoost(FactionCatalog.Empire.Name, EventCatalog.FactionBoostStatMultiplier))!;
Require(blessedRoundBattle.PlayerDamageDealt > neutralRoundBattle.PlayerDamageDealt, "the faction blessing makes the blessed roster hit harder in the round autobattle");
var roundFiveAfterBoost = (RunState.NewRun() with
{
    Round = 5,
    Phase = RunPhase.Combat,
    PendingFactionBoostId = FactionCatalog.Empire.Name,
    Team = boostTeam,
    Combat = CombatState.Start(round2.CombatRuneSeed)
}).ClaimReward(7);
Require(!roundFiveAfterBoost.PendingFactionBoost.IsActive, "resolving the battle consumes the pending faction blessing");

// Sacrifice a hero for an artifact (GDD "удаление героя ради артефакта").
var sacrificeRun = (RunState.NewRun() with
{
    Round = 4,
    Bench = new List<HeroInstance> { new("sac_bench", "iron_guard", 1) },
    Team = new List<BoardHero> { new(new HeroInstance("sac_team", "oath_archer", 1), new TacticalPosition(2, 0)) }
}).EnterEvent();
var expectedRelic = ArtifactCatalog.OfferThree(sacrificeRun.CurrentRoundDefinition.CombatRuneSeed)[0];
var afterBenchSacrifice = sacrificeRun.AcceptSacrificeHeroForArtifact("sac_bench");
Require(afterBenchSacrifice.Bench.All(hero => hero.InstanceId != "sac_bench"), "sacrificing a bench hero removes it from the run");
Require(afterBenchSacrifice.Artifacts.Any(a => a.Id == expectedRelic.Id), "the sacrifice grants the round's deterministic artifact");
Require(afterBenchSacrifice.RoundEventResolved, "the sacrifice resolves the event");
var afterTeamSacrifice = sacrificeRun.AcceptSacrificeHeroForArtifact("sac_team");
Require(afterTeamSacrifice.Team.All(slot => slot.Hero.InstanceId != "sac_team"), "sacrificing a board hero removes it from the run");
Require(afterTeamSacrifice.Artifacts.Count == sacrificeRun.Artifacts.Count + 1, "the sacrifice adds exactly one relic");
RequireThrows(() => sacrificeRun.AcceptSacrificeHeroForArtifact("missing_hero"), "the sacrifice rejects an unknown hero");
RequireThrows(() => afterBenchSacrifice.AcceptSacrificeHeroForArtifact("sac_team"), "a resolved event cannot be sacrificed into again");

// First-run onboarding script (GDD "Обучение и onboarding"): one mechanic revealed per round.
Require(OnboardingScript.Steps.Count == 7, "the onboarding script covers tutorial rounds 1-7");
Require(OnboardingScript.Steps.Select(step => step.Round).SequenceEqual(Enumerable.Range(1, 7)), "onboarding steps fire on consecutive rounds 1-7 in order");
Require(OnboardingScript.Steps.Select(step => step.Mechanic).Distinct().Count() == 7, "each onboarding round reveals a distinct mechanic");
Require(OnboardingScript.Steps.All(step => step.DesignGoal == PveRunSchedule.GetRound(step.Round).DesignGoal), "each onboarding step mirrors its round's GDD design goal");
Require(OnboardingScript.ForRound(1)!.Mechanic == OnboardingMechanic.BuyAndPlaceHero, "round 1 teaches buying and placing a hero");
Require(OnboardingScript.ForRound(2)!.Mechanic == OnboardingMechanic.RedAndBlueRunes, "round 2 teaches red and blue runes");
Require(OnboardingScript.ForRound(3)!.Mechanic == OnboardingMechanic.TankAndPositioning, "round 3 teaches the tank and positioning");
Require(OnboardingScript.ForRound(4)!.Mechanic == OnboardingMechanic.RiskAndReward, "round 4 introduces risk and reward");
Require(OnboardingScript.ForRound(5)!.Mechanic == OnboardingMechanic.ShieldsAndHealing, "round 5 checks shields and healing");
Require(OnboardingScript.ForRound(6)!.Mechanic == OnboardingMechanic.BacklineThreat, "round 6 shows the backline threat");
Require(OnboardingScript.ForRound(7)!.Mechanic == OnboardingMechanic.MagicDamage, "round 7 teaches playing against magic damage");
Require(OnboardingScript.Steps.All(step => !string.IsNullOrWhiteSpace(step.Title) && !string.IsNullOrWhiteSpace(step.Hint)), "every onboarding step carries an interactive title and hint");
Require(OnboardingScript.IsTutorialRound(1) && !OnboardingScript.IsTutorialRound(8), "rounds 1-7 are tutorial rounds and round 8 is not");
Require(OnboardingScript.ForRound(8) is null && !OnboardingScript.TryGetForRound(9, out _), "rounds past the tutorial carry no onboarding step");
Require(OnboardingScript.RevealedBy(3).SequenceEqual(new[] { OnboardingMechanic.BuyAndPlaceHero, OnboardingMechanic.RedAndBlueRunes, OnboardingMechanic.TankAndPositioning }), "the onboarding reveals mechanics cumulatively in round order");
Require(OnboardingScript.ForRun(RunState.NewRun() with { Round = 2 })!.Mechanic == OnboardingMechanic.RedAndBlueRunes, "the onboarding step resolves from a run's current round");
RequireThrows(() => OnboardingScript.ForRun(null!), "the onboarding script rejects a null run");

// Synergy panel view-model (GDD UI screen "Панель синергий").
var synergyTeam = new List<BoardHero>
{
    new(new HeroInstance("panel_ig", "iron_guard", 1), new TacticalPosition(2, 0)),
    new(new HeroInstance("panel_bc", "bulwark_captain", 1), new TacticalPosition(2, 1))
};
var synergyPanel = SynergyPanelModel.Build(synergyTeam);
Require(synergyPanel.HasActiveSynergies, "two same-faction same-class heroes activate synergies on the panel");
var empireEntry = synergyPanel.ActiveFactions.Single(entry => entry.Id == "empire");
Require(empireEntry.Kind == SynergyKind.Faction && empireEntry.UnitCount == 2 && empireEntry.IsActive, "the panel reports the active Empire faction with two heroes");
Require(empireEntry.Strength == SynergyStrength.Active, "an active synergy with a higher tier left is coloured active");
Require(empireEntry.NextTier is not null && empireEntry.NextTier.RequiredCount == 4 && empireEntry.HeroesToNextTier == 2, "the panel reports the next Empire breakpoint and heroes still needed");
Require(empireEntry.NextTierHeroes.Contains("Лучница Присяги") && !empireEntry.NextTierHeroes.Contains("Железный Страж"), "the panel lists fielding-eligible heroes that close the next breakpoint");
var defenderEntry = synergyPanel.ActiveClasses.Single(entry => entry.Id == "defender");
Require(defenderEntry.Kind == SynergyKind.Class && defenderEntry.UnitCount == 2 && defenderEntry.NextTier is not null && defenderEntry.NextTier.RequiredCount == 4, "the panel reports the active Defender class and its next breakpoint");
Require(synergyPanel.UpcomingThresholds.Count >= 2 && synergyPanel.UpcomingThresholds.All(entry => entry.HasNextTier), "the panel lists upcoming breakpoints, each with a next tier");
Require(synergyPanel.UpcomingThresholds.SequenceEqual(synergyPanel.UpcomingThresholds.OrderBy(entry => entry.HeroesToNextTier)), "upcoming breakpoints are ordered by how close they are");

// Beginner highlight (GDD "Слишком сложный onboarding": "первые синергии должны быть очевидными").
Require(synergyPanel.BeginnerHighlight is not null && synergyPanel.BeginnerHighlight.IsActive, "the panel spotlights a started synergy for the player to pursue first");
Require(synergyPanel.BeginnerHint is not null && synergyPanel.BeginnerHint.Contains(synergyPanel.BeginnerHighlight!.Name), "the beginner hint names the highlighted synergy");
Require(synergyPanel.BeginnerHighlight!.UnitCount > 0, "the beginner highlight is always a synergy the team has already started");

var emptyPanel = SynergyPanelModel.Build(new List<BoardHero>());
Require(emptyPanel.Entries.Count == 0 && !emptyPanel.HasActiveSynergies, "an empty team has no synergies on the panel");
Require(emptyPanel.BeginnerHighlight is null && emptyPanel.BeginnerHint is null, "an empty team has no beginner synergy highlight");

var soloPanel = SynergyPanelModel.Build(new List<BoardHero>
{
    new(new HeroInstance("solo_ig", "iron_guard", 1), new TacticalPosition(2, 0))
});
Require(soloPanel.ActiveFactions.Count == 0 && soloPanel.Entries.Count == 2, "a lone hero shows building synergies but activates none");
var soloEmpire = soloPanel.Entries.Single(entry => entry.Id == "empire");
Require(soloEmpire.Strength == SynergyStrength.Building && soloEmpire.HeroesToNextTier == 1, "a single faction hero is one short of the first tier and coloured building");
Require(soloPanel.BeginnerHighlight is not null && soloPanel.BeginnerHighlight.HeroesToNextTier == 1, "even one placed hero gives the player an obvious first synergy to chase");

var synergyRunPanel = SynergyPanelModel.Build(RunState.NewRun() with { Team = synergyTeam });
Require(synergyRunPanel.HasActiveSynergies, "the synergy panel builds from a run's placed team");
RequireThrows(() => SynergyPanelModel.Build((RunState)null!), "the synergy panel rejects a null run");
RequireThrows(() => SynergyPanelModel.Build((IReadOnlyList<BoardHero>)null!), "the synergy panel rejects a null team");

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

static RuneEffect Effect(
    RuneEffectKind kind,
    double power,
    bool mass = false,
    int commanderEnergy = 0,
    int chainNumber = 1,
    RuneType rune = RuneType.Red,
    RuneMatchTier tier = RuneMatchTier.Match3,
    int matchedRunesCount = 3)
{
    return new RuneEffect(
        Rune: rune,
        Kind: kind,
        Tier: tier,
        MatchedRunesCount: matchedRunesCount,
        ChainNumber: chainNumber,
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
