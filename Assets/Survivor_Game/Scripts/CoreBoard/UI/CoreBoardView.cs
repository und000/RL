using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 코어 보드 편집 화면. Tab으로 열고 닫으며, 트레이의 칩을 보드로 끌어다 꽂는다.
/// 드래그 중에는 모델을 건드리지 않고 미리보기만 움직이다가, 놓는 순간 한 번만 반영한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("Core Board/Core Board View")]
public class CoreBoardView : MonoBehaviour
{
    private static CoreBoardView active;
    private static int blockedThroughFrame = -1;
    public static bool IsBlockingGameplay => active != null || Time.frameCount <= blockedThroughFrame;
    public static bool IsMenuOpen => active != null;
    private bool ownsPause;
    private float previousTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { active = null; blockedThroughFrame = -1; }

    [Header("연결")]
    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private CoreBoardController board;
    [SerializeField] private ChipInventory inventory;
    [Tooltip("전투 중 편집을 막기 위해 참조한다. 없으면 항상 편집할 수 있다.")]
    [SerializeField] private RunManager runManager;

    [Header("열고 닫기")]
    [SerializeField] private Key toggleKey = Key.Tab;
    [SerializeField] private bool openOnStart;
    [Tooltip("칩을 회전시키는 키.")]
    [SerializeField] private Key rotateKey = Key.R;

    [Header("크기")]
    [SerializeField, Min(16f)] private float cellSize = 48f;
    [SerializeField] private Vector2 windowPadding = new Vector2(28f, 24f);
    [SerializeField, Min(40f)] private float trayHeight = 120f;

    [Header("색 - 창")]
    [SerializeField] private Color screenDimColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color windowColor = new Color(0.05f, 0.07f, 0.11f, 0.98f);
    [SerializeField] private Color trayColor = new Color(0.09f, 0.12f, 0.17f, 0.95f);

    [Header("색 - 보드 칸")]
    [SerializeField] private Color emptyCellColor = new Color(0.16f, 0.19f, 0.25f, 1f);
    [SerializeField] private Color blockedCellColor = new Color(0.07f, 0.07f, 0.08f, 1f);
    [SerializeField] private Color powerRailColor = new Color(0.95f, 0.8f, 0.25f, 1f);
    [SerializeField] private Color busCellColor = new Color(0.22f, 0.3f, 0.4f, 1f);

