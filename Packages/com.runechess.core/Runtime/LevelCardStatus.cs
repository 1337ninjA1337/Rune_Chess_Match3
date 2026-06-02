namespace RuneChess.Core
{
    /// <summary>
    /// Progression status of a PvE round on the level-select screen
    /// (GDD: "карточки доступных PvE-уровней ... со статусом прохождения").
    /// </summary>
    public enum LevelCardStatus
    {
        Completed,
        Current,
        Locked
    }
}
