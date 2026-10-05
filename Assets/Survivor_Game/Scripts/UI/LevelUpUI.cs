using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>레벨마다 아이템 4종 중 하나를 지급한다. 리롤 3회는 한 런 전체가 공유한다.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Level Up UI")]
public class LevelUpUI : MonoBehaviour
{
    [Serializable]
    public class ChoiceSlot
    {
        public Button button;
        public TMP_Text title;
        public TMP_Text description;
        public Image icon;
    }
    private static LevelUpUI active;
    private static int blockedThroughFrame = -1;
    public static bool IsPopupOpen => active != null || Time.frameCount <= blockedThroughFrame;
    [Header("연결")]
    [SerializeField] private PlayerLevel playerLevel;
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private LevelUpRewardPool rewardPool;
    [SerializeField] private ChoiceSlot[] slots = Array.Empty<ChoiceSlot>();
    [Tooltip("한 런 전체가 공유하는 리롤 횟수. 레벨업/스테이지 전환으로 충전되지 않는다.")]
    [SerializeField, Min(0)] private int rerollsPerRun = 3;
    private readonly Queue<int> pendingLevels = new Queue<int>();
    private LevelUpRewardDraft draft;
    private PlayerHealth health;
    private RunManager run;
    private GameObject player;
    private bool ownsPause;
    private bool configurationFailed;
    private float previousTimeScale;
    private int lastActionFrame = -1;
    public int RemainingRerolls => draft != null ? draft.RemainingRerolls : 0;
    private bool RunEnded => (health != null && health.IsDead) || (run != null && run.IsRunOver);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { active = null; blockedThroughFrame = -1; }

    private void Awake()
    {
        draft = new LevelUpRewardDraft(rerollsPerRun);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) GameFontManager.ApplyFont(text);
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            if (slots[i]?.button != null) slots[i].button.onClick.AddListener(() => Choose(index));
        }
        if (rerollButton != null) rerollButton.onClick.AddListener(Reroll);
    }

    private void Start()
    {
        if (playerLevel == null)
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            playerLevel = tagged != null ? tagged.GetComponentInChildren<PlayerLevel>(true) : null;
        }
        if (playerLevel == null || levelUpPanel == null || rewardPool == null || rerollButton == null ||
            slots.Length != LevelUpRewardDraft.ChoiceCount || !Array.TrueForAll(slots, s => s != null && s.button != null))
        {
            Debug.LogError("레벨업 선택 UI에 플레이어, 보상 풀, 카드 4개와 리롤 버튼이 필요합니다.", this);
            enabled = false;
            return;
        }
        player = playerLevel.gameObject;
        health = playerLevel.GetComponentInParent<PlayerHealth>();
        run = FindFirstObjectByType<RunManager>();
        playerLevel.OnLevelUp += HandleLevelUp;
        for (int level = 2; level <= playerLevel.GetCurrentLevel(); level++) pendingLevels.Enqueue(level);
    }

    private void HandleLevelUp(int level)
    {
        if (RunEnded) return;
        pendingLevels.Enqueue(level);
        TryShowNext();
    }

    private void Update()
    {
        if (RunEnded) { pendingLevels.Clear(); Close(false); return; }
        if (!ownsPause) TryShowNext();
    }

    private List<RoomRewardDefinition> Eligible() => rewardPool.GetEligible(item => item.CanGrant(player));

    private void TryShowNext()
    {
        if (!isActiveAndEnabled || configurationFailed || ownsPause || IsPopupOpen || RunEnded ||
            pendingLevels.Count == 0 || Time.timeScale <= 0f || RoomChoiceUI.IsBlockingGameplay || StageTransitionUI.IsBlockingGameplay) return;
        if (!draft.Refresh(Eligible(), false))
        {
            configurationFailed = true;
            Debug.LogError("지급 가능한 레벨업 아이템이 4종 미만입니다. 보상 풀과 인벤토리를 확인하세요.", this);
            return;
        }
        StaggerImpactFeedback.CancelActive();
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        active = this;
        Time.timeScale = 0f;
        lastActionFrame = Time.frameCount;
        int level = pendingLevels.Dequeue();
        if (titleText != null) titleText.text = "LEVEL UP!  Lv. " + level;
        if (messageText != null) messageText.text = "아이템 하나를 선택하세요";
        levelUpPanel.SetActive(true);
        RefreshCards();
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(slots[0].button.gameObject);
    }

    private void RefreshCards()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            RoomRewardDefinition item = draft.Choices[i];
            ChoiceSlot slot = slots[i];
            slot.button.interactable = true;
            if (slot.title != null) { slot.title.text = item.DisplayName; slot.title.color = item.GradeColor; }
            if (slot.description != null) slot.description.text = string.IsNullOrWhiteSpace(item.Description) ? item.BuildLabel() : item.Description;
            if (slot.icon != null)
            {
                slot.icon.sprite = item.Icon;
                slot.icon.preserveAspect = true;
                slot.icon.enabled = item.Icon != null;
            }
        }
        TMP_Text label = rerollButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "리롤 · 남은 " + RemainingRerolls + "회";
        rerollButton.interactable = RemainingRerolls > 0 && draft.HasAlternative(Eligible());
    }

    private bool CanAct => isActiveAndEnabled && ownsPause && !RunEnded && Time.frameCount > lastActionFrame;

    public void Reroll()
    {
        if (!CanAct || !draft.Refresh(Eligible(), true)) return;
        lastActionFrame = Time.frameCount;
        RefreshCards();
    }

    public void Choose(int index)
    {
        if (!CanAct || index < 0 || index >= draft.Choices.Count) return;
        lastActionFrame = Time.frameCount;
        RoomRewardDefinition item = draft.Choices[index];
        if (!item.CanGrant(player) || !item.Grant(player))
        {
            if (messageText != null) messageText.text = "지급할 수 없는 아이템입니다. 다른 아이템을 선택하세요";
            slots[index].button.interactable = false;
            return;
        }
        Close(true);
    }

    private void Close(bool resume)
    {
        if (!ownsPause) return;
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (resume && !RunEnded && Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = previousTimeScale;
        ownsPause = false;
        if (active == this) active = null;
        blockedThroughFrame = Time.frameCount;
    }

    private void OnDisable() { Close(!RunEnded); }
    private void OnDestroy()
    {
        if (playerLevel != null) playerLevel.OnLevelUp -= HandleLevelUp;
        if (rerollButton != null) rerollButton.onClick.RemoveListener(Reroll);
    }
}
