namespace RuneChess.Core
{
    /// <summary>
    /// One offered choice on the event screen ("Экран события"). It carries the
    /// event identity, the Russian copy the card renders (title, description, the
    /// risk/reward summary and the accept/decline labels) and the concrete deltas
    /// the choice applies. Keeping the deltas as explicit data lets the screen
    /// render a meaningful card today and lets the separate P1 event-mechanics task
    /// apply the outcome without re-deriving balance numbers.
    /// </summary>
    public sealed record EventOption(
        EventChoiceKind Kind,
        string Id,
        string Title,
        string Description,
        string RiskLabel,
        string RewardLabel,
        string AcceptLabel,
        string DeclineLabel = "Отказаться",
        int HealthCost = 0,
        int GoldReward = 0,
        int GoldCost = 0,
        int HealthReward = 0,
        int XpReward = 0,
        string FactionId = "",
        bool GrantsHero = false,
        bool RemovesHero = false,
        bool GrantsArtifact = false,
        bool AppliesCurse = false)
    {
        /// <summary>True when accepting the event costs the player run health.</summary>
        public bool CostsHealth => HealthCost > 0;

        /// <summary>True when accepting the event grants gold.</summary>
        public bool GrantsGold => GoldReward > 0;

        /// <summary>True when accepting the event costs the player gold.</summary>
        public bool CostsGold => GoldCost > 0;

        /// <summary>True when accepting the event restores run health.</summary>
        public bool RestoresHealth => HealthReward > 0;

        /// <summary>True when accepting the event grants XP toward the next player level.</summary>
        public bool GrantsXp => XpReward > 0;

        /// <summary>True when accepting the event buffs a specific faction next battle.</summary>
        public bool BuffsFaction => Kind == EventChoiceKind.FactionBoost;
    }
}
