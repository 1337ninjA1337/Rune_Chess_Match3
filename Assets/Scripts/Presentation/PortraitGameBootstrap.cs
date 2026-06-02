using System;
using System.Collections;
using System.Collections.Generic;
using RuneChess.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuneChess.Presentation
{
    public sealed class PortraitGameBootstrap : MonoBehaviour
    {
        private const float ContentWidth = 390f;
        private const float ContentHeight = 844f;
        private const float RuneTileSize = 34f;
        private const float RuneTileSpacing = 3f;
        private const float SwapAnimationSeconds = 0.16f;
        private const float MatchFadeSeconds = 0.12f;
        private const float DropAnimationSeconds = 0.22f;

        private static readonly UnitVisual[] Units =
        {
            new UnitVisual("V", "Vanguard", "Tank", 0, 1, false, 0.72f, 0.28f, GameColors.EnemyCellOccupied),
            new UnitVisual("M", "Mystic", "Caster", 0, 4, false, 0.54f, 0.76f, GameColors.Commander),
            new UnitVisual("G", "Guard", "Bruiser", 1, 2, false, 0.88f, 0.18f, GameColors.Health),
            new UnitVisual("A", "Archer", "Carry", 2, 1, true, 0.68f, 0.62f, GameColors.Gold),
            new UnitVisual("S", "Sentinel", "Tank", 2, 4, true, 0.93f, 0.22f, GameColors.AllyCellOccupied),
            new UnitVisual("H", "Healer", "Support", 3, 2, true, 0.81f, 0.84f, GameColors.Heal)
        };

        private static readonly UnitVisual[] Bench =
        {
            new UnitVisual("IG", "Iron Guard", "Tank", 0, 0, true, 1f, 0.12f, GameColors.AllyCellOccupied),
            new UnitVisual("OA", "Oath Archer", "Carry", 0, 0, true, 1f, 0.42f, GameColors.Health),
            new UnitVisual("FM", "Field Medic", "Heal", 0, 0, true, 1f, 0.55f, GameColors.Heal)
        };

        private static readonly UnitVisual[] Shop =
        {
            new UnitVisual("WC", "Wild Claw", "Bruiser", 0, 0, true, 1f, 0f, GameColors.Health),
            new UnitVisual("TS", "Thorn Shaman", "Summon", 0, 0, true, 1f, 0f, GameColors.Heal),
            new UnitVisual("MC", "Mist Cut", "Assassin", 0, 0, true, 1f, 0f, GameColors.Commander)
        };

        private RunState runState = RunState.NewRun();
        private Match3Board runeBoard;
        private BoardPoint? selectedRune;
        private Match3MoveHint currentHint;
        private Transform runeGridRoot;
        private Transform contentRoot;
        private Text runeMetaText;
        private readonly Dictionary<BoardPoint, RuneTileView> runeTiles = new Dictionary<BoardPoint, RuneTileView>();
        private int runeSeed = 1337;
        private int runeMovesUsed;
        private int runeScore;
        private string runeStatus = "READY";
        private bool isRuneAnimationRunning;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private void Start()
        {
            EnsureMainCamera();
            EnsureEventSystem();
            BuildPortraitGameSurface();
        }

        private static void EnsureMainCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                camera = cameraObject.AddComponent<Camera>();
            }
            else if (!camera.CompareTag("MainCamera"))
            {
                camera.tag = "MainCamera";
            }

            camera.enabled = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GameColors.Background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void ResetMatch3Board()
        {
            runeBoard = CreatePlayableBoard(runeSeed);
            selectedRune = null;
            currentHint = runeBoard.FindFirstLegalMoveHint();
            runeMovesUsed = 0;
            runeScore = 0;
            runeStatus = "READY";
        }

        private static Match3Board CreatePlayableBoard(int seed)
        {
            for (var offset = 0; offset < 200; offset += 1)
            {
                var candidateSeed = unchecked(seed + offset);
                var candidate = Match3Board.CreateDeterministic(candidateSeed);

                try
                {
                    var stable = candidate.ResolveChainReactions(unchecked(candidateSeed + 1000), 16).Board;
                    if (stable.FindFirstLegalMoveHint() != null)
                    {
                        return stable;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Try another deterministic seed if an unlucky board chains too deeply.
                }
            }

            return Match3Board.CreateDeterministic(seed);
        }

        private void BuildPortraitGameSurface()
        {
            var canvas = CreateCanvas();
            var root = CreatePanel("Rune Chess Game Surface", canvas.transform, GameColors.Background);
            Stretch(root);

            var content = CreatePanel("Portrait Game Frame", root.transform, GameColors.Frame);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(ContentWidth, ContentHeight);
            contentRect.anchoredPosition = Vector2.zero;

            var stack = content.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(10, 10, 10, 10);
            stack.spacing = 6;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            contentRoot = content.transform;
            AddMainMenu(contentRoot);
        }

        private void AddMainMenu(Transform parent)
        {
            var button = CreatePanel("Start Game Button", parent, GameColors.ButtonPrimary);
            AddLayoutElement(button, 56);
            AddOutline(button, GameColors.WithAlpha(GameColors.Text, 0.24f));

            var image = button.GetComponent<Image>();
            image.raycastTarget = true;

            var action = button.AddComponent<Button>();
            action.targetGraphic = image;
            action.onClick.AddListener(StartGame);

            CreateOverlayText("Начать игру", button.transform, 18, GameColors.Background, TextAnchor.MiddleCenter);
        }

        private void StartGame()
        {
            if (contentRoot == null)
            {
                return;
            }

            ClearChildren(contentRoot);
            ResetMatch3Board();
            AddRunePanel(contentRoot);
        }

        private void AddHeader(Transform parent)
        {
            var header = CreatePanel("Run Header", parent, GameColors.Panel);
            AddLayoutElement(header, 60);
            AddOutline(header, GameColors.Border);

            var row = header.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 6, 6);
            row.spacing = 8;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childForceExpandWidth = true;

            var title = CreatePanel("Title", header.transform, Color.clear);
            var titleLayout = title.AddComponent<VerticalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childForceExpandHeight = false;
            CreateText("RUNE CHESS", title.transform, 19, GameColors.Text, TextAnchor.MiddleLeft);
            CreateText($"ROUND {runState.Round}   NEXT {runState.NextEnemyId}", title.transform, 10, GameColors.Muted, TextAnchor.MiddleLeft);

            var stats = CreatePanel("Run Stats", header.transform, Color.clear);
            var statsLayout = stats.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 5;
            statsLayout.childAlignment = TextAnchor.MiddleRight;
            statsLayout.childControlWidth = false;
            statsLayout.childForceExpandWidth = false;

            CreateStat(stats.transform, "HP", runState.RunHealth.ToString(), GameColors.Health);
            CreateStat(stats.transform, "G", runState.Gold.ToString(), GameColors.Gold);
            CreateStat(stats.transform, "LV", runState.PlayerLevel.ToString(), GameColors.Mana);
        }

        private void AddBattlePanel(Transform parent)
        {
            var panel = CreatePanel("Battle Panel", parent, GameColors.PanelDeep);
            AddLayoutElement(panel, 218);
            AddOutline(panel, GameColors.Border);

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 7, 7);
            stack.spacing = 6;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddPanelHeader(panel.transform, "TACTICAL FIELD", "6x4 AUTO BATTLE");
            AddTacticalGrid(panel.transform);
            AddBattleTelemetry(panel.transform);
        }

        private void AddTacticalGrid(Transform parent)
        {
            var gridRoot = CreatePanel("Tactical Grid", parent, Color.clear);
            AddLayoutElement(gridRoot, 150);

            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = TacticalField.Mvp.Columns;
            grid.spacing = new Vector2(3, 3);
            grid.cellSize = new Vector2(57, 36);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var row = 0; row < TacticalField.Mvp.Rows; row += 1)
            {
                for (var column = 0; column < TacticalField.Mvp.Columns; column += 1)
                {
                    var position = new TacticalPosition(row, column);
                    var state = GetDemoCellState(position);
                    var cell = CreatePanel($"Cell {row}:{column}", gridRoot.transform, GameColors.TacticalCellColor(state));
                    AddOutline(cell, GameColors.WithAlpha(GameColors.Border, 0.45f));
                    AddCellLaneMarker(cell.transform, position);

                    UnitVisual unit;
                    if (TryGetUnitAt(row, column, out unit))
                    {
                        AddUnitToken(cell.transform, unit);
                    }
                }
            }
        }

        private void AddBattleTelemetry(Transform parent)
        {
            var row = CreatePanel("Battle Telemetry", parent, Color.clear);
            AddLayoutElement(row, 24);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateStatusPill(row.transform, "ALLY", "3 UNITS", GameColors.AllyCellOccupied);
            CreateStatusPill(row.transform, "CHAIN", "x2 READY", GameColors.Commander);
            CreateStatusPill(row.transform, "ENEMY", "3 UNITS", GameColors.EnemyCellOccupied);
        }

        private void AddRunePanel(Transform parent)
        {
            var panel = CreatePanel("Rune Panel", parent, GameColors.Panel);
            AddLayoutElement(panel, 326);
            AddOutline(panel, GameColors.Border);

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 7, 7);
            stack.spacing = 5;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            runeMetaText = AddPanelHeader(panel.transform, "RUNE BOARD", BuildRuneMeta());
            AddRuneGrid(panel.transform);
        }

        private void AddRuneEffectStrip(Transform parent)
        {
            var strip = CreatePanel("Rune Effect Strip", parent, Color.clear);
            AddLayoutElement(strip, 22);

            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateRuneChip(strip.transform, RuneType.Red, "DMG");
            CreateRuneChip(strip.transform, RuneType.Blue, "MANA");
            CreateRuneChip(strip.transform, RuneType.Green, "HEAL");
            CreateRuneChip(strip.transform, RuneType.Yellow, "SHLD");
            CreateRuneChip(strip.transform, RuneType.Purple, "MAG");
            CreateRuneChip(strip.transform, RuneType.White, "CMD");
        }

        private void AddRuneGrid(Transform parent)
        {
            var gridRoot = CreatePanel("Rune Grid", parent, Color.clear);
            runeGridRoot = gridRoot.transform;
            gridRoot.GetComponent<Image>().raycastTarget = false;
            AddLayoutElement(gridRoot, 270);

            RebuildRuneGrid();
        }

        private void RebuildRuneGrid()
        {
            if (runeGridRoot == null)
            {
                return;
            }

            for (var index = runeGridRoot.childCount - 1; index >= 0; index -= 1)
            {
                var child = runeGridRoot.GetChild(index);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            runeTiles.Clear();

            for (var row = 0; row < Match3Board.Rows; row += 1)
            {
                for (var column = 0; column < Match3Board.Columns; column += 1)
                {
                    var point = new BoardPoint(row, column);
                    var rune = runeBoard[point];
                    runeTiles[point] = CreateRuneTile(point, rune, GetRuneSlotPosition(point), true, true);
                }
            }
        }

        private RuneTileView CreateRuneTile(BoardPoint point, RuneType rune, Vector2 anchoredPosition, bool interactive, bool showHints)
        {
            var isSelected = showHints && IsSelectedRune(point);
            var isHint = showHints && IsHintCell(point);
            var isHintSwap = showHints && IsCurrentHintSwapCell(point);
            var isLegalTarget = showHints && IsLegalTargetForSelection(point);
            var tile = CreatePanel($"Rune {point.Row}:{point.Column}", runeGridRoot, GameColors.RuneColor(rune));
            var image = tile.GetComponent<Image>();
            image.raycastTarget = interactive;
            AddOutline(tile, GetRuneOutlineColor(isSelected, isLegalTarget, isHintSwap, isHint));

            var rect = tile.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(RuneTileSize, RuneTileSize);
            rect.anchoredPosition = anchoredPosition;

            if (interactive)
            {
                var button = tile.AddComponent<Button>();
                button.targetGraphic = image;
                var capturedPoint = point;
                button.onClick.AddListener(() => OnRuneClicked(capturedPoint));
            }

            var group = tile.AddComponent<CanvasGroup>();
            var core = CreatePanel("Rune Core", tile.transform, GameColors.WithAlpha(GameColors.Text, 0.24f));
            core.GetComponent<Image>().raycastTarget = false;
            SetAnchoredFill(core, isSelected ? 0.16f : 0.26f, isSelected ? 0.16f : 0.26f, isSelected ? 0.16f : 0.26f, isSelected ? 0.16f : 0.26f);
            CreateOverlayText(RuneLabel(rune), tile.transform, 12, rune == RuneType.White ? GameColors.Background : GameColors.Text, TextAnchor.MiddleCenter);

            if (isLegalTarget)
            {
                AddRuneMarker(tile.transform, "GO", GameColors.Gold);
            }
            else if (isHintSwap)
            {
                AddRuneMarker(tile.transform, "H", GameColors.Text);
            }

            return new RuneTileView(point, rune, tile, rect, group);
        }

        private void OnRuneClicked(BoardPoint point)
        {
            if (isRuneAnimationRunning)
            {
                return;
            }

            if (!selectedRune.HasValue)
            {
                SelectRune(point);
                return;
            }

            var first = selectedRune.Value;
            if (first == point)
            {
                selectedRune = null;
                runeStatus = "READY";
                RefreshRuneHud();
                return;
            }

            if (!Match3Board.CanSwap(first, point))
            {
                SelectRune(point);
                return;
            }

            if (!runeBoard.TryCreateMoveHint(first, point, out _))
            {
                selectedRune = null;
                runeStatus = "NO MATCH";
                RefreshRuneHud();
                return;
            }

            runeSeed = unchecked(runeSeed + 1);
            var swapped = runeBoard.Swap(first, point);
            var resolution = swapped.ResolveChainReactions(runeSeed, 16);
            StartCoroutine(AnimateRuneSwapAndResolve(first, point, swapped, resolution));
        }

        private IEnumerator AnimateRuneSwapAndResolve(BoardPoint first, BoardPoint second, Match3Board swapped, Match3ChainResolution resolution)
        {
            isRuneAnimationRunning = true;
            selectedRune = null;
            currentHint = null;
            runeStatus = "SWAP";
            UpdateRuneMeta();
            SetRuneButtonsInteractable(false);

            yield return AnimateSwap(first, second);

            runeBoard = swapped;
            RebuildRuneGrid();

            foreach (var step in resolution.Steps)
            {
                runeStatus = step.ChainNumber == 1 ? "MATCH" : $"CHAIN {step.ChainNumber}";
                UpdateRuneMeta();

                yield return AnimateMatchedCells(step.MatchedCells);
                yield return AnimateDrop(step.BoardAfterRemoval, step.BoardAfterDrop);

                runeBoard = step.BoardAfterDrop;
                RebuildRuneGrid();
            }

            runeMovesUsed += 1;
            runeScore += resolution.TotalMatchPower;
            runeStatus = $"+{resolution.TotalMatchPower}  {resolution.TotalMatchedRunesCount} RUNES";

            EnsureBoardStillPlayable();
            isRuneAnimationRunning = false;
            RefreshRuneHud();
        }

        private IEnumerator AnimateSwap(BoardPoint first, BoardPoint second)
        {
            RuneTileView firstTile;
            RuneTileView secondTile;
            if (!runeTiles.TryGetValue(first, out firstTile) || !runeTiles.TryGetValue(second, out secondTile))
            {
                yield break;
            }

            firstTile.GameObject.transform.SetAsLastSibling();
            secondTile.GameObject.transform.SetAsLastSibling();

            var firstMove = new RuneTileMove(firstTile, firstTile.Rect.anchoredPosition, GetRuneSlotPosition(second));
            var secondMove = new RuneTileMove(secondTile, secondTile.Rect.anchoredPosition, GetRuneSlotPosition(first));
            yield return AnimateTileMoves(new List<RuneTileMove> { firstMove, secondMove }, SwapAnimationSeconds);
        }

        private IEnumerator AnimateMatchedCells(IReadOnlyCollection<BoardPoint> matchedCells)
        {
            var matchedTiles = new List<RuneTileView>();
            foreach (var point in matchedCells)
            {
                RuneTileView tile;
                if (runeTiles.TryGetValue(point, out tile))
                {
                    matchedTiles.Add(tile);
                }
            }

            if (matchedTiles.Count == 0)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < MatchFadeSeconds)
            {
                var progress = Mathf.Clamp01(elapsed / MatchFadeSeconds);
                var eased = EaseOutCubic(progress);
                foreach (var tile in matchedTiles)
                {
                    tile.Group.alpha = 1f - eased;
                    tile.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, eased);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            foreach (var tile in matchedTiles)
            {
                runeTiles.Remove(tile.Point);
                Destroy(tile.GameObject);
            }
        }

        private IEnumerator AnimateDrop(Match3Board boardAfterRemoval, Match3Board boardAfterDrop)
        {
            var moves = new List<RuneTileMove>();

            for (var column = 0; column < Match3Board.Columns; column += 1)
            {
                var writeRow = Match3Board.Rows - 1;
                for (var readRow = Match3Board.Rows - 1; readRow >= 0; readRow -= 1)
                {
                    var readPoint = new BoardPoint(readRow, column);
                    if (!boardAfterRemoval.GetRuneOrEmpty(readPoint).HasValue)
                    {
                        continue;
                    }

                    RuneTileView existingTile;
                    if (runeTiles.TryGetValue(readPoint, out existingTile))
                    {
                        var targetPoint = new BoardPoint(writeRow, column);
                        moves.Add(new RuneTileMove(existingTile, existingTile.Rect.anchoredPosition, GetRuneSlotPosition(targetPoint)));
                    }

                    writeRow -= 1;
                }

                var spawnIndex = 0;
                for (; writeRow >= 0; writeRow -= 1)
                {
                    var targetPoint = new BoardPoint(writeRow, column);
                    var rune = boardAfterDrop[targetPoint];
                    var startPosition = GetRuneSlotPosition(-1 - spawnIndex, column);
                    var newTile = CreateRuneTile(targetPoint, rune, startPosition, false, false);
                    moves.Add(new RuneTileMove(newTile, startPosition, GetRuneSlotPosition(targetPoint)));
                    spawnIndex += 1;
                }
            }

            yield return AnimateTileMoves(moves, DropAnimationSeconds);
        }

        private IEnumerator AnimateTileMoves(IReadOnlyList<RuneTileMove> moves, float durationSeconds)
        {
            if (moves.Count == 0)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                var progress = Mathf.Clamp01(elapsed / durationSeconds);
                var eased = EaseOutCubic(progress);
                for (var index = 0; index < moves.Count; index += 1)
                {
                    var move = moves[index];
                    move.Tile.Rect.anchoredPosition = Vector2.LerpUnclamped(move.From, move.To, eased);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            for (var index = 0; index < moves.Count; index += 1)
            {
                var move = moves[index];
                move.Tile.Rect.anchoredPosition = move.To;
            }
        }

        private void SetRuneButtonsInteractable(bool interactable)
        {
            foreach (var tile in runeTiles.Values)
            {
                var button = tile.GameObject.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }

        private void SelectRune(BoardPoint point)
        {
            selectedRune = point;
            runeStatus = HasLegalTarget(point) ? $"SEL {FormatPoint(point)}" : "NO MOVE HERE";
            RefreshRuneHud();
        }

        private void EnsureBoardStillPlayable()
        {
            currentHint = runeBoard.FindFirstLegalMoveHint();
            if (currentHint != null)
            {
                return;
            }

            runeSeed = unchecked(runeSeed + 11);
            runeBoard = CreatePlayableBoard(runeSeed);
            currentHint = runeBoard.FindFirstLegalMoveHint();
            runeStatus = "RESHUFFLE";
        }

        private void RefreshRuneHud()
        {
            currentHint = runeBoard.FindFirstLegalMoveHint();
            UpdateRuneMeta();
            RebuildRuneGrid();
        }

        private void UpdateRuneMeta()
        {
            if (runeMetaText != null)
            {
                runeMetaText.text = BuildRuneMeta();
            }
        }

        private string BuildRuneMeta()
        {
            if (isRuneAnimationRunning)
            {
                return $"{runeStatus}  M{runeMovesUsed}  S{runeScore}";
            }

            if (selectedRune.HasValue)
            {
                return $"{runeStatus}  PICK GO";
            }

            var hintText = currentHint == null ? "NO HINT" : $"HINT {FormatPoint(currentHint.From)}>{FormatPoint(currentHint.To)}";
            return $"{runeStatus}  {hintText}  M{runeMovesUsed}  S{runeScore}";
        }

        private bool IsSelectedRune(BoardPoint point)
        {
            return selectedRune.HasValue && selectedRune.Value == point;
        }

        private bool IsHintCell(BoardPoint point)
        {
            return !selectedRune.HasValue && currentHint != null && ContainsPoint(currentHint.HighlightedCells, point);
        }

        private bool IsCurrentHintSwapCell(BoardPoint point)
        {
            return !selectedRune.HasValue && currentHint != null && (currentHint.From == point || currentHint.To == point);
        }

        private bool IsLegalTargetForSelection(BoardPoint point)
        {
            return !isRuneAnimationRunning && selectedRune.HasValue && selectedRune.Value != point && runeBoard.TryCreateMoveHint(selectedRune.Value, point, out _);
        }

        private bool HasLegalTarget(BoardPoint point)
        {
            foreach (var candidate in GetAdjacentPoints(point))
            {
                if (runeBoard.TryCreateMoveHint(point, candidate, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<BoardPoint> GetAdjacentPoints(BoardPoint point)
        {
            var candidates = new[]
            {
                new BoardPoint(point.Row - 1, point.Column),
                new BoardPoint(point.Row + 1, point.Column),
                new BoardPoint(point.Row, point.Column - 1),
                new BoardPoint(point.Row, point.Column + 1)
            };

            foreach (var candidate in candidates)
            {
                if (Match3Board.Contains(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private static Color GetRuneOutlineColor(bool isSelected, bool isLegalTarget, bool isHintSwap, bool isHint)
        {
            if (isSelected || isLegalTarget)
            {
                return GameColors.Gold;
            }

            if (isHintSwap)
            {
                return GameColors.Text;
            }

            if (isHint)
            {
                return GameColors.WithAlpha(GameColors.Text, 0.55f);
            }

            return GameColors.WithAlpha(GameColors.Border, 0.55f);
        }

        private static Vector2 GetRuneSlotPosition(BoardPoint point)
        {
            return GetRuneSlotPosition(point.Row, point.Column);
        }

        private static Vector2 GetRuneSlotPosition(int row, int column)
        {
            var boardWidth = (Match3Board.Columns * RuneTileSize) + ((Match3Board.Columns - 1) * RuneTileSpacing);
            var boardHeight = (Match3Board.Rows * RuneTileSize) + ((Match3Board.Rows - 1) * RuneTileSpacing);
            var step = RuneTileSize + RuneTileSpacing;
            var x = (-boardWidth * 0.5f) + (RuneTileSize * 0.5f) + (column * step);
            var y = (boardHeight * 0.5f) - (RuneTileSize * 0.5f) - (row * step);
            return new Vector2(x, y);
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - Mathf.Clamp01(value);
            return 1f - (inverse * inverse * inverse);
        }

        private void AddRuneMarker(Transform parent, string label, Color color)
        {
            var marker = CreatePanel($"Rune Marker {label}", parent, GameColors.WithAlpha(GameColors.Background, 0.72f));
            marker.GetComponent<Image>().raycastTarget = false;
            var rect = marker.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.52f, 0.58f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CreateOverlayText(label, marker.transform, 8, color, TextAnchor.MiddleCenter);
        }

        private static string FormatPoint(BoardPoint point)
        {
            return $"{point.Row + 1}:{point.Column + 1}";
        }

        private static bool ContainsPoint(IReadOnlyCollection<BoardPoint> points, BoardPoint point)
        {
            foreach (var candidate in points)
            {
                if (candidate == point)
                {
                    return true;
                }
            }

            return false;
        }

        private static string RuneLabel(RuneType rune)
        {
            switch (rune)
            {
                case RuneType.Red:
                    return "R";
                case RuneType.Blue:
                    return "B";
                case RuneType.Green:
                    return "G";
                case RuneType.Yellow:
                    return "Y";
                case RuneType.Purple:
                    return "P";
                case RuneType.White:
                    return "W";
                default:
                    return "?";
            }
        }

        private void AddPreparationPanel(Transform parent)
        {
            var panel = CreatePanel("Preparation Panel", parent, GameColors.PanelDeep);
            AddLayoutElement(panel, 212);
            AddOutline(panel, GameColors.Border);

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 7, 7);
            stack.spacing = 6;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddPanelHeader(panel.transform, "PREP", "BENCH + SHOP");
            AddHeroRow(panel.transform, "Bench", Bench, 48, false);
            AddHeroRow(panel.transform, "Shop", Shop, 62, true);
            AddActionRow(panel.transform);
        }

        private void AddHeroRow(Transform parent, string label, UnitVisual[] heroes, float height, bool showCost)
        {
            var row = CreatePanel($"{label} Row", parent, Color.clear);
            AddLayoutElement(row, height);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateText(label.ToUpperInvariant(), row.transform, 10, GameColors.Muted, TextAnchor.MiddleCenter);
            foreach (var hero in heroes)
            {
                CreateHeroCard(row.transform, hero, showCost);
            }
        }

        private void AddActionRow(Transform parent)
        {
            var row = CreatePanel("Action Row", parent, Color.clear);
            AddLayoutElement(row, 34);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateActionButton(row.transform, "SHOP", GameColors.Button);
            CreateActionButton(row.transform, "REROLL", GameColors.Button);
            CreateActionButton(row.transform, "XP", GameColors.Button);
            CreateActionButton(row.transform, "FIGHT", GameColors.ButtonPrimary);
        }

        private Text AddPanelHeader(Transform parent, string title, string meta)
        {
            var row = CreatePanel($"{title} Header", parent, Color.clear);
            AddLayoutElement(row, 22);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateText(title, row.transform, 13, GameColors.Text, TextAnchor.MiddleLeft);
            return CreateText(meta, row.transform, 9, GameColors.Muted, TextAnchor.MiddleRight);
        }

        private void AddCellLaneMarker(Transform parent, TacticalPosition position)
        {
            var markerColor = position.IsPlayerSide ? GameColors.WithAlpha(GameColors.Heal, 0.14f) : GameColors.WithAlpha(GameColors.Health, 0.16f);
            var marker = CreatePanel("Lane Marker", parent, markerColor);
            var rect = marker.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, position.IsFrontline ? 0f : 0.84f);
            rect.anchorMax = new Vector2(1f, position.IsFrontline ? 0.16f : 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void AddUnitToken(Transform parent, UnitVisual unit)
        {
            var token = CreatePanel($"Unit {unit.ShortName}", parent, unit.Tint);
            SetAnchoredFill(token, 0.10f, 0.10f, 0.10f, 0.10f);
            AddOutline(token, unit.IsPlayer ? GameColors.Heal : GameColors.Health);

            CreateOverlayText(unit.ShortName, token.transform, 14, GameColors.Text, TextAnchor.MiddleCenter);
            AddOverlayBar(token.transform, "HP", GameColors.Health, unit.Health, 0.05f, 0.12f);
            AddOverlayBar(token.transform, "MP", GameColors.Mana, unit.Mana, 0.84f, 0.91f);
        }

        private void CreateHeroCard(Transform parent, UnitVisual hero, bool showCost)
        {
            var card = CreatePanel($"Hero {hero.Name}", parent, GameColors.PanelRaised);
            AddOutline(card, GameColors.WithAlpha(hero.Tint, 0.85f));

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 4, 4);
            layout.spacing = 1;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false;

            var swatch = CreatePanel("Hero Swatch", card.transform, hero.Tint);
            AddLayoutElement(swatch, 12);
            CreateText(hero.Name, card.transform, showCost ? 11 : 10, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText(showCost ? $"2G  {hero.Role}" : hero.Role, card.transform, 9, GameColors.Muted, TextAnchor.MiddleCenter);
        }

        private void CreateRuneChip(Transform parent, RuneType rune, string label)
        {
            var chip = CreatePanel($"Rune {label}", parent, GameColors.RuneColor(rune));
            AddOutline(chip, GameColors.WithAlpha(GameColors.Text, 0.32f));
            CreateOverlayText(label, chip.transform, 8, rune == RuneType.White ? GameColors.Background : GameColors.Text, TextAnchor.MiddleCenter);
        }

        private void CreateStatusPill(Transform parent, string label, string value, Color accent)
        {
            var pill = CreatePanel($"Status {label}", parent, GameColors.WithAlpha(accent, 0.22f));
            AddOutline(pill, GameColors.WithAlpha(accent, 0.55f));

            var layout = pill.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 2, 2);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateText(label, pill.transform, 8, GameColors.Muted, TextAnchor.MiddleLeft);
            CreateText(value, pill.transform, 9, GameColors.Text, TextAnchor.MiddleRight);
        }

        private void CreateStat(Transform parent, string label, string value, Color accent)
        {
            var stat = CreatePanel($"Stat {label}", parent, GameColors.WithAlpha(accent, 0.20f));
            var layoutElement = stat.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 48;
            layoutElement.preferredHeight = 48;
            layoutElement.flexibleWidth = 0f;
            AddOutline(stat, GameColors.WithAlpha(accent, 0.55f));

            var stack = stat.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(4, 4, 4, 4);
            stack.spacing = 1;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childForceExpandHeight = false;

            CreateText(value, stat.transform, 16, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText(label, stat.transform, 8, accent, TextAnchor.MiddleCenter);
        }

        private void CreateActionButton(Transform parent, string label, Color color)
        {
            var button = CreatePanel($"Button {label}", parent, color);
            AddOutline(button, GameColors.WithAlpha(GameColors.Text, 0.20f));
            CreateOverlayText(label, button.transform, 11, label == "FIGHT" ? GameColors.Background : GameColors.Text, TextAnchor.MiddleCenter);
        }

        private TacticalCellState GetDemoCellState(TacticalPosition position)
        {
            UnitVisual unit;
            if (TryGetUnitAt(position.Row, position.Column, out unit))
            {
                return unit.IsPlayer ? TacticalCellState.OccupiedAlly : TacticalCellState.OccupiedEnemy;
            }

            return position.IsPlayerSide ? TacticalCellState.AvailableForPlacement : TacticalCellState.Unavailable;
        }

        private bool TryGetUnitAt(int row, int column, out UnitVisual unit)
        {
            for (var index = 0; index < Units.Length; index += 1)
            {
                var candidate = Units[index];
                if (candidate.Row == row && candidate.Column == column)
                {
                    unit = candidate;
                    return true;
                }
            }

            unit = null;
            return false;
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ContentWidth, ContentHeight);
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 7;
            text.resizeTextMaxSize = size;

            return text;
        }

        private Text CreateOverlayText(string value, Transform parent, int size, Color color, TextAnchor alignment)
        {
            var text = CreateText(value, parent, size, color, alignment);
            Stretch(text.gameObject);
            return text;
        }

        private void AddOverlayBar(Transform parent, string name, Color fill, float value, float anchorMinY, float anchorMaxY)
        {
            var track = CreatePanel($"{name} Track", parent, GameColors.BarTrack);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.10f, anchorMinY);
            trackRect.anchorMax = new Vector2(0.90f, anchorMaxY);
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;

            var bar = CreatePanel($"{name} Fill", track.transform, fill);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddLayoutElement(GameObject gameObject, float preferredHeight)
        {
            var layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 1f;
        }

        private static void AddOutline(GameObject gameObject, Color color)
        {
            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void SetAnchoredFill(GameObject gameObject, float left, float right, float top, float bottom)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
                var child = parent.GetChild(index);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private static void Stretch(GameObject gameObject)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class RuneTileView
        {
            public RuneTileView(BoardPoint point, RuneType rune, GameObject gameObject, RectTransform rect, CanvasGroup group)
            {
                Point = point;
                Rune = rune;
                GameObject = gameObject;
                Rect = rect;
                Group = group;
            }

            public BoardPoint Point { get; }
            public RuneType Rune { get; }
            public GameObject GameObject { get; }
            public RectTransform Rect { get; }
            public CanvasGroup Group { get; }
        }

        private sealed class RuneTileMove
        {
            public RuneTileMove(RuneTileView tile, Vector2 from, Vector2 to)
            {
                Tile = tile;
                From = from;
                To = to;
            }

            public RuneTileView Tile { get; }
            public Vector2 From { get; }
            public Vector2 To { get; }
        }

        private sealed class UnitVisual
        {
            public UnitVisual(string shortName, string name, string role, int row, int column, bool isPlayer, float health, float mana, Color tint)
            {
                ShortName = shortName;
                Name = name;
                Role = role;
                Row = row;
                Column = column;
                IsPlayer = isPlayer;
                Health = health;
                Mana = mana;
                Tint = tint;
            }

            public string ShortName { get; }
            public string Name { get; }
            public string Role { get; }
            public int Row { get; }
            public int Column { get; }
            public bool IsPlayer { get; }
            public float Health { get; }
            public float Mana { get; }
            public Color Tint { get; }
        }
    }
}
