using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 중 종단 칩의 전원 연결 상태를 표시하는 코어 보드 상태창.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("Core Board/Core Board HUD")]
public class CoreBoardHudUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private CoreBoardController board;

    [Header("배치")]
    [SerializeField] private Vector2 panelSize = new Vector2(250f, 54f);
    [SerializeField] private Vector2 screenMargin = new Vector2(18f, 18f);

    [Header("배경")]
    [SerializeField] private Color barBackgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.85f);

    [Header("회로 램프")]
    [SerializeField] private float lampSize = 14f;
    [SerializeField] private float lampSpacing = 6f;
    [SerializeField] private Color lampOnColor = new Color(0.45f, 0.95f, 0.6f, 1f);
    [SerializeField] private Color lampOffColor = new Color(0.22f, 0.24f, 0.28f, 1f);

    private readonly List<Image> lamps = new List<Image>();
    private readonly List<PlacedChip> terminals = new List<PlacedChip>();

    private RectTransform panelRect;
    private RectTransform lampRow;

    private void Awake()
    {
        panelRect = (RectTransform)transform;
        ApplyPlacement();
        BuildFrame();
    }

    private void Start()
    {
        if (board == null) board = FindFirstObjectByType<CoreBoardController>();
        if (board == null || !board.IsReady)
        {
            Debug.LogWarning("CoreBoardController가 없어 코어 보드 HUD를 끕니다.", this);
            gameObject.SetActive(false);
            return;
        }
    }

    private void Update()
    {
        if (board == null || !board.IsReady) return;

        CoreBoardState state = board.State;
        RefreshLamps(state);
    }

    /// <summary>램프는 꽂힌 종단 칩 하나에 하나씩 대응한다.</summary>
    private void RefreshLamps(CoreBoardState state)
    {
        terminals.Clear();
        foreach (PlacedChip placed in state.Placements)
        {
            if (placed.Chip != null && placed.Chip.Category == ChipCategory.Terminal)
            {
                terminals.Add(placed);
            }
        }
        terminals.Sort((left, right) => left.Id.CompareTo(right.Id));

        if (lamps.Count != terminals.Count) RebuildLamps(terminals.Count);

        for (int index = 0; index < terminals.Count; index++)
        {
            PlacedChip terminal = terminals[index];
            Color color;
            if (state.IsEnergized(terminal)) color = lampOnColor;
            else color = lampOffColor;
            lamps[index].color = color;
        }
    }

    private void RebuildLamps(int count)
    {
        foreach (Image lamp in lamps)
        {
            if (lamp != null) CoreBoardView.DetachAndDestroy(lamp.transform);
        }
        lamps.Clear();

        for (int index = 0; index < count; index++)
        {
            RectTransform lamp = CreateChild("Lamp" + index, lampRow);
            lamp.anchorMin = new Vector2(0f, 0.5f);
            lamp.anchorMax = new Vector2(0f, 0.5f);
            lamp.sizeDelta = Vector2.one * lampSize;
            lamp.anchoredPosition = new Vector2(
                lampSize * 0.5f + index * (lampSize + lampSpacing), 0f);
            lamps.Add(CreateImage(lamp, lampOffColor));
        }
    }

    // 뼈대 --------------------------------------------------------------

    private void ApplyPlacement()
    {
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = screenMargin;
    }

    private void BuildFrame()
    {
        RectTransform background = CreateChild("Background", panelRect);
        Stretch(background);
        CreateImage(background, barBackgroundColor);

        float contentWidth = panelSize.x - 20f;
        TMP_Text label = CreateLabel(CreateChild("Title", panelRect), 15f,
            TextAlignmentOptions.Left);
        label.text = "회로 연결";
        PlaceTopLeft(label.rectTransform, new Vector2(contentWidth, 20f), new Vector2(10f, -6f));

        lampRow = CreateChild("Lamps", panelRect);
        lampRow.anchorMin = new Vector2(0f, 0f);
        lampRow.anchorMax = new Vector2(0f, 0f);
        lampRow.pivot = new Vector2(0f, 0f);
        lampRow.sizeDelta = new Vector2(contentWidth, lampSize + 6f);
        lampRow.anchoredPosition = new Vector2(10f, 8f);
    }

    private static void Stretch(RectTransform target)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    private static void PlaceTopLeft(RectTransform target, Vector2 size, Vector2 position)
    {
        target.anchorMin = new Vector2(0f, 1f);
        target.anchorMax = new Vector2(0f, 1f);
        target.pivot = new Vector2(0f, 1f);
        target.sizeDelta = size;
        target.anchoredPosition = position;
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

    private static Image CreateImage(RectTransform target, Color color)
    {
        Image image = target.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateLabel(
        RectTransform target, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = target.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = size;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private void OnValidate()
    {
        panelSize.x = Mathf.Max(120f, panelSize.x);
        panelSize.y = Mathf.Max(40f, panelSize.y);
    }
}
