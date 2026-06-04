using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// One bench hero row on the preparation screen (GDD UI screen 3 "Экран подготовки",
    /// element "скамейка героев"). Carries the data the Unity layer needs to draw a bench
    /// card and dispatch deploy/sell taps, plus the gold the hero would sell for.
    /// </summary>
    public sealed record PreparationBenchSlot(
        string InstanceId,
        string HeroId,
        string Name,
        HeroRarity Rarity,
        int Cost,
        int Stars,
        RuneType RuneAffinity,
        HeroRole Role,
        int SellValue,
        bool IsSelected);

    /// <summary>
    /// One shop offer row on the preparation screen (element "магазин героев"). It mirrors
    /// a <see cref="ShopOffer"/> enriched with catalog data and the affordability flags the
    /// buy button needs, so the presentation layer never touches <see cref="HeroCatalog"/>
    /// or the gold/bench rules directly.
    /// </summary>
    public sealed record PreparationShopOffer(
        int OfferIndex,
        string OfferId,
        string HeroId,
        string Name,
        HeroRarity Rarity,
        int Cost,
        RuneType RuneAffinity,
        HeroRole Role,
        bool CanAfford,
        bool BenchHasSpace)
    {
        /// <summary>True when the offer can actually be bought right now (gold and bench space).</summary>
        public bool CanBuy => CanAfford && BenchHasSpace;
    }

    /// <summary>
    /// One enemy in the "предпросмотр следующего врага" panel: the data-driven roster unit
    /// the current round will field, resolved to a display name and its board placement.
    /// </summary>
    public sealed record PreparationEnemyUnit(
        string HeroId,
        string Name,
        int Stars,
        TacticalPosition Position,
        bool IsFrontline);

    /// <summary>
    /// Aggregate view-model for the round preparation screen (GDD UI screen 3
    /// "Экран подготовки к раунду"). It gathers every element the screen shows: the
    /// player tactical field, the bench, the shop, gold and player level, the buy-XP and
    /// reroll actions, the active synergy indicators, the next-enemy preview and the
    /// start-battle button.
    ///
    /// All the rules (placement highlighting, affordability, synergy counts, enemy roster)
    /// live in core so the Unity MonoBehaviour only renders these fields and dispatches
    /// taps back into <see cref="RunState"/>. Keeping it pure also lets the whole screen be
    /// smoke-tested without the editor.
    /// </summary>
    public sealed record PreparationScreenModel(
        int Round,
        PveRoundType RoundType,
        string EnemyName,
        string DesignGoal,
        bool HasCombat,
        int Gold,
        int PlayerLevel,
        int MaxPlayerLevel,
        int Xp,
        int XpForNextLevel,
        bool IsMaxLevel,
        int HeroLimit,
        TacticalPlacementModel Placement,
        IReadOnlyList<PreparationBenchSlot> Bench,
        int BenchCapacity,
        IReadOnlyList<PreparationShopOffer> Shop,
        IReadOnlyList<SynergyProgress> ActiveSynergies,
        IReadOnlyList<PreparationEnemyUnit> EnemyPreview,
        int RerollCost,
        bool CanReroll,
        int BuyXpCost,
        int XpPerPurchase,
        bool CanBuyXp,
        bool CanStartBattle)
    {
        /// <summary>Number of heroes already deployed onto the player half.</summary>
        public int PlacedHeroCount => Placement.PlacedHeroCount;

        /// <summary>True when the bench has no free slot left for new heroes.</summary>
        public bool BenchIsFull => Bench.Count >= BenchCapacity;

        /// <summary>"Ур. N" badge for the player-level indicator.</summary>
        public string PlayerLevelLabel => $"Ур. {PlayerLevel}";

        /// <summary>"5 золота" style label for the gold indicator.</summary>
        public string GoldLabel => $"{Gold} золота";

        /// <summary>XP progress label, e.g. "2 / 4 XP" or "MAX" at the level cap.</summary>
        public string XpLabel => IsMaxLevel ? "MAX" : $"{Xp} / {XpForNextLevel} XP";

        /// <summary>Reroll button label with its current cost.</summary>
        public string RerollLabel => $"Reroll ({RerollCost})";

        /// <summary>Buy-XP button label with its current cost.</summary>
        public string BuyXpLabel => $"Купить опыт ({BuyXpCost})";

        /// <summary>Start-battle button label.</summary>
        public string StartBattleLabel => "В бой";

        /// <summary>"N / N героев" label for the field-limit indicator.</summary>
        public string FieldLimitLabel => $"{PlacedHeroCount} / {HeroLimit} героев";

        /// <summary>
        /// Build the preparation-screen model from the live run. When
        /// <paramref name="selectedBenchInstanceId"/> names a hero on the bench, that bench
        /// row is marked selected and the matching tactical cells light up as drop targets.
        /// </summary>
        public static PreparationScreenModel Build(
            RunState run,
            string? selectedBenchInstanceId = null,
            EconomyConfig? economy = null)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var config = economy ?? EconomyConfig.Default;
            var placement = TacticalPlacementModel.Build(run, selectedBenchInstanceId, config);
            // Re-read the normalized selection so a stale id (e.g. an already-placed hero)
            // does not mark a bench row selected when it no longer highlights the board.
            var activeSelection = placement.SelectedBenchInstanceId;
            var roundDefinition = run.CurrentRoundDefinition;
            var heroLimit = config.GetHeroLimitForLevel(run.PlayerLevel);
            var benchHasSpace = run.Bench.Count < config.StartingBenchSize;

            var bench = new List<PreparationBenchSlot>(run.Bench.Count);
            foreach (var instance in run.Bench)
            {
                var definition = HeroCatalog.Get(instance.HeroId);
                bench.Add(new PreparationBenchSlot(
                    instance.InstanceId,
                    instance.HeroId,
                    definition.Name,
                    definition.Rarity,
                    definition.Cost,
                    instance.Stars,
                    definition.RuneAffinity,
                    definition.Role,
                    HeroEconomy.CalculateSellValue(definition.Cost, instance.Stars),
                    instance.InstanceId == activeSelection));
            }

            var shop = new List<PreparationShopOffer>(run.Shop.Offers.Count);
            for (var index = 0; index < run.Shop.Offers.Count; index += 1)
            {
                var offer = run.Shop.Offers[index];
                var definition = HeroCatalog.Get(offer.HeroId);
                shop.Add(new PreparationShopOffer(
                    index,
                    offer.OfferId,
                    offer.HeroId,
                    definition.Name,
                    definition.Rarity,
                    offer.Cost,
                    definition.RuneAffinity,
                    definition.Role,
                    run.Gold >= offer.Cost,
                    benchHasSpace));
            }

            var enemyPreview = new List<PreparationEnemyUnit>(roundDefinition.EnemyUnits.Count);
            foreach (var enemy in roundDefinition.EnemyUnits)
            {
                var definition = HeroCatalog.Get(enemy.HeroId);
                enemyPreview.Add(new PreparationEnemyUnit(
                    enemy.HeroId,
                    definition.Name,
                    enemy.Stars,
                    enemy.Position,
                    enemy.Position.IsFrontline));
            }

            var isMaxLevel = run.PlayerLevel >= config.MaxPlayerLevel;
            var xpForNextLevel = isMaxLevel ? 0 : config.GetXpCostForNextLevel(run.PlayerLevel);
            var inPreparation = run.Phase == RunPhase.Preparation;

            return new PreparationScreenModel(
                Round: run.Round,
                RoundType: roundDefinition.Type,
                EnemyName: roundDefinition.EnemyName,
                DesignGoal: roundDefinition.DesignGoal,
                HasCombat: roundDefinition.HasCombat,
                Gold: run.Gold,
                PlayerLevel: run.PlayerLevel,
                MaxPlayerLevel: config.MaxPlayerLevel,
                Xp: run.Xp,
                XpForNextLevel: xpForNextLevel,
                IsMaxLevel: isMaxLevel,
                HeroLimit: heroLimit,
                Placement: placement,
                Bench: bench,
                BenchCapacity: config.StartingBenchSize,
                Shop: shop,
                ActiveSynergies: SynergyCalculator.ActiveSynergies(run.Team),
                EnemyPreview: enemyPreview,
                RerollCost: config.RerollCost,
                CanReroll: inPreparation && run.Gold >= config.RerollCost,
                BuyXpCost: config.BuyXpCost,
                XpPerPurchase: config.XpPerPurchase,
                CanBuyXp: inPreparation && run.Gold >= config.BuyXpCost,
                CanStartBattle: inPreparation && run.Team.Count > 0);
        }
    }
}
