using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// MVP catalog of roguelite events for the event screen ("Экран события").
    /// It defines the four GDD event archetypes as explicit data so the screen can
    /// render any of them and the later P1 event-mechanics task can apply the
    /// outcomes from the same numbers. Balance magnitudes live here as named
    /// constants per the codex rule to keep tunable values out of logic.
    /// </summary>
    public static class EventCatalog
    {
        // Trade-health-for-gold balance (relic merchant, GDD round 4 "риск и награда").
        public const int TradeHealthCost = 2;
        public const int TradeGoldReward = 6;

        // Cursed-free-hero balance: the gifted hero fights at this fraction of its stats for
        // the rest of the run (GDD "бесплатный герой с проклятием").
        public const double CursedHeroStatMultiplier = 0.8;

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

        /// <summary>Every MVP event, in the GDD listing order.</summary>
        public static IReadOnlyList<EventOption> All { get; } = Array.AsReadOnly(new[]
        {
            TradeHealthForGold,
            CursedFreeHero,
            FactionBoost,
            SacrificeHeroForArtifact
        });

        /// <summary>The canonical event for a given archetype.</summary>
        public static EventOption Get(EventChoiceKind kind) => kind switch
        {
            EventChoiceKind.TradeHealthForGold => TradeHealthForGold,
            EventChoiceKind.CursedFreeHero => CursedFreeHero,
            EventChoiceKind.FactionBoost => FactionBoost,
            EventChoiceKind.SacrificeHeroForArtifact => SacrificeHeroForArtifact,
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
