using System;
using System.Globalization;

namespace RuneChess.Core
{
    /// <summary>
    /// Single source of truth for the <see cref="BattleSpeed"/> tiers: the simulation-tick
    /// multiplier each speed applies, its compact label, and the cycle order. Both the settings
    /// screen (GDD UI screen 10 "скорость боя") and the in-combat speed-up button (GDD "кнопка
    /// ускорения боя") read these so the two controls never drift apart. Faster speeds are a
    /// later-version convenience upgrade; the multiplier only changes how fast the autobattle
    /// plays, never any combat outcome, so it is purely a pacing choice. Pure data so the tiers
    /// can be smoke-tested without Unity.
    /// </summary>
    public static class BattleSpeedOptions
    {
        /// <summary>Tick multiplier for normal (1x) battle speed.</summary>
        public const double NormalMultiplier = 1.0;

        /// <summary>Tick multiplier for the fast (later-upgrade) battle speed.</summary>
        public const double FastMultiplier = 1.5;

        /// <summary>The simulation-tick multiplier a battle speed applies.</summary>
        public static double Multiplier(BattleSpeed speed) => speed switch
        {
            BattleSpeed.Normal => NormalMultiplier,
            BattleSpeed.Fast => FastMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, "Unknown battle speed.")
        };

        /// <summary>A compact button/label for a battle speed, e.g. "x1.0" or "x1.5".</summary>
        public static string Label(BattleSpeed speed) =>
            "x" + Multiplier(speed).ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>The next battle speed in the toggle cycle (Normal &lt;-&gt; Fast for the MVP).</summary>
        public static BattleSpeed Next(BattleSpeed speed) => speed switch
        {
            BattleSpeed.Normal => BattleSpeed.Fast,
            BattleSpeed.Fast => BattleSpeed.Normal,
            _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, "Unknown battle speed.")
        };

        /// <summary>True when the speed runs the autobattle faster than normal.</summary>
        public static bool IsSpedUp(BattleSpeed speed) => Multiplier(speed) > NormalMultiplier;
    }
}
