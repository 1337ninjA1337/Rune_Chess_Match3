using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    public enum RuneType
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        White
    }

    public static class RuneTypes
    {
        public const string RedId = "red";
        public const string BlueId = "blue";
        public const string GreenId = "green";
        public const string YellowId = "yellow";
        public const string PurpleId = "purple";
        public const string WhiteId = "white";

        public static IReadOnlyList<RuneType> All { get; } = Array.AsReadOnly(new[]
        {
            RuneType.Red,
            RuneType.Blue,
            RuneType.Green,
            RuneType.Yellow,
            RuneType.Purple,
            RuneType.White
        });

        public static string GetId(RuneType rune)
        {
            return rune switch
            {
                RuneType.Red => RedId,
                RuneType.Blue => BlueId,
                RuneType.Green => GreenId,
                RuneType.Yellow => YellowId,
                RuneType.Purple => PurpleId,
                RuneType.White => WhiteId,
                _ => throw new ArgumentOutOfRangeException(nameof(rune), rune, "Unknown rune type.")
            };
        }

        public static RuneType ParseId(string id)
        {
            if (TryParseId(id, out var rune))
            {
                return rune;
            }

            throw new ArgumentException($"Unknown rune id '{id}'.", nameof(id));
        }

        public static bool TryParseId(string? id, out RuneType rune)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                rune = default;
                return false;
            }

            var normalizedId = id.Trim();
            if (normalizedId.Equals(RedId, StringComparison.OrdinalIgnoreCase))
            {
                rune = RuneType.Red;
                return true;
            }

            if (normalizedId.Equals(BlueId, StringComparison.OrdinalIgnoreCase))
            {
                rune = RuneType.Blue;
                return true;
            }

            if (normalizedId.Equals(GreenId, StringComparison.OrdinalIgnoreCase))
            {
                rune = RuneType.Green;
                return true;
            }

            if (normalizedId.Equals(YellowId, StringComparison.OrdinalIgnoreCase))
            {
                rune = RuneType.Yellow;
                return true;
            }

            if (normalizedId.Equals(PurpleId, StringComparison.OrdinalIgnoreCase))
            {
                rune = RuneType.Purple;
                return true;
            }

            if (normalizedId.Equals(WhiteId, StringComparison.OrdinalIgnoreCase))
            {
                rune = RuneType.White;
                return true;
            }

            rune = default;
            return false;
        }
    }
}