    [Header("색 - 칩")]
    [SerializeField] private Color sourceChipColor = new Color(0.95f, 0.78f, 0.3f, 1f);
    [SerializeField] private Color amplifierChipColor = new Color(0.45f, 0.7f, 0.95f, 1f);
    [SerializeField] private Color terminalChipColor = new Color(0.95f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color junctionChipColor = new Color(0.7f, 0.55f, 0.9f, 1f);
    [SerializeField] private Color passiveChipColor = new Color(0.6f, 0.65f, 0.7f, 1f);
    [Tooltip("전류가 닿지 않은 칩은 이 색으로 죽는다.")]
    [SerializeField] private Color deadChipColor = new Color(0.28f, 0.3f, 0.33f, 1f);
    [Header("색 - 핀")]
    [SerializeField] private Color inputPinColor = new Color(0.4f, 0.95f, 0.6f, 1f);
    [SerializeField] private Color outputPinColor = new Color(1f, 0.65f, 0.3f, 1f);

    [Header("색 - 미리보기")]
    [SerializeField] private Color validPreviewColor = new Color(0.4f, 0.95f, 0.5f, 0.75f);
    [SerializeField] private Color invalidPreviewColor = new Color(0.95f, 0.35f, 0.35f, 0.75f);

    private readonly List<ChipView> boardChipViews = new List<ChipView>();
    private readonly List<Vector2Int> shapeBuffer = new List<Vector2Int>(8);
    private bool needsRebuild;

    private RectTransform root;
    private RectTransform panel;
    private RectTransform window;
    private MinimapUI travelMap;
    private RectTransform boardArea;
    private RectTransform chipLayer;
    private RectTransform trayContent;
    private ScrollRect trayScroll;
    private RectTransform dragLayer;
    private TMP_Text circuitLabel;
    private TMP_Text hintLabel;
    private TMP_Text chipDetails;
    private ScrollRect detailsScroll;
    private Camera uiCamera;

    private Vector2 boardPixelSize;
    private bool isOpen;

    private ChipView dragGhost;
    private ChipView dragSource;
    private ChipDefinition dragChip;
    private int dragRotation;
    private int dragFromPlacedId;
    private bool dragOverBoard;
    private Vector2Int dragCell;
    private PlacementResult dragResult;
    private CoreBoardStats previewBaseline;
    private Vector2Int previewCell;
    private int previewRotation;
    private bool previewOverBoard;

    public bool IsOpen => isOpen;

    /// <summary>전투 중에는 배치를 바꿀 수 없다. 열어서 보는 것은 언제나 된다.</summary>
    public bool EditingAllowed
    {
        get
        {
            if (LevelUpUI.IsPopupOpen) return false;
            if (runManager == null || runManager.CurrentFloor == null) return true;
            foreach (RoomInstance room in runManager.CurrentFloor.Rooms)
            {
                if (room != null && room.IsCombatActive) return false;
            }
            return true;
        }
    }

    private void Awake()
    {
        root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        // 레벨업 카드 위에서도 보드 열람을 지원한다. 스테이지 전환 화면(30000)보다는 아래다.
        Canvas overlay = GetComponent<Canvas>();
        if (overlay == null) overlay = gameObject.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 20000;
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
    }

    private void Start()
    {
        if (board == null) board = FindFirstObjectByType<CoreBoardController>();
        if (inventory == null) inventory = FindFirstObjectByType<ChipInventory>();
        if (runManager == null) runManager = FindFirstObjectByType<RunManager>();

        if (board == null || !board.IsReady)
        {
            Debug.LogError("CoreBoardView에 쓸 수 있는 CoreBoardController가 없습니다.", this);
            enabled = false;
            return;
        }

        BuildFrame();
        board.OnBoardChanged += HandleBoardChanged;
        board.OnLayoutChanged += HandleLayoutChanged;
        if (inventory != null) inventory.OnInventoryChanged += RebuildTray;

        RebuildAll();
        SetOpen(openOnStart);
    }

    private void OnDestroy()
    {
        SetOpen(false);
        if (board != null)
        {
            board.OnBoardChanged -= HandleBoardChanged;
            board.OnLayoutChanged -= HandleLayoutChanged;
        }
        if (inventory != null) inventory.OnInventoryChanged -= RebuildTray;
    }

    private void Update()
    {
        if (runManager != null && runManager.IsRunOver)
        {
            SetOpen(false);
            return;
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[toggleKey].wasPressedThisFrame) SetOpen(!isOpen);
        if (isOpen && keyboard.escapeKey.wasPressedThisFrame) SetOpen(false);

        // 드래그 중 회전. 미리보기만 돌려 보고 놓을 때 확정한다.
        if (dragGhost != null && keyboard[rotateKey].wasPressedThisFrame)
        {
            dragRotation = BoardGeometry.NormalizeRotation(dragRotation + 1);
            dragGhost.SetRotation(dragRotation);
            EvaluateDragTarget();
        }
        // 드래그 위치를 유지한 채 긴 미리보기를 읽을 수 있다.
        if (dragGhost != null && detailsScroll != null && Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (wheel != 0f && chipDetails.rectTransform.rect.height > detailsScroll.viewport.rect.height)
                detailsScroll.verticalNormalizedPosition = Mathf.Clamp01(detailsScroll.verticalNormalizedPosition + wheel * .002f);
        }
    }

    public void SetOpen(bool open)
    {
        if (open == isOpen)
        {
            if (panel != null) panel.gameObject.SetActive(open);
            return;
        }
        if (open && (StageTransitionUI.IsBlockingGameplay || RoomChoiceUI.IsBlockingGameplay || RunPauseUI.IsBlockingGameplay ||
            (runManager != null && runManager.IsRunOver) || (active != null && active != this))) return;
        if (open)
        {
            StaggerImpactFeedback.CancelActive();
            active = this;
            // 보상창 위의 열람은 기존 일시정지를 빌린다. 닫을 때 보상창의 정지를 풀지 않는다.
            ownsPause = !LevelUpUI.IsPopupOpen && Time.timeScale > 0f;
            if (ownsPause) { previousTimeScale = Time.timeScale; Time.timeScale = 0f; }
            root.SetAsLastSibling();
        }
        else
        {
            if (ownsPause && (runManager == null || !runManager.IsRunOver) &&
                !LevelUpUI.IsPopupOpen && !StageTransitionUI.IsBlockingGameplay && Mathf.Approximately(Time.timeScale, 0f))
                Time.timeScale = previousTimeScale;
            ownsPause = false;
            if (active == this) active = null;
            blockedThroughFrame = Time.frameCount;
        }
        isOpen = open;
        // 루트는 계속 살려 둬야 Update가 돌아 Tab으로 다시 열 수 있다.
        if (panel != null) panel.gameObject.SetActive(open);
        if (!open)
        {
            CancelDrag();
            return;
        }

        if (needsRebuild) RebuildAll();
        else RefreshLabels();
    }

    private void OnDisable() { SetOpen(false); }

    private void LateUpdate()
    {
        if (!isOpen || window == null || root.rect.width <= 0f) return;
        // 지도(왼쪽), 인벤토리(가운데), 설명(오른쪽)을 함께 화면에 맞춘다.
        float totalWidth = window.sizeDelta.x + 460f + 330f;
        float totalHeight = Mathf.Max(window.sizeDelta.y, 420f);
        float scale = Mathf.Min(1f, Mathf.Min((root.rect.width - 32f) / totalWidth, (root.rect.height - 32f) / totalHeight));
        scale = Mathf.Max(.1f, scale);
        window.localScale = Vector3.one * scale;
        window.anchoredPosition = new Vector2(65f * scale, 0f);
    }

    private void TravelToRoom(RoomInstance room)
    {
        if (dragGhost != null || !isOpen || runManager == null) return;
        runManager.TryTravelToRoom(room, this);
    }

    // 화면 뼈대 ---------------------------------------------------------

    private void BuildFrame()
    {
        panel = CreateChild("Panel", root);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        CreateImage(panel, screenDimColor, true);

        CoreBoardLayout layout = board.State.Layout;
        boardPixelSize = new Vector2(layout.Width * cellSize, layout.Height * cellSize);

        Vector2 windowSize = new Vector2(
            Mathf.Max(boardPixelSize.x, 420f) + windowPadding.x * 2f,
            boardPixelSize.y + trayHeight + windowPadding.y * 3f + 110f);

        window = CreateChild("Window", panel);
        window.sizeDelta = windowSize;
        CreateImage(window, windowColor, true);
        RectTransform mapRect = CreateChild("TravelMap", window);
        travelMap = mapRect.gameObject.AddComponent<MinimapUI>();
        travelMap.ConfigureEmbedded(runManager, TravelToRoom);
        mapRect.anchoredPosition = new Vector2(-windowSize.x * .5f - 240f, 0f);

        RectTransform title = CreateChild("Title", window);
        title.sizeDelta = new Vector2(windowSize.x - windowPadding.x * 2f, 30f);
        title.anchoredPosition = new Vector2(0f, windowSize.y * 0.5f - windowPadding.y - 15f);
        CreateLabel(title, "코어 보드", 22f, TextAlignmentOptions.Left);
        RectTransform detailsPanel = CreateChild("ChipDetails", window);
        detailsPanel.sizeDelta = new Vector2(320f, 420f);
        detailsPanel.anchoredPosition = new Vector2(windowSize.x * .5f + 170f, 0f);
        CreateImage(detailsPanel, windowColor, true);
        detailsScroll = detailsPanel.gameObject.AddComponent<ScrollRect>();
        detailsScroll.horizontal = false;
        detailsScroll.movementType = ScrollRect.MovementType.Clamped;
        detailsScroll.scrollSensitivity = 24f;
        RectTransform viewport = CreateChild("Viewport", detailsPanel);
        viewport.sizeDelta = new Vector2(296f, 396f);
        viewport.gameObject.AddComponent<RectMask2D>();
        CreateImage(viewport, Color.clear, true);
        RectTransform detailsText = CreateChild("Text", viewport);
        detailsText.anchorMin = new Vector2(0f, 1f);
        detailsText.anchorMax = new Vector2(1f, 1f);
        detailsText.pivot = new Vector2(.5f, 1f);
        detailsText.sizeDelta = new Vector2(0f, 396f);
        chipDetails = CreateLabel(detailsText, "칩에 마우스를 올리면\n효과와 활성 상태를 확인합니다.", 18f, TextAlignmentOptions.TopLeft);
        detailsText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        detailsScroll.viewport = viewport;
        detailsScroll.content = detailsText;

        boardArea = CreateChild("BoardArea", window);
        boardArea.sizeDelta = boardPixelSize;
        boardArea.anchoredPosition = new Vector2(
            0f, windowSize.y * 0.5f - windowPadding.y - 50f - boardPixelSize.y * 0.5f);
        BuildBoardCells(layout);

        chipLayer = CreateChild("ChipLayer", boardArea);
        chipLayer.sizeDelta = boardPixelSize;

        circuitLabel = CreateLabel(
            CreateChild("Circuits", window), string.Empty, 15f, TextAlignmentOptions.TopLeft);
        RectTransform circuitRect = circuitLabel.rectTransform;
        circuitRect.sizeDelta = new Vector2(windowSize.x - windowPadding.x * 2f, 56f);
        circuitRect.anchoredPosition = new Vector2(
            0f, boardArea.anchoredPosition.y - boardPixelSize.y * 0.5f - 32f);

        RectTransform tray = CreateChild("Tray", window);
        tray.sizeDelta = new Vector2(windowSize.x - windowPadding.x * 2f, trayHeight);
        tray.anchoredPosition = new Vector2(
            0f, -windowSize.y * 0.5f + windowPadding.y + trayHeight * 0.5f);
        CreateImage(tray, trayColor, true);
        trayScroll = tray.gameObject.AddComponent<ScrollRect>();
        trayScroll.horizontal = false;
        trayScroll.movementType = ScrollRect.MovementType.Clamped;
        trayScroll.scrollSensitivity = 30f;
        RectTransform trayViewport = CreateChild("Viewport", tray);
        trayViewport.sizeDelta = new Vector2(tray.sizeDelta.x - 18f, trayHeight);
        trayViewport.anchoredPosition = new Vector2(-9f, 0f);
        trayViewport.gameObject.AddComponent<RectMask2D>();
        CreateImage(trayViewport, Color.clear, true);
        trayContent = CreateChild("TrayContent", trayViewport);
        trayContent.anchorMin = new Vector2(0f, 1f);
        trayContent.anchorMax = Vector2.one;
        trayContent.pivot = new Vector2(.5f, 1f);
        trayContent.sizeDelta = new Vector2(0f, trayHeight);
        trayScroll.viewport = trayViewport;
        trayScroll.content = trayContent;
        RectTransform bar = CreateChild("Scrollbar", tray);
        bar.sizeDelta = new Vector2(12f, trayHeight);
        bar.anchoredPosition = new Vector2(tray.sizeDelta.x * .5f - 6f, 0f);
        CreateImage(bar, emptyCellColor, true);
        RectTransform handle = CreateChild("Handle", bar);
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = Vector2.one;
        handle.sizeDelta = Vector2.zero;
        var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = CreateImage(handle, passiveChipColor, true);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        trayScroll.verticalScrollbar = scrollbar;

        hintLabel = CreateLabel(
            CreateChild("Hint", window), string.Empty, 14f, TextAlignmentOptions.Center);
        RectTransform hintRect = hintLabel.rectTransform;
        hintRect.sizeDelta = new Vector2(windowSize.x - windowPadding.x * 2f, 22f);
        hintRect.anchoredPosition = new Vector2(
            0f, tray.anchoredPosition.y + trayHeight * 0.5f + 14f);

        dragLayer = CreateChild("DragLayer", panel);
        dragLayer.anchorMin = Vector2.zero;
        dragLayer.anchorMax = Vector2.one;
        dragLayer.offsetMin = Vector2.zero;
        dragLayer.offsetMax = Vector2.zero;
    }

    private void BuildBoardCells(CoreBoardLayout layout)
    {
        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                RectTransform slot = CreateChild("Cell_" + cell, boardArea);
                slot.sizeDelta = Vector2.one * (cellSize - 4f);
                slot.anchoredPosition = CellCenterLocal(cell);
                CreateImage(slot, GetCellColor(layout.GetCell(cell)), false);

            }
        }
    }

    private Color GetCellColor(BoardCellType type)
    {
        switch (type)
        {
            case BoardCellType.Blocked: return blockedCellColor;
            case BoardCellType.PowerRail: return powerRailColor;
            case BoardCellType.Bus: return busCellColor;
            default: return emptyCellColor;
        }
    }

    private Vector2 CellCenterLocal(Vector2Int cell) => new Vector2(
        (cell.x + 0.5f) * cellSize - boardPixelSize.x * 0.5f,
        (cell.y + 0.5f) * cellSize - boardPixelSize.y * 0.5f);

    // 다시 그리기 -------------------------------------------------------

    /// <summary>
    /// 보드 배치가 바뀌어도 창이 닫혀 있으면 다시 그리지 않고
    /// 표시만 밀린 것으로 두었다가 열 때 한 번에 갱신한다.
    /// </summary>
    private void HandleBoardChanged(CoreBoardStats stats)
    {
        if (!isOpen)
        {
            needsRebuild = true;
            return;
        }
        RebuildAll();
    }

    /// <summary>
    /// 보드가 넓어지면 칸 수와 창 크기가 통째로 달라지므로 화면을 새로 짓는다.
    /// 런 한 번에 몇 차례뿐이라 비용은 문제되지 않는다.
    /// </summary>
    private void HandleLayoutChanged()
    {
        bool wasOpen = isOpen;
        CancelDrag();

        if (panel != null) DetachAndDestroy(panel);
        boardChipViews.Clear();

        BuildFrame();
        RebuildAll();
        SetOpen(wasOpen);
    }

    private void RebuildAll()
    {
        CancelDrag();
        needsRebuild = false;
        RebuildBoardChips();
        RebuildTray();
        RefreshLabels();
    }

    private void RebuildBoardChips()
    {
        foreach (ChipView view in boardChipViews)
        {
            if (view != null) DetachAndDestroy(view.transform);
        }
        boardChipViews.Clear();

        foreach (PlacedChip placed in board.State.Placements)
        {
            if (placed.Chip == null) continue;

            ChipView view = CreateChipView(
                chipLayer, placed.Chip, placed.Rotation, placed.Id, true,
                ResolveChipColor(placed));
            view.Rect.anchoredPosition = CellCenterLocal(placed.Origin);
            boardChipViews.Add(view);
        }
    }

    /// <summary>전류가 닿아 작동하는 칩과 무전원 칩을 색으로 구분한다.</summary>
    private Color ResolveChipColor(PlacedChip placed)
    {
        CoreBoardState state = board.State;
        if (state.IsEnergized(placed)) return GetChipColor(placed.Chip.Category);
        return deadChipColor;
    }

    private void RebuildTray()
    {
        if (dragGhost != null) CancelDrag();
        if (trayContent == null) return;

        for (int index = trayContent.childCount - 1; index >= 0; index--)
        {
            DetachAndDestroy(trayContent.GetChild(index));
        }
        if (inventory == null) return;

        float scrollPosition = trayScroll.verticalNormalizedPosition;
        List<ChipTrayLayout.Entry> entries = ChipTrayLayout.Group(inventory.Chips);
        float width = trayScroll.viewport.sizeDelta.x;
        float slot = Mathf.Min(width, cellSize * 2.4f);
        int perRow = ChipTrayLayout.Columns(width, slot);
        float height = ChipTrayLayout.Height(entries.Count, perRow, slot, trayHeight);
        trayContent.sizeDelta = new Vector2(0f, height);
        float startX = -perRow * slot * .5f + slot * .5f;

        for (int index = 0; index < entries.Count; index++)
        {
            ChipDefinition chip = entries[index].Chip;
            Vector2 center = new Vector2(startX + index % perRow * slot, -index / perRow * slot - slot * .5f);
            float scale = ChipTrayLayout.ShapeScale(chip, cellSize, slot - 12f, slot - 40f);
            ChipView view = CreateChipView(
                trayContent, chip, 0, 0, true, GetChipColor(chip.Category));
            view.Rect.anchorMin = view.Rect.anchorMax = new Vector2(.5f, 1f);
            view.Rect.localScale = Vector3.one * scale;
            view.Rect.anchoredPosition = center + new Vector2(0f, 12f) + ShapeCenterOffset(chip, 0) * scale;
            RectTransform caption = CreateChild("NameAndCount", trayContent);
            caption.anchorMin = caption.anchorMax = new Vector2(.5f, 1f);
            caption.sizeDelta = new Vector2(slot - 6f, 30f);
            caption.anchoredPosition = center + new Vector2(0f, -slot * .5f + 17f);
            TMP_Text label = CreateLabel(caption, chip.DisplayName + " ×" + entries[index].Count, 12f, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 12f;
        }
        trayScroll.StopMovement();
        trayScroll.verticalNormalizedPosition = entries.Count == 0 ? 1f : Mathf.Clamp01(scrollPosition);
    }

    /// <summary>칩 모양의 무게중심을 기준점으로 되돌리는 보정. 트레이에서 가운데 맞출 때 쓴다.</summary>
    private Vector2 ShapeCenterOffset(ChipDefinition chip, int rotation)
    {
        chip.GetRotatedCells(rotation, shapeBuffer);
        if (shapeBuffer.Count == 0) return Vector2.zero;

        Vector2Int minimum = shapeBuffer[0];
        Vector2Int maximum = shapeBuffer[0];
        foreach (Vector2Int cell in shapeBuffer)
        {
            minimum = Vector2Int.Min(minimum, cell);
            maximum = Vector2Int.Max(maximum, cell);
        }
        Vector2 center = (Vector2)(minimum + maximum) * 0.5f;
        return -center * cellSize;
    }

    private ChipView CreateChipView(
        RectTransform parent,
        ChipDefinition chip,
        int rotation,
        int placedId,
        bool interactive,
        Color color)
    {
        RectTransform rect = CreateChild("Chip_" + chip.name, parent);
        ChipView view = rect.gameObject.AddComponent<ChipView>();
        view.Bind(
            this, chip, rotation, placedId, cellSize, interactive,
            color * chip.TintColor, inputPinColor, outputPinColor);
        return view;
    }

    private Color GetChipColor(ChipCategory category)
    {
        switch (category)
        {
            case ChipCategory.Source: return sourceChipColor;
            case ChipCategory.Amplifier: return amplifierChipColor;
            case ChipCategory.Terminal: return terminalChipColor;
            case ChipCategory.Junction: return junctionChipColor;
            default: return passiveChipColor;
        }
    }

    private void RefreshLabels()
    {
        if (circuitLabel != null)
        {
            List<ResolvedCircuit> circuits = board.State.Solution.Circuits;
            if (circuits.Count == 0)
            {
                circuitLabel.text = "이어진 회로 없음";
            }
            else
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                foreach (ResolvedCircuit circuit in circuits)
                {
                    if (builder.Length > 0) builder.Append('\n');
                    builder.Append(circuit.Describe());
                    if (circuit.UniformFamily) builder.Append("  [계열 통일]");
                }
                circuitLabel.text = builder.ToString();
            }
        }

        if (hintLabel != null)
        {
            hintLabel.text = EditingAllowed
                ? "드래그 1개 · R 회전 · 우클릭 빼기 · 휠 스크롤"
                : LevelUpUI.IsPopupOpen ? "보상 선택 중에는 열람만 가능합니다 · Tab 닫기" : "전투 중에는 열람만 가능합니다 · Tab 닫기";
            hintLabel.color = EditingAllowed
                ? new Color(0.55f, 0.62f, 0.7f)
                : new Color(1f, 0.55f, 0.4f);
        }
    }

    public void ShowChipDetails(ChipView view)
    {
        if (dragGhost != null || view == null || view.Chip == null || chipDetails == null) return;
        string status = view.PlacedId == 0 ? "미장착" : board.State.IsEnergized(board.State.GetChip(view.PlacedId)) ? "활성" : "전원 미연결";
        chipDetails.text = view.Chip.DisplayName + " · " + status + "\n\n" + RewardPresentation.DescribeChip(view.Chip, board);
        if (detailsScroll != null) detailsScroll.verticalNormalizedPosition = 1f;
    }

    // 드래그 ------------------------------------------------------------

    internal void BeginDrag(ChipView source, PointerEventData eventData)
    {
        if (!EditingAllowed || source == null || source.Chip == null) return;
        if (trayScroll != null) { trayScroll.StopMovement(); trayScroll.enabled = false; }

        dragSource = source;
        dragChip = source.Chip;
        dragRotation = source.Rotation;
        dragFromPlacedId = source.PlacedId;
        previewBaseline = null;

        // 원본은 지우지 않고 흐리게만 둔다. 지우면 드래그 이벤트가 끊긴다.
        source.SetTint(new Color(1f, 1f, 1f, 0.25f));

        RectTransform ghostRect = CreateChild("Ghost", dragLayer);
        dragGhost = ghostRect.gameObject.AddComponent<ChipView>();
        dragGhost.Bind(
            this, dragChip, dragRotation, 0, cellSize, false,
            validPreviewColor, inputPinColor, outputPinColor);

        UpdateDrag(eventData);
    }

    internal void UpdateDrag(PointerEventData eventData)
    {
        if (dragGhost == null) return;

        dragOverBoard = TryGetCellUnderPointer(eventData, out dragCell);
        if (dragOverBoard)
        {
            dragGhost.Rect.position = boardArea.TransformPoint(CellCenterLocal(dragCell));
        }
        else if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragLayer, eventData.position, uiCamera, out Vector3 world))
        {
            dragGhost.Rect.position = world;
        }

        EvaluateDragTarget();
    }

    /// <summary>지금 놓으면 되는지 다시 판정해 미리보기 색과 안내 문구를 갱신한다.</summary>
    private void EvaluateDragTarget()
    {
        if (dragGhost == null) return;
        if (!EditingAllowed) { CancelDrag(); RefreshLabels(); return; }
        if (previewBaseline == board.State.Stats && previewCell == dragCell &&
            previewRotation == dragRotation && previewOverBoard == dragOverBoard) return;
        previewBaseline = board.State.Stats;
        previewCell = dragCell;
        previewRotation = dragRotation;
        previewOverBoard = dragOverBoard;
        CoreBoardState preview = null;
        PlacedChip proposed = null;

        if (!dragOverBoard)
        {
            dragResult = PlacementResult.Fail(PlacementError.OutOfBoard, Vector2Int.zero);
            // 보드 밖은 "트레이로 빼기"라서 꽂힌 칩에게는 유효한 동작이다.
            dragGhost.SetTint(dragFromPlacedId != 0 ? validPreviewColor : invalidPreviewColor);
            if (hintLabel != null) hintLabel.text = dragFromPlacedId != 0 ? "놓으면 트레이로 뺍니다" : "보드 위에 놓아 주세요";
            if (dragFromPlacedId != 0) board.State.TryPreviewRemoval(dragFromPlacedId, out preview);
            ShowDragPreview(preview, null);
            return;
        }

        dragResult = board.State.CanPlace(dragChip, dragCell, dragRotation, dragFromPlacedId);
        dragGhost.SetTint(dragResult.IsValid ? validPreviewColor : invalidPreviewColor);
        if (hintLabel != null) hintLabel.text = dragResult.Describe();
        if (dragResult.IsValid)
            board.State.TryPreviewPlacement(dragChip, dragCell, dragRotation, dragFromPlacedId, out preview, out proposed);
        ShowDragPreview(preview, proposed);
    }

    private void ShowDragPreview(CoreBoardState preview, PlacedChip proposed)
    {
        if (chipDetails != null)
            chipDetails.text = preview != null
                ? BoardPreviewPresentation.Describe(board.State, preview, dragChip, proposed)
                : dragChip.DisplayName + "\n\n" + (dragOverBoard ? dragResult.Describe() : "보드 위에서 배치 결과를 확인하세요.");
        if (detailsScroll != null) detailsScroll.verticalNormalizedPosition = 1f;
        foreach (ChipView view in boardChipViews)
        {
            if (view == null || view == dragSource) continue;
            if (preview == null) { view.ResetTint(); continue; }
            bool energized = preview.IsEnergized(preview.GetChip(view.PlacedId));
            view.SetTint((energized ? GetChipColor(view.Chip.Category) : deadChipColor) * view.Chip.TintColor);
        }
    }

    internal void EndDrag(PointerEventData eventData)
    {
        if (dragGhost == null) return;
        if (!EditingAllowed) { CancelDrag(); RefreshLabels(); return; }
        UpdateDrag(eventData);
        if (dragGhost == null) return;

        ChipDefinition chip = dragChip;
        int placedId = dragFromPlacedId;
        bool overBoard = dragOverBoard;
        Vector2Int cell = dragCell;
        int rotation = dragRotation;
        bool valid = dragResult.IsValid;

        CancelDrag();

        if (overBoard && valid)
        {
            if (placedId != 0) board.TryMove(placedId, cell, rotation);
            else if (inventory != null && inventory.Contains(chip) && board.TryPlace(chip, cell, rotation))
            {
                inventory.Remove(chip);
            }
        }
        else if (!overBoard && placedId != 0)
        {
            // 보드 밖에 놓으면 트레이로 되돌린다.
            if (board.Remove(placedId) && inventory != null) inventory.Add(chip);
        }

        RebuildAll();
    }

    /// <summary>보드에 꽂힌 칩을 우클릭으로 바로 빼낸다.</summary>
    internal void RemoveToTray(ChipView view)
    {
        if (!EditingAllowed || view == null || view.PlacedId == 0) return;

        ChipDefinition chip = view.Chip;
        if (board.Remove(view.PlacedId) && inventory != null) inventory.Add(chip);
    }

    private void CancelDrag()
    {
        if (trayScroll != null) trayScroll.enabled = true;
        bool wasDragging = dragGhost != null;
        if (dragGhost != null) Destroy(dragGhost.gameObject);
        if (dragSource != null) dragSource.ResetTint();
        foreach (ChipView view in boardChipViews) if (view != null) view.ResetTint();
        if (wasDragging && chipDetails != null) chipDetails.text = "칩에 마우스를 올리면\n효과와 활성 상태를 확인합니다.";
        previewBaseline = null;

        dragGhost = null;
        dragSource = null;
        dragChip = null;
        dragFromPlacedId = 0;
        dragOverBoard = false;
    }

    private bool TryGetCellUnderPointer(PointerEventData eventData, out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardArea, eventData.position, uiCamera, out Vector2 local))
        {
            return false;
        }

        Vector2 fromCorner = local + boardPixelSize * 0.5f;
        cell = new Vector2Int(
            Mathf.FloorToInt(fromCorner.x / cellSize),
            Mathf.FloorToInt(fromCorner.y / cellSize));
        return board.State.Layout.Contains(cell);
    }

    // 만들기 도우미 -----------------------------------------------------

    /// <summary>
    /// Destroy는 프레임 끝에야 처리되므로, 낡은 뷰가 그 사이 포인터 이벤트를 받거나
    /// 자식 수를 헷갈리게 만든다. 부모에서 먼저 떼어내고 없앤다.
    /// </summary>
    internal static void DetachAndDestroy(Transform target)
    {
        if (target == null) return;
        target.SetParent(null, false);
        Destroy(target.gameObject);
    }

    private static RectTransform CreateChild(string childName, RectTransform parent)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform));
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static Image CreateImage(RectTransform target, Color color, bool blockRaycast)
    {
        Image image = target.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = blockRaycast;
        return image;
    }

    private static TMP_Text CreateLabel(
        RectTransform target, string text, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = target.gameObject.AddComponent<TextMeshProUGUI>();
        GameFontManager.ApplyFont(label);
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.color = new Color(0.85f, 0.9f, 0.95f);
        return label;
    }
}
