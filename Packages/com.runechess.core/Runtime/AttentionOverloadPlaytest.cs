using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// One scripted moment of a quick attention-overload playtest: a human-readable name plus the
    /// set of <see cref="BattleCue"/>s the two combat zones fire at the same instant. A scenario
    /// stands in for a representative beat a tester would actually try to read — a calm tick, one
    /// big combo, both zones flashing together, a heavy pileup — so the playtest is a reproducible,
    /// deterministic script rather than a one-off manual session.
    /// </summary>
    public sealed record AttentionOverloadScenario(string Name, IReadOnlyList<BattleCue> Cues);

    /// <summary>
    /// The verdict for one scripted scenario: whether the readability/attention/pacing stack kept
    /// the screen readable, plus the observed values a tester would jot down (cues surfaced, focus
    /// zone, recommended speed and slowdown) and a short reason when it fails.
    /// </summary>
    public sealed record AttentionOverloadResult(
        string Scenario,
        bool Passed,
        int CuesFired,
        int CuesShown,
        BattleZone? Focus,
        int RecommendedSpeedPercent,
        int RecommendedSlowdownMs,
        bool PlayerFallingBehind,
        string Reason);

    /// <summary>
    /// A deterministic, scripted stand-in for the "быстрые игровые тесты на перегруз внимания"
    /// playtest task (codex/GDD: "Бой должен быть читаемым: не перегружай экран одновременными
    /// эффектами"; "Проверить, что UI не перегружает внимание игрока"). It drives the existing
    /// readability stack — <see cref="BattleAttentionModel"/> (shared cross-zone emphasis budget,
    /// focus, off-focus dimming) and <see cref="BattlePacingModel"/> (adaptive slowdown when the
    /// player falls behind) — through a curated set of representative combat moments and asserts the
    /// screen stays readable under each: the on-screen cue count never exceeds the shared budget, a
    /// focus zone is always chosen when anything is happening, the off-focus zone dims under a
    /// fight-swinging beat, and the clock eases (without crawling to a halt) exactly when more
    /// must-watch beats arrive than the player can track. Pure data so the "playtest" runs as a
    /// regression suite; the live human attention-overload session on device remains the documented
    /// verification gap (no Unity runtime in this environment).
    /// </summary>
    public static class AttentionOverloadPlaytest
    {
        /// <summary>A quiet support tick on the rune board — the calm baseline a tester reads with ease.</summary>
        private static BattleCue MinorMatch3 =>
            new(BattleZone.Match3, BattleEventSalience.Minor, ChainDepth: 0, Power: 4.0, Label: "Малый эффект руны");

        /// <summary>A single escalating combo — the one beat the clock already slows for.</summary>
        private static BattleCue BigComboMatch3 =>
            new(BattleZone.Match3, BattleEventSalience.Critical, ChainDepth: 3, Power: 30.0, Label: "Великая руна");

        private static BattleCue AutoMajor(string label = "Способность героя") =>
            BattleCue.AutoBattle(BattleEventSalience.Major, power: 18.0, label: label);

        private static BattleCue AutoCritical(string label = "Гибель союзника") =>
            BattleCue.AutoBattle(BattleEventSalience.Critical, power: 40.0, label: label);

        private static IReadOnlyList<BattleCue> Repeat(BattleCue cue, int count) =>
            Enumerable.Range(0, count).Select(_ => cue).ToList();

        /// <summary>
        /// The curated quick-test moments, ordered from calm to extreme overload. Each is a beat a
        /// tester would deliberately set up to see whether the screen stays legible.
        /// </summary>
        public static IReadOnlyList<AttentionOverloadScenario> Scenarios { get; } = Array.AsReadOnly(new[]
        {
            new AttentionOverloadScenario(
                "Спокойный тик (один малый эффект)",
                new[] { MinorMatch3 }),
            new AttentionOverloadScenario(
                "Одно крупное комбо во время автобоя",
                new[] { AutoMajor(), BigComboMatch3 }),
            new AttentionOverloadScenario(
                "Обе зоны вспыхивают одновременно",
                new[] { AutoCritical(), BigComboMatch3, MinorMatch3 }),
            new AttentionOverloadScenario(
                "Стена малых эффектов (не must-watch)",
                Repeat(MinorMatch3, 10)),
            new AttentionOverloadScenario(
                "Тяжёлый завал must-watch событий",
                Repeat(AutoMajor("Залп атак"), BattlePacingModel.TrackableEventBudget
                    + BattlePacingModel.HeavyOverloadExcess)),
            new AttentionOverloadScenario(
                "Экстремальный завал",
                Repeat(AutoMajor("Залп атак"), BattlePacingModel.TrackableEventBudget
                    + BattlePacingModel.HeavyOverloadExcess + 6))
        });

        /// <summary>
        /// Run one scripted scenario through the readability stack and report what a tester would
        /// observe. A scenario passes when every readability guarantee holds for that beat.
        /// </summary>
        public static AttentionOverloadResult Run(AttentionOverloadScenario scenario)
        {
            if (scenario is null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            var cues = scenario.Cues;
            var shown = BattleAttentionModel.SelectVisibleCues(cues);
            var focus = BattleAttentionModel.PrimaryFocus(cues);
            var speed = BattlePacingModel.RecommendedSpeedPercent(cues);
            var slowdown = BattlePacingModel.RecommendedSlowdownMilliseconds(cues);
            var fallingBehind = BattlePacingModel.IsPlayerFallingBehind(cues);

            var reason = Diagnose(cues, shown, focus, speed, slowdown, fallingBehind);

            return new AttentionOverloadResult(
                Scenario: scenario.Name,
                Passed: reason is null,
                CuesFired: cues.Count,
                CuesShown: shown.Count,
                Focus: focus,
                RecommendedSpeedPercent: speed,
                RecommendedSlowdownMs: slowdown,
                PlayerFallingBehind: fallingBehind,
                Reason: reason ?? "Экран читаем.");
        }

        /// <summary>Run every scripted scenario, in order.</summary>
        public static IReadOnlyList<AttentionOverloadResult> RunAll() =>
            Scenarios.Select(Run).ToList();

        /// <summary>True when every scripted scenario keeps the screen readable — the playtest's overall verdict.</summary>
        public static bool AllReadable() => RunAll().All(result => result.Passed);

        /// <summary>
        /// Check every readability guarantee for one beat. Returns <c>null</c> when the beat is
        /// readable, otherwise a short reason describing the first guarantee that broke.
        /// </summary>
        private static string? Diagnose(
            IReadOnlyList<BattleCue> cues,
            IReadOnlyList<BattleCue> shown,
            BattleZone? focus,
            int speed,
            int slowdown,
            bool fallingBehind)
        {
            // The screen never emphasises more than the shared cross-zone budget at once.
            if (!BattleAttentionModel.RespectsAttentionBudget(shown))
            {
                return $"показано {shown.Count} эффектов сверх бюджета {BattleAttentionModel.MaxSimultaneousCues}";
            }

            // Anything on screen must have a focus zone so the player's eye is led to one place.
            if (cues.Count > 0 && focus is null)
            {
                return "есть события, но фокус-зона не выбрана";
            }

            if (cues.Count == 0 && focus is not null)
            {
                return "пустой экран не должен иметь фокус-зону";
            }

            // A fight-swinging beat in the focus zone dims the other zone so two critical flashes
            // never compete for the same glance.
            var focusHasCritical = focus.HasValue
                && cues.Any(cue => cue.Zone == focus.Value && cue.Salience == BattleEventSalience.Critical);
            if (focusHasCritical != BattleAttentionModel.ShouldDimOffFocusZone(cues))
            {
                return "решение о затемнении второй зоны расходится с критическим событием в фокусе";
            }

            // Pacing tier must match the load: normal within budget, slower when falling behind,
            // never below the overloaded floor.
            if (fallingBehind)
            {
                if (speed >= CombatState.NormalCombatSpeedPercent)
                {
                    return "игрок не успевает, но бой идёт на полной скорости";
                }

                if (speed < BattlePacingModel.OverloadedCombatSpeedPercent)
                {
                    return $"скорость {speed}% ниже допустимого минимума {BattlePacingModel.OverloadedCombatSpeedPercent}%";
                }

                if (slowdown <= 0)
                {
                    return "перегруз внимания, но окно замедления не назначено";
                }

                if (slowdown > BattlePacingModel.MaxAdaptiveSlowdownMilliseconds)
                {
                    return $"окно замедления {slowdown}мс превышает потолок {BattlePacingModel.MaxAdaptiveSlowdownMilliseconds}мс";
                }
            }
            else
            {
                if (speed != CombatState.NormalCombatSpeedPercent)
                {
                    return $"в пределах бюджета бой должен идти на 100%, а идёт на {speed}%";
                }

                if (slowdown != 0)
                {
                    return "в пределах бюджета адаптивного замедления быть не должно";
                }
            }

            return null;
        }
    }
}
