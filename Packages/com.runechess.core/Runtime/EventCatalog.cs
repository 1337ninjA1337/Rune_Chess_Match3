using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Catalog of roguelite events for the event screen ("Экран события"). It defines
    /// every offered event as explicit data so the screen can render any of them and
    /// the run resolves each one straight from the offered option's deltas (see
    /// <see cref="EventOption"/> and <c>RunState.OfferedEvent</c>), guaranteeing that
    /// what the card shows is exactly what gets applied. The four GDD archetypes lead
    /// the pool; the remaining pure-economy events broaden roguelite variety. Balance
    /// magnitudes live here as named constants per the codex rule to keep tunable
    /// values out of logic.
    /// </summary>
    public static class EventCatalog
    {
        // Trade-health-for-gold balance (relic merchant, GDD round 4 "риск и награда").
        public const int TradeHealthCost = 2;
        public const int TradeGoldReward = 6;

        // Bold-trade balance: a riskier merchant variant of the same archetype — more run
        // health for more gold. Distinct numbers from the canonical trade prove the
        // resolution applies the offered option's own deltas, not a fixed catalog singleton.
        public const int BoldTradeHealthCost = 4;
        public const int BoldTradeGoldReward = 13;

        // Healing-spring balance: spend gold to restore run health, never above the run's
        // starting maximum (the inverse of the merchant trade).
        public const int HealingSpringGoldCost = 5;
        public const int HealingSpringHealthReward = 5;

        // Windfall balance: a free gold find, roughly one round of base income, with no cost.
        public const int WindfallGoldReward = 4;

        // Training-grounds balance: buy XP toward the next player level. Mirrors the shop's
        // <c>EconomyConfig.BuyXpCost</c>/<c>XpPerPurchase</c> so the event is variety, not power creep.
        public const int TrainingGoldCost = 4;
        public const int TrainingXpReward = 4;

        // Cursed-free-hero balance: the gifted hero fights at this fraction of its stats for
        // the rest of the run (GDD "бесплатный герой с проклятием").
        public const double CursedHeroStatMultiplier = 0.8;

        // Faction-blessing balance: blessed-faction allies fight the next battle at this
        // multiple of their health and attack (GDD "усиление одной фракции на следующий бой").
        public const double FactionBoostStatMultiplier = 1.25;

        /// <summary>Trade run health for gold (GDD "обмен здоровья на золото").</summary>
        public static EventOption TradeHealthForGold { get; } = new(
            Kind: EventChoiceKind.TradeHealthForGold,
            Id: "event_trade_health_for_gold",
            Title: "Сделка торговца",
            Description: $"Торговец предлагает {TradeGoldReward} золота в обмен на {TradeHealthCost} здоровья забега.",
            RiskLabel: $"-{TradeHealthCost} здоровья забега",
            RewardLabel: $"+{TradeGoldReward} золота",
            AcceptLabel: "Согласиться на сделку",
            HealthCost: TradeHealthCost,
            GoldReward: TradeGoldReward);

        /// <summary>Take a free hero that carries a curse (GDD "бесплатный герой с проклятием").</summary>
        public static EventOption CursedFreeHero { get; } = new(
            Kind: EventChoiceKind.CursedFreeHero,
            Id: "event_cursed_free_hero",
            Title: "Проклятый дар",
            Description: "Бесплатный герой присоединится к отряду, но несёт проклятие до конца забега.",
            RiskLabel: "Проклятие на герое",
            RewardLabel: "Бесплатный герой",
            AcceptLabel: "Принять дар",
            GrantsHero: true,
            AppliesCurse: true);

        /// <summary>Empower one faction for the next battle (GDD "усиление фракции на следующий бой").</summary>
        public static EventOption FactionBoost { get; } = new(
            Kind: EventChoiceKind.FactionBoost,
            Id: "event_faction_boost",
            Title: "Благословение фракции",
            Description: "Одна фракция вашего отряда получает усиление на следующий бой.",
            RiskLabel: "Только на один бой",
            RewardLabel: "Усиление фракции",
            AcceptLabel: "Призвать благословение");

        /// <summary>Sacrifice a hero in exchange for an artifact (GDD "удаление героя ради артефакта").</summary>
        public static EventOption SacrificeHeroForArtifact { get; } = new(
            Kind: EventChoiceKind.SacrificeHeroForArtifact,
            Id: "event_sacrifice_hero_for_artifact",
            Title: "Жертва ради реликвии",
            Description: "Уберите одного героя из отряда, чтобы получить артефакт.",
            RiskLabel: "-1 герой",
            RewardLabel: "Артефакт",
            AcceptLabel: "Принести жертву",
            RemovesHero: true,
            GrantsArtifact: true);

        /// <summary>Heal run health by spending gold (the inverse of the merchant trade).</summary>
        public static EventOption HealingSpring { get; } = new(
            Kind: EventChoiceKind.GoldForHealth,
            Id: "event_healing_spring",
            Title: "Целебный родник",
            Description: $"Заплатите {HealingSpringGoldCost} золота, чтобы восстановить {HealingSpringHealthReward} здоровья забега.",
            RiskLabel: $"-{HealingSpringGoldCost} золота",
            RewardLabel: $"+{HealingSpringHealthReward} здоровья забега",
            AcceptLabel: "Испить из родника",
            GoldCost: HealingSpringGoldCost,
            HealthReward: HealingSpringHealthReward);

        /// <summary>A free gold find with no cost (a lucky cache).</summary>
        public static EventOption GoldWindfall { get; } = new(
            Kind: EventChoiceKind.GoldWindfall,
            Id: "event_gold_windfall",
            Title: "Забытый тайник",
            Description: $"Вы находите забытый тайник с {WindfallGoldReward} золота.",
            RiskLabel: "Без риска",
            RewardLabel: $"+{WindfallGoldReward} золота",
            AcceptLabel: "Забрать золото",
            GoldReward: WindfallGoldReward);

        /// <summary>Spend gold to gain XP toward the next player level (war maneuvers).</summary>
        public static EventOption TrainingGrounds { get; } = new(
            Kind: EventChoiceKind.TrainingBoon,
            Id: "event_training_grounds",
            Title: "Военные манёвры",
            Description: $"Потратьте {TrainingGoldCost} золота на манёвры и получите {TrainingXpReward} опыта.",
            RiskLabel: $"-{TrainingGoldCost} золота",
            RewardLabel: $"+{TrainingXpReward} опыта",
            AcceptLabel: "Провести манёвры",
            GoldCost: TrainingGoldCost,
            XpReward: TrainingXpReward);

        /// <summary>A riskier merchant trade variant of the <see cref="EventChoiceKind.TradeHealthForGold"/> archetype.</summary>
        public static EventOption BoldTrade { get; } = new(
            Kind: EventChoiceKind.TradeHealthForGold,
            Id: "event_bold_trade",
            Title: "Дерзкая сделка",
            Description: $"Торговец предлагает {BoldTradeGoldReward} золота в обмен на {BoldTradeHealthCost} здоровья забега.",
            RiskLabel: $"-{BoldTradeHealthCost} здоровья забега",
            RewardLabel: $"+{BoldTradeGoldReward} золота",
            AcceptLabel: "Заключить дерзкую сделку",
            HealthCost: BoldTradeHealthCost,
            GoldReward: BoldTradeGoldReward);

        /// <summary>
        /// Every event in the pool. The four GDD archetypes lead so the single MVP event
        /// round (round 4, seed 1640) keeps offering the canonical merchant trade
        /// (1640 % 8 == 0), with the broader pool surfacing on other seeds and future
        /// event rounds.
        /// </summary>
        public static IReadOnlyList<EventOption> All { get; } = Array.AsReadOnly(new[]
        {
            TradeHealthForGold,
            CursedFreeHero,
            FactionBoost,
            SacrificeHeroForArtifact,
            HealingSpring,
            GoldWindfall,
            TrainingGrounds,
            BoldTrade
        });

        /// <summary>The canonical event for a given archetype.</summary>
        public static EventOption Get(EventChoiceKind kind) => kind switch
        {
            EventChoiceKind.TradeHealthForGold => TradeHealthForGold,
            EventChoiceKind.CursedFreeHero => CursedFreeHero,
            EventChoiceKind.FactionBoost => FactionBoost,
            EventChoiceKind.SacrificeHeroForArtifact => SacrificeHeroForArtifact,
            EventChoiceKind.GoldForHealth => HealingSpring,
            EventChoiceKind.GoldWindfall => GoldWindfall,
            EventChoiceKind.TrainingBoon => TrainingGrounds,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), "Unknown event choice kind.")
        };

        /// <summary>Look up an event option by id (case-insensitive).</summary>
        public static bool TryGet(string id, out EventOption option)
        {
            option = All.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))!;
            return option is not null;
        }
    }
}
