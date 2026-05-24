using RuneChess.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RuneChess.Presentation;

public sealed class PortraitGameBootstrap : MonoBehaviour
{
    private static readonly string[] BenchNames = { "Iron Guard", "Oath Archer", "Field Medic" };
    private RunState runState = RunState.NewRun();

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    private void Start()
    {
        BuildPortraitGameSurface();
    }

    private void BuildPortraitGameSurface()
    {
        var canvas = CreateCanvas();
        var root = CreatePanel("Rune Chess Portrait Surface", canvas.transform, GameColors.Background);
        Stretch(root);

        var content = CreatePanel("Content", root.transform, Color.clear);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(390, 820);
        contentRect.anchoredPosition = Vector2.zero;

        var stack = content.AddComponent<VerticalLayoutGroup>();
        stack.spacing = 8;
        stack.padding = new RectOffset(16, 16, 16, 16);
        stack.childAlignment = TextAnchor.MiddleCenter;
        stack.childControlWidth = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;

        AddHeader(content.transform);
        AddSectionTitle(content.transform, "TACTICAL FIELD", $"{TacticalField.Mvp.Columns}x{TacticalField.Mvp.Rows}");
        AddTacticalGrid(content.transform);
        AddSectionTitle(content.transform, "RUNE BOARD", "7x7");
        AddRuneGrid(content.transform);
        AddActionPanel(content.transform);
    }

    private void AddHeader(Transform parent)
    {
        var header = CreatePanel("Run Header", parent, Color.clear);
        AddLayoutElement(header, 70);

        var horizontal = header.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = 8;
        horizontal.childAlignment = TextAnchor.MiddleCenter;
        horizontal.childControlWidth = true;
        horizontal.childForceExpandWidth = true;

        var titleBlock = CreatePanel("Title Block", header.transform, Color.clear);
        var titleStack = titleBlock.AddComponent<VerticalLayoutGroup>();
        titleStack.childAlignment = TextAnchor.MiddleLeft;
        titleStack.childForceExpandHeight = false;
        CreateText($"ROUND {runState.Round}", titleBlock.transform, 13, GameColors.Gold, TextAnchor.MiddleLeft);
        CreateText("Rune Chess", titleBlock.transform, 24, GameColors.Text, TextAnchor.MiddleLeft);

        var stats = CreatePanel("Stats", header.transform, Color.clear);
        var statsLayout = stats.AddComponent<HorizontalLayoutGroup>();
        statsLayout.spacing = 6;
        statsLayout.childControlWidth = false;
        statsLayout.childForceExpandWidth = false;

        CreateStat(stats.transform, "HP", runState.RunHealth.ToString(), GameColors.Health);
        CreateStat(stats.transform, "G", runState.Gold.ToString(), GameColors.Gold);
        CreateStat(stats.transform, "LV", runState.PlayerLevel.ToString(), GameColors.Mana);
    }

    private void AddSectionTitle(Transform parent, string title, string meta)
    {
        var row = CreatePanel($"{title} Header", parent, Color.clear);
        AddLayoutElement(row, 24);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        CreateText(title, row.transform, 14, GameColors.Text, TextAnchor.MiddleLeft);
        CreateText(meta, row.transform, 12, GameColors.Muted, TextAnchor.MiddleRight);
    }

    private void AddTacticalGrid(Transform parent)
    {
        var gridRoot = CreatePanel("Tactical Grid", parent, Color.clear);
        AddLayoutElement(gridRoot, 236);

        var grid = gridRoot.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = TacticalField.Mvp.Columns;
        grid.spacing = new Vector2(4, 4);
        grid.cellSize = new Vector2(55, 55);
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (var row = 0; row < TacticalField.Mvp.Rows; row += 1)
        {
            for (var column = 0; column < TacticalField.Mvp.Columns; column += 1)
            {
                var isEnemySide = row < 2;
                var cell = CreatePanel($"Cell {row}:{column}", gridRoot.transform, isEnemySide ? GameColors.EnemyCell : GameColors.PlayerCell);
                AddCellUnitIfNeeded(cell.transform, row, column);
            }
        }
    }

