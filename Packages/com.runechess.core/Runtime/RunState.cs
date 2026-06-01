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
        string? DefeatReason
    )
    {
        private const int MergeCopiesRequired = 3;

        public PveRoundDefinition CurrentRoundDefinition => PveRunSchedule.GetRound(Round);
        public bool IsFinalRound => Round >= PveRunSchedule.FinalRound;
        public bool IsRunWon => Phase == RunPhase.Victory;
        public bool IsRunLost => Phase == RunPhase.Defeat;
        public bool IsRunComplete => Phase is RunPhase.Victory or RunPhase.Defeat;

        public static RunState NewRun(
            CommanderState? commander = null,
            EconomyConfig? economy = null,
            ShopState? shop = null
        )
        {
            var config = economy ?? EconomyConfig.Default;

            return new RunState(
                Round: 1,
                RunHealth: config.StartingRunHealth,
                Gold: config.StartingGold,
                Xp: config.StartingXp,
                PlayerLevel: config.StartingPlayerLevel,
                Commander: commander ?? CommanderState.StoneOath,
                Team: new List<BoardHero>(),
                Bench: new List<HeroInstance>(),
                Shop: shop ?? ShopState.StartingShop,
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

        public RunState PlaceHeroFromBench(string instanceId, TacticalPosition position)
        {
            EnsurePreparationPhase();

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

            if (Team.Count >= PlayerLevel)
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

        public RunState MergeOneStarHeroes(string heroId)
        {
            return MergeHeroes(heroId, sourceStars: 1, resultStars: 2);
        }

        public RunState BuyXp(EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;

            if (Gold < config.BuyXpCost)
            {
                throw new InvalidOperationException("Not enough gold to buy XP.");
            }

            return this with
            {
                Gold = Gold - config.BuyXpCost,
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

        public RunState RerollShop(IReadOnlyList<ShopOffer> nextOffers, EconomyConfig? economy = null)
        {
            EnsurePreparationPhase();
            var config = economy ?? EconomyConfig.Default;

            if (nextOffers.Count != config.StartingShopSize)
            {
                throw new ArgumentException("Reroll result must match the configured shop size.", nameof(nextOffers));
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

            return this with { Combat = Combat.SwapRunes(a, b, comboDepth) };
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

            var nextHealth = Math.Max(0, RunHealth - damage);
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

            return this with
            {
                Phase = RunPhase.Defeat,
                Combat = null,
                DefeatReason = defeatReason
            };
        }

        public RunState ClaimReward(int? goldReward = null)
        {
            if (Phase != RunPhase.Combat)
            {
                throw new InvalidOperationException("Rewards can only be claimed after combat resolution.");
            }

            var resolvedGoldReward = goldReward ?? CurrentRoundDefinition.BaseGoldReward;
            var chainGoldBonus = Combat?.EarnedChainFourGoldBonus == true
                ? CombatState.ChainFourGoldBonus
                : 0;
            if (resolvedGoldReward < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goldReward), "Gold reward cannot be negative.");
            }

            return this with
            {
                Gold = Gold + resolvedGoldReward + chainGoldBonus,
                Phase = IsFinalRound ? RunPhase.Victory : RunPhase.Reward,
                Combat = null,
                DefeatReason = null
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

            return this with
            {
                Round = Round + 1,
                Phase = RunPhase.Preparation,
                Shop = nextShop ?? ShopState.StartingShop,
                NextEnemyId = nextEnemyId,
                Combat = null,
                DefeatReason = null
            };
        }

        private void EnsurePreparationPhase()
        {
            if (Phase != RunPhase.Preparation)
            {
                throw new InvalidOperationException("This action is only available during preparation.");
            }
        }

        private RunState MergeHeroes(string heroId, int sourceStars, int resultStars)
        {
            EnsurePreparationPhase();

            if (string.IsNullOrWhiteSpace(heroId))
            {
                throw new ArgumentException("Hero id is required.", nameof(heroId));
            }

            var bench = Bench.ToList();
            var team = Team.ToList();
            var benchIndices = Enumerable.Range(0, bench.Count)
                .Where(index => IsMergeCandidate(bench[index], heroId, sourceStars))
                .ToList();
            var teamIndices = Enumerable.Range(0, team.Count)
                .Where(index => IsMergeCandidate(team[index].Hero, heroId, sourceStars))
                .ToList();

            if (benchIndices.Count + teamIndices.Count < MergeCopiesRequired)
            {
                throw new InvalidOperationException($"Three matching {sourceStars}-star copies are required to merge.");
            }

            if (teamIndices.Count > 0)
            {
                var survivorTeamIndex = teamIndices[0];
                var benchIndicesToRemove = new List<int>();
                var teamIndicesToRemove = new List<int>();
                var consumedCopies = 1;

                foreach (var index in benchIndices)
                {
                    if (consumedCopies >= MergeCopiesRequired)
                    {
                        break;
                    }

                    benchIndicesToRemove.Add(index);
                    consumedCopies += 1;
                }

                foreach (var index in teamIndices.Skip(1))
                {
                    if (consumedCopies >= MergeCopiesRequired)
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

            var selectedBenchIndices = benchIndices.Take(MergeCopiesRequired).ToList();
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
