using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// The lever through which match-3 is allowed to influence a battle, mirroring the GDD/codex
    /// product rule "Match-3 влияет на темп, способности и clutch-моменты, но не заменяет стратегию
    /// состава." Match-3 may steer the fight along these axes, but the auto battler (team
    /// composition) stays the primary source of combat output.
    /// </summary>
    public enum Match3Lever
    {
        /// <summary>Tempo: burst pressure that speeds the fight up (damage runes).</summary>
        Tempo,

        /// <summary>Abilities: mana that brings hero casts online sooner (blue runes).</summary>
        Abilities,

        /// <summary>Clutch moments: heals, shields and commander energy that swing a fight (green/yellow/white).</summary>
        Clutch
    }

    /// <summary>
    /// Single source of truth for the MVP "core loop" design pillars (GDD "Цель MVP" and codex
    /// "Продуктовые правила"): team composition must remain the main strategic decision, while
    /// match-3 stays a meaningful-but-secondary layer that influences tempo, abilities and clutch
    /// moments without replacing or being made pointless by the auto battler. Holding these pillars
    /// as explicit, validated data lets <see cref="DesignPillarProjection"/> and the smoke suite
    /// assert that the shipped combat tuning actually respects them, instead of leaving them as prose.
    /// </summary>
    public static class DesignPillarTargets
    {
        /// <summary>
        /// Lowest share of player combat output that team composition (the auto battler) must keep,
        /// so the squad you build stays the dominant strategic decision (codex: "Состав отряда
        /// остается главным стратегическим решением").
        /// </summary>
        public const double CompositionDominanceFloor = 0.5;

        /// <summary>
        /// Highest share of player combat output match-3 is allowed to reach. Staying under this
        /// keeps match-3 from suppressing the auto battler (codex: "Match-3 ... не заменяет стратегию
        /// состава"). It is the complement of <see cref="CompositionDominanceFloor"/>.
        /// </summary>
        public const double Match3OutputShareCeiling = 1.0 - CompositionDominanceFloor;

        /// <summary>The match-3 moves a player makes per battle (reused from the balance goals).</summary>
        public static BalanceRange Match3MovesPerBattle => BalanceTargets.Match3MovesPerBattle;

        /// <summary>The three levers match-3 is expected to influence, per the GDD product rule.</summary>
        public static IReadOnlyList<Match3Lever> Match3Levers { get; } =
            Array.AsReadOnly(new[] { Match3Lever.Tempo, Match3Lever.Abilities, Match3Lever.Clutch });

        /// <summary>
        /// Map a resolved rune effect kind onto the design lever it serves. Damage drives tempo,
        /// mana feeds abilities, and healing/shields/commander energy are the clutch levers.
        /// </summary>
        public static Match3Lever LeverFor(RuneEffectKind kind)
        {
            return kind switch
            {
                RuneEffectKind.PhysicalDamage => Match3Lever.Tempo,
                RuneEffectKind.MagicDamage => Match3Lever.Tempo,
                RuneEffectKind.Mana => Match3Lever.Abilities,
                RuneEffectKind.Healing => Match3Lever.Clutch,
                RuneEffectKind.Shield => Match3Lever.Clutch,
                RuneEffectKind.CommanderEnergy => Match3Lever.Clutch,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown rune effect kind.")
            };
        }
    }
}
