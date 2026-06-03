using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// A single key unit shown on the combat HUD: its label, side and current
    /// health/mana as 0..1 fractions ready for bar rendering. Pure data so the
    /// Unity layer only draws it.
    /// </summary>
    public sealed record CombatHudUnit(
        string Name,
        bool IsPlayer,
        double HealthFraction,
        double ManaFraction)
    {
        /// <summary>Health clamped to a renderable 0..1 bar fraction.</summary>
        public double HealthBar => Math.Clamp(HealthFraction, 0.0, 1.0);

        /// <summary>Mana clamped to a renderable 0..1 bar fraction.</summary>
        public double ManaBar => Math.Clamp(ManaFraction, 0.0, 1.0);
    }

    /// <summary>
    /// View-model for the combat screen heads-up display: the battle timer, the
    /// combat-speed indicator (normal vs the large-combo slowdown), the match-3
    /// progress numbers and the key units to show with health/mana bars. It reads
    /// from <see cref="CombatState"/> so the Unity layer renders without owning any
    /// timing rules; keeping it in core lets the formatting be smoke-tested.
    /// </summary>
    public sealed record CombatHudModel(
        int RemainingSeconds,
        int DurationSeconds,
        string TimerLabel,
        double TimerFraction,
        bool IsTimerExpired,
        int CombatSpeedPercent,
        bool IsSlowed,
        string SpeedLabel,
        int Match3MovesUsed,
        int LastMatchPower,
        IReadOnlyList<CombatHudUnit> KeyUnits)
    {
        /// <summary>Format a whole-second duration as a <c>m:ss</c> countdown label.</summary>
        public static string FormatTimer(int totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:00}";
        }

        /// <summary>
        /// Build the HUD view-model from the live combat state. Optional
        /// <paramref name="keyUnits"/> are the units (allies and enemies) the screen
        /// should surface with health/mana bars.
        /// </summary>
        public static CombatHudModel Build(CombatState combat, IReadOnlyList<CombatHudUnit>? keyUnits = null)
        {
            if (combat is null)
            {
                throw new ArgumentNullException(nameof(combat));
            }

            var fraction = combat.DurationSeconds <= 0
                ? 0.0
                : (double)combat.RemainingSeconds / combat.DurationSeconds;

            return new CombatHudModel(
                RemainingSeconds: combat.RemainingSeconds,
                DurationSeconds: combat.DurationSeconds,
                TimerLabel: FormatTimer(combat.RemainingSeconds),
                TimerFraction: Math.Clamp(fraction, 0.0, 1.0),
                IsTimerExpired: combat.IsTimerExpired,
                CombatSpeedPercent: combat.CombatSpeedPercent,
                IsSlowed: combat.IsCombatSlowed,
                SpeedLabel: combat.IsCombatSlowed ? $"SLOW {combat.CombatSpeedPercent}%" : "NORMAL",
                Match3MovesUsed: combat.Match3MovesUsed,
                LastMatchPower: combat.LastMatchPower,
                KeyUnits: keyUnits ?? Array.Empty<CombatHudUnit>());
        }
    }
}
