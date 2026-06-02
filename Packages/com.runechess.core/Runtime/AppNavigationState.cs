using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Pure C# navigation model for the single-Unity-scene MVP. It tracks the
    /// current <see cref="AppScreen"/> and the legal transitions between screens
    /// so the presentation layer can switch screens without reloading the scene
    /// (GDD UI flow: menu -> commander/level select -> preparation -> combat ->
    /// level results -> next level or run summary).
    /// </summary>
    public sealed record AppNavigationState(AppScreen Current, AppScreen? Previous)
    {
        private static readonly IReadOnlyDictionary<AppScreen, IReadOnlyList<AppScreen>> Transitions =
            new Dictionary<AppScreen, IReadOnlyList<AppScreen>>
            {
                [AppScreen.MainMenu] = new[] { AppScreen.CommanderSelect, AppScreen.LevelSelect, AppScreen.Settings },
                [AppScreen.CommanderSelect] = new[] { AppScreen.MainMenu, AppScreen.LevelSelect },
                [AppScreen.LevelSelect] = new[] { AppScreen.Preparation, AppScreen.MainMenu },
                [AppScreen.Preparation] = new[] { AppScreen.Combat, AppScreen.LevelSelect },
                [AppScreen.Combat] = new[] { AppScreen.LevelComplete },
                [AppScreen.LevelComplete] = new[] { AppScreen.Preparation, AppScreen.RunSummary, AppScreen.LevelSelect },
                [AppScreen.RunSummary] = new[] { AppScreen.MainMenu, AppScreen.LevelSelect },
                [AppScreen.Settings] = new[] { AppScreen.MainMenu }
            };

        /// <summary>A fresh app session sitting on the main menu.</summary>
        public static AppNavigationState AtMainMenu { get; } = new(AppScreen.MainMenu, null);

        /// <summary>Legal screens reachable from <paramref name="screen"/>.</summary>
        public static IReadOnlyList<AppScreen> AllowedNext(AppScreen screen) =>
            Transitions.TryGetValue(screen, out var next) ? next : Array.Empty<AppScreen>();

        /// <summary>True when <paramref name="next"/> can be reached from the current screen.</summary>
        public bool CanNavigateTo(AppScreen next) => AllowedNext(Current).Contains(next);

        /// <summary>Switch to a legal next screen, remembering the current one.</summary>
        public AppNavigationState NavigateTo(AppScreen next)
        {
            if (!CanNavigateTo(next))
            {
                throw new InvalidOperationException($"Cannot navigate from {Current} to {next}.");
            }

            return new AppNavigationState(next, Current);
        }

        /// <summary>Return to the previous screen if one was recorded.</summary>
        public AppNavigationState Back() =>
            Previous is { } previous ? new AppNavigationState(previous, null) : this;

        /// <summary>
        /// The screen that should be shown for an in-run phase, so a live run can
        /// drive the preparation, combat, results and summary screens.
        /// </summary>
        public static AppScreen ScreenForPhase(RunPhase phase) => phase switch
        {
            RunPhase.Preparation => AppScreen.Preparation,
            RunPhase.Combat => AppScreen.Combat,
            RunPhase.Reward => AppScreen.LevelComplete,
            RunPhase.Event => AppScreen.LevelComplete,
            RunPhase.Victory => AppScreen.RunSummary,
            RunPhase.Defeat => AppScreen.RunSummary,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown run phase.")
        };
    }
}
