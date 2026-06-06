namespace RuneChess.Core
{
    /// <summary>
    /// A one-battle stat blessing on a single faction (GDD event "усиление одной фракции на
    /// следующий бой"). It scales the health and attack of allied units that belong to
    /// <see cref="FactionName"/> when the next autobattle is built. <see cref="None"/> is the
    /// neutral value carried when no faction blessing is pending.
    /// </summary>
    public readonly record struct FactionBoost(string FactionName, double StatMultiplier)
    {
        /// <summary>The neutral boost: no faction, no scaling.</summary>
        public static FactionBoost None { get; } = new(string.Empty, 1.0);

        /// <summary>True when the boost actually buffs a faction this battle.</summary>
        public bool IsActive =>
            !string.IsNullOrEmpty(FactionName) && StatMultiplier > 0.0 && StatMultiplier != 1.0;

        /// <summary>True when the boost applies to a hero of the given faction.</summary>
        public bool Buffs(string heroFaction) =>
            IsActive && string.Equals(FactionName, heroFaction, System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Return <paramref name="unit"/> scaled by the boost when it belongs to the buffed
        /// faction, otherwise the unit unchanged. The current-health ratio is preserved.
        /// </summary>
        public BattleUnit Apply(BattleUnit unit, string heroFaction)
        {
            if (unit is null || !Buffs(heroFaction))
            {
                return unit!;
            }

            var maxHealth = unit.MaxHealth * StatMultiplier;
            var healthRatio = unit.MaxHealth <= 0.0 ? 0.0 : unit.CurrentHealth / unit.MaxHealth;
            return unit with
            {
                MaxHealth = maxHealth,
                CurrentHealth = maxHealth * healthRatio,
                Attack = unit.Attack * StatMultiplier
            };
        }
    }
}
