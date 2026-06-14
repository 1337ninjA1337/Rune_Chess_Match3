using System;

namespace RuneChess.Core
{
    /// <summary>
    /// View-model for the run-level vitals on the combat HUD: run health (GDD "полоса и число
    /// HP забега"), gold, and the player level with XP progress toward the next level
    /// (GDD "уровень игрока и XP-бар до следующего уровня"). It exposes health and XP both as a
    /// renderable 0..1 bar fraction and as a "current / max" label so the Unity layer draws bars
    /// without owning any economy rules. Pure data, derived from <see cref="RunState"/> and
    /// <see cref="EconomyConfig"/>, so the formatting can be smoke-tested.
    /// </summary>
    public sealed record RunVitalsModel(
        int RunHealth,
        int MaxRunHealth,
        double HealthFraction,
        string HealthLabel,
        int Gold,
        int PlayerLevel,
        int MaxPlayerLevel,
        bool IsMaxLevel,
        int Xp,
        int XpForNextLevel,
        double XpFraction,
        string XpLabel)
    {
        /// <summary>Health clamped to a renderable 0..1 bar fraction.</summary>
        public double HealthBar => Math.Clamp(HealthFraction, 0.0, 1.0);

        /// <summary>XP progress clamped to a renderable 0..1 bar fraction.</summary>
        public double XpBar => Math.Clamp(XpFraction, 0.0, 1.0);

        /// <summary>Compact level label for the HUD, e.g. <c>LV 3</c>.</summary>
        public string LevelLabel => $"LV {PlayerLevel}";

        /// <summary>
        /// Build the run-vitals view-model from the live run state. Max run health is the
        /// configured starting health (the value run healing is capped to), and XP progress is
        /// measured against the cost to reach the next player level; at max level the XP bar reads
        /// full and is labelled <c>MAX</c> since there is no further level to gain.
        /// </summary>
        public static RunVitalsModel Build(RunState run, EconomyConfig? economy = null)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var config = economy ?? EconomyConfig.Default;

            var maxHealth = config.StartingRunHealth;
            var healthFraction = maxHealth <= 0
                ? 0.0
                : (double)run.RunHealth / maxHealth;

            var isMaxLevel = run.PlayerLevel >= config.MaxPlayerLevel;
            var xpForNext = isMaxLevel ? 0 : config.GetXpCostForNextLevel(run.PlayerLevel);
            var xpFraction = isMaxLevel || xpForNext <= 0
                ? 1.0
                : (double)run.Xp / xpForNext;
            var xpLabel = isMaxLevel ? "MAX" : $"{run.Xp} / {xpForNext}";

            return new RunVitalsModel(
                RunHealth: run.RunHealth,
                MaxRunHealth: maxHealth,
                HealthFraction: Math.Clamp(healthFraction, 0.0, 1.0),
                HealthLabel: $"{run.RunHealth} / {maxHealth}",
                Gold: run.Gold,
                PlayerLevel: run.PlayerLevel,
                MaxPlayerLevel: config.MaxPlayerLevel,
                IsMaxLevel: isMaxLevel,
                Xp: run.Xp,
                XpForNextLevel: xpForNext,
                XpFraction: Math.Clamp(xpFraction, 0.0, 1.0),
                XpLabel: xpLabel);
        }
    }
}
