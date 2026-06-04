using System;

namespace RuneChess.Core
{
    /// <summary>UI language options for the settings screen (GDD UI screen 10 "язык").</summary>
    public enum SettingsLanguage
    {
        Russian,
        English
    }

    /// <summary>Graphics quality options (GDD "качество графики").</summary>
    public enum GraphicsQuality
    {
        Low,
        Medium,
        High
    }

    /// <summary>Battle speed options (GDD "скорость боя"; faster speeds are a later upgrade).</summary>
    public enum BattleSpeed
    {
        Normal,
        Fast
    }

    /// <summary>
    /// User settings state for the settings screen (GDD UI screen 10 "Настройки":
    /// sound, music, vibration, language, graphics quality, battle speed, and a reset
    /// of the tutorial). Pure data with explicit transitions so the toggles can be
    /// smoke-tested without Unity; the presentation layer renders the controls.
    /// </summary>
    public sealed record SettingsModel(
        bool SoundEnabled,
        bool MusicEnabled,
        bool VibrationEnabled,
        SettingsLanguage Language,
        GraphicsQuality GraphicsQuality,
        BattleSpeed BattleSpeed,
        bool TutorialCompleted)
    {
        /// <summary>Default settings for a fresh install: everything on, Russian, medium quality.</summary>
        public static SettingsModel Default { get; } = new(
            SoundEnabled: true,
            MusicEnabled: true,
            VibrationEnabled: true,
            Language: SettingsLanguage.Russian,
            GraphicsQuality: GraphicsQuality.Medium,
            BattleSpeed: BattleSpeed.Normal,
            TutorialCompleted: false);

        /// <summary>Battle simulation speed multiplier applied to combat ticks.</summary>
        public double BattleSpeedMultiplier => BattleSpeed switch
        {
            BattleSpeed.Normal => 1.0,
            BattleSpeed.Fast => 1.5,
            _ => throw new ArgumentOutOfRangeException(nameof(BattleSpeed), BattleSpeed, "Unknown battle speed.")
        };

        public SettingsModel ToggleSound() => this with { SoundEnabled = !SoundEnabled };

        public SettingsModel ToggleMusic() => this with { MusicEnabled = !MusicEnabled };

        public SettingsModel ToggleVibration() => this with { VibrationEnabled = !VibrationEnabled };

        public SettingsModel WithLanguage(SettingsLanguage language) => this with { Language = language };

        public SettingsModel WithGraphicsQuality(GraphicsQuality quality) => this with { GraphicsQuality = quality };

        public SettingsModel WithBattleSpeed(BattleSpeed speed) => this with { BattleSpeed = speed };

        /// <summary>Cycle to the next language (Russian &lt;-&gt; English for the MVP).</summary>
        public SettingsModel CycleLanguage() => this with
        {
            Language = Language == SettingsLanguage.Russian ? SettingsLanguage.English : SettingsLanguage.Russian
        };

        /// <summary>Cycle graphics quality Low -&gt; Medium -&gt; High -&gt; Low.</summary>
        public SettingsModel CycleGraphicsQuality() => this with
        {
            GraphicsQuality = GraphicsQuality switch
            {
                GraphicsQuality.Low => GraphicsQuality.Medium,
                GraphicsQuality.Medium => GraphicsQuality.High,
                GraphicsQuality.High => GraphicsQuality.Low,
                _ => throw new ArgumentOutOfRangeException(nameof(GraphicsQuality), GraphicsQuality, "Unknown quality.")
            }
        };

        /// <summary>Toggle the battle speed between Normal and Fast.</summary>
        public SettingsModel CycleBattleSpeed() => this with
        {
            BattleSpeed = BattleSpeed == BattleSpeed.Normal ? BattleSpeed.Fast : BattleSpeed.Normal
        };

        /// <summary>Mark the tutorial as finished so onboarding hints stop appearing.</summary>
        public SettingsModel CompleteTutorial() => this with { TutorialCompleted = true };

        /// <summary>Reset onboarding so the tutorial plays again (GDD "сброс обучения").</summary>
        public SettingsModel ResetTutorial() => this with { TutorialCompleted = false };
    }
}
