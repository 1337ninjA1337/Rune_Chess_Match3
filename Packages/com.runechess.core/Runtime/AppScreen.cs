namespace RuneChess.Core
{
    /// <summary>
    /// Application-level screens for the single-scene MVP navigation flow
    /// (GDD UI screens: main menu, commander select, level select, preparation,
    /// combat, per-level results, and the end-of-run summary).
    /// </summary>
    public enum AppScreen
    {
        MainMenu,
        CommanderSelect,
        LevelSelect,
        Preparation,
        Combat,
        LevelComplete,
        RunSummary,
        Collection,
        Settings
    }
}
