using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 런 사이에 잔존 코드를 써서 기체를 손보는 화면. 줄은 프리팹에 미리 깔아 두고,
/// 여기서는 개조 표의 항목을 그 줄들에 나눠 담기만 한다.
/// 줄보다 항목이 많으면 남는 것은 뜨지 않으므로 프리팹에서 줄을 늘리면 된다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Meta Upgrade UI")]
public class MetaUpgradeUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("보여 줄 개조 표. 비워 두면 씬의 Meta Progress Runtime에서 가져온다.")]
    [SerializeField] private MetaUpgradeTree tree;
    [Tooltip("프리팹에 미리 놓아 둔 줄들. 항목 수만큼만 켜진다.")]
    [SerializeField] private MetaUpgradeNodeView[] nodeViews;

    [Header("표시")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("지금 가진 잔존 코드.")]
    [SerializeField] private TMP_Text salvageText;
    [Tooltip("이번 런이 남긴 몫의 내역. 비워 두면 띄우지 않는다.")]
    [SerializeField] private TMP_Text awardText;
    [Tooltip("여태 기록. 비워 두면 띄우지 않는다.")]
    [SerializeField] private TMP_Text recordText;
    [Tooltip("이번 런의 몫을 읽어 올 곳. 비워 두면 씬에서 찾는다.")]
    [SerializeField] private RunSalvageReporter reporter;

    [Header("문구")]
    [Tooltip("프리팹에는 폰트 경고를 피하려고 영문 자리표시가 들어 있다.")]
    [SerializeField] private string titleLabel = "기체 개조";
    [SerializeField] private string salvageFormat = "잔존 코드 {0}";

    private void Awake()
    {
        // 프리팹에 한글을 저장해 두면 폰트가 붙기 전 한 프레임 네모로 그려진다.
        GameFontManager.ApplyFont(titleText);
        GameFontManager.ApplyFont(salvageText);
        GameFontManager.ApplyFont(awardText);
        GameFontManager.ApplyFont(recordText);
        if (titleText != null) titleText.text = titleLabel;
    }

    private void OnEnable()
    {
        if (reporter == null) reporter = FindFirstObjectByType<RunSalvageReporter>();
        if (tree == null) tree = MetaProgressRuntime.Tree;

        MetaProgress.OnChanged += RefreshAll;
        BindNodes();
        RefreshAll();
    }

    private void OnDisable()
    {
        MetaProgress.OnChanged -= RefreshAll;
    }

    /// <summary>개조 표의 항목을 프리팹에 깔린 줄에 차례로 얹는다.</summary>
    private void BindNodes()
    {
        if (nodeViews == null) return;

        IReadOnlyList<MetaUpgradeNode> nodes = tree != null
            ? tree.Nodes : new List<MetaUpgradeNode>();

        for (int index = 0; index < nodeViews.Length; index++)
        {
            MetaUpgradeNodeView view = nodeViews[index];
            if (view == null) continue;
            view.Bind(index < nodes.Count ? nodes[index] : null, HandleRaise);
        }

        if (tree != null && nodes.Count > nodeViews.Length)
        {
            Debug.LogWarning(
                "개조 항목이 " + nodes.Count + "개인데 줄은 " + nodeViews.Length +
                "개뿐이라 " + (nodes.Count - nodeViews.Length) +
                "개가 화면에 뜨지 않습니다. 프리팹에서 줄을 늘려 주세요.", this);
        }
    }

    private void HandleRaise(MetaUpgradeNode node)
    {
        if (!MetaProgress.TryRaise(node)) return;

        // 산 것이 바로 이번 런에 먹히도록 합산을 다시 시킨다.
        MetaProgressRuntime.Refresh();
        // 값이 바뀌면 MetaProgress가 알려 주므로 여기서 따로 갱신하지 않는다.
    }

    private void RefreshAll()
    {
        RefreshHeader();
        if (nodeViews == null) return;

        foreach (MetaUpgradeNodeView view in nodeViews)
        {
            if (view != null && view.Node != null) view.Refresh();
        }
    }

    private void RefreshHeader()
    {
        if (salvageText != null)
        {
            salvageText.text = string.Format(salvageFormat, MetaProgress.Salvage);
        }
        if (awardText != null) awardText.text = BuildAwardLine();
        if (recordText != null) recordText.text = BuildRecordLine();
    }

    /// <summary>이번 런이 얼마를 남겼는지 한 줄로 편다.</summary>
    private string BuildAwardLine()
    {
        if (reporter == null || !reporter.HasAward) return string.Empty;

        SalvageBreakdown award = reporter.LastAward;
        string line = "이번 회수 +" + award.Total +
            "   방 " + award.Rooms + "  층 " + award.Floors +
            "  챕터 " + award.Chapters;
        if (award.ClearBonus > 0) line += "  돌파 +" + award.ClearBonus;
        if (award.RateBonus != 0) line += "  개조 +" + award.RateBonus;
        return line;
    }

    private string BuildRecordLine()
    {
        MetaProgressState state = MetaProgress.State;
        string best = state.bestChapter > 0
            ? state.bestChapter + "장 " + state.bestFloor + "층"
            : "없음";
        return "최고 기록 " + best +
            "   시도 " + state.runCount + "회   돌파 " + state.clearCount + "회";
    }
}
