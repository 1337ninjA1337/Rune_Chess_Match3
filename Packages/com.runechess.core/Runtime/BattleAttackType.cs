namespace RuneChess.Core
{
    public enum BattleAttackType
    {
        Melee,
        Ranged
    }

    public static class BattleAttackTypes
    {
        public const string MeleeId = "melee";
        public const string RangedId = "ranged";

        /// <summary>Maps a hero definition's attack-type string to the enum; unknown values default to melee.</summary>
        public static BattleAttackType FromId(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id) && id.Trim().Equals(RangedId, System.StringComparison.OrdinalIgnoreCase))
            {
                return BattleAttackType.Ranged;
            }

            return BattleAttackType.Melee;
        }
    }
}
