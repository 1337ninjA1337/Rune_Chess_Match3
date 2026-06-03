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
        private const string RuntimeCanvasName = "RuneChessRuntimeCanvas";
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

        private static readonly UnitVisual[] Shop =
        {
            new UnitVisual("WC", "Wild Claw", "Bruiser", 0, 0, true, 1f, 0f, GameColors.Health),
            new UnitVisual("TS", "Thorn Shaman", "Summon", 0, 0, true, 1f, 0f, GameColors.Heal),
            new UnitVisual("MC", "Mist Cut", "Assassin", 0, 0, true, 1f, 0f, GameColors.Commander)
        };

        private RunState runState = RunState.NewRun();
        private string selectedBenchInstanceId;
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
        private AppNavigationState navigationState = AppNavigationState.AtMainMenu;
        private Text mainMenuStatusText;
        private bool isScreenTransitionRunning;
        private bool isRuneAnimationRunning;
        private static PortraitGameBootstrap activeInstance;

        private void Awake()
        {
            if (activeInstance != null && activeInstance != this)
            {
                Destroy(gameObject);
                return;
            }

            activeInstance = this;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private void Start()
        {
            if (activeInstance != this)
            {
                return;
            }

            EnsureMainCamera();
            EnsureEventSystem();
            ClearGeneratedCanvases();
            BuildPortraitGameSurface();
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
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
            navigationState = AppNavigationState.AtMainMenu;

            var menu = CreatePanel("Main Menu", parent, GameColors.PanelDeep);
            AddLayoutElement(menu, 824);
            AddOutline(menu, GameColors.Border);

            var stack = menu.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(12, 12, 12, 12);
            stack.spacing = 8;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddMainMenuTitle(menu.transform);
            AddSelectedCommanderCard(menu.transform);
            AddMainMenuButtons(menu.transform);
            AddMainMenuRunPreview(menu.transform);
            AddMainMenuStatus(menu.transform);
        }

        private void AddMainMenuTitle(Transform parent)
        {
            var title = CreatePanel("Main Menu Title", parent, Color.clear);
            AddLayoutElement(title, 128);

            var stack = title.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(4, 4, 10, 4);
            stack.spacing = 4;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childForceExpandHeight = false;

            CreateText("RUNE CHESS", title.transform, 34, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText("MATCH-3 AUTO BATTLER", title.transform, 13, GameColors.Muted, TextAnchor.MiddleCenter);

            var runeStrip = CreatePanel("Main Menu Rune Strip", title.transform, Color.clear);
            AddLayoutElement(runeStrip, 28);

            var layout = runeStrip.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateRuneChip(runeStrip.transform, RuneType.Red, "R");
            CreateRuneChip(runeStrip.transform, RuneType.Blue, "B");
            CreateRuneChip(runeStrip.transform, RuneType.Green, "G");
            CreateRuneChip(runeStrip.transform, RuneType.Yellow, "Y");
            CreateRuneChip(runeStrip.transform, RuneType.Purple, "P");
            CreateRuneChip(runeStrip.transform, RuneType.White, "W");
        }

        private void AddSelectedCommanderCard(Transform parent)
        {
            var commanderDefinition = CommanderCatalog.Get(runState.Commander.Id);
            var card = CreatePanel("Selected Commander Card", parent, GameColors.Panel);
            AddLayoutElement(card, 196);
            AddOutline(card, GameColors.WithAlpha(GameColors.Commander, 0.72f));

            var stack = card.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(10, 10, 8, 8);
            stack.spacing = 6;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddPanelHeader(card.transform, "ВЫБРАННЫЙ КОМАНДИР", $"{runState.Commander.Energy:0}/{runState.Commander.MaxEnergy:0} ENERGY");

            var identity = CreatePanel("Commander Identity", card.transform, GameColors.WithAlpha(GameColors.Commander, 0.16f));
            AddLayoutElement(identity, 54);
            AddOutline(identity, GameColors.WithAlpha(GameColors.Commander, 0.45f));

            var identityStack = identity.AddComponent<VerticalLayoutGroup>();
            identityStack.padding = new RectOffset(8, 8, 5, 5);
            identityStack.spacing = 1;
            identityStack.childAlignment = TextAnchor.MiddleCenter;
            identityStack.childForceExpandHeight = false;
            CreateText(runState.Commander.Name, identity.transform, 20, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText(commanderDefinition.Id.ToUpperInvariant(), identity.transform, 9, GameColors.Commander, TextAnchor.MiddleCenter);

            CreateText(commanderDefinition.Passive, card.transform, 11, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText(commanderDefinition.StartingBonus.Description, card.transform, 10, GameColors.Gold, TextAnchor.MiddleCenter);
            CreateText(string.Join(" / ", commanderDefinition.RecommendedStyles), card.transform, 9, GameColors.Muted, TextAnchor.MiddleCenter);
        }

        private void AddMainMenuButtons(Transform parent)
        {
            var buttons = CreatePanel("Main Menu Buttons", parent, Color.clear);
            AddLayoutElement(buttons, 196);

            var stack = buttons.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 7;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            CreateMenuButton(buttons.transform, "Новый забег", "ROUND 1", GameColors.ButtonPrimary, true, StartNewRunFromMenu);
            CreateMenuButton(buttons.transform, "Выбор уровня", "PVE MAP", GameColors.Button, false, ShowLevelSelectScreen);
            CreateMenuButton(buttons.transform, "Настройки", "AUDIO / GAME", GameColors.Button, false, () => SelectMainMenuDestination(AppScreen.Settings));
        }

        private void AddMainMenuRunPreview(Transform parent)
        {
            var preview = CreatePanel("Main Menu Run Preview", parent, GameColors.Panel);
            AddLayoutElement(preview, 124);
            AddOutline(preview, GameColors.WithAlpha(GameColors.Border, 0.65f));

            var stack = preview.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 7, 7);
            stack.spacing = 6;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddPanelHeader(preview.transform, "ЗАБЕГ", $"ROUND {runState.Round}  {runState.Phase.ToString().ToUpperInvariant()}");

            var stats = CreatePanel("Main Menu Run Stats", preview.transform, Color.clear);
            AddLayoutElement(stats, 48);
            var statsLayout = stats.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 6;
            statsLayout.childAlignment = TextAnchor.MiddleCenter;
            statsLayout.childControlWidth = true;
            statsLayout.childForceExpandWidth = true;

            CreateStatusPill(stats.transform, "HP", runState.RunHealth.ToString(), GameColors.Health);
            CreateStatusPill(stats.transform, "GOLD", runState.Gold.ToString(), GameColors.Gold);
            CreateStatusPill(stats.transform, "LEVEL", runState.PlayerLevel.ToString(), GameColors.Mana);

            CreateText($"NEXT: {runState.NextEnemyId}", preview.transform, 10, GameColors.Muted, TextAnchor.MiddleCenter);
        }

        private void AddMainMenuStatus(Transform parent)
        {
            var status = CreatePanel("Main Menu Status", parent, GameColors.WithAlpha(GameColors.Commander, 0.16f));
            AddLayoutElement(status, 34);
            AddOutline(status, GameColors.WithAlpha(GameColors.Commander, 0.45f));
            mainMenuStatusText = CreateOverlayText(BuildMainMenuStatus(), status.transform, 11, GameColors.Text, TextAnchor.MiddleCenter);
        }

        private void CreateMenuButton(Transform parent, string label, string meta, Color color, bool primary, Action onClick)
        {
            var buttonObject = CreatePanel($"Menu Button {label}", parent, color);
            AddLayoutElement(buttonObject, 56);
            AddOutline(buttonObject, GameColors.WithAlpha(primary ? GameColors.Background : GameColors.Text, 0.25f));

            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var row = buttonObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(12, 12, 6, 6);
            row.spacing = 8;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childForceExpandWidth = true;

            var foreground = primary ? GameColors.Background : GameColors.Text;
            CreateText(label, buttonObject.transform, 17, foreground, TextAnchor.MiddleLeft);
            CreateText(meta, buttonObject.transform, 10, primary ? GameColors.Background : GameColors.Muted, TextAnchor.MiddleRight);
        }

        private void StartNewRunFromMenu()
        {
            runState = RunState.NewRun(runState.Commander.Id);
            navigationState = AppNavigationState.AtMainMenu
                .NavigateTo(AppScreen.LevelSelect)
                .NavigateTo(AppScreen.Preparation);
            StartLevelTransitionToPreparation(LevelSelectModel.Build(runState)[runState.Round - 1]);
        }

        private void ShowMainMenu()
        {
            if (contentRoot == null)
            {
                return;
            }

            ClearChildren(contentRoot);
            AddMainMenu(contentRoot);
        }

        private void ShowLevelSelectScreen()
        {
            if (contentRoot == null)
            {
                return;
            }

            SetNavigationForScreen(AppScreen.LevelSelect);
            ClearChildren(contentRoot);
            AddLevelSelectScreen(contentRoot);
        }

        private void AddLevelSelectScreen(Transform parent)
        {
            var screen = CreatePanel("Level Select Screen", parent, GameColors.PanelDeep);
            AddLayoutElement(screen, 824);
            AddOutline(screen, GameColors.Border);

            var stack = screen.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(10, 10, 10, 10);
            stack.spacing = 7;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddLevelSelectHeader(screen.transform);
            AddLevelCardList(screen.transform);
            AddLevelSelectActions(screen.transform);
        }

        private void AddLevelSelectHeader(Transform parent)
        {
            var header = CreatePanel("Level Select Header", parent, GameColors.Panel);
            AddLayoutElement(header, 72);
            AddOutline(header, GameColors.WithAlpha(GameColors.Border, 0.65f));

            var stack = header.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 7, 7);
            stack.spacing = 2;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childForceExpandHeight = false;

            CreateText("ВЫБОР УРОВНЯ", header.transform, 22, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText($"ROUND {runState.Round}/{PveRunSchedule.FinalRound}  {runState.Commander.Name}", header.transform, 10, GameColors.Muted, TextAnchor.MiddleCenter);
        }

        private void AddLevelCardList(Transform parent)
        {
            var list = CreatePanel("Level Card List", parent, Color.clear);
            AddLayoutElement(list, 612);

            var stack = list.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 5;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var cards = LevelSelectModel.Build(runState);
            foreach (var card in cards)
            {
                CreateLevelCard(list.transform, card);
            }
        }

        private void AddLevelSelectActions(Transform parent)
        {
            var row = CreatePanel("Level Select Actions", parent, Color.clear);
            AddLayoutElement(row, 58);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 7;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateMenuButton(row.transform, "Меню", "BACK", GameColors.Button, false, ShowMainMenu);
            CreateMenuButton(row.transform, "Текущий раунд", $"R{runState.Round}", GameColors.ButtonPrimary, true, StartCurrentLevelFromSelect);
        }

        private void CreateLevelCard(Transform parent, LevelCard card)
        {
            var cardColor = GetLevelCardColor(card.Status);
            var cardObject = CreatePanel($"Level Card {card.Round}", parent, cardColor);
            AddLayoutElement(cardObject, 56);
            AddOutline(cardObject, GetLevelCardBorderColor(card.Status));

            var image = cardObject.GetComponent<Image>();
            image.raycastTarget = card.Status == LevelCardStatus.Current;

            if (card.Status == LevelCardStatus.Current)
            {
                var button = cardObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => StartLevelFromSelect(card));
            }

            var row = cardObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(7, 7, 5, 5);
            row.spacing = 6;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childForceExpandWidth = true;

            AddLevelCardRoundColumn(cardObject.transform, card);
            AddLevelCardInfoColumn(cardObject.transform, card);
            AddLevelCardRewardColumn(cardObject.transform, card);
        }

        private void AddLevelCardRoundColumn(Transform parent, LevelCard card)
        {
            var column = CreatePanel($"Level {card.Round} Round", parent, GameColors.WithAlpha(GetLevelStatusColor(card.Status), 0.18f));
            AddOutline(column, GameColors.WithAlpha(GetLevelStatusColor(card.Status), 0.45f));
            SetLayoutWidth(column, 68, 0f);

            var stack = column.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(4, 4, 3, 3);
            stack.spacing = 1;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childForceExpandHeight = false;

            CreateText($"R{card.Round}", column.transform, 17, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText(GetRoundTypeLabel(card.Type), column.transform, 8, GetLevelStatusColor(card.Status), TextAnchor.MiddleCenter);
        }

        private void AddLevelCardInfoColumn(Transform parent, LevelCard card)
        {
            var column = CreatePanel($"Level {card.Round} Info", parent, Color.clear);
            SetLayoutWidth(column, 184, 2.4f);

            var stack = column.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 1;
            stack.childAlignment = TextAnchor.MiddleLeft;
            stack.childForceExpandHeight = false;

            CreateText(card.EnemyName, column.transform, 12, GameColors.Text, TextAnchor.MiddleLeft);
            CreateText(card.DesignGoal, column.transform, 9, GameColors.Muted, TextAnchor.MiddleLeft);
            CreateText($"{GetDifficultyLabel(card.DifficultyTier)}  {(card.HasCombat ? "COMBAT" : "EVENT")}", column.transform, 8, GameColors.WithAlpha(GameColors.Text, 0.72f), TextAnchor.MiddleLeft);
        }

        private void AddLevelCardRewardColumn(Transform parent, LevelCard card)
        {
            var column = CreatePanel($"Level {card.Round} Reward", parent, Color.clear);
            SetLayoutWidth(column, 100, 1f);

            var stack = column.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 1;
            stack.childAlignment = TextAnchor.MiddleRight;
            stack.childForceExpandHeight = false;

            CreateText(GetLevelStatusLabel(card.Status), column.transform, 10, GetLevelStatusColor(card.Status), TextAnchor.MiddleRight);
            CreateText(card.RewardSummary, column.transform, 9, GameColors.Gold, TextAnchor.MiddleRight);
        }

        private void StartCurrentLevelFromSelect()
        {
            var currentCard = LevelSelectModel.Build(runState)[runState.Round - 1];
            StartLevelFromSelect(currentCard);
        }

        private void StartLevelFromSelect(LevelCard card)
        {
            if (card.Status != LevelCardStatus.Current)
            {
                return;
            }

            navigationState = navigationState.Current == AppScreen.LevelSelect
                ? navigationState.NavigateTo(AppScreen.Preparation)
                : AppNavigationState.AtMainMenu.NavigateTo(AppScreen.LevelSelect).NavigateTo(AppScreen.Preparation);
            StartLevelTransitionToPreparation(card);
        }

        private void SelectMainMenuDestination(AppScreen screen)
        {
            if (!AppNavigationState.AtMainMenu.CanNavigateTo(screen))
            {
                return;
            }

            navigationState = AppNavigationState.AtMainMenu.NavigateTo(screen);
            if (mainMenuStatusText != null)
            {
                mainMenuStatusText.text = BuildMainMenuStatus();
            }
        }

        private string BuildMainMenuStatus()
        {
            return $"{navigationState.Current.ToString().ToUpperInvariant()}  CMD {runState.Commander.Energy:0}/{runState.Commander.MaxEnergy:0}";
        }

        private static string GetRoundTypeLabel(PveRoundType type)
        {
            switch (type)
            {
                case PveRoundType.Tutorial:
                    return "TUTOR";
                case PveRoundType.Combat:
                    return "FIGHT";
                case PveRoundType.Event:
                    return "EVENT";
                case PveRoundType.Elite:
                    return "ELITE";
                case PveRoundType.Boss:
                    return "BOSS";
                case PveRoundType.EnhancedShop:
                    return "SHOP";
                case PveRoundType.FinalBoss:
                    return "FINAL";
                default:
                    return "ROUND";
            }
        }

        private static string GetDifficultyLabel(PveDifficultyTier tier)
        {
            switch (tier)
            {
                case PveDifficultyTier.Fundamentals:
                    return "BASE";
                case PveDifficultyTier.ChoicesAndCounters:
                    return "CHOICE";
                case PveDifficultyTier.SynergyCheck:
                    return "SYNERGY";
                case PveDifficultyTier.FullBuildCheck:
                    return "BUILD";
                default:
                    return "PACE";
            }
        }

        private static string GetLevelStatusLabel(LevelCardStatus status)
        {
            switch (status)
            {
                case LevelCardStatus.Completed:
                    return "DONE";
                case LevelCardStatus.Current:
                    return "READY";
                case LevelCardStatus.Locked:
                    return "LOCKED";
                default:
                    return "STATUS";
            }
        }

        private static Color GetLevelCardColor(LevelCardStatus status)
        {
            switch (status)
            {
                case LevelCardStatus.Completed:
                    return GameColors.WithAlpha(GameColors.PanelRaised, 0.86f);
                case LevelCardStatus.Current:
                    return GameColors.PanelRaised;
                case LevelCardStatus.Locked:
                    return GameColors.WithAlpha(GameColors.Panel, 0.68f);
                default:
                    return GameColors.Panel;
            }
        }

        private static Color GetLevelCardBorderColor(LevelCardStatus status)
        {
            switch (status)
            {
                case LevelCardStatus.Completed:
                    return GameColors.WithAlpha(GameColors.Heal, 0.62f);
                case LevelCardStatus.Current:
                    return GameColors.Gold;
                case LevelCardStatus.Locked:
                    return GameColors.WithAlpha(GameColors.Border, 0.35f);
                default:
                    return GameColors.Border;
            }
        }

        private static Color GetLevelStatusColor(LevelCardStatus status)
        {
            switch (status)
            {
                case LevelCardStatus.Completed:
                    return GameColors.Heal;
                case LevelCardStatus.Current:
                    return GameColors.Gold;
                case LevelCardStatus.Locked:
                    return GameColors.Muted;
                default:
                    return GameColors.Text;
            }
        }

        private void ShowPreparationScreen()
        {
            if (contentRoot == null)
            {
                return;
            }

            SetNavigationForScreen(AppScreen.Preparation);
            EnsurePreparationDemoRoster();
            ClearChildren(contentRoot);
            AddHeader(contentRoot);
            AddPreparationPanel(contentRoot);
            AddScreenNavigationRow(
                contentRoot,
                "К уровням",
                "MAP",
                ShowLevelSelectScreen,
                "Начать бой",
                "COMBAT",
                ShowCombatScreen);
        }

        private void StartLevelTransitionToPreparation(LevelCard card)
        {
            if (isScreenTransitionRunning || contentRoot == null)
            {
                return;
            }

            StartCoroutine(PlayLevelTransitionToPreparation(card));
        }

        private IEnumerator PlayLevelTransitionToPreparation(LevelCard card)
        {
            isScreenTransitionRunning = true;

            var overlay = CreateLevelTransitionOverlay(card, 0f);
            var group = overlay.GetComponent<CanvasGroup>();
            yield return FadeCanvasGroup(group, 0f, 1f, 0.18f);
            yield return new WaitForSeconds(0.42f);

            ShowPreparationScreen();

            overlay = CreateLevelTransitionOverlay(card, 1f);
            group = overlay.GetComponent<CanvasGroup>();
            yield return FadeCanvasGroup(group, 1f, 0f, 0.22f);
            Destroy(overlay);

            isScreenTransitionRunning = false;
        }

        private GameObject CreateLevelTransitionOverlay(LevelCard card, float alpha)
        {
            var overlay = CreatePanel("Level Transition Overlay", contentRoot, GameColors.WithAlpha(GameColors.Background, 0.96f));
            var layoutElement = overlay.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            Stretch(overlay);
            overlay.transform.SetAsLastSibling();

            var group = overlay.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.blocksRaycasts = true;

            var stack = overlay.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(24, 24, 240, 240);
            stack.spacing = 8;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            CreateText($"ROUND {card.Round}", overlay.transform, 24, GameColors.Gold, TextAnchor.MiddleCenter);
            CreateText(card.EnemyName, overlay.transform, 28, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText(card.DesignGoal, overlay.transform, 13, GameColors.Muted, TextAnchor.MiddleCenter);
            CreateText(card.RewardSummary, overlay.transform, 11, GameColors.Gold, TextAnchor.MiddleCenter);

            return overlay;
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float durationSeconds)
        {
            var elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                var progress = Mathf.Clamp01(elapsed / durationSeconds);
                group.alpha = Mathf.Lerp(from, to, EaseOutCubic(progress));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            group.alpha = to;
        }

        private void ShowCombatScreen()
        {
            if (contentRoot == null)
            {
                return;
            }

            SetNavigationForScreen(AppScreen.Combat);
            ClearChildren(contentRoot);
            ResetMatch3Board();
            AddHeader(contentRoot);
            AddBattlePanel(contentRoot);
            AddRunePanel(contentRoot);
            AddRuneEffectStrip(contentRoot);
            AddScreenNavigationRow(
                contentRoot,
                "Подготовка",
                "BACK",
                ShowPreparationScreen,
                "Завершить",
                "REWARD",
                ShowRewardScreen);
        }

        private void ShowRewardScreen()
        {
            if (contentRoot == null)
            {
                return;
            }

            ResolveRoundReward();
            SetNavigationForScreen(AppScreen.LevelComplete);
            ClearChildren(contentRoot);
            AddHeader(contentRoot);
            AddRewardSummaryPanel(contentRoot);

            var atRunEnd = runState.IsFinalRound || runState.Phase == RunPhase.Victory || runState.Phase == RunPhase.Defeat;
            AddScreenNavigationRow(
                contentRoot,
                "К уровням",
                "MAP",
                ShowLevelSelectScreen,
                atRunEnd ? "Итог забега" : "Следующий",
                atRunEnd ? "SUMMARY" : "NEXT",
                AdvanceToNextLevel);
        }

        /// <summary>
        /// Drive the run state machine into the reward phase when the level-complete screen
        /// opens, so the round gold is claimed once and the run can advance. Guarded so
        /// re-entering the screen never double-claims or throws.
        /// </summary>
        private void ResolveRoundReward()
        {
            if (runState.Phase == RunPhase.Preparation && runState.Team.Count > 0)
            {
                runState = runState.StartCombat().ClaimReward();
            }
        }

        /// <summary>
        /// Advance from the level-complete screen to the next round's preparation without
        /// leaving the run or reloading the scene. On the final round (or a resolved run)
        /// it routes to the run summary instead.
        /// </summary>
        private void AdvanceToNextLevel()
        {
            if (runState.IsFinalRound || runState.Phase == RunPhase.Victory || runState.Phase == RunPhase.Defeat)
            {
                ShowRunSummaryScreen();
                return;
            }

            if (runState.Phase == RunPhase.Reward)
            {
                runState = runState.AdvanceRound();
            }

            var card = LevelSelectModel.Build(runState)[runState.Round - 1];
            StartLevelTransitionToPreparation(card);
        }

        private void ShowRunSummaryScreen()
        {
            if (contentRoot == null)
            {
                return;
            }

            SetNavigationForScreen(AppScreen.RunSummary);
            ClearChildren(contentRoot);
            AddHeader(contentRoot);
            AddRunSummaryPanel(contentRoot);
            AddScreenNavigationRow(
                contentRoot,
                "В меню",
                "MENU",
                ShowMainMenu,
                "К уровням",
                "MAP",
                ShowLevelSelectScreen);
        }

        private void AddRunSummaryPanel(Transform parent)
        {
            var summary = RunSummaryModel.Build(runState);
            var panel = CreatePanel("Run Summary Panel", parent, GameColors.PanelDeep);
            AddLayoutElement(panel, 560);
            AddOutline(panel, GameColors.WithAlpha(summary.IsVictory ? GameColors.Gold : GameColors.Health, 0.72f));

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(10, 10, 10, 10);
            stack.spacing = 8;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddPanelHeader(panel.transform, "ИТОГ ЗАБЕГА", summary.IsVictory ? "RUN CLEARED" : "RUN ENDED");
            CreateText(summary.ResultLabel, panel.transform, 22, summary.IsVictory ? GameColors.Heal : GameColors.Health, TextAnchor.MiddleCenter);
            CreateText($"Пройдено раундов: {summary.ProgressLabel}", panel.transform, 13, GameColors.Text, TextAnchor.MiddleCenter);

            var stats = CreatePanel("Run Summary Stats", panel.transform, Color.clear);
            AddLayoutElement(stats, 40);
            var statsLayout = stats.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 6;
            statsLayout.childAlignment = TextAnchor.MiddleCenter;
            statsLayout.childControlWidth = true;
            statsLayout.childForceExpandWidth = true;

            CreateStatusPill(stats.transform, "HP RUN", summary.RunHealth.ToString(), GameColors.Health);
            CreateStatusPill(stats.transform, "GOLD", summary.Gold.ToString(), GameColors.Gold);
            CreateStatusPill(stats.transform, "LEVEL", summary.PlayerLevel.ToString(), GameColors.Mana);
            CreateStatusPill(stats.transform, "TEAM", summary.Team.Count.ToString(), GameColors.Commander);

            if (summary.BestHero is { } best)
            {
                var bestPanel = CreatePanel("Best Hero", panel.transform, GameColors.WithAlpha(GameColors.Gold, 0.16f));
                AddOutline(bestPanel, GameColors.WithAlpha(GameColors.Gold, 0.5f));
                AddLayoutElement(bestPanel, 46);
                var bestStack = bestPanel.AddComponent<VerticalLayoutGroup>();
                bestStack.padding = new RectOffset(8, 8, 4, 4);
                bestStack.childAlignment = TextAnchor.MiddleCenter;
                bestStack.childForceExpandHeight = false;
                CreateText($"ЛУЧШИЙ ГЕРОЙ: {best.Name} {new string('*', Mathf.Clamp(best.Stars, 0, 3))}", bestPanel.transform, 14, GameColors.Gold, TextAnchor.MiddleCenter);
                CreateText($"{best.Faction} / {best.Class} / {best.Cost}g", bestPanel.transform, 10, GameColors.Muted, TextAnchor.MiddleCenter);
            }

            CreateText("СОСТАВ КОМАНДЫ", panel.transform, 12, GameColors.Text, TextAnchor.MiddleCenter);
            if (summary.Team.Count == 0)
            {
                CreateText("Команда пуста", panel.transform, 11, GameColors.Muted, TextAnchor.MiddleCenter);
            }
            else
            {
                foreach (var hero in summary.Team)
                {
                    var row = CreatePanel($"Team {hero.HeroId}", panel.transform, GameColors.PanelRaised);
                    AddLayoutElement(row, 30);
                    var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                    rowLayout.padding = new RectOffset(8, 8, 2, 2);
                    rowLayout.childAlignment = TextAnchor.MiddleLeft;
                    rowLayout.childControlWidth = true;
                    rowLayout.childForceExpandWidth = true;
                    CreateText($"{hero.Name} {new string('*', Mathf.Clamp(hero.Stars, 0, 3))}", row.transform, 12, GameColors.Text, TextAnchor.MiddleLeft);
                    CreateText($"{hero.Class}", row.transform, 10, GameColors.Muted, TextAnchor.MiddleRight);
                }
            }
        }

        private void SetNavigationForScreen(AppScreen screen)
        {
            if (navigationState.Current == screen)
            {
                return;
            }

            if (navigationState.CanNavigateTo(screen))
            {
                navigationState = navigationState.NavigateTo(screen);
                return;
            }

            switch (screen)
            {
                case AppScreen.MainMenu:
                    navigationState = AppNavigationState.AtMainMenu;
                    break;
                case AppScreen.LevelSelect:
                    navigationState = AppNavigationState.AtMainMenu.NavigateTo(AppScreen.LevelSelect);
                    break;
                case AppScreen.Preparation:
                    navigationState = AppNavigationState.AtMainMenu
                        .NavigateTo(AppScreen.LevelSelect)
                        .NavigateTo(AppScreen.Preparation);
                    break;
                case AppScreen.Combat:
                    navigationState = AppNavigationState.AtMainMenu
                        .NavigateTo(AppScreen.LevelSelect)
                        .NavigateTo(AppScreen.Preparation)
                        .NavigateTo(AppScreen.Combat);
                    break;
                case AppScreen.LevelComplete:
                    navigationState = AppNavigationState.AtMainMenu
                        .NavigateTo(AppScreen.LevelSelect)
                        .NavigateTo(AppScreen.Preparation)
                        .NavigateTo(AppScreen.Combat)
                        .NavigateTo(AppScreen.LevelComplete);
                    break;
                default:
                    navigationState = AppNavigationState.AtMainMenu;
                    break;
            }
        }

        private void AddScreenNavigationRow(
            Transform parent,
            string leftLabel,
            string leftMeta,
            Action leftAction,
            string rightLabel,
            string rightMeta,
            Action rightAction)
        {
            var row = CreatePanel("Screen Navigation Row", parent, Color.clear);
            AddLayoutElement(row, 58);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 7;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateMenuButton(row.transform, leftLabel, leftMeta, GameColors.Button, false, leftAction);
            CreateMenuButton(row.transform, rightLabel, rightMeta, GameColors.ButtonPrimary, true, rightAction);
        }

        private void AddRewardSummaryPanel(Transform parent)
        {
            var round = runState.CurrentRoundDefinition;
            var panel = CreatePanel("Level Complete Panel", parent, GameColors.PanelDeep);
            AddLayoutElement(panel, 430);
            AddOutline(panel, GameColors.WithAlpha(GameColors.Gold, 0.72f));

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(10, 10, 10, 10);
            stack.spacing = 8;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var summary = BuildLevelCompleteModel(round);

            AddPanelHeader(panel.transform, "ЗАВЕРШЕНИЕ УРОВНЯ", $"ROUND {round.Round} COMPLETE");
            CreateText(summary.ResultLabel, panel.transform, 22, summary.IsVictory ? GameColors.Heal : GameColors.Health, TextAnchor.MiddleCenter);
            CreateText(round.EnemyName, panel.transform, 16, GameColors.Text, TextAnchor.MiddleCenter);
            CreateText(round.DesignGoal, panel.transform, 11, GameColors.Muted, TextAnchor.MiddleCenter);

            AddLevelCompleteStatGrid(panel.transform, summary);

            var reward = new LevelCard(
                round.Round,
                round.Type,
                round.EnemyName,
                round.DesignGoal,
                round.BaseGoldReward,
                round.RoundReward,
                round.DifficultyTier,
                round.HasCombat,
                LevelCardStatus.Current);

            CreateText(reward.RewardSummary, panel.transform, 13, GameColors.Gold, TextAnchor.MiddleCenter);
            CreateText($"FLOW: {navigationState.Previous?.ToString().ToUpperInvariant() ?? "COMBAT"} > {navigationState.Current.ToString().ToUpperInvariant()}", panel.transform, 9, GameColors.Muted, TextAnchor.MiddleCenter);
        }

        /// <summary>
        /// Resolve the per-level statistics for the completion screen. Combat totals come
        /// from a deterministic core autobattle of the player's placed team against the
        /// round's data-driven PvE roster; match-3 moves and gold come from the live run state.
        /// </summary>
        private LevelCompleteModel BuildLevelCompleteModel(PveRoundDefinition round)
        {
            // The round is cleared by reaching this screen (ResolveRoundReward claims it);
            // a depleted run is the only defeat state. The combat magnitudes come from the
            // deterministic round autobattle so damage/healing/shields are real numbers.
            var outcome = runState.Phase == RunPhase.Defeat
                ? BattleOutcome.PlayerDefeat
                : BattleOutcome.PlayerVictory;
            var battle = LevelCombatSimulator.ResolveRoundMatch(runState.Team, round);
            if (battle is null)
            {
                return LevelCompleteModel.Build(
                    outcome: outcome,
                    durationSeconds: runState.Combat?.ElapsedSeconds ?? 0,
                    match3MovesUsed: runeMovesUsed,
                    damageDealt: 0.0,
                    healingDone: 0.0,
                    shieldGranted: 0.0,
                    goldEarned: round.BaseGoldReward);
            }

            return LevelCompleteModel.Build(
                outcome: outcome,
                durationSeconds: (int)Math.Round(battle.ElapsedSeconds, MidpointRounding.AwayFromZero),
                match3MovesUsed: runeMovesUsed,
                damageDealt: battle.PlayerDamageDealt,
                healingDone: battle.PlayerHealingDone,
                shieldGranted: battle.PlayerShieldGranted,
                goldEarned: round.BaseGoldReward);
        }

        /// <summary>Render the six level-complete stat pills as two rows of three.</summary>
        private void AddLevelCompleteStatGrid(Transform parent, LevelCompleteModel summary)
        {
            var stats = summary.StatRow();
            var accents = new[]
            {
                GameColors.Mana,
                GameColors.Commander,
                GameColors.Health,
                GameColors.Heal,
                GameColors.Shield,
                GameColors.Gold
            };

            for (var rowStart = 0; rowStart < stats.Length; rowStart += 3)
            {
                var row = CreatePanel($"Level Complete Stat Row {rowStart / 3}", parent, Color.clear);
                AddLayoutElement(row, 40);

                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 6;
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = true;
                rowLayout.childForceExpandWidth = true;

                for (var i = rowStart; i < rowStart + 3 && i < stats.Length; i += 1)
                {
                    CreateStatusPill(row.transform, $"{stats[i].Label} {stats[i].Meta}".Trim(), stats[i].Value, accents[i]);
                }
            }
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
            AddLayoutElement(panel, 272);
            AddOutline(panel, GameColors.Border);

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 7, 7);
            stack.spacing = 6;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var combat = runState.Combat ?? CombatState.Start(runeSeed);
            var hud = CombatHudModel.Build(combat, BuildKeyHudUnits());

            AddPanelHeader(panel.transform, "TACTICAL FIELD", $"SPEED {hud.SpeedLabel}");
            AddBattleTimerRow(panel.transform, hud);
            AddTacticalGrid(panel.transform);
            AddBattleKeyUnitsRow(panel.transform, hud);
        }

        private void AddBattleTimerRow(Transform parent, CombatHudModel hud)
        {
            var row = CreatePanel("Battle Timer Row", parent, Color.clear);
            AddLayoutElement(row, 24);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateStatusPill(row.transform, "TIME", hud.TimerLabel, GameColors.Health);
            CreateStatusPill(row.transform, "MATCHES", hud.Match3MovesUsed.ToString(), GameColors.Mana);
            CreateStatusPill(row.transform, "POWER", $"x{hud.LastMatchPower}", GameColors.Commander);
        }

        private void AddBattleKeyUnitsRow(Transform parent, CombatHudModel hud)
        {
            var row = CreatePanel("Battle Key Units", parent, Color.clear);
            AddLayoutElement(row, 42);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            foreach (var unit in hud.KeyUnits)
            {
                CreateKeyUnitCard(row.transform, unit);
            }
        }

        private void CreateKeyUnitCard(Transform parent, CombatHudUnit unit)
        {
            var card = CreatePanel($"Key {unit.Name}", parent, unit.IsPlayer ? GameColors.AllyCellOccupied : GameColors.EnemyCellOccupied);
            AddOutline(card, unit.IsPlayer ? GameColors.Heal : GameColors.Health);
            CreateOverlayText(unit.Name, card.transform, 10, GameColors.Text, TextAnchor.MiddleCenter);
            AddOverlayBar(card.transform, "HP", GameColors.Health, (float)unit.HealthBar, 0.06f, 0.18f);
            AddOverlayBar(card.transform, "MP", GameColors.Mana, (float)unit.ManaBar, 0.82f, 0.94f);
        }

        private List<CombatHudUnit> BuildKeyHudUnits()
        {
            var allies = new List<CombatHudUnit>();
            var enemies = new List<CombatHudUnit>();
            foreach (var unit in Units)
            {
                var hudUnit = new CombatHudUnit(unit.ShortName, unit.IsPlayer, unit.Health, unit.Mana);
                if (unit.IsPlayer)
                {
                    if (allies.Count < 2)
                    {
                        allies.Add(hudUnit);
                    }
                }
                else if (enemies.Count < 2)
                {
                    enemies.Add(hudUnit);
                }
            }

            var keyUnits = new List<CombatHudUnit>(allies);
            keyUnits.AddRange(enemies);
            return keyUnits;
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
            AddLayoutElement(panel, 514);
            AddOutline(panel, GameColors.Border);

            var stack = panel.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(8, 8, 7, 7);
            stack.spacing = 6;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            AddPanelHeader(panel.transform, "PREPARATION", $"G{runState.Gold}  LV{runState.PlayerLevel}");
            AddPreparationTacticalPanel(panel.transform);
            AddPreparationEconomyRow(panel.transform);
            AddPreparationBenchRow(panel.transform);
            AddHeroRow(panel.transform, "Shop", Shop, 70, true);
            AddActionRow(panel.transform);
        }

        private void AddPreparationTacticalPanel(Transform parent)
        {
            var field = CreatePanel("Preparation Tactical Field", parent, GameColors.Panel);
            AddLayoutElement(field, 190);
            AddOutline(field, GameColors.WithAlpha(GameColors.Heal, 0.45f));

            var stack = field.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(7, 7, 6, 6);
            stack.spacing = 5;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var model = TacticalPlacementModel.Build(runState, selectedBenchInstanceId);
            var meta = model.HasSelection
                ? (model.CanPlaceMore ? "ВЫБЕРИ КЛЕТКУ" : "ПОЛЕ ЗАПОЛНЕНО")
                : $"ПОЛЕ {model.PlacedHeroCount}/{model.FieldLimit}";
            AddPanelHeader(field.transform, "TACTICAL FIELD", meta);
            AddPreparationTacticalGrid(field.transform, model);
        }

        private void AddPreparationTacticalGrid(Transform parent, TacticalPlacementModel model)
        {
            var gridRoot = CreatePanel("Preparation Tactical Grid", parent, Color.clear);
            AddLayoutElement(gridRoot, 150);

            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = model.Field.Columns;
            grid.spacing = new Vector2(3, 3);
            grid.cellSize = new Vector2(57, 36);
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (var cell in model.Cells)
            {
                var cellObject = CreatePanel(
                    $"Cell {cell.Position.Row}:{cell.Position.Column}",
                    gridRoot.transform,
                    GameColors.TacticalCellColor(cell.State));

                var outlineColor = cell.IsPlacementTarget
                    ? GameColors.WithAlpha(GameColors.Heal, 0.95f)
                    : GameColors.WithAlpha(GameColors.Border, 0.45f);
                AddOutline(cellObject, outlineColor);
                AddCellLaneMarker(cellObject.transform, cell.Position);

                if (cell.IsOccupiedByAlly)
                {
                    var instanceId = cell.HeroInstanceId;
                    AddPreparationHeroToken(cellObject.transform, cell.HeroId, () => OnBoardHeroClicked(instanceId));
                }
                else if (cell.IsPlacementTarget)
                {
                    var position = cell.Position;
                    MakeClickable(cellObject, () => OnPlacementCellClicked(position));
                }
            }
        }

        private void AddPreparationEconomyRow(Transform parent)
        {
            var row = CreatePanel("Preparation Economy Row", parent, Color.clear);
            AddLayoutElement(row, 40);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            var fieldLimit = EconomyConfig.Default.GetHeroLimitForLevel(runState.PlayerLevel);
            CreateStatusPill(row.transform, "HP", runState.RunHealth.ToString(), GameColors.Health);
            CreateStatusPill(row.transform, "GOLD", runState.Gold.ToString(), GameColors.Gold);
            CreateStatusPill(row.transform, "LV", runState.PlayerLevel.ToString(), GameColors.Mana);
            CreateStatusPill(row.transform, "FIELD", $"{runState.Team.Count}/{fieldLimit}", GameColors.Heal);
        }

        private void AddPreparationBenchRow(Transform parent)
        {
            var row = CreatePanel("Bench Row", parent, Color.clear);
            AddLayoutElement(row, 56);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateText("BENCH", row.transform, 10, GameColors.Muted, TextAnchor.MiddleCenter);

            if (runState.Bench.Count == 0)
            {
                CreateText("СКАМЕЙКА ПУСТА", row.transform, 9, GameColors.Muted, TextAnchor.MiddleCenter);
                return;
            }

            foreach (var hero in runState.Bench)
            {
                var instanceId = hero.InstanceId;
                var selected = instanceId == selectedBenchInstanceId;
                AddBenchHeroCard(row.transform, hero, selected, () => OnBenchHeroClicked(instanceId));
            }
        }

        private void AddBenchHeroCard(Transform parent, HeroInstance hero, bool selected, Action onClick)
        {
            var definition = HeroCatalog.TryGet(hero.HeroId, out var known) ? known : null;
            var tint = definition is null ? GameColors.PanelRaised : GameColors.RuneColor(definition.RuneAffinity);
            var name = definition?.Name ?? hero.HeroId;
            var role = definition is null ? string.Empty : definition.Role.ToString().ToUpperInvariant();

            var card = CreatePanel($"Bench {hero.InstanceId}", parent, selected ? GameColors.PanelRaised : GameColors.Panel);
            AddOutline(card, selected ? GameColors.WithAlpha(GameColors.Heal, 0.95f) : GameColors.WithAlpha(tint, 0.75f));
            MakeClickable(card, onClick);

            var layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 4, 4);
            layout.spacing = 1;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false;

            var swatch = CreatePanel("Bench Swatch", card.transform, tint);
            swatch.GetComponent<Image>().raycastTarget = false;
            AddLayoutElement(swatch, 12);
            CreateText(HeroShortName(name), card.transform, 11, GameColors.Text, TextAnchor.MiddleCenter).raycastTarget = false;
            CreateText(selected ? "ВЫБРАН" : role, card.transform, 8, selected ? GameColors.Heal : GameColors.Muted, TextAnchor.MiddleCenter).raycastTarget = false;
        }

        private void AddPreparationHeroToken(Transform parent, string heroId, Action onClick)
        {
            var definition = HeroCatalog.TryGet(heroId, out var known) ? known : null;
            var tint = definition is null ? GameColors.AllyCellOccupied : GameColors.RuneColor(definition.RuneAffinity);
            var label = HeroShortName(definition?.Name ?? heroId);

            var token = CreatePanel($"Board {heroId}", parent, tint);
            SetAnchoredFill(token, 0.10f, 0.10f, 0.10f, 0.10f);
            AddOutline(token, GameColors.Heal);
            MakeClickable(token, onClick);
            CreateOverlayText(label, token.transform, 13, GameColors.Text, TextAnchor.MiddleCenter);
        }

        private static void MakeClickable(GameObject target, Action onClick)
        {
            var image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            var button = target.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
        }

        private void OnBenchHeroClicked(string instanceId)
        {
            selectedBenchInstanceId = selectedBenchInstanceId == instanceId ? null : instanceId;
            ShowPreparationScreen();
        }

        private void OnPlacementCellClicked(TacticalPosition position)
        {
            if (string.IsNullOrEmpty(selectedBenchInstanceId))
            {
                return;
            }

            try
            {
                runState = runState.PlaceHeroFromBench(selectedBenchInstanceId, position);
            }
            catch (InvalidOperationException)
            {
                // Illegal placement (cell taken, field full, wrong side); leave state untouched.
            }

            selectedBenchInstanceId = null;
            ShowPreparationScreen();
        }

        private void OnBoardHeroClicked(string instanceId)
        {
            try
            {
                runState = runState.MoveHeroToBench(instanceId);
            }
            catch (InvalidOperationException)
            {
                // Bench full or hero not on board; leave state untouched.
            }

            selectedBenchInstanceId = null;
            ShowPreparationScreen();
        }

        private void EnsurePreparationDemoRoster()
        {
            if (runState.Phase != RunPhase.Preparation)
            {
                return;
            }

            // Seed a small demo bench only on a fresh run so the visual MVP can show
            // the bench -> field placement flow without a wired shop. Once the player
            // has any hero on the bench or board this is a no-op.
            if (runState.Team.Count > 0 || runState.Bench.Count > 0)
            {
                return;
            }

            var demoBench = new List<HeroInstance>
            {
                new HeroInstance("demo_iron_guard", "iron_guard", 1),
                new HeroInstance("demo_oath_archer", "oath_archer", 1),
                new HeroInstance("demo_field_medic", "field_medic", 1)
            };

            runState = runState with { Bench = demoBench };
        }

        private static string HeroShortName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var initials = string.Empty;
            var words = name.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                initials += char.ToUpperInvariant(word[0]);
                if (initials.Length >= 3)
                {
                    break;
                }
            }

            return initials.Length > 0 ? initials : name.Substring(0, 1).ToUpperInvariant();
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
            CreateActionButton(row.transform, "FIGHT", GameColors.ButtonPrimary, ShowCombatScreen);
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
            marker.GetComponent<Image>().raycastTarget = false;
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

        private void CreateActionButton(Transform parent, string label, Color color, Action onClick = null)
        {
            var buttonObject = CreatePanel($"Button {label}", parent, color);
            AddOutline(buttonObject, GameColors.WithAlpha(GameColors.Text, 0.20f));

            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = onClick != null;
            if (onClick != null)
            {
                var button = buttonObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => onClick());
            }

            CreateOverlayText(label, buttonObject.transform, 11, label == "FIGHT" ? GameColors.Background : GameColors.Text, TextAnchor.MiddleCenter);
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
            canvasObject.name = RuntimeCanvasName;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ContentWidth, ContentHeight);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void ClearGeneratedCanvases()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas == null)
                {
                    continue;
                }

                var canvasObject = canvas.gameObject;
                if (canvasObject.name == RuntimeCanvasName
                    || canvasObject.name == "Canvas"
                    || canvasObject.transform.Find("Rune Chess Game Surface") != null)
                {
                    Destroy(canvasObject);
                }
            }
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

        private static void SetLayoutWidth(GameObject gameObject, float preferredWidth, float flexibleWidth)
        {
            var layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = flexibleWidth;
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
