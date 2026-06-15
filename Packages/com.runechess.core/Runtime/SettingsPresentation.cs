using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>The settings control a <see cref="SettingsRow"/> drives.</summary>
    public enum SettingsControl
    {
        Sound,
        Music,
        Vibration,
        Language,
        GraphicsQuality,
        BattleSpeed,
        ResetTutorial
    }

    /// <summary>How a settings row is rendered: an on/off toggle, a cycling option, or an action button.</summary>
    public enum SettingsRowKind
    {
        Toggle,
        Option,
        Action
    }

    /// <summary>
    /// One rendered row on the settings screen: which control it drives, its label, the current
    /// value text and the row kind. <see cref="IsOn"/> is only meaningful for <see cref="SettingsRowKind.Toggle"/>
    /// rows (off for options and actions). Pure data so the Unity layer lays out the row without
    /// re-deriving any value text.
    /// </summary>
    public sealed record SettingsRow(
        SettingsControl Control,
        string Label,
        string ValueLabel,
        SettingsRowKind Kind,
        bool IsOn);

    /// <summary>
    /// Engine-agnostic presentation glue for the settings screen overhaul (GDD UI screen 10
    /// «Настройки»). The <see cref="SettingsModel"/> already holds the state and the toggle/cycle
    /// transitions; this presentation says HOW the screen reads in the new portrait visual language
    /// by flattening the seven controls (sound, music, vibration, language, graphics quality, battle
    /// speed, reset tutorial) into one ordered, uniformly-rendered row list with readable Russian
    /// value text, and tying an enabled toggle to the green rune token (the shared «on/positive»
    /// accent). Rendering itself is Unity-only (documented gap); the contract is verified headless by
    /// <c>tools/CoreSmoke</c>. All art direction is original (codex.md IP rule).
    /// </summary>
    public static class SettingsPresentation
    {
        /// <summary>Accent for an enabled toggle (the shared green «on/positive» rune token).</summary>
        public static uint ToggleOnColor => UiTheme.RuneColor(RuneType.Green);

        /// <summary>Readable value text for the «вкл/выкл» state of a toggle.</summary>
        public static string OnOffLabel(bool on) => on ? "Вкл" : "Выкл";

        /// <summary>Readable value text for the UI language.</summary>
        public static string LanguageLabel(SettingsLanguage language)
        {
            return language switch
            {
                SettingsLanguage.Russian => "Русский",
                SettingsLanguage.English => "English",
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language.")
            };
        }

        /// <summary>Readable value text for the graphics quality.</summary>
        public static string GraphicsQualityLabel(GraphicsQuality quality)
        {
            return quality switch
            {
                GraphicsQuality.Low => "Низкое",
                GraphicsQuality.Medium => "Среднее",
                GraphicsQuality.High => "Высокое",
                _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Unknown graphics quality.")
            };
        }

        /// <summary>Readable value text for the battle speed (e.g. «x1.0»).</summary>
        public static string BattleSpeedLabel(BattleSpeed speed) => BattleSpeedOptions.Label(speed);

        /// <summary>
        /// The settings rows in reading order: the three audio/haptic toggles, the language, graphics
        /// and battle-speed options, then the reset-tutorial action. Value text comes straight from
        /// the <paramref name="model"/> so the view never re-derives it.
        /// </summary>
        public static IReadOnlyList<SettingsRow> Rows(SettingsModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return new[]
            {
                new SettingsRow(SettingsControl.Sound, "Звук", OnOffLabel(model.SoundEnabled), SettingsRowKind.Toggle, model.SoundEnabled),
                new SettingsRow(SettingsControl.Music, "Музыка", OnOffLabel(model.MusicEnabled), SettingsRowKind.Toggle, model.MusicEnabled),
                new SettingsRow(SettingsControl.Vibration, "Вибрация", OnOffLabel(model.VibrationEnabled), SettingsRowKind.Toggle, model.VibrationEnabled),
                new SettingsRow(SettingsControl.Language, "Язык", LanguageLabel(model.Language), SettingsRowKind.Option, false),
                new SettingsRow(SettingsControl.GraphicsQuality, "Качество графики", GraphicsQualityLabel(model.GraphicsQuality), SettingsRowKind.Option, false),
                new SettingsRow(SettingsControl.BattleSpeed, "Скорость боя", BattleSpeedLabel(model.BattleSpeed), SettingsRowKind.Option, false),
                new SettingsRow(SettingsControl.ResetTutorial, "Сброс обучения", model.TutorialCompleted ? "Пройдено" : "Не пройдено", SettingsRowKind.Action, false)
            };
        }
    }
}
