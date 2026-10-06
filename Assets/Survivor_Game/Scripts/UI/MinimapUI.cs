using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 우측 하단에 뜨는 사각형 미니맵. 생성된 층의 방을 격자로 그리고,
/// 지나간 방·클리어한 방·지금 있는 방을 색으로 구분한다.
/// Canvas 아래 빈 오브젝트에 붙이면 나머지는 런타임에 알아서 만든다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("UI/Minimap UI")]
public class MinimapUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private RunManager runManager;
    [Tooltip("비워 두면 Player 태그로 찾는다.")]
    [SerializeField] private Transform player;

    [Header("패널")]
    [SerializeField] private Vector2 panelSize = new Vector2(480f, 340f);
    [Tooltip("화면 우측·하단 모서리에서 띄울 여백.")]
    [SerializeField] private Vector2 screenMargin = new Vector2(18f, 18f);
    [SerializeField, Min(0f)] private float borderThickness = 2f;
    [Tooltip("패널 안쪽에서 방 격자가 닿지 않게 둘 여백.")]
    [SerializeField, Min(0f)] private float innerPadding = 10f;

    [Header("방 칸")]
    [SerializeField] private Vector2 roomSize = new Vector2(40f, 28f);
    [Tooltip("방과 방 사이 간격. 이 틈에 통로가 그려진다.")]
    [SerializeField] private Vector2 roomGap = new Vector2(16f, 16f);
    [SerializeField, Min(1f)] private float corridorThickness = 8f;
    [SerializeField, Min(1f)] private float playerMarkerSize = 6f;

    [Header("색 - 틀")]
    [SerializeField] private Color borderColor = new Color(0.35f, 0.75f, 0.95f, 0.85f);
    [SerializeField] private Color backgroundColor = new Color(0.03f, 0.05f, 0.09f, 0.78f);
    [SerializeField] private Color corridorColor = new Color(0.35f, 0.55f, 0.7f, 0.75f);

    [Header("색 - 방")]
    [SerializeField] private Color normalRoomColor = new Color(0.55f, 0.68f, 0.8f, 1f);
    [SerializeField] private Color startRoomColor = new Color(0.45f, 0.85f, 0.6f, 1f);
    [SerializeField] private Color eliteRoomColor = new Color(0.95f, 0.6f, 0.3f, 1f);
    [SerializeField] private Color treasureRoomColor = new Color(0.95f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color shopRoomColor = new Color(0.5f, 0.8f, 0.95f, 1f);
    [SerializeField] private Color bossRoomColor = new Color(0.95f, 0.35f, 0.4f, 1f);
    [Tooltip("출구 방에 들어가 발견한 뒤 표시한다. 개방 시 이 색을 쓴다.")]
    [SerializeField] private Color exitMarkerColor = new Color(.2f, .9f, 1f, 1f);
    [Tooltip("발견만 하고 아직 들어가지 않은 방.")]
    [SerializeField] private Color undiscoveredColor = new Color(0.25f, 0.3f, 0.38f, 0.7f);

    [Header("색 - 상태")]
    [Tooltip("아직 클리어하지 않은 방은 이 색 쪽으로 어두워진다.")]
    [SerializeField] private Color unclearedTint = new Color(0.1f, 0.12f, 0.16f, 1f);
    [SerializeField, Range(0f, 1f)] private float unclearedBlend = 0.55f;
    [SerializeField] private Color currentRoomColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float currentRoomHighlight = 0.5f;
    [SerializeField] private Color playerColor = new Color(1f, 1f, 1f, 0.95f);

    private readonly Dictionary<RoomInstance, Image> roomIcons =
        new Dictionary<RoomInstance, Image>();
    private readonly Dictionary<RoomInstance, RoomMapIcon> glyphs = new Dictionary<RoomInstance, RoomMapIcon>();
    private readonly Dictionary<RoomInstance, Button> buttons = new Dictionary<RoomInstance, Button>();
    private readonly List<(RoomInstance from, RoomInstance to, Image image)> corridors = new List<(RoomInstance, RoomInstance, Image)>();
    private bool embedded;
    private System.Action<RoomInstance> travel;
    private CanvasGroup visibility;
    private float nextRefresh;

    public void ConfigureEmbedded(RunManager run, System.Action<RoomInstance> onTravel)
    {
        runManager = run;
        player = run != null ? run.Player : null;
        travel = onTravel;
        embedded = true;
        panelSize = new Vector2(440f, 340f);
        roomSize = new Vector2(52f, 38f);
        roomGap = new Vector2(16f, 16f);
        innerPadding = 32f;
        ApplyPanelPlacement();
        RectTransform title = CreateChild("Title", panelRect);
        title.sizeDelta = new Vector2(420f, 26f);
        title.anchoredPosition = new Vector2(0f, 150f);
        AddText(title, "방 지도 · 방문한 방 클릭으로 이동", 18f);
        RectTransform legend = CreateChild("Legend", panelRect);
        legend.sizeDelta = new Vector2(420f, 26f);
        legend.anchoredPosition = new Vector2(0f, -150f);
        AddText(legend, "전투 중 이동 불가 · 출구도 발견 후 표시", 14f);
    }

    private static void AddText(RectTransform rect, string value, float size)
    {
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        GameFontManager.ApplyFont(text);
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }
    private readonly Dictionary<Vector2Int, RoomInstance> roomsByCell =
        new Dictionary<Vector2Int, RoomInstance>();

    private RectTransform panelRect;
    private RectTransform contentRect;
    private RectTransform playerMarker;
    private RoomMapIcon exitMarker;
    private GeneratedFloor floor;
    private RoomInstance currentRoom;
    private Vector2Int gridMinimum;
    private Vector2 contentSize;
    private bool placementDirty;

    private void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        ApplyPanelPlacement();
        BuildFrame();
        visibility = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        if (runManager == null)
        {
            Debug.LogError("씬에 RunManager가 없어 미니맵을 그릴 수 없습니다.", this);
            enabled = false;
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        runManager.OnFloorStarted += HandleFloorStarted;

        // RunManager가 먼저 Start를 돌았다면 이벤트는 이미 지나갔으므로 직접 집어 온다.
        if (runManager.CurrentFloor != null) Rebuild(runManager.CurrentFloor);
    }

    private void OnDestroy()
    {
        if (runManager != null) runManager.OnFloorStarted -= HandleFloorStarted;
    }

    private void HandleFloorStarted(int chapterIndex, int floorIndex, FloorProfile profile)
    {
        Rebuild(runManager.CurrentFloor);
    }

    private void Update()
    {
        if (placementDirty)
        {
            placementDirty = false;
            ApplyPanelPlacement();
        }

        // 층이 통째로 갈리면 파괴된 방을 붙들고 있게 되므로 다시 짓는다.
        if (runManager != null && runManager.CurrentFloor != floor)
        {
            Rebuild(runManager.CurrentFloor);
        }
        if (floor == null) return;

        UpdateCurrentRoom();
        visibility.alpha = !embedded && CoreBoardView.IsMenuOpen ? 0f : 1f;
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + .1f;
            RefreshRoomColors();
        }
        UpdatePlayerMarker();
    }

    // 패널 뼈대 ---------------------------------------------------------

    /// <summary>패널을 화면 우측 하단에 고정한다.</summary>
    private void ApplyPanelPlacement()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();

        if (embedded)
        {
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(.5f, .5f);
            panelRect.sizeDelta = panelSize;
            return;
        }
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = new Vector2(-screenMargin.x, screenMargin.y);
    }

    /// <summary>테두리·배경·내용 컨테이너·플레이어 점을 한 번만 만들어 둔다.</summary>
    private void BuildFrame()
    {
        Image border = GetComponent<Image>();
        if (border == null) border = gameObject.AddComponent<Image>();
        border.color = borderColor;
        border.raycastTarget = false;

        RectTransform background = CreateChild("Background", panelRect);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = new Vector2(borderThickness, borderThickness);
        background.offsetMax = new Vector2(-borderThickness, -borderThickness);
        CreateImage(background, backgroundColor);

        contentRect = CreateChild("Content", panelRect);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        playerMarker = CreateChild("PlayerMarker", contentRect);
        playerMarker.sizeDelta = Vector2.one * playerMarkerSize;
        CreateImage(playerMarker, playerColor);
        playerMarker.gameObject.SetActive(false);
    }

    private static RectTransform CreateChild(string childName, RectTransform parent)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform));
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static Image CreateImage(RectTransform target, Color color)
    {
        Image image = target.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // 층 구축 -----------------------------------------------------------

    private void Rebuild(GeneratedFloor generatedFloor)
    {
        ClearRooms();
        floor = generatedFloor;
        if (floor == null || floor.Rooms.Count == 0) return;

        foreach (RoomInstance room in floor.Rooms)
        {
            if (room != null) roomsByCell[room.GridPosition] = room;
        }

        MeasureGrid();
        BuildCorridors();
        BuildRoomIcons();

        nextRefresh = 0f;
        playerMarker.SetAsLastSibling();
    }

    private void ClearRooms()
    {
        exitMarker = null;
        roomIcons.Clear();
        glyphs.Clear();
        buttons.Clear();
        corridors.Clear();
        roomsByCell.Clear();
        currentRoom = null;
        floor = null;

        if (contentRect == null) return;
        for (int index = contentRect.childCount - 1; index >= 0; index--)
        {
            Transform child = contentRect.GetChild(index);
            if (child == playerMarker) continue;
            // Destroy는 프레임 끝에 처리되므로 먼저 떼어내 낡은 칸이 남지 않게 한다.
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
        playerMarker.gameObject.SetActive(false);
        contentRect.localScale = Vector3.one;
    }

    /// <summary>격자 크기를 재고, 패널 안에 다 들어가도록 내용 전체를 축소한다.</summary>
    private void MeasureGrid()
    {
        Vector2Int minimum = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int maximum = new Vector2Int(int.MinValue, int.MinValue);
        foreach (RoomInstance room in floor.Rooms)
        {
            if (room == null) continue;
            minimum = Vector2Int.Min(minimum, room.GridPosition);
            maximum = Vector2Int.Max(maximum, room.GridPosition);
        }

        gridMinimum = minimum;
        Vector2Int span = maximum - minimum + Vector2Int.one;
        Vector2 pitch = roomSize + roomGap;
        contentSize = new Vector2(
            span.x * pitch.x - roomGap.x,
            span.y * pitch.y - roomGap.y);

        float consumed = 2f * (borderThickness + innerPadding);
        float scaleX = (panelSize.x - consumed) / Mathf.Max(1f, contentSize.x);
        float scaleY = (panelSize.y - consumed) / Mathf.Max(1f, contentSize.y);
        float scale = Mathf.Min(1f, Mathf.Min(scaleX, scaleY));
        contentRect.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>격자 좌표를 내용 컨테이너 안의 위치로 바꾼다.</summary>
    private Vector2 CellToLocal(Vector2Int gridPosition)
    {
        Vector2 pitch = roomSize + roomGap;
        Vector2 offset = new Vector2(
            (gridPosition.x - gridMinimum.x) * pitch.x,
            (gridPosition.y - gridMinimum.y) * pitch.y);
        return offset - (contentSize - roomSize) * 0.5f;
    }

    private void BuildRoomIcons()
    {
        foreach (RoomInstance room in floor.Rooms)
        {
            if (room == null) continue;

            RectTransform icon = CreateChild("Room_" + room.GridPosition, contentRect);
            icon.sizeDelta = roomSize;
            icon.anchoredPosition = CellToLocal(room.GridPosition);

            Image image = CreateImage(icon, normalRoomColor);
            image.enabled = false;
            roomIcons[room] = image;
            RectTransform glyphRect = CreateChild("RoomSymbol", icon);
            glyphRect.sizeDelta = Vector2.one * (roomSize.y - 8f);
            RoomMapIcon glyph = glyphRect.gameObject.AddComponent<RoomMapIcon>();
            glyph.raycastTarget = false;
            glyphs[room] = glyph;
            if (embedded)
            {
                image.raycastTarget = true;
                Button button = icon.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                RoomInstance target = room;
                button.onClick.AddListener(() => travel?.Invoke(target));
                buttons[room] = button;
            }
            if (room == floor.ExitRoom)
            {
                RectTransform marker = CreateChild("ExitMarker", icon);
                marker.anchorMin = marker.anchorMax = Vector2.one;
                marker.anchoredPosition = new Vector2(-3f, 0f);
                marker.sizeDelta = Vector2.one * 14f;
                RoomMapIcon exitGlyph = marker.gameObject.AddComponent<RoomMapIcon>();
                exitGlyph.SetSymbol(RoomMapSymbol.Exit);
                exitGlyph.raycastTarget = false;
                exitGlyph.color = exitMarkerColor;
                exitMarker = exitGlyph;
                exitMarker.enabled = false;
                RectTransform exitCaption = CreateChild("ExitCaption", icon);
                exitCaption.sizeDelta = new Vector2(roomSize.x, 12f);
                exitCaption.anchoredPosition = new Vector2(0f, -roomSize.y * .5f - 6f);
                AddText(exitCaption, "출구", 10f);
            }
            icon.gameObject.SetActive(false);
        }
    }

    /// <summary>맞닿은 방 사이의 틈에 통로를 그린다. 동쪽·북쪽만 봐서 중복을 피한다.</summary>
    private void BuildCorridors()
    {
        Vector2 pitch = roomSize + roomGap;

        foreach (RoomInstance room in floor.Rooms)
        {
            if (room == null) continue;

            TryBuildCorridor(room, RoomDirection.East, pitch);
            TryBuildCorridor(room, RoomDirection.North, pitch);
        }
    }

    private void TryBuildCorridor(RoomInstance room, RoomDirection direction, Vector2 pitch)
    {
        Vector2Int neighbourCell = room.GridPosition + direction.ToOffset();
        if (!roomsByCell.TryGetValue(neighbourCell, out RoomInstance neighbour)) return;
        RoomDoor door = room.GetDoor(direction);
        if (door == null || door.LinkedDoor == null || door.LinkedDoor.Owner != neighbour) return;

        bool horizontal = direction == RoomDirection.East;
        RectTransform corridor = CreateChild("Corridor_" + room.GridPosition, contentRect);
        corridor.sizeDelta = horizontal
            ? new Vector2(roomGap.x + 2f, corridorThickness)
            : new Vector2(corridorThickness, roomGap.y + 2f);
        corridor.anchoredPosition = CellToLocal(room.GridPosition) + (horizontal
            ? new Vector2(pitch.x * 0.5f, 0f)
            : new Vector2(0f, pitch.y * 0.5f));
        Image line = CreateImage(corridor, corridorColor);
        line.enabled = false;
        corridors.Add((room, neighbour, line));
    }

    // 갱신 --------------------------------------------------------------

    private void UpdateCurrentRoom()
    {
        if (player == null) return;

        RoomInstance room = FindRoomNear(player.position);
        if (room == null) return;

        currentRoom = room;

    }

    /// <summary>플레이어를 품고 있는 방. 통로 위에 있으면 가장 가까운 방으로 친다.</summary>
    private RoomInstance FindRoomNear(Vector3 worldPosition)
    {
        RoomInstance nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (RoomInstance room in floor.Rooms)
        {
            if (room == null) continue;

            Vector2 delta = (Vector2)worldPosition - (Vector2)room.transform.position;
            Vector2 half = room.CellSize * 0.5f;
            if (Mathf.Abs(delta.x) <= half.x && Mathf.Abs(delta.y) <= half.y) return room;

            float distance = delta.sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearest = room;
        }
        return nearest;
    }

    private void RefreshRoomColors()
    {
        foreach (var corridor in corridors)
            corridor.image.enabled = corridor.from.HasEntered && corridor.to.HasEntered;
        foreach (KeyValuePair<RoomInstance, Image> pair in roomIcons)
        {
            RoomInstance room = pair.Key;
            Image image = pair.Value;
            if (room == null || image == null) continue;
            bool visited = room.HasEntered;
            bool discovered = RoomMapState.Visible(visited);
            image.gameObject.SetActive(discovered);
            image.enabled = discovered;
            if (!discovered) continue;
            image.color = ResolveRoomColor(room, visited, room == currentRoom);
            RoomMapIcon glyph = glyphs[room];
            glyph.SetSymbol(RoomMapState.Symbol(room.Kind, visited, room.IsCleared, room.HasMapRewards));
            glyph.color = image.color.grayscale > .55f ? Color.black : Color.white;
            if (buttons.TryGetValue(room, out Button button)) button.interactable = runManager.CanTravelToRoom(room);
            if (room == floor.ExitRoom && exitMarker != null)
            {
                exitMarker.enabled = visited;
                exitMarker.color = floor.IsCombatCleared ? exitMarkerColor : new Color(1f, .65f, .2f);
            }
        }
    }

    private Color ResolveRoomColor(RoomInstance room, bool visited, bool isCurrent)
    {
        if (!visited) return undiscoveredColor;

        Color color = GetKindColor(room.Kind);
        if (!room.IsCleared) color = Color.Lerp(color, unclearedTint, unclearedBlend);
        if (isCurrent) color = Color.Lerp(color, currentRoomColor, currentRoomHighlight);
        return color;
    }

    private Color GetKindColor(RoomKind kind)
    {
        switch (kind)
        {
            case RoomKind.Start: return startRoomColor;
            case RoomKind.Elite: return eliteRoomColor;
            case RoomKind.Treasure: return treasureRoomColor;
            case RoomKind.Shop: return shopRoomColor;
            case RoomKind.Boss: return bossRoomColor;
            default: return normalRoomColor;
        }
    }

    /// <summary>플레이어 점은 방 안에서의 상대 위치까지 반영해 부드럽게 움직인다.</summary>
    private void UpdatePlayerMarker()
    {
        if (player == null || currentRoom == null || !currentRoom.HasEntered)
        {
            playerMarker.gameObject.SetActive(false);
            return;
        }

        playerMarker.gameObject.SetActive(true);

        Vector2 delta = (Vector2)player.position - (Vector2)currentRoom.transform.position;
        Vector2 cellSize = currentRoom.CellSize;
        Vector2 normalized = new Vector2(
            Mathf.Clamp(delta.x / Mathf.Max(0.01f, cellSize.x), -0.5f, 0.5f),
            Mathf.Clamp(delta.y / Mathf.Max(0.01f, cellSize.y), -0.5f, 0.5f));

        playerMarker.anchoredPosition =
            CellToLocal(currentRoom.GridPosition) + normalized * roomSize;
    }

    private void OnValidate()
    {
        panelSize.x = Mathf.Max(40f, panelSize.x);
        panelSize.y = Mathf.Max(40f, panelSize.y);
        roomSize.x = Mathf.Max(2f, roomSize.x);
        roomSize.y = Mathf.Max(2f, roomSize.y);
        roomGap.x = Mathf.Max(0f, roomGap.x);
        roomGap.y = Mathf.Max(0f, roomGap.y);

        // OnValidate 안에서 RectTransform을 건드리면 Unity가 경고하므로 Update로 미룬다.
        placementDirty = true;
    }
}
