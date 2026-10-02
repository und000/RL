using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 중에도 늘 떠 있는 코어 보드 상태창. 좌측 하단에 발열 게이지와
/// 종단 칩 램프를 띄운다. 램프가 툭 꺼지는 것이 스로틀링의 유일한 신호이므로
/// 보드 화면을 열지 않아도 무슨 일이 벌어졌는지 알 수 있어야 한다.
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
    [SerializeField] private Vector2 panelSize = new Vector2(250f, 78f);
    [SerializeField] private Vector2 screenMargin = new Vector2(18f, 18f);

    [Header("발열 게이지")]
    [SerializeField] private Color barBackgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.85f);
    [SerializeField] private Color barSafeColor = new Color(0.35f, 0.7f, 0.9f, 1f);
    [SerializeField] private Color barHotColor = new Color(0.95f, 0.65f, 0.25f, 1f);
    [SerializeField] private Color barOverheatColor = new Color(1f, 0.3f, 0.28f, 1f);
    [Tooltip("이 비율을 넘으면 게이지가 주황으로 넘어간다.")]
    [SerializeField, Range(0.1f, 1f)] private float hotThreshold = 0.75f;

    [Header("회로 램프")]
    [SerializeField] private float lampSize = 14f;
    [SerializeField] private float lampSpacing = 6f;
    [SerializeField] private Color lampOnColor = new Color(0.45f, 0.95f, 0.6f, 1f);
    [SerializeField] private Color lampOffColor = new Color(0.22f, 0.24f, 0.28f, 1f);
    [SerializeField] private Color lampThrottledColor = new Color(1f, 0.35f, 0.3f, 1f);

    [Header("오버클럭")]
    [SerializeField] private Color overclockIdleColor = new Color(0.4f, 0.45f, 0.52f, 1f);
    [SerializeField] private Color overclockActiveColor = new Color(1f, 0.75f, 0.25f, 1f);
    [Tooltip("과열로 회로가 꺼진 순간 번쩍이는 시간.")]
    [SerializeField, Min(0.05f)] private float throttleFlashDuration = 0.25f;

    private readonly List<Image> lamps = new List<Image>();
    private readonly List<PlacedChip> terminals = new List<PlacedChip>();

    private RectTransform panelRect;
    private RectTransform lampRow;
    private RectTransform barFill;
    private Image flashImage;
    private TMP_Text heatLabel;
    private TMP_Text overclockLabel;
    private float barWidth;
    private float flashUntil;

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

        board.OnCircuitThrottled += HandleThrottled;
    }

    private void OnDestroy()
    {
        if (board != null) board.OnCircuitThrottled -= HandleThrottled;
    }

    private void HandleThrottled(PlacedChip chip)
    {
        flashUntil = Time.time + throttleFlashDuration;
    }

    private void Update()
    {
        if (board == null || !board.IsReady) return;

        CoreBoardState state = board.State;
        RefreshHeat(state);
        RefreshLamps(state);
        RefreshOverclock(state);
        RefreshFlash();
    }

    private void RefreshHeat(CoreBoardState state)
    {
        float ratio = state.HeatCapacity > 0
            ? (float)state.TotalHeat / state.HeatCapacity
            : 0f;

        barFill.sizeDelta = new Vector2(barWidth * Mathf.Clamp01(ratio), barFill.sizeDelta.y);

        Color color = barSafeColor;
        if (state.IsOverheated) color = barOverheatColor;
        else if (ratio >= hotThreshold) color = barHotColor;
        barFill.GetComponent<Image>().color = color;

        heatLabel.text = $"발열 {state.TotalHeat} / {state.HeatCapacity}";
        heatLabel.color = state.IsOverheated ? barOverheatColor : new Color(0.8f, 0.86f, 0.92f);
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
            if (state.IsThrottled(terminal)) color = lampThrottledColor;
            else if (state.IsEnergized(terminal)) color = lampOnColor;
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

    private void RefreshOverclock(CoreBoardState state)
    {
        overclockLabel.text = state.OverclockActive ? "오버클럭 ON [Q]" : "오버클럭 [Q]";
        overclockLabel.color = state.OverclockActive
            ? overclockActiveColor
            : overclockIdleColor;
    }

    private void RefreshFlash()
    {
        float remaining = flashUntil - Time.time;
        float alpha = remaining > 0f
            ? Mathf.Clamp01(remaining / throttleFlashDuration) * 0.5f
            : 0f;

        Color color = barOverheatColor;
        color.a = alpha;
        flashImage.color = color;
        flashImage.enabled = alpha > 0f;
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

        barWidth = panelSize.x - 20f;

        RectTransform barSlot = CreateChild("HeatBar", panelRect);
        barSlot.anchorMin = new Vector2(0f, 1f);
        barSlot.anchorMax = new Vector2(0f, 1f);
        barSlot.pivot = new Vector2(0f, 1f);
        barSlot.sizeDelta = new Vector2(barWidth, 10f);
        barSlot.anchoredPosition = new Vector2(10f, -28f);
        CreateImage(barSlot, new Color(0.14f, 0.16f, 0.2f, 1f));

        barFill = CreateChild("Fill", barSlot);
        barFill.anchorMin = new Vector2(0f, 0f);
        barFill.anchorMax = new Vector2(0f, 1f);
        barFill.pivot = new Vector2(0f, 0.5f);
        barFill.sizeDelta = new Vector2(0f, 0f);
        barFill.anchoredPosition = Vector2.zero;
        CreateImage(barFill, barSafeColor);

        heatLabel = CreateLabel(CreateChild("HeatLabel", panelRect), 15f,
            TextAlignmentOptions.Left);
        PlaceTopLeft(heatLabel.rectTransform, new Vector2(barWidth, 20f), new Vector2(10f, -6f));

        overclockLabel = CreateLabel(CreateChild("Overclock", panelRect), 14f,
            TextAlignmentOptions.Right);
        PlaceTopLeft(
            overclockLabel.rectTransform, new Vector2(barWidth, 20f), new Vector2(10f, -6f));

        lampRow = CreateChild("Lamps", panelRect);
        lampRow.anchorMin = new Vector2(0f, 0f);
        lampRow.anchorMax = new Vector2(0f, 0f);
        lampRow.pivot = new Vector2(0f, 0f);
        lampRow.sizeDelta = new Vector2(barWidth, lampSize + 6f);
        lampRow.anchoredPosition = new Vector2(10f, 8f);

        RectTransform flash = CreateChild("Flash", panelRect);
        Stretch(flash);
        flashImage = CreateImage(flash, new Color(0f, 0f, 0f, 0f));
        flashImage.enabled = false;
        flash.SetAsLastSibling();
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
