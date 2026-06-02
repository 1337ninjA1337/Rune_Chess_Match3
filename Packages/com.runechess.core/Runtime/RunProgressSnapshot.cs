using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    public sealed record RunProgressSnapshot(
        int Version,
        int Round,
        int RunHealth,
        int Gold,
        int Xp,
        int PlayerLevel,
        CommanderState Commander,
        IReadOnlyList<BoardHero> Team,
        IReadOnlyList<HeroInstance> Bench,
        ShopState Shop,
        IReadOnlyList<ArtifactState> Artifacts,
        RunPhase Phase,
        string NextEnemyId,
        CombatProgressSnapshot? Combat,
        string? DefeatReason
    )
    {
        public const int CurrentVersion = 8;

        public static RunProgressSnapshot Capture(RunState state)
        {
            return new RunProgressSnapshot(
                Version: CurrentVersion,
                Round: state.Round,
                RunHealth: state.RunHealth,
                Gold: state.Gold,
                Xp: state.Xp,
                PlayerLevel: state.PlayerLevel,
                Commander: state.Commander,
                Team: state.Team.ToList(),
                Bench: state.Bench.ToList(),
                Shop: CopyShop(state.Shop),
                Artifacts: state.Artifacts.ToList(),
                Phase: state.Phase,
                NextEnemyId: state.NextEnemyId,
                Combat: state.Combat is null ? null : CombatProgressSnapshot.Capture(state.Combat),
                DefeatReason: state.DefeatReason
            );
        }

        public RunState Restore()
        {
            if (Version != CurrentVersion)
            {
                throw new InvalidOperationException("Run progress snapshot version is not supported.");
            }

            var combat = Combat?.Restore();
            if (Phase == RunPhase.Combat && combat is null)
            {
                throw new InvalidOperationException("Combat phase progress requires a combat snapshot.");
            }

            return new RunState(
                Round: Round,
                RunHealth: RunHealth,
                Gold: Gold,
                Xp: Xp,
                PlayerLevel: PlayerLevel,
                Commander: Commander,
                Team: Team.ToList(),
                Bench: Bench.ToList(),
                Shop: CopyShop(Shop),
                Artifacts: Artifacts.ToList(),
                Phase: Phase,
                NextEnemyId: NextEnemyId,
                Combat: combat,
                DefeatReason: DefeatReason
            );
        }

        private static ShopState CopyShop(ShopState shop)
        {
            return new ShopState(shop.Offers.ToList(), shop.RerollsThisRound);
        }
    }

    public sealed record CombatProgressSnapshot(
        IReadOnlyList<RuneType> Runes,
        IReadOnlyList<bool> GreatRuneFlags,
        int Match3MovesUsed,
        int LastMatchedRunesCount,
        int LastComboDepth,
        int LastMatchPower,
        int DurationSeconds,
        int ElapsedSeconds,
        int GlobalCooldownMillisecondsRemaining,
        int SecondsSinceLastRuneSwap,
        int SlowdownMillisecondsRemaining,
        bool EarnedChainFourGoldBonus,
        double LastCommanderEnergyGain,
        int LastMatch4ComboCount,
        int LastBonusBlueRunesCreated
    )
    {
        public static CombatProgressSnapshot Capture(CombatState state)
        {
            var runes = new List<RuneType>(Match3Board.Rows * Match3Board.Columns);
            var greatRuneFlags = new List<bool>(Match3Board.Rows * Match3Board.Columns);
            for (var row = 0; row < Match3Board.Rows; row += 1)
            {
                for (var column = 0; column < Match3Board.Columns; column += 1)
                {
                    var point = new BoardPoint(row, column);
                    runes.Add(state.RuneBoard[point]);
                    greatRuneFlags.Add(state.RuneBoard.IsGreatRune(point));
                }
            }

            return new CombatProgressSnapshot(
                Runes: runes,
                GreatRuneFlags: greatRuneFlags,
                Match3MovesUsed: state.Match3MovesUsed,
                LastMatchedRunesCount: state.LastMatchedRunesCount,
                LastComboDepth: state.LastComboDepth,
                LastMatchPower: state.LastMatchPower,
                DurationSeconds: state.DurationSeconds,
                ElapsedSeconds: state.ElapsedSeconds,
                GlobalCooldownMillisecondsRemaining: state.GlobalCooldownMillisecondsRemaining,
                SecondsSinceLastRuneSwap: state.SecondsSinceLastRuneSwap,
                SlowdownMillisecondsRemaining: state.SlowdownMillisecondsRemaining,
                EarnedChainFourGoldBonus: state.EarnedChainFourGoldBonus,
                LastCommanderEnergyGain: state.LastCommanderEnergyGain,
                LastMatch4ComboCount: state.LastMatch4ComboCount,
                LastBonusBlueRunesCreated: state.LastBonusBlueRunesCreated
            );
        }

        public CombatState Restore()
        {
            if (Runes.Count != Match3Board.CellCount || GreatRuneFlags.Count != Match3Board.CellCount)
            {
                throw new InvalidOperationException("Combat progress rune board has an invalid cell count.");
            }

            if (DurationSeconds <= 0)
            {
                throw new InvalidOperationException("Combat progress duration must be positive.");
            }

            if (ElapsedSeconds is < 0 || ElapsedSeconds > DurationSeconds)
            {
                throw new InvalidOperationException("Combat progress elapsed time is outside the timer duration.");
            }

            if (GlobalCooldownMillisecondsRemaining < 0)
            {
                throw new InvalidOperationException("Combat progress cooldown cannot be negative.");
            }

            if (SecondsSinceLastRuneSwap < 0)
            {
                throw new InvalidOperationException("Combat progress idle timer cannot be negative.");
            }

            if (SlowdownMillisecondsRemaining < 0)
            {
                throw new InvalidOperationException("Combat progress slowdown cannot be negative.");
            }

            return new CombatState(
                RuneBoard: new Match3Board(Runes
                    .Select((rune, index) => new RuneCell(rune, GreatRuneFlags[index]))
                    .ToList()),
                Match3MovesUsed: Match3MovesUsed,
                LastMatchedRunesCount: LastMatchedRunesCount,
                LastComboDepth: LastComboDepth,
                LastMatchPower: LastMatchPower,
                DurationSeconds: DurationSeconds,
                ElapsedSeconds: ElapsedSeconds,
                GlobalCooldownMillisecondsRemaining: GlobalCooldownMillisecondsRemaining,
                SecondsSinceLastRuneSwap: SecondsSinceLastRuneSwap,
                SlowdownMillisecondsRemaining: SlowdownMillisecondsRemaining,
                EarnedChainFourGoldBonus: EarnedChainFourGoldBonus,
                LastCommanderEnergyGain: LastCommanderEnergyGain,
                LastMatch4ComboCount: LastMatch4ComboCount,
                LastBonusBlueRunesCreated: LastBonusBlueRunesCreated
            );
        }
    }

    public sealed class RunProgressStore
    {
        private RunProgressSnapshot? snapshot;

        public bool HasSavedRun => snapshot is not null;
        public RunProgressSnapshot? Snapshot => snapshot;

        public void Save(RunState state)
        {
            snapshot = RunProgressSnapshot.Capture(state);
        }

        public bool TryLoad(out RunState state)
        {
            if (snapshot is null)
            {
                state = RunState.NewRun();
                return false;
            }

            state = snapshot.Restore();
            return true;
        }

        public void Clear()
        {
            snapshot = null;
        }
    }
}
