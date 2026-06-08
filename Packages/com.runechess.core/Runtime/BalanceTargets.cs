using System;

namespace RuneChess.Core
{
    /// <summary>
    /// An inclusive integer range, used to express the GDD balance goals that read as
    /// "обычно 3-4 героев", "1-2 активные синергии", "5-6 героев на поле" and so on.
    /// </summary>
    public readonly record struct BalanceRange(int Min, int Max)
    {
        public int Min { get; init; } = Min <= Max
            ? Min
            : throw new ArgumentException("Balance range minimum cannot exceed its maximum.", nameof(Min));

        public int Max { get; init; } = Max;

        public bool Contains(int value) => value >= Min && value <= Max;

        public override string ToString() => $"{Min}-{Max}";
    }

    /// <summary>
    /// A double-valued inclusive range, used for the battle-duration goal (45-75 seconds).
    /// </summary>
    public readonly record struct BalanceDurationRange(double Min, double Max)
    {
        public double Min { get; init; } = Min <= Max
            ? Min
            : throw new ArgumentException("Balance duration range minimum cannot exceed its maximum.", nameof(Min));

        public double Max { get; init; } = Max;

        public bool Contains(double value) => value >= Min && value <= Max;

        public override string ToString() => $"{Min:0.#}-{Max:0.#}s";
    }

    /// <summary>
    /// Single source of truth for the GDD "Баланс целей MVP" section
    /// (GDD_Rune_Chess_Match3.md, "### Баланс целей MVP"). These are the player-progression
    /// goals the economy and combat tuning must satisfy. Holding them as explicit, validated
    /// data lets <see cref="BalanceProjection"/> and the smoke suite assert that the shipped
    /// configuration actually reaches each goal, instead of leaving the goals as prose only.
    /// </summary>
    public static class BalanceTargets
    {
        /// <summary>"К концу раунда 3 игрок обычно имеет 3-4 героев."</summary>
        public static BalanceRange HeroesOwnedByRound3 { get; } = new(3, 4);

        /// <summary>Round by which the ownership target above is measured.</summary>
        public const int HeroOwnershipMilestoneRound = 3;

        /// <summary>"К раунду 5 игрок должен иметь хотя бы одного героя 2 звезд."</summary>
        public const int FirstTwoStarMilestoneRound = 5;

        /// <summary>"К раунду 8 игрок должен иметь 1-2 активные синергии."</summary>
        public static BalanceRange ActiveSynergiesByRound8 { get; } = new(1, 2);

        /// <summary>Round by which the synergy target above is measured.</summary>
        public const int SynergyMilestoneRound = 8;

        /// <summary>"К финалу игрок должен иметь 5-6 героев на поле."</summary>
        public static BalanceRange HeroesFieldedByFinal { get; } = new(5, 6);

        /// <summary>"В среднем игрок делает 4-8 match-3 ходов за бой."</summary>
        public static BalanceRange Match3MovesPerBattle { get; } = new(4, 8);

        /// <summary>"Один бой длится 45-75 секунд."</summary>
        public static BalanceDurationRange BattleDurationSeconds { get; } = new(45.0, 75.0);

        /// <summary>
        /// Modeled average cadence of a deliberate match-3 swap during a readable battle.
        /// Chosen so the swing of <see cref="BattleDurationSeconds"/> (45-75s) maps onto the
        /// <see cref="Match3MovesPerBattle"/> goal (4-8 moves): 45/10=4.5, 60/10=6, 75/10=7.5.
        /// </summary>
        public const double SecondsPerMatch3Move = 10.0;

        /// <summary>
        /// Expected number of deliberate match-3 moves a player makes in a battle of the given
        /// duration, derived from <see cref="SecondsPerMatch3Move"/>.
        /// </summary>
        public static int ExpectedMatch3MovesInBattle(double battleDurationSeconds)
        {
            if (battleDurationSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(battleDurationSeconds), "Battle duration must be positive.");
            }

            return (int)Math.Round(battleDurationSeconds / SecondsPerMatch3Move, MidpointRounding.AwayFromZero);
        }
    }
}
