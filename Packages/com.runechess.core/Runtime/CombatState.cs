using System;

namespace RuneChess.Core
{
    public sealed record CombatState(
        Match3Board RuneBoard,
        int Match3MovesUsed,
        int LastMatchedRunesCount,
        int LastComboDepth,
        int LastMatchPower,
        int DurationSeconds,
        int ElapsedSeconds,
        int GlobalCooldownMillisecondsRemaining,
        int SecondsSinceLastRuneSwap,
        int SlowdownMillisecondsRemaining,
        bool EarnedChainFourGoldBonus = false
    )
    {
        public const int DefaultDurationSeconds = 60;
        public const int SwapGlobalCooldownMilliseconds = 250;
        public const int MatchHintDelaySeconds = 8;
        public const int NormalCombatSpeedPercent = 100;
        public const int LargeComboCombatSpeedPercent = 70;
        public const int LargeComboSlowdownMilliseconds = 1000;
        public const int LargeComboMatchedRunesThreshold = 4;
        public const int LargeComboComboDepthThreshold = 1;
        public const int ChainFourGoldBonus = 1;
        public const int ChainFourGoldBonusMinimumChainNumber = 4;

        public int RemainingSeconds => Math.Max(0, DurationSeconds - ElapsedSeconds);
        public bool IsTimerExpired => RemainingSeconds == 0;
        public bool IsSwapOnCooldown => GlobalCooldownMillisecondsRemaining > 0;
        public bool ShouldShowMatchHint => SecondsSinceLastRuneSwap >= MatchHintDelaySeconds;
        public bool IsCombatSlowed => SlowdownMillisecondsRemaining > 0;
        public int CombatSpeedPercent => IsCombatSlowed ? LargeComboCombatSpeedPercent : NormalCombatSpeedPercent;
        public Match3MoveHint? CurrentMatchHint => ShouldShowMatchHint ? RuneBoard.FindFirstLegalMoveHint() : null;

        public static CombatState Start(int runeSeed, int durationSeconds = DefaultDurationSeconds)
        {
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Combat duration must be positive.");
            }

            return new CombatState(
                RuneBoard: Match3Board.CreateDeterministic(runeSeed),
                Match3MovesUsed: 0,
                LastMatchedRunesCount: 0,
                LastComboDepth: 0,
                LastMatchPower: 0,
                DurationSeconds: durationSeconds,
                ElapsedSeconds: 0,
                GlobalCooldownMillisecondsRemaining: 0,
                SecondsSinceLastRuneSwap: 0,
                SlowdownMillisecondsRemaining: 0
            );
        }

        public CombatState AdvanceTimer(int elapsedSeconds)
        {
            if (elapsedSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed combat time cannot be negative.");
            }

            var elapsedMilliseconds = elapsedSeconds > int.MaxValue / 1000
                ? int.MaxValue
                : elapsedSeconds * 1000;
            var timedEffectsAdvanced = AdvanceTimedEffectsMilliseconds(elapsedMilliseconds);

            return timedEffectsAdvanced with
            {
                ElapsedSeconds = Math.Min(DurationSeconds, ElapsedSeconds + elapsedSeconds),
                SecondsSinceLastRuneSwap = AddClamped(SecondsSinceLastRuneSwap, elapsedSeconds)
            };
        }

        public CombatState AdvanceCooldownMilliseconds(int elapsedMilliseconds)
        {
            return AdvanceTimedEffectsMilliseconds(elapsedMilliseconds);
        }

        public CombatState AdvanceTimedEffectsMilliseconds(int elapsedMilliseconds)
        {
            if (elapsedMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds), "Elapsed combat effect time cannot be negative.");
            }

            return this with
            {
                GlobalCooldownMillisecondsRemaining = Math.Max(0, GlobalCooldownMillisecondsRemaining - elapsedMilliseconds),
                SlowdownMillisecondsRemaining = Math.Max(0, SlowdownMillisecondsRemaining - elapsedMilliseconds)
            };
        }

        public CombatState SwapRunes(BoardPoint a, BoardPoint b, int comboDepth = 0)
        {
            if (comboDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comboDepth), "Combo depth cannot be negative.");
            }

            if (IsSwapOnCooldown)
            {
                throw new InvalidOperationException("Rune swap is on global cooldown.");
            }

            var swapped = RuneBoard.SwapIfCreatesMatch(a, b);
            var resolution = swapped.ResolveChainReactions(Match3MovesUsed + 1);
            var matchedRunesCount = resolution.TotalMatchedRunesCount;
            var resolvedComboDepth = comboDepth + resolution.MaxComboDepth;
            var matchPower = resolution.GetTotalMatchPower(comboDepth);
            var earnedChainFourBonus = resolution.Steps.Any(step =>
                step.ChainNumber >= ChainFourGoldBonusMinimumChainNumber);
            var slowdownMilliseconds = ShouldTriggerLargeComboSlowdown(matchedRunesCount, resolvedComboDepth)
                ? LargeComboSlowdownMilliseconds
                : 0;

            return this with
            {
                RuneBoard = resolution.Board,
                Match3MovesUsed = Match3MovesUsed + 1,
                LastMatchedRunesCount = matchedRunesCount,
                LastComboDepth = resolvedComboDepth,
                LastMatchPower = matchPower,
                GlobalCooldownMillisecondsRemaining = SwapGlobalCooldownMilliseconds,
                SecondsSinceLastRuneSwap = 0,
                SlowdownMillisecondsRemaining = Math.Max(SlowdownMillisecondsRemaining, slowdownMilliseconds),
                EarnedChainFourGoldBonus = EarnedChainFourGoldBonus || earnedChainFourBonus
            };
        }

        private static bool ShouldTriggerLargeComboSlowdown(int matchedRunesCount, int comboDepth)
        {
            return matchedRunesCount >= LargeComboMatchedRunesThreshold || comboDepth >= LargeComboComboDepthThreshold;
        }

        private static int AddClamped(int a, int b)
        {
            return a > int.MaxValue - b ? int.MaxValue : a + b;
        }
    }
}
