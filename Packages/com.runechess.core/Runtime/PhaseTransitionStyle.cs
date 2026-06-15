using System;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic styling and timing for the flourishes between run phases (GDD/codex visual
    /// overhaul: "Переходы фаз и адаптив"). It owns the original "Бой!" battle-start banner shown on
    /// the preparation→combat transition — its caption, how long it holds, and its fade-in/hold/
    /// fade-out ramp — so the Unity layer just plays the timing it is handed. Wording and timing are
    /// original to this project; no Dota/Valve references. Rendering the banner is Unity-only
    /// (documented verification gap); the timing contract is verified headless via <c>tools/CoreSmoke</c>.
    /// </summary>
    public static class PhaseTransitionStyle
    {
        /// <summary>Original battle-start banner caption shown on the preparation→combat transition.</summary>
        public const string BattleStartBannerText = "Бой!";

        /// <summary>How long the battle-start banner holds at full opacity before it fades out.</summary>
        public const double BattleBannerHoldSeconds = 0.6;

        /// <summary>How long the banner takes to fade in, and to fade out, around the hold.</summary>
        public const double BattleBannerFadeSeconds = 0.25;

        /// <summary>Total on-screen lifetime of the battle-start banner: fade-in + hold + fade-out.</summary>
        public static double BattleBannerTotalSeconds =>
            (BattleBannerFadeSeconds * 2.0) + BattleBannerHoldSeconds;

        /// <summary>Warm accent the banner text/sweep uses (shared gold accent token).</summary>
        public static uint BattleBannerColor => UiTheme.GoldColor;

        /// <summary>
        /// Banner opacity at <paramref name="secondsIntoBanner"/>: ramps 0→1 over the fade-in, holds
        /// at full through the hold window, then ramps 1→0 over the fade-out, and stays at 0 once the
        /// banner's lifetime has elapsed. Lets the Unity layer drive the banner from a single clock.
        /// </summary>
        public static double BattleBannerOpacityAt(double secondsIntoBanner)
        {
            if (secondsIntoBanner < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondsIntoBanner), "Time into the banner cannot be negative.");
            }

            if (secondsIntoBanner >= BattleBannerTotalSeconds)
            {
                return 0.0;
            }

            if (secondsIntoBanner < BattleBannerFadeSeconds)
            {
                return secondsIntoBanner / BattleBannerFadeSeconds;
            }

            var holdEnd = BattleBannerFadeSeconds + BattleBannerHoldSeconds;
            if (secondsIntoBanner <= holdEnd)
            {
                return 1.0;
            }

            var intoFadeOut = secondsIntoBanner - holdEnd;
            return 1.0 - (intoFadeOut / BattleBannerFadeSeconds);
        }

        // --- Combat→reward transition (task: бой → награда с подведением итога раунда). An outcome
        // banner plus a staged reveal of the round's reward tally, reading the existing
        // RoundRewardBreakdown so the lines shown match the gold actually credited. ---

        /// <summary>Outcome banner caption for the combat→reward transition (original wording).</summary>
        public static string OutcomeBannerText(BattleOutcome outcome) => outcome switch
        {
            BattleOutcome.PlayerVictory => "Победа!",
            BattleOutcome.PlayerDefeat => "Поражение",
            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome), outcome, "Only a resolved battle has an outcome banner.")
        };

        /// <summary>Stagger between reward-summary lines as the round tally reveals one beat at a time.</summary>
        public const double RewardSummaryLineStaggerSeconds = 0.15;

        /// <summary>
        /// How many tally lines the reward summary reveals: the base gold, each non-zero bonus
        /// component, and the final total. Mirrors the composition of <see cref="RoundRewardBreakdown"/>
        /// so the reveal never shows an empty bonus line or omits one that was actually paid.
        /// </summary>
        public static int RewardSummaryLineCount(RoundRewardBreakdown breakdown)
        {
            if (breakdown is null)
            {
                throw new ArgumentNullException(nameof(breakdown));
            }

            var lines = 1; // base gold
            if (breakdown.ChainBonusGold > 0)
            {
                lines++;
            }

            if (breakdown.AlchemistBonusGold > 0)
            {
                lines++;
            }

            if (breakdown.ArtifactBonusGold > 0)
            {
                lines++;
            }

            return lines + 1; // total line
        }

        /// <summary>When the reward line at <paramref name="lineIndex"/> appears, staggered after the first.</summary>
        public static double RewardSummaryLineRevealSecondsAt(int lineIndex)
        {
            if (lineIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lineIndex), "Reward line index cannot be negative.");
            }

            return lineIndex * RewardSummaryLineStaggerSeconds;
        }

        /// <summary>Total time to reveal every reward-summary line for a breakdown.</summary>
        public static double RewardSummaryRevealSeconds(RoundRewardBreakdown breakdown)
            => RewardSummaryLineCount(breakdown) * RewardSummaryLineStaggerSeconds;

        // --- Next-enemy preview reveal on the preparation screen (task: появление превью следующего
        // врага на экране подготовки). The upcoming roster reveals unit by unit so the player reads
        // who they will face before committing their board. Drives PreparationScreenModel.EnemyPreview. ---

        /// <summary>Stagger between each previewed enemy unit appearing on the preparation screen.</summary>
        public const double EnemyPreviewUnitStaggerSeconds = 0.12;

        /// <summary>How long each previewed enemy unit takes to fade/slide in once it begins revealing.</summary>
        public const double EnemyPreviewUnitRevealSeconds = 0.2;

        /// <summary>When the previewed enemy unit at <paramref name="unitIndex"/> begins revealing.</summary>
        public static double EnemyPreviewUnitRevealStartAt(int unitIndex)
        {
            if (unitIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitIndex), "Preview unit index cannot be negative.");
            }

            return unitIndex * EnemyPreviewUnitStaggerSeconds;
        }

        /// <summary>Total time to reveal the whole previewed roster of <paramref name="unitCount"/> units.</summary>
        public static double EnemyPreviewRevealSeconds(int unitCount)
        {
            if (unitCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitCount), "Preview unit count cannot be negative.");
            }

            if (unitCount == 0)
            {
                return 0.0;
            }

            return EnemyPreviewUnitRevealStartAt(unitCount - 1) + EnemyPreviewUnitRevealSeconds;
        }

        /// <summary>
        /// Reveal opacity (0→1) for a single previewed unit at <paramref name="secondsIntoReveal"/>
        /// into its own reveal, holding fully visible once revealed.
        /// </summary>
        public static double EnemyPreviewUnitOpacityAt(double secondsIntoReveal)
        {
            if (secondsIntoReveal < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondsIntoReveal), "Time into the reveal cannot be negative.");
            }

            if (secondsIntoReveal >= EnemyPreviewUnitRevealSeconds)
            {
                return 1.0;
            }

            return secondsIntoReveal / EnemyPreviewUnitRevealSeconds;
        }
    }
}
