using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    public sealed record RunState(
        int Round,
        int RunHealth,
        int Gold,
        int Xp,
        int PlayerLevel,
        CommanderState Commander,
        IReadOnlyList<BoardHero> Team,
        IReadOnlyList<HeroInstance> Bench,
        ShopState Shop,
        IReadOnlyList<ArtifactState> Artifacts,
        RunPhase Phase,
        string NextEnemyId,
        CombatState? Combat,
        string? DefeatReason,
        bool RoundArtifactClaimed = false,
        bool RoundHeroClaimed = false,
        bool RoundEventResolved = false,
        string PendingFactionBoostId = ""
    )
    {
        private const int MergeCopiesRequired = HeroEconomy.CopiesPerStarUpgrade;
        private const int RuneArchonMatch4CombosPerBlueRune = 3;
        internal const int AlchemistChainReactionGoldBonus = 1;

        public PveRoundDefinition CurrentRoundDefinition => PveRunSchedule.GetRound(Round);

        /// <summary>Economy modifiers contributed by the artifacts this run currently owns.</summary>
        public ArtifactModifiers Modifiers => ArtifactModifiers.From(Artifacts);

        /// <summary>
        /// Rune-effect modifiers contributed by the rune artifacts this run owns. Passed
        /// into <see cref="BattleState.ApplyRuneEffects"/> so match-3 rune effects honour
        /// the run's artifacts alongside its synergies.
        /// </summary>
        public ArtifactRuneModifiers RuneModifiers => ArtifactRuneModifiers.From(Artifacts);

        /// <summary>
        /// Start-of-combat stat modifiers contributed by the combat artifacts this run
        /// owns. Passed into <see cref="BattleState.Create"/> so the player's units enter
        /// the autobattle already buffed by their artifacts.
        /// </summary>
        public ArtifactCombatModifiers CombatModifiers => ArtifactCombatModifiers.From(Artifacts);

        /// <summary>
        /// One-battle faction blessing pending from a <see cref="EventChoiceKind.FactionBoost"/>
        /// event, or <see cref="FactionBoost.None"/> when none is queued. Passed into the next
        /// round autobattle so the blessed faction fights stronger, then cleared when that
        /// battle resolves.
        /// </summary>
        public FactionBoost PendingFactionBoost => string.IsNullOrEmpty(PendingFactionBoostId)
            ? FactionBoost.None
            : new FactionBoost(PendingFactionBoostId, EventCatalog.FactionBoostStatMultiplier);

        /// <summary>True when a faction is empowered for the next battle by a resolved event.</summary>
        public bool HasPendingFactionBoost => !string.IsNullOrEmpty(PendingFactionBoostId);

        /// <summary>True when the current round is a roguelite event round.</summary>
        public bool IsEventRound => CurrentRoundDefinition.Type == PveRoundType.Event;

        public bool IsFinalRound => Round >= PveRunSchedule.FinalRound;
        public bool IsRunWon => Phase == RunPhase.Victory;
        public bool IsRunLost => Phase == RunPhase.Defeat;
        public bool IsRunComplete => Phase is RunPhase.Victory or RunPhase.Defeat;

        public static RunState NewRun(
            string commanderId,
            EconomyConfig? economy = null,
            ShopState? shop = null
        )
        {
            return NewRun(CommanderCatalog.Get(commanderId).CreateInitialState(), economy, shop);
        }

        public static RunState NewRun(
            CommanderState? commander = null,
            EconomyConfig? economy = null,
            ShopState? shop = null
        )
        {
            var config = economy ?? EconomyConfig.Default;
            var selectedCommander = commander ?? CommanderCatalog.Default.CreateInitialState();
            var selectedCommanderDefinition = CommanderCatalog.TryGet(selectedCommander.Id, out var knownCommander)
                ? knownCommander
                : null;
            var startingGold = config.StartingGold;
            var startingBench = new List<HeroInstance>();

            if (selectedCommanderDefinition is not null)
            {
                ApplyStartingBonus(selectedCommanderDefinition.StartingBonus, ref selectedCommander, ref startingGold, startingBench);
            }

            return new RunState(
                Round: 1,
                RunHealth: config.StartingRunHealth,
                Gold: startingGold,
                Xp: config.StartingXp,
                PlayerLevel: config.StartingPlayerLevel,
                Commander: selectedCommander,
                Team: new List<BoardHero>(),
                Bench: startingBench,
                Shop: shop ?? ShopState.ForPlayerLevel(config.StartingPlayerLevel, config),
                Artifacts: new List<ArtifactState>(),
                Phase: RunPhase.Preparation,
                NextEnemyId: PveRunSchedule.GetRound(PveRunSchedule.FirstRound).EnemyId,
                Combat: null,
                DefeatReason: null
            );
        }

        public RunState BuyHero(int offerIndex, EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;

            if (offerIndex < 0 || offerIndex >= Shop.Offers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offerIndex), "Shop offer index is outside the current shop.");
            }

            if (Bench.Count >= config.StartingBenchSize)
            {
                throw new InvalidOperationException("Bench is full.");
            }

            var offer = Shop.Offers[offerIndex];
            if (Gold < offer.Cost)
            {
                throw new InvalidOperationException("Not enough gold to buy this hero.");
            }

            var bench = Bench.ToList();
            bench.Add(new HeroInstance(CreateInstanceId(offer.HeroId), offer.HeroId, Stars: 1));

            var offers = Shop.Offers.ToList();
            offers.RemoveAt(offerIndex);

            return this with
            {
                Gold = Gold - offer.Cost,
                Bench = bench,
                Shop = Shop with { Offers = offers }
            };
        }

        private static void ApplyStartingBonus(
            CommanderStartingBonus bonus,
            ref CommanderState commander,
            ref int startingGold,
            List<HeroInstance> startingBench
        )
        {
            switch (bonus.Kind)
            {
                case CommanderStartingBonusKind.CommanderEnergy:
                    commander = commander.GainEnergy(bonus.Amount);
                    break;
                case CommanderStartingBonusKind.BenchHero:
                    startingBench.Add(new HeroInstance(
                        InstanceId: $"starting_{bonus.HeroId}_1",
                        HeroId: bonus.HeroId!,
                        Stars: 1));
                    break;
                case CommanderStartingBonusKind.Gold:
                    startingGold += bonus.Amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bonus), "Unknown commander starting bonus kind.");
            }
        }

        public RunState PlaceHeroFromBench(string instanceId, TacticalPosition position, EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;

            if (!position.IsInsideMvpField)
            {
                throw new InvalidOperationException("Tactical position is outside the MVP field.");
            }

            if (!position.IsPlayerSide)
            {
                throw new InvalidOperationException("Heroes can only be placed on the player side during preparation.");
            }

            if (Team.Any(slot => slot.Position == position))
            {
                throw new InvalidOperationException("That tactical cell is already occupied.");
            }

            if (Team.Count >= config.GetHeroLimitForLevel(PlayerLevel))
            {
                throw new InvalidOperationException("The number of heroes on the field is limited by player level.");
            }

            var bench = Bench.ToList();
            var hero = bench.FirstOrDefault(hero => hero.InstanceId == instanceId)
                ?? throw new InvalidOperationException("Hero instance is not on the bench.");

            bench.Remove(hero);

            var team = Team.ToList();
            team.Add(new BoardHero(hero, position));

            return this with
            {
                Bench = bench,
                Team = team
            };
        }

        public RunState MoveHeroToBench(string instanceId, EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;

            if (Bench.Count >= config.StartingBenchSize)
            {
                throw new InvalidOperationException("Bench is full.");
            }

            var team = Team.ToList();
            var boardHero = team.FirstOrDefault(hero => hero.Hero.InstanceId == instanceId)
                ?? throw new InvalidOperationException("Hero instance is not on the tactical board.");

            team.Remove(boardHero);

            var bench = Bench.ToList();
            bench.Add(boardHero.Hero);

            return this with
            {
                Team = team,
                Bench = bench
            };
        }

        public RunState MergeOneStarHeroes(string heroId, int copiesRequired = MergeCopiesRequired)
        {
            return MergeHeroes(heroId, sourceStars: 1, resultStars: 2, copiesRequired);
        }

        public RunState MergeTwoStarHeroes(string heroId)
        {
            return MergeHeroes(heroId, sourceStars: 2, resultStars: 3, copiesRequired: MergeCopiesRequired);
        }

        public RunState SellHero(string instanceId, int baseCost)
        {
            EnsurePreparationPhase();

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Hero instance id is required.", nameof(instanceId));
            }

            var bench = Bench.ToList();
            var benchIndex = bench.FindIndex(hero => hero.InstanceId == instanceId);
            if (benchIndex >= 0)
            {
                var hero = bench[benchIndex];
                var sellValue = HeroEconomy.CalculateSellValue(baseCost, hero.Stars);
                bench.RemoveAt(benchIndex);

                return this with
                {
                    Gold = Gold + sellValue,
                    Bench = bench
                };
            }

            var team = Team.ToList();
            var teamIndex = team.FindIndex(slot => slot.Hero.InstanceId == instanceId);
            if (teamIndex >= 0)
            {
                var hero = team[teamIndex].Hero;
                var sellValue = HeroEconomy.CalculateSellValue(baseCost, hero.Stars);
                team.RemoveAt(teamIndex);

                return this with
                {
                    Gold = Gold + sellValue,
                    Team = team
                };
            }

            throw new InvalidOperationException("Hero instance was not found on the bench or board.");
        }

        /// <summary>
        /// Gold cost of one experience purchase after the economy artifact discount
        /// ("Том Ученика"), never below zero.
        /// </summary>
        public int EffectiveBuyXpCost(EconomyConfig? economy = null)
        {
            var config = economy ?? EconomyConfig.Default;
            return Math.Max(0, config.BuyXpCost - Modifiers.BuyXpDiscount);
        }

        public RunState BuyXp(EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;
            var cost = EffectiveBuyXpCost(config);

            if (Gold < cost)
            {
                throw new InvalidOperationException("Not enough gold to buy XP.");
            }

            return this with
            {
                Gold = Gold - cost,
                Xp = Xp + config.XpPerPurchase
            };
        }

        public RunState LevelUp(int xpCost)
        {
            EnsurePreparationPhase();

            if (Xp < xpCost)
            {
                throw new InvalidOperationException("Not enough XP to level up.");
            }

            return this with
            {
                Xp = Xp - xpCost,
                PlayerLevel = PlayerLevel + 1
            };
        }

        public RunState LevelUp(EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;
            return LevelUp(config.GetXpCostForNextLevel(PlayerLevel));
        }

        public RunState RerollShop(IReadOnlyList<ShopOffer> nextOffers, EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;

            if (nextOffers.Count != config.GetShopSizeForLevel(PlayerLevel))
            {
                throw new ArgumentException("Reroll result must match the configured shop size for the player level.", nameof(nextOffers));
            }

            if (Gold < config.RerollCost)
            {
                throw new InvalidOperationException("Not enough gold to reroll the shop.");
            }

            return this with
            {
                Gold = Gold - config.RerollCost,
                Shop = new ShopState(nextOffers.ToList(), Shop.RerollsThisRound + 1)
            };
        }

        public RunState AddArtifact(ArtifactState artifact)
        {
            var artifacts = Artifacts.ToList();
            artifacts.Add(artifact);

            return this with { Artifacts = artifacts };
        }

        /// <summary>
        /// The three artifacts the current reward round offers, drawn deterministically
        /// from the round seed (GDD "Экран награды": выбор одного из трёх артефактов).
        /// Empty when the current round grants no artifact choice.
        /// </summary>
        public IReadOnlyList<RewardArtifactOption> RewardArtifactOptions()
        {
            var reward = CurrentRoundDefinition.RoundReward;
            if (!RoundOffersArtifactChoice(reward))
            {
                return Array.Empty<RewardArtifactOption>();
            }

            return ArtifactCatalog.OfferThree(CurrentRoundDefinition.CombatRuneSeed, rare: reward.RareArtifact);
        }

        /// <summary>
        /// Claim one of the three artifacts the reward screen offers for the current round
        /// (GDD "выбор одного из трёх артефактов после подходящих раундов"). The chosen id
        /// must be one of <see cref="RewardArtifactOptions"/>; only artifact-reward rounds
        /// allow a pick and only one artifact may be taken per round.
        /// </summary>
        public RunState ClaimRewardArtifact(string artifactId)
        {
            if (Phase != RunPhase.Reward)
            {
                throw new InvalidOperationException("Artifacts can only be chosen on the reward screen.");
            }

            if (string.IsNullOrWhiteSpace(artifactId))
            {
                throw new ArgumentException("Artifact id is required.", nameof(artifactId));
            }

            if (RoundArtifactClaimed)
            {
                throw new InvalidOperationException("An artifact has already been chosen for this round.");
            }

            var options = RewardArtifactOptions();
            if (options.Count == 0)
            {
                throw new InvalidOperationException("This round does not offer an artifact choice.");
            }

            var chosen = options.FirstOrDefault(option =>
                string.Equals(option.Id, artifactId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("The chosen artifact was not one of the offered choices.");

            return (this with { RoundArtifactClaimed = true }).AddArtifact(chosen.ToArtifactState());
        }

        private static bool RoundOffersArtifactChoice(PveRoundReward reward) =>
            reward.Artifact || reward.RareArtifact || reward.ArtifactOrGold;

        /// <summary>
        /// The hero choices the current reward round offers (GDD "награда героем"), drawn
        /// deterministically from the round seed. The 1-cost starter round draws Common heroes;
        /// the hero-choice rounds draw Common and Rare heroes. Empty when the round grants no hero.
        /// </summary>
        public IReadOnlyList<RewardHeroOption> RewardHeroOptions()
        {
            var reward = CurrentRoundDefinition.RoundReward;
            if (!reward.GrantsStarterHero && !reward.HeroChoice)
            {
                return Array.Empty<RewardHeroOption>();
            }

            var maxRarity = reward.GrantsStarterHero ? HeroRarity.Common : HeroRarity.Rare;
            return HeroCatalog.OfferRewardHeroes(CurrentRoundDefinition.CombatRuneSeed, maxRarity);
        }

        /// <summary>
        /// Claim the hero reward offered for the current round (GDD "награды героем после
        /// выбранных раундов"). The chosen id must be one of <see cref="RewardHeroOptions"/>;
        /// only hero-reward rounds allow a pick, only one hero may be taken per round and the
        /// bench must have a free slot. The hero joins the bench at one star.
        /// </summary>
        public RunState ClaimRewardHero(string heroId, EconomyConfig? economy = null)
        {
            if (Phase != RunPhase.Reward)
            {
                throw new InvalidOperationException("Hero rewards can only be chosen on the reward screen.");
            }

            if (string.IsNullOrWhiteSpace(heroId))
            {
                throw new ArgumentException("Hero id is required.", nameof(heroId));
            }

            if (RoundHeroClaimed)
            {
                throw new InvalidOperationException("A hero reward has already been chosen for this round.");
            }

            var options = RewardHeroOptions();
            if (options.Count == 0)
            {
                throw new InvalidOperationException("This round does not offer a hero reward.");
            }

            var chosen = options.FirstOrDefault(option =>
                string.Equals(option.Id, heroId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("The chosen hero was not one of the offered choices.");

            var config = economy ?? EconomyConfig.Default;
            if (Bench.Count >= config.StartingBenchSize)
            {
                throw new InvalidOperationException("Bench is full.");
            }

            var bench = Bench.ToList();
            bench.Add(new HeroInstance(CreateInstanceId(chosen.Id), chosen.Id, Stars: 1));

            return this with
            {
                Bench = bench,
                RoundHeroClaimed = true
            };
        }

        public RunState StartCombat(int? runeSeed = null, int? durationSeconds = null)
        {
            EnsurePreparationPhase();

            if (Team.Count == 0)
            {
                throw new InvalidOperationException("At least one hero must be placed before combat starts.");
            }

            return this with
            {
                Phase = RunPhase.Combat,
                Combat = CombatState.Start(
                    runeSeed ?? CurrentRoundDefinition.CombatRuneSeed,
                    durationSeconds ?? CombatState.DefaultDurationSeconds
                )
            };
        }

        public RunState SwapRunes(BoardPoint a, BoardPoint b, int comboDepth = 0)
        {
            if (Phase != RunPhase.Combat)
            {
                throw new InvalidOperationException("Runes can only be swapped during combat.");
            }

            if (Combat is null)
            {
                throw new InvalidOperationException("Combat rune board has not been initialized.");
            }

            var nextCombat = Combat.SwapRunes(a, b, comboDepth);
            var nextCommander = Commander.GainEnergy(nextCombat.LastCommanderEnergyGain);
            if (nextCommander.Id == CommanderCatalog.RuneArchon.Id && nextCombat.LastMatch4ComboCount > 0)
            {
                nextCommander = nextCommander.AddMatch4Combos(
                    nextCombat.LastMatch4ComboCount,
                    RuneArchonMatch4CombosPerBlueRune,
                    out var blueRuneTriggers);

                if (blueRuneTriggers > 0)
                {
                    nextCombat = nextCombat.AddBonusBlueRunes(blueRuneTriggers);
                }
            }

            return this with
            {
                Combat = nextCombat,
                Commander = nextCommander
            };
        }

        public RunState ResolveCombatTick(
            int elapsedSeconds,
            bool allEnemiesDefeated = false,
            bool allAlliesDefeated = false,
            int playerHealthPercent = 100,
            int enemyHealthPercent = 100,
            int? goldReward = null
        )
        {
            if (Phase != RunPhase.Combat)
            {
                throw new InvalidOperationException("Combat ticks can only be resolved during combat.");
            }

            if (Combat is null)
            {
                throw new InvalidOperationException("Combat state has not been initialized.");
            }

            ValidateHealthPercent(playerHealthPercent, nameof(playerHealthPercent));
            ValidateHealthPercent(enemyHealthPercent, nameof(enemyHealthPercent));

            var updatedRun = this with { Combat = Combat.AdvanceTimer(elapsedSeconds) };

            if (allAlliesDefeated)
            {
                return updatedRun.ResolveCombatDefeat("all_allies_defeated");
            }

            if (allEnemiesDefeated)
            {
                return updatedRun.ClaimReward(goldReward);
            }

            if (updatedRun.Combat?.IsTimerExpired != true)
            {
                return updatedRun;
            }

            return enemyHealthPercent > playerHealthPercent
                ? updatedRun.ResolveCombatDefeat("timer_enemy_health_advantage")
                : updatedRun.ClaimReward(goldReward);
        }

        public RunState ApplyRunDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Run damage cannot be negative.");
            }

            var minimumHealth = CurrentRoundDefinition.PreventsRunDefeat ? 1 : 0;
            var nextHealth = Math.Max(minimumHealth, RunHealth - damage);
            return this with
            {
                RunHealth = nextHealth,
                Phase = nextHealth == 0 ? RunPhase.Defeat : Phase,
                Combat = nextHealth == 0 ? null : Combat,
                DefeatReason = nextHealth == 0 ? "run_health_depleted" : DefeatReason
            };
        }

        public RunState ResolveCombatDefeat(string defeatReason = "combat_condition_failed")
        {
            if (Phase != RunPhase.Combat)
            {
                throw new InvalidOperationException("Combat defeat can only be resolved during combat.");
            }

            if (string.IsNullOrWhiteSpace(defeatReason))
            {
                throw new ArgumentException("Defeat reason is required.", nameof(defeatReason));
            }

            if (CurrentRoundDefinition.PreventsRunDefeat)
            {
                return this with
                {
                    Phase = RunPhase.Reward,
                    Combat = null,
                    DefeatReason = null,
                    RoundArtifactClaimed = false,
                    RoundHeroClaimed = false,
                    PendingFactionBoostId = ""
                };
            }

            return this with
            {
                Phase = RunPhase.Defeat,
                Combat = null,
                DefeatReason = defeatReason,
                PendingFactionBoostId = ""
            };
        }

        /// <summary>
        /// The aggregate reward (gold breakdown and offered choices) the current round would pay
        /// if its combat resolved now (GDD "итоговый расчёт наград за раунд"). Reads the live
        /// combat state for chain bonuses, so call while still in the combat phase.
        /// </summary>
        public RoundRewardBreakdown RoundReward(int? goldReward = null) =>
            RoundRewardBreakdown.ForCombatResolution(this, goldReward);

        public RunState ClaimReward(int? goldReward = null)
        {
            if (Phase != RunPhase.Combat)
            {
                throw new InvalidOperationException("Rewards can only be claimed after combat resolution.");
            }

            // The full round payout — base gold plus chain/alchemist/artifact bonuses — is
            // computed in one place so the gold credited here matches the reward screen exactly.
            var rewardBreakdown = RoundRewardBreakdown.ForCombatResolution(this, goldReward);

            return this with
            {
                Gold = Gold + rewardBreakdown.TotalGold,
                Phase = IsFinalRound ? RunPhase.Victory : RunPhase.Reward,
                Combat = null,
                DefeatReason = null,
                RoundArtifactClaimed = false,
                RoundHeroClaimed = false,
                PendingFactionBoostId = ""
            };
        }

        public RunState AdvanceRound(ShopState? nextShop = null)
        {
            var nextRound = PveRunSchedule.GetRound(Round + 1);
            return AdvanceRound(nextRound.EnemyId, nextShop);
        }

        public RunState AdvanceRound(string nextEnemyId, ShopState? nextShop = null)
        {
            if (Phase != RunPhase.Reward && Phase != RunPhase.Event)
            {
                throw new InvalidOperationException("Rounds can only advance from reward or event phases.");
            }

            if (Phase == RunPhase.Event && !RoundEventResolved)
            {
                throw new InvalidOperationException("The event must be accepted or declined before advancing.");
            }

            return this with
            {
                Round = Round + 1,
                Phase = RunPhase.Preparation,
                Shop = nextShop ?? ShopState.ForPlayerLevel(PlayerLevel),
                NextEnemyId = nextEnemyId,
                Combat = null,
                DefeatReason = null,
                RoundEventResolved = false
            };
        }

        /// <summary>
        /// Enter the current round's event encounter (GDD "Экран события"). Only no-combat
        /// event rounds offer an encounter, and only from preparation. Transitions the run
        /// into the event phase so the player can accept or decline the offered event.
        /// </summary>
        public RunState EnterEvent()
        {
            EnsurePreparationPhase();

            if (CurrentRoundDefinition.Type != PveRoundType.Event)
            {
                throw new InvalidOperationException("Only event rounds offer an event encounter.");
            }

            return this with { Phase = RunPhase.Event, RoundEventResolved = false };
        }

        /// <summary>The event archetype offered by the current event round, drawn deterministically from the round seed.</summary>
        public EventOption OfferedEvent => EventScreenModel.Build(this).Choice;

        /// <summary>
        /// Decline the offered event (GDD "Отказаться") and leave it resolved so the run can
        /// advance. Declining applies no outcome; the player simply moves on.
        /// </summary>
        public RunState DeclineEvent()
        {
            EnsureUnresolvedEvent();
            return this with { RoundEventResolved = true };
        }

        /// <summary>
        /// Accept the relic-merchant trade (GDD "обмен здоровья на золото"): pay run health to
        /// gain gold using the balance numbers on <see cref="EventCatalog.TradeHealthForGold"/>.
        /// The trade is only allowed while it keeps the run alive (run health stays at least 1).
        /// </summary>
        public RunState AcceptTradeHealthForGold()
        {
            EnsureUnresolvedEvent();

            var option = EventCatalog.TradeHealthForGold;
            if (RunHealth <= option.HealthCost)
            {
                throw new InvalidOperationException("Not enough run health to safely make this trade.");
            }

            return this with
            {
                RunHealth = RunHealth - option.HealthCost,
                Gold = Gold + option.GoldReward,
                RoundEventResolved = true
            };
        }

        /// <summary>
        /// Accept the cursed gift (GDD "бесплатный герой с проклятием"): a free Common hero,
        /// picked deterministically from the round seed, joins the bench at one star carrying a
        /// curse that weakens it in combat for the rest of the run. The bench must have a free slot.
        /// </summary>
        public RunState AcceptCursedFreeHero(EconomyConfig? economy = null)
        {
            EnsureUnresolvedEvent();

            var config = economy ?? EconomyConfig.Default;
            if (Bench.Count >= config.StartingBenchSize)
            {
                throw new InvalidOperationException("Bench is full.");
            }

            var gift = HeroCatalog.OfferRewardHeroes(CurrentRoundDefinition.CombatRuneSeed, HeroRarity.Common, count: 1)[0];
            var bench = Bench.ToList();
            bench.Add(new HeroInstance(CreateInstanceId(gift.Id), gift.Id, Stars: 1, Cursed: true));

            return this with
            {
                Bench = bench,
                RoundEventResolved = true
            };
        }

        /// <summary>
        /// Accept the faction blessing (GDD "усиление одной фракции на следующий бой"): the
        /// chosen faction — which must be one the player currently fields — fights the next
        /// battle stronger. The blessing is recorded on the run and consumed when that battle
        /// resolves. The faction may be named by its catalog id ("empire") or display name.
        /// </summary>
        public RunState AcceptFactionBoost(string factionId)
        {
            EnsureUnresolvedEvent();

            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("Faction id is required.", nameof(factionId));
            }

            var trimmed = factionId.Trim();
            var faction = FactionCatalog.All.FirstOrDefault(entry =>
                string.Equals(entry.Id, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Unknown faction '{factionId}'.", nameof(factionId));

            var fieldsFaction = Team
                .Concat(Bench.Select(hero => new BoardHero(hero, default)))
                .Any(boardHero => string.Equals(
                    HeroCatalog.Get(boardHero.Hero.HeroId).Faction,
                    faction.Name,
                    StringComparison.OrdinalIgnoreCase));
            if (!fieldsFaction)
            {
                throw new InvalidOperationException("The blessing can only empower a faction the player fields.");
            }

            return this with
            {
                PendingFactionBoostId = faction.Name,
                RoundEventResolved = true
            };
        }

        /// <summary>
        /// Accept the relic sacrifice (GDD "удаление героя ради артефакта"): remove the chosen
        /// hero — from the bench or the board — and gain an artifact picked deterministically
        /// from the round seed. The hero is gone from the run; the artifact joins the run's relics.
        /// </summary>
        public RunState AcceptSacrificeHeroForArtifact(string instanceId)
        {
            EnsureUnresolvedEvent();

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Hero instance id is required.", nameof(instanceId));
            }

            var bench = Bench.ToList();
            var team = Team.ToList();
            var benchIndex = bench.FindIndex(hero => hero.InstanceId == instanceId);
            if (benchIndex >= 0)
            {
                bench.RemoveAt(benchIndex);
            }
            else
            {
                var teamIndex = team.FindIndex(slot => slot.Hero.InstanceId == instanceId);
                if (teamIndex < 0)
                {
                    throw new InvalidOperationException("Hero instance was not found on the bench or board.");
                }

                team.RemoveAt(teamIndex);
            }

            var relic = ArtifactCatalog.OfferThree(CurrentRoundDefinition.CombatRuneSeed)[0];

            return (this with
            {
                Bench = bench,
                Team = team,
                RoundEventResolved = true
            }).AddArtifact(relic.ToArtifactState());
        }

        /// <summary>Guards an event resolution: the run must be on an unresolved event encounter.</summary>
        private void EnsureUnresolvedEvent()
        {
            if (Phase != RunPhase.Event)
            {
                throw new InvalidOperationException("Events can only be resolved during an event encounter.");
            }

            if (RoundEventResolved)
            {
                throw new InvalidOperationException("This event has already been resolved.");
            }
        }

        private void EnsurePreparationPhase()
        {
            if (Phase != RunPhase.Preparation)
            {
                throw new InvalidOperationException("This action is only available during preparation.");
            }
        }

        private RunState MergeHeroes(string heroId, int sourceStars, int resultStars, int copiesRequired)
        {
            EnsurePreparationPhase();

            if (string.IsNullOrWhiteSpace(heroId))
            {
                throw new ArgumentException("Hero id is required.", nameof(heroId));
            }

            if (copiesRequired is < 2 or > MergeCopiesRequired)
            {
                throw new ArgumentOutOfRangeException(nameof(copiesRequired), "Hero merge copy count must be between two and three.");
            }

            var bench = Bench.ToList();
            var team = Team.ToList();
            var benchIndices = Enumerable.Range(0, bench.Count)
                .Where(index => IsMergeCandidate(bench[index], heroId, sourceStars))
                .ToList();
            var teamIndices = Enumerable.Range(0, team.Count)
                .Where(index => IsMergeCandidate(team[index].Hero, heroId, sourceStars))
                .ToList();

            if (benchIndices.Count + teamIndices.Count < copiesRequired)
            {
                throw new InvalidOperationException($"{copiesRequired} matching {sourceStars}-star copies are required to merge.");
            }

            if (teamIndices.Count > 0)
            {
                var survivorTeamIndex = teamIndices[0];
                var benchIndicesToRemove = new List<int>();
                var teamIndicesToRemove = new List<int>();
                var consumedCopies = 1;

                foreach (var index in benchIndices)
                {
                    if (consumedCopies >= copiesRequired)
                    {
                        break;
                    }

                    benchIndicesToRemove.Add(index);
                    consumedCopies += 1;
                }

                foreach (var index in teamIndices.Skip(1))
                {
                    if (consumedCopies >= copiesRequired)
                    {
                        break;
                    }

                    teamIndicesToRemove.Add(index);
                    consumedCopies += 1;
                }

                team[survivorTeamIndex] = team[survivorTeamIndex] with
                {
                    Hero = team[survivorTeamIndex].Hero with { Stars = resultStars }
                };

                RemoveAtDescending(bench, benchIndicesToRemove);
                RemoveAtDescending(team, teamIndicesToRemove);

                return this with
                {
                    Bench = bench,
                    Team = team
                };
            }

            var selectedBenchIndices = benchIndices.Take(copiesRequired).ToList();
            var survivorBenchIndex = selectedBenchIndices[0];
            bench[survivorBenchIndex] = bench[survivorBenchIndex] with { Stars = resultStars };
            RemoveAtDescending(bench, selectedBenchIndices.Skip(1));

            return this with { Bench = bench };
        }

        private static void ValidateHealthPercent(int value, string paramName)
        {
            if (value is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(paramName, "Health percent must be between 0 and 100.");
            }
        }

        private static bool IsMergeCandidate(HeroInstance hero, string heroId, int stars)
        {
            return hero.HeroId == heroId && hero.Stars == stars;
        }

        private static void RemoveAtDescending<T>(List<T> items, IEnumerable<int> indices)
        {
            foreach (var index in indices.OrderByDescending(index => index))
            {
                items.RemoveAt(index);
            }
        }

        private string CreateInstanceId(string heroId)
        {
            var nextNumber = Bench.Count + Team.Count + 1;
            return $"{heroId}_{Round}_{nextNumber}";
        }
    }
}
