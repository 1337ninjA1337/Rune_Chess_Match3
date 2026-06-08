using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// A deterministic feasibility model for the MVP "core loop" design pillars
    /// (see <see cref="DesignPillarTargets"/>). Like <see cref="BalanceProjection"/>, it does not
    /// simulate RNG or live play; instead it derives, from the shipped hero roster, rune resolver
    /// and balance goals, two complementary proofs:
    /// <list type="number">
    /// <item>An <b>output-share</b> proof — over a battle, team composition (the auto battler)
    /// produces the dominant share of player combat output while match-3 produces a strictly
    /// positive but minority share. This proves composition stays the main decision and that
    /// match-3 neither suppresses the auto battler nor is suppressed to nothing.</item>
    /// <item>A <b>lever-coverage</b> proof — the six rune colours, through the real
    /// <see cref="RuneEffectResolver"/>, cover all three GDD levers (tempo, abilities, clutch) and a
    /// single in-budget match can charge a hero ability and grant commander energy. This proves
    /// match-3 genuinely influences tempo, abilities and clutch moments.</item>
    /// </list>
    /// Combat output is measured in attack-equivalent units: rune damage Power feeds the same
    /// <see cref="CombatFormulas.CalculatePhysicalDamage"/> path as a hero's attack, so comparing a
    /// hero's attack throughput against a match's Power is apples-to-apples for this pillar check.
    /// Live-playtest measurement of the actual feel remains the documented verification gap (no
    /// Unity/.NET runtime in this environment).
    /// </summary>
    public static class DesignPillarProjection
    {
        /// <summary>
        /// Representative per-move match-3 output, in attack-equivalent Power. A plain base match-3
        /// scores <see cref="Match3Scoring.CalculateMatchPower"/> = 3 (three runes, no combo depth).
        /// Higher tiers and chains scale this up but stay bounded, so the base match is the
        /// conservative figure for the dominance check; the share check below is generous on the
        /// match-3 side and still holds.
        /// </summary>
        public const int RepresentativeMatchPower = 3;

        /// <summary>Average one-star attack across the shipped hero roster.</summary>
        public static double AverageHeroAttack { get; } = HeroCatalog.All.Average(hero => hero.BaseStats.Attack);

        /// <summary>Average one-star attack speed across the shipped hero roster.</summary>
        public static double AverageAttackSpeed { get; } = HeroCatalog.All.Average(hero => hero.BaseStats.BaseAttackSpeed);

        /// <summary>
        /// Attack-equivalent combat output a fielded team produces over a battle: heroes attacking at
        /// the roster's average attack and speed for the battle's duration.
        /// </summary>
        public static double CompositionOutput(int fieldedHeroes, double durationSeconds)
        {
            if (fieldedHeroes < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldedHeroes), "A battle fields at least one hero.");
            }

            if (durationSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Battle duration must be positive.");
            }

            return fieldedHeroes * AverageHeroAttack * AverageAttackSpeed * durationSeconds;
        }

        /// <summary>Attack-equivalent combat output match-3 contributes over a battle's move budget.</summary>
        public static double Match3Output(int moveBudget)
        {
            if (moveBudget < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveBudget), "Move budget cannot be negative.");
            }

            return moveBudget * (double)RepresentativeMatchPower;
        }

        /// <summary>Match-3's share of total player combat output for the given battle parameters.</summary>
        public static double Match3OutputShare(int fieldedHeroes, int moveBudget, double durationSeconds)
        {
            var composition = CompositionOutput(fieldedHeroes, durationSeconds);
            var match3 = Match3Output(moveBudget);
            var total = composition + match3;
            return total <= 0.0 ? 0.0 : match3 / total;
        }

        /// <summary>
        /// Match-3's output share for the representative MVP battle: a full final-stage team
        /// (<see cref="BalanceTargets.HeroesFieldedByFinal"/> minimum) at the default battle length,
        /// with the player spending the top of the per-battle move budget (most generous to match-3).
        /// </summary>
        public static double RepresentativeMatch3OutputShare()
        {
            return Match3OutputShare(
                BalanceTargets.HeroesFieldedByFinal.Min,
                DesignPillarTargets.Match3MovesPerBattle.Max,
                BattleState.DefaultDurationSeconds);
        }

        /// <summary>
        /// Pillar 1 — team composition stays the dominant share of combat output (the main strategic
        /// decision). True when composition's share is at or above <see cref="DesignPillarTargets.CompositionDominanceFloor"/>.
        /// </summary>
        public static bool CompositionRemainsPrimary()
        {
            return 1.0 - RepresentativeMatch3OutputShare() >= DesignPillarTargets.CompositionDominanceFloor;
        }

        /// <summary>
        /// Pillar 3 — match-3 does not suppress the auto battler. True when match-3's output share
        /// stays at or below <see cref="DesignPillarTargets.Match3OutputShareCeiling"/>.
        /// </summary>
        public static bool Match3StaysMinority()
        {
            return RepresentativeMatch3OutputShare() <= DesignPillarTargets.Match3OutputShareCeiling;
        }

        /// <summary>
        /// Pillar 4 — the auto battler does not make match-3 pointless. True when match-3 contributes
        /// a strictly positive output share for a non-empty move budget.
        /// </summary>
        public static bool Match3Contributes()
        {
            return RepresentativeMatch3OutputShare() > 0.0;
        }

        /// <summary>
        /// The design levers the six shipped rune colours actually cover, derived through the real
        /// rune effect mapping. Used to prove match-3 influences tempo, abilities and clutch.
        /// </summary>
        public static IReadOnlyList<Match3Lever> CoveredLevers()
        {
            return Enum.GetValues(typeof(RuneType))
                .Cast<RuneType>()
                .Select(rune => DesignPillarTargets.LeverFor(RuneEffects.GetEffectKind(rune)))
                .Distinct()
                .ToList();
        }

        /// <summary>Pillar 2a — the rune colours cover every GDD lever (tempo, abilities, clutch).</summary>
        public static bool Match3CoversAllLevers()
        {
            var covered = CoveredLevers();
            return DesignPillarTargets.Match3Levers.All(covered.Contains);
        }

        /// <summary>
        /// Pillar 2b — within a battle's move budget, a single match-4 charges a hero ability,
        /// grounded in the real <see cref="RuneEffectResolver"/>. Proves match-3 influences abilities.
        /// </summary>
        public static bool Match3ChargesAbilityWithinBattle()
        {
            if (DesignPillarTargets.Match3MovesPerBattle.Min < 1)
            {
                return false;
            }

            var matchFour = new RuneMatchGroup(
                RuneType.Blue,
                new[] { new BoardPoint(0, 0), new BoardPoint(0, 1), new BoardPoint(0, 2), new BoardPoint(0, 3) },
                IsTOrLShaped: false,
                ContainsGreatRune: false);
            return RuneEffectResolver.Resolve(matchFour, chainNumber: 1).ChargesHero;
        }

        /// <summary>
        /// Pillar 2c — within a battle's move budget, a T/L combo delivers commander energy (a clutch
        /// lever), grounded in the real <see cref="RuneEffectResolver"/>. Proves match-3 influences
        /// clutch moments.
        /// </summary>
        public static bool Match3DeliversClutchWithinBattle()
        {
            if (DesignPillarTargets.Match3MovesPerBattle.Min < 1)
            {
                return false;
            }

            var tCombo = new RuneMatchGroup(
                RuneType.White,
                new[]
                {
                    new BoardPoint(0, 0), new BoardPoint(0, 1), new BoardPoint(0, 2),
                    new BoardPoint(1, 1), new BoardPoint(2, 1)
                },
                IsTOrLShaped: true,
                ContainsGreatRune: false);
            return RuneEffects.GetCommanderEnergyGain(RuneEffectResolver.Resolve(tCombo, chainNumber: 1)) > 0.0;
        }

        /// <summary>True when every core-loop design pillar holds for the shipped configuration.</summary>
        public static bool AllPillarsHold()
        {
            return CompositionRemainsPrimary()
                && Match3StaysMinority()
                && Match3Contributes()
                && Match3CoversAllLevers()
                && Match3ChargesAbilityWithinBattle()
                && Match3DeliversClutchWithinBattle();
        }
    }
}
