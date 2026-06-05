using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One artifact the reward screen can offer as a "выбор одного из трёх" choice
    /// (GDD UI screen "Экран награды"). It carries the identity and a short Russian
    /// description so the screen renders a meaningful card. The full balance model
    /// (rarity, effect, trigger) lives on <see cref="ArtifactDefinition"/>; this is the
    /// flattened card the screen consumes. Picking an option stores it on the run via
    /// <see cref="ToArtifactState"/>.
    /// </summary>
    public sealed record RewardArtifactOption(string Id, string Name, string Description, bool IsRare = false)
    {
        /// <summary>Convert the chosen option into the run's stored artifact record.</summary>
        public ArtifactState ToArtifactState() => new(Id, Name);
    }

    /// <summary>
    /// MVP catalog of artifacts. Each entry is a full <see cref="ArtifactDefinition"/>
    /// (id, name, rarity, effect, trigger, description). Rounds that grant an artifact
    /// present a deterministic set of three choices from the matching pool: ordinary
    /// rounds draw from the common pool, rare-artifact rounds (the GDD boss) draw from
    /// the rare pool. Determinism keeps the reward screen smoke-testable and
    /// reproducible per round seed.
    /// </summary>
    public static class ArtifactCatalog
    {
        /// <summary>Number of choices offered on an artifact reward screen.</summary>
        public const int OfferCount = 3;

        private static readonly IReadOnlyList<ArtifactDefinition> Catalog = new List<ArtifactDefinition>
        {
            // Common pool: ordinary reward rounds.
            new("blood_chalice", "Кровавый Кубок", ArtifactRarity.Common, ArtifactEffectKind.Rune, ArtifactTrigger.OnRuneMatch,
                "Лечение от зелёных рун усиливается."),
            new("iron_banner", "Железное Знамя", ArtifactRarity.Common, ArtifactEffectKind.Combat, ArtifactTrigger.CombatStart,
                "Передняя линия получает дополнительную броню."),
            new("spark_capacitor", "Искровой Конденсатор", ArtifactRarity.Common, ArtifactEffectKind.Rune, ArtifactTrigger.OnRuneMatch,
                "Синие руны дают больше маны."),
            new("hunters_mark", "Метка Охотника", ArtifactRarity.Common, ArtifactEffectKind.Combat, ArtifactTrigger.Passive,
                "Стрелки бьют сильнее по задней линии."),
            new("warding_totem", "Оберегающий Тотем", ArtifactRarity.Common, ArtifactEffectKind.Rune, ArtifactTrigger.OnRuneMatch,
                "Жёлтые руны дают больший щит."),
            new("ember_core", "Тлеющее Ядро", ArtifactRarity.Common, ArtifactEffectKind.Rune, ArtifactTrigger.OnRuneMatch,
                "Красные руны добавляют немного физического урона."),
            new("merchant_seal", "Печать Торговца", ArtifactRarity.Common, ArtifactEffectKind.Economy, ArtifactTrigger.RoundEnd,
                "После каждого боя приносит +1 золото."),
            new("apprentice_tome", "Том Ученика", ArtifactRarity.Common, ArtifactEffectKind.Economy, ArtifactTrigger.Passive,
                "Покупка опыта стоит на 1 золото меньше."),

            // Rare pool: elite, boss and other rare-artifact rounds.
            new("crown_of_command", "Венец Командования", ArtifactRarity.Rare, ArtifactEffectKind.Combat, ArtifactTrigger.Passive,
                "Командир набирает энергию быстрее."),
            new("clockwork_heart", "Заводное Сердце", ArtifactRarity.Rare, ArtifactEffectKind.Combat, ArtifactTrigger.CombatStart,
                "Призванные юниты живут дольше."),
            new("swift_boots", "Сапоги Скорости", ArtifactRarity.Rare, ArtifactEffectKind.Combat, ArtifactTrigger.CombatStart,
                "Союзники получают +5% скорости атаки."),
            new("greedy_idol", "Жадный Идол", ArtifactRarity.Rare, ArtifactEffectKind.Economy, ArtifactTrigger.RoundEnd,
                "Процентный доход даёт на 1 золото больше."),
            new("chain_conduit", "Проводник Цепей", ArtifactRarity.Rare, ArtifactEffectKind.Rune, ArtifactTrigger.OnChainReaction,
                "Цепные реакции дают больший бонус."),
            new("abyssal_sigil", "Печать Бездны", ArtifactRarity.Epic, ArtifactEffectKind.Rune, ArtifactTrigger.OnRuneMatch,
                "Фиолетовые руны накладывают усиленный дебафф."),
            new("prism_lens", "Призменная Линза", ArtifactRarity.Epic, ArtifactEffectKind.Rune, ArtifactTrigger.OnRuneMatch,
                "Белые руны сильнее усиливают следующий цвет."),
            new("guardian_aegis", "Эгида Стража", ArtifactRarity.Epic, ArtifactEffectKind.Combat, ArtifactTrigger.CombatStart,
                "Щиты держатся дольше до первого урона."),
            new("phoenix_feather", "Перо Феникса", ArtifactRarity.Legendary, ArtifactEffectKind.Combat, ArtifactTrigger.OnAllyDeath,
                "Один павший герой возрождается раз за бой."),
            new("soul_harvest", "Жатва Душ", ArtifactRarity.Legendary, ArtifactEffectKind.Combat, ArtifactTrigger.Passive,
                "Убийства врагов восстанавливают здоровье союзникам.")
        };

        /// <summary>Every artifact definition, common pool first then rare.</summary>
        public static IReadOnlyList<ArtifactDefinition> All { get; } = Catalog
            .OrderBy(artifact => artifact.IsRare)
            .ThenBy(artifact => (int)artifact.Rarity)
            .ToList();

        private static readonly IReadOnlyList<ArtifactDefinition> CommonPool =
            All.Where(artifact => !artifact.IsRare).ToList();

        private static readonly IReadOnlyList<ArtifactDefinition> RarePool =
            All.Where(artifact => artifact.IsRare).ToList();

        /// <summary>Look up an artifact definition by id (case-insensitive).</summary>
        public static bool TryGet(string id, out ArtifactDefinition artifact)
        {
            artifact = All.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))!;
            return artifact is not null;
        }

        /// <summary>Look up an artifact definition by id, throwing when it is unknown.</summary>
        public static ArtifactDefinition Get(string id)
        {
            if (!TryGet(id, out var artifact))
            {
                throw new ArgumentException($"Unknown artifact id '{id}'.", nameof(id));
            }

            return artifact;
        }

        /// <summary>
        /// Build the deterministic three-option offer for a reward screen. The same
        /// <paramref name="seed"/> always yields the same three distinct options, so the
        /// choice is reproducible per round. <paramref name="rare"/> draws from the rare pool.
        /// </summary>
        public static IReadOnlyList<RewardArtifactOption> OfferThree(int seed, bool rare = false)
        {
            var pool = rare ? RarePool : CommonPool;
            if (pool.Count < OfferCount)
            {
                throw new InvalidOperationException("Artifact pool is too small to offer three distinct choices.");
            }

            return pool
                .Select((artifact, index) => (artifact, key: DeterministicKey(seed, index)))
                .OrderBy(entry => entry.key)
                .ThenBy(entry => entry.artifact.Id, StringComparer.Ordinal)
                .Take(OfferCount)
                .Select(entry => entry.artifact.ToRewardOption())
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
