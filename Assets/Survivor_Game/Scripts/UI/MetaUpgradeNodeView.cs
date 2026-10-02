using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 영구 개조 한 줄. 프리팹에서 모양을 다 짜 두고, 이 스크립트는 거기 있는
/// 오브젝트에 값만 넣는다. 새로 만들지 않으므로 배치는 프리팹에서 고친다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Meta Upgrade Node View")]
public class MetaUpgradeNodeView : MonoBehaviour
{
    [Header("구성 요소")]
    [Tooltip("등급 색을 칠할 띠.")]
    [SerializeField] private Image gradeBar;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("지금 단계에서의 효과. 아직 안 올렸으면 한 단계치를 미리 보여 준다.")]
    [SerializeField] private TMP_Text effectText;
    [Tooltip("3 / 5 처럼 단계를 적는 곳.")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("올리는 버튼. 값을 치를 수 없으면 꺼진다.")]
    [SerializeField] private Button raiseButton;
    [Tooltip("버튼 안의 값 표시.")]
    [SerializeField] private TMP_Text costText;

    [Header("문구")]
    [SerializeField] private string maxedLabel = "최대";
    [SerializeField] private string lockedLabel = "잠김";
    [SerializeField] private Color lockedColor = new Color(0.40f, 0.43f, 0.50f);

    private MetaUpgradeNode node;
    private Action<MetaUpgradeNode> onRaise;

    public MetaUpgradeNode Node => node;

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        GameFontManager.ApplyFont(nameText);
        GameFontManager.ApplyFont(effectText);
        GameFontManager.ApplyFont(levelText);
        GameFontManager.ApplyFont(costText);

        if (raiseButton != null) raiseButton.onClick.AddListener(HandleRaiseClicked);
    }

    private void OnDestroy()
    {
        if (raiseButton != null) raiseButton.onClick.RemoveListener(HandleRaiseClicked);
    }

    private void HandleRaiseClicked()
    {
        if (node != null) onRaise?.Invoke(node);
    }

    /// <summary>이 줄이 맡을 항목을 정한다. null이면 줄 자체를 감춘다.</summary>
    public void Bind(MetaUpgradeNode upgrade, Action<MetaUpgradeNode> raiseHandler)
    {
        node = upgrade;
        onRaise = raiseHandler;
        gameObject.SetActive(upgrade != null);
        if (upgrade != null) Refresh();
    }

    /// <summary>단계나 잔존 코드가 바뀐 뒤 표시를 다시 맞춘다.</summary>
    public void Refresh()
    {
        if (node == null) return;

        int level = MetaProgress.GetLevel(node);
        bool available = MetaProgress.IsAvailable(node);
        bool maxed = level >= node.MaxLevel;
        Color color = available
            ? ItemGradeInfo.Color(node.Grade) : lockedColor;

        if (gradeBar != null) gradeBar.color = color;
        if (nameText != null)
        {
            nameText.text = node.DisplayName;
            nameText.color = color;
        }
        if (effectText != null)
        {
            // 아직 열리지 않았으면 효과 대신 무엇이 필요한지 알려 준다.
            effectText.text = available
                ? node.DescribeEffects(level) : node.DescribeRequirements();
            effectText.color = available ? Color.white : lockedColor;
        }
        if (levelText != null)
        {
            levelText.text = level + " / " + node.MaxLevel;
            levelText.color = maxed ? color : Color.white;
        }

        RefreshButton(level, available, maxed);
    }

    private void RefreshButton(int level, bool available, bool maxed)
    {
        if (raiseButton == null) return;

        bool canRaise = MetaProgress.CanRaise(node);
        raiseButton.interactable = canRaise;

        if (costText == null) return;

        if (!available) costText.text = lockedLabel;
        else if (maxed) costText.text = maxedLabel;
        else costText.text = node.CostToRaise(level).ToString();
    }
}
