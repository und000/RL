using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>출구에서 다음 방을 선택하는 편집 가능한 프리팹 UI.</summary>
[DisallowMultipleComponent]
public class RoomChoiceUI : MonoBehaviour
{
    [Serializable]
    public class ChoiceSlot
    {
        public Button button;
        public TMP_Text title;
        public TMP_Text description;
        public TMP_Text symbol;
        public Image accent;
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text subtitle;
    [SerializeField] private Button cancelButton;
    [SerializeField] private ChoiceSlot[] slots = Array.Empty<ChoiceSlot>();
    [Header("방 종류 색")]
    [SerializeField] private Color normalColor = new Color(.4f, .75f, 1f);
    [SerializeField] private Color eliteColor = new Color(1f, .4f, .35f);
    [SerializeField] private Color treasureColor = new Color(1f, .8f, .3f);
    [SerializeField] private Color shopColor = new Color(.4f, 1f, .7f);
    [SerializeField, Min(0f)] private float cardSpacing = 290f;

    private RunManager manager;
    private IReadOnlyList<RoomRouteChoice> choices;
    private Action<RoomRouteChoice> onChoose;
    private bool ownsPause;
    private float previousTimeScale;
    private static RoomChoiceUI active;
    private static int blockedThroughFrame = -1;

    public bool IsVisible => ownsPause;
    public bool IsConfigured => panel != null && cancelButton != null && slots != null &&
        slots.Length >= 3 && Array.TrueForAll(slots, slot => slot != null && slot.button != null);
    public static bool IsBlockingGameplay => active != null || Time.frameCount <= blockedThroughFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { active = null; blockedThroughFrame = -1; }

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            if (slots[i]?.button != null) slots[i].button.onClick.AddListener(() => Choose(index));
        }
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) GameFontManager.ApplyFont(text);
    }

    public bool Show(RunManager owner, IReadOnlyList<RoomRouteChoice> offered, string destination,
        Action<RoomRouteChoice> selectionHandler)
    {
        if (!isActiveAndEnabled || !IsConfigured || owner == null || offered == null || offered.Count < 2 ||
            offered.Count > slots.Length || selectionHandler == null || active != null ||
            GameInputKeys.IsGameplayBlocked || Time.timeScale <= 0f)
            return false;
        manager = owner;
        choices = offered;
        onChoose = selectionHandler;
        for (int i = 0; i < slots.Length; i++)
        {
            ChoiceSlot slot = slots[i];
            slot.button.gameObject.SetActive(i < choices.Count);
            if (i >= choices.Count) continue;
            RoomKind kind = choices[i].Kind;
            var rect = (RectTransform)slot.button.transform;
            rect.anchoredPosition = new Vector2((i - (choices.Count - 1) * .5f) * cardSpacing,
                rect.anchoredPosition.y);
            slot.button.interactable = true;
            if (slot.title != null) slot.title.text = RoomRoutePlanner.GetTitle(kind);
            if (slot.description != null) slot.description.text = RoomRoutePlanner.GetDescription(kind);
            if (slot.symbol != null)
                slot.symbol.text = kind == RoomKind.Elite ? "!" : kind == RoomKind.Treasure ? "+" :
                    kind == RoomKind.Shop ? "$" : "X";
            if (slot.accent != null) slot.accent.color = ColorFor(kind);
        }
        if (heading != null) heading.text = "다음 방 선택";
        if (subtitle != null) subtitle.text = destination + "  ·  하나를 선택해 이동하세요";
        TMP_Text cancelLabel = cancelButton.GetComponentInChildren<TMP_Text>(true);
        if (cancelLabel != null) cancelLabel.text = "돌아가기  [Esc]";
        StaggerImpactFeedback.CancelActive();
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        active = this;
        Time.timeScale = 0f;
        panel.SetActive(true);
        return true;
    }

    private Color ColorFor(RoomKind kind)
    {
        switch (kind)
        {
            case RoomKind.Elite: return eliteColor;
            case RoomKind.Treasure: return treasureColor;
            case RoomKind.Shop: return shopColor;
            default: return normalColor;
        }
    }

    private void Update()
    {
        if (!ownsPause) return;
        if (manager == null || manager.IsRunOver || !manager.isActiveAndEnabled) { Hide(); return; }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
    }

    private void Choose(int index)
    {
        if (!ownsPause || manager == null || index < 0 || index >= choices.Count) return;
        RoomRouteChoice choice = choices[index];
        Action<RoomRouteChoice> callback = onChoose;
        Hide();
        callback?.Invoke(choice);
    }

    private void Cancel()
    {
        if (ownsPause) Hide();
    }

    public void Hide()
    {
        if (ownsPause)
        {
            // 사망 결과창이 시간을 멈춘 경우에는 그 일시정지를 해제하지 않는다.
            if ((manager == null || !manager.IsRunOver) && Mathf.Approximately(Time.timeScale, 0f))
                Time.timeScale = previousTimeScale;
            ownsPause = false;
            if (active == this) active = null;
            blockedThroughFrame = Time.frameCount;
        }
        if (panel != null) panel.SetActive(false);
        choices = null;
        onChoose = null;
    }

    private void OnDisable() { Hide(); }
}