    private void AddRuneGrid(Transform parent)
    {
        var gridRoot = CreatePanel("Rune Grid", parent, Color.clear);
        AddLayoutElement(gridRoot, 354);

        var grid = gridRoot.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Match3Board.Columns;
        grid.spacing = new Vector2(4, 4);
        grid.cellSize = new Vector2(48, 48);
        grid.childAlignment = TextAnchor.MiddleCenter;

        var board = Match3Board.CreateDeterministic(1337);
        for (var row = 0; row < Match3Board.Rows; row += 1)
        {
            for (var column = 0; column < Match3Board.Columns; column += 1)
            {
                var rune = board[row, column];
                var tile = CreatePanel($"Rune {row}:{column}", gridRoot.transform, GameColors.RuneColor(rune));
                var core = CreatePanel("Core", tile.transform, new Color(1f, 1f, 1f, 0.32f));
                var coreRect = core.GetComponent<RectTransform>();
                coreRect.anchorMin = new Vector2(0.30f, 0.30f);
                coreRect.anchorMax = new Vector2(0.70f, 0.70f);
                coreRect.offsetMin = Vector2.zero;
                coreRect.offsetMax = Vector2.zero;
            }
        }
    }

    private void AddActionPanel(Transform parent)
    {
        var panel = CreatePanel("Action Panel", parent, GameColors.Panel);
        AddLayoutElement(panel, 104);

        var stack = panel.AddComponent<VerticalLayoutGroup>();
        stack.spacing = 8;
        stack.padding = new RectOffset(8, 8, 8, 8);

        var bench = CreatePanel("Bench", panel.transform, Color.clear);
        AddLayoutElement(bench, 34);
        var benchLayout = bench.AddComponent<HorizontalLayoutGroup>();
        benchLayout.spacing = 6;
        benchLayout.childControlWidth = true;
        benchLayout.childForceExpandWidth = true;

        foreach (var name in BenchNames)
        {
            var slot = CreatePanel(name, bench.transform, GameColors.PanelRaised);
            CreateText(name, slot.transform, 12, GameColors.Text, TextAnchor.MiddleCenter);
        }

        var actions = CreatePanel("Actions", panel.transform, Color.clear);
        AddLayoutElement(actions, 38);
        var actionLayout = actions.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 6;
        actionLayout.childControlWidth = true;
        actionLayout.childForceExpandWidth = true;

        foreach (var label in new[] { "Shop", "Reroll", "XP", "Fight" })
        {
            var color = label == "Fight" ? GameColors.Gold : GameColors.PanelRaised;
            var button = CreatePanel(label, actions.transform, color);
            CreateText(label, button.transform, 12, label == "Fight" ? GameColors.Background : GameColors.Text, TextAnchor.MiddleCenter);
        }
    }

    private void AddCellUnitIfNeeded(Transform parent, int row, int column)
    {
        var key = $"{row}:{column}";
        var unit = key switch
        {
            "0:1" => ("V", false),
            "0:4" => ("M", false),
            "1:2" => ("G", false),
            "2:1" => ("A", true),
            "2:4" => ("S", true),
            "3:2" => ("H", true),
            _ => default
        };

        if (unit == default)
        {
            return;
        }

        var fill = unit.Item2 ? new Color(0.18f, 0.44f, 0.36f) : new Color(0.50f, 0.23f, 0.29f);
        var marker = CreatePanel("Unit", parent, fill);
        var rect = marker.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.14f, 0.14f);
        rect.anchorMax = new Vector2(0.86f, 0.86f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        CreateText(unit.Item1, marker.transform, 16, GameColors.Text, TextAnchor.MiddleCenter);
    }

    private Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private Text CreateText(string value, Transform parent, int size, Color color, TextAnchor alignment)
    {
        var textObject = new GameObject($"Text {value}", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = alignment;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 8;
        text.resizeTextMaxSize = size;
        Stretch(textObject);
        return text;
    }

    private static void AddLayoutElement(GameObject gameObject, float preferredHeight)
    {
        var layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleWidth = 1f;
    }

    private static void Stretch(GameObject gameObject)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
