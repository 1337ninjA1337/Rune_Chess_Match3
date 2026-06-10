using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Balance knobs for a <see cref="LivePvpMatch"/> (GDD "Будущие режимы": "Live PvP —
    /// Матч на 4 или 8 игроков. Игроки параллельно проходят раунды и теряют здоровье после
    /// поражений."). Kept as explicit data so the lobby size and starting health can be
    /// tuned without touching the match state machine (codex: "Все числовые значения
    /// баланса держи в явных конфигурациях").
    /// </summary>
    public sealed record LivePvpConfig(int StartingHealth)
    {
        /// <summary>The only lobby sizes the GDD allows for a Live PvP match.</summary>
        public static IReadOnlyList<int> ValidLobbySizes { get; } = new[] { 4, 8 };

        /// <summary>Default tuning: an eight-seat-capable lobby with 100 starting health.</summary>
        public static LivePvpConfig Default { get; } = new(StartingHealth: 100);

        public int StartingHealth { get; init; } = StartingHealth > 0
            ? StartingHealth
            : throw new ArgumentOutOfRangeException(
                nameof(StartingHealth), "Live PvP starting health must be positive.");

        /// <summary>A Live PvP match seats exactly 4 or 8 players — nothing in between.</summary>
        public static bool IsValidLobbySize(int size) => size == 4 || size == 8;
    }
}
