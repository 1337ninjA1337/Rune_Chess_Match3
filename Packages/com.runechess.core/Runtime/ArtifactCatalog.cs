using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One artifact the reward screen can offer as a "выбор одного из трёх" choice
    /// (GDD UI screen "Экран награды"). It carries the identity and a short Russian
    /// description so the screen renders a meaningful card; the full balance model
    /// (rarity, effect, trigger) is the separate P1 artifact task, so the runtime
    /// effect mechanics are intentionally not encoded here yet. Picking an option
    /// stores it on the run via <see cref="ToArtifactState"/>.
    /// </summary>
    public sealed record RewardArtifactOption(string Id, string Name, string Description, bool IsRare = false)
    {
        /// <summary>Convert the chosen option into the run's stored artifact record.</summary>
        public ArtifactState ToArtifactState() => new(Id, Name);
    }

    /// <summary>
    /// MVP catalog of artifact reward options. Rounds that grant an artifact present a
    /// deterministic set of three choices from this pool; rare-artifact rounds (the GDD
    /// boss) draw from the rare pool. Determinism keeps the reward screen smoke-testable
    /// and reproducible per round seed.
    /// </summary>
    public static class ArtifactCatalog
    {
        /// <summary>Number of choices offered on an artifact reward screen.</summary>
        public const int OfferCount = 3;

        private static readonly IReadOnlyList<RewardArtifactOption> CommonOptions = new List<RewardArtifactOption>
        {
            new("blood_chalice", "Кровавый Кубок", "Лечение от зелёных рун усиливается."),
            new("iron_banner", "Железное Знамя", "Передняя линия получает дополнительную броню."),
            new("spark_capacitor", "Искровой Конденсатор", "Синие руны дают больше маны."),
            new("hunters_mark", "Метка Охотника", "Стрелки бьют сильнее по задней линии."),
            new("warding_totem", "Оберегающий Тотем", "Жёлтые руны дают больший щит."),
            new("ember_core", "Тлеющее Ядро", "Красные руны добавляют немного физического урона.")
        };

        private static readonly IReadOnlyList<RewardArtifactOption> RareOptions = new List<RewardArtifactOption>
        {
            new("crown_of_command", "Венец Командования", "Командир набирает энергию быстрее.", IsRare: true),
            new("abyssal_sigil", "Печать Бездны", "Фиолетовые руны накладывают усиленный дебафф.", IsRare: true),
            new("clockwork_heart", "Заводное Сердце", "Призванные юниты живут дольше.", IsRare: true),
            new("phoenix_feather", "Перо Феникса", "Один павший герой возрождается раз за бой.", IsRare: true)
        };

        /// <summary>Every artifact option, common pool first then rare.</summary>
        public static IReadOnlyList<RewardArtifactOption> All { get; } =
            CommonOptions.Concat(RareOptions).ToList();

        /// <summary>Look up an artifact option by id (case-insensitive).</summary>
        public static bool TryGet(string id, out RewardArtifactOption option)
        {
            option = All.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))!;
            return option is not null;
        }

        /// <summary>Look up an artifact option by id, throwing when it is unknown.</summary>
        public static RewardArtifactOption Get(string id)
        {
            if (!TryGet(id, out var option))
            {
                throw new ArgumentException($"Unknown artifact id '{id}'.", nameof(id));
            }

            return option;
        }

        /// <summary>
        /// Build the deterministic three-option offer for a reward screen. The same
        /// <paramref name="seed"/> always yields the same three distinct options, so the
        /// choice is reproducible per round. <paramref name="rare"/> draws from the rare pool.
        /// </summary>
        public static IReadOnlyList<RewardArtifactOption> OfferThree(int seed, bool rare = false)
        {
            var pool = rare ? RareOptions : CommonOptions;
            if (pool.Count < OfferCount)
            {
                throw new InvalidOperationException("Artifact pool is too small to offer three distinct choices.");
            }

            return pool
                .Select((option, index) => (option, key: DeterministicKey(seed, index)))
                .OrderBy(entry => entry.key)
                .ThenBy(entry => entry.option.Id, StringComparer.Ordinal)
                .Take(OfferCount)
                .Select(entry => entry.option)
                .ToList();
        }

        /// <summary>Stable scrambling key so a seed shuffles the pool reproducibly.</summary>
        private static uint DeterministicKey(int seed, int index)
        {
            unchecked
            {
                var hash = ((uint)seed * 2654435761u) + ((uint)index * 40503u);
                hash ^= hash >> 13;
                hash *= 2246822519u;
                hash ^= hash >> 16;
                return hash;
            }
        }
    }
}
