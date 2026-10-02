using System;
using System.Text;
using UnityEngine;

/// <summary>
/// 런 사이에 남는 영구 개조 한 항목. 잔존 코드를 치러 단계를 올리며,
/// 앞선 항목을 요구할 수 있어 갈래가 생긴다.
/// </summary>
[CreateAssetMenu(
    fileName = "Meta_New", menuName = "Survivor/Meta/Upgrade Node")]
public class MetaUpgradeNode : ScriptableObject
{
    [Header("표시")]
    [Tooltip("저장 파일에 적히는 이름표. 한 번 정하면 바꾸지 않는다. " +
        "에셋 이름을 바꿔도 진행이 날아가지 않도록 따로 둔 것이다. " +
        "비워 두면 에셋 이름을 쓴다.")]
    [SerializeField] private string id;
    [SerializeField] private string displayName = "새 개조";
    [SerializeField, TextArea(2, 4)] private string description;
    [Tooltip("등급. 색과 무게감만 정하고 성능에는 관여하지 않는다.")]
    [SerializeField] private ItemGrade grade = ItemGrade.Standard;
    [Tooltip("목록에서의 자리. 작을수록 앞에 온다.")]
    [SerializeField] private int order;

    [Header("단계")]
    [SerializeField, Min(1)] private int maxLevel = 1;
    [Tooltip("1단계 가격.")]
    [SerializeField, Min(0)] private int baseCost = 20;
    [Tooltip("한 단계 올라갈 때마다 가격에 더해지는 값.")]
    [SerializeField, Min(0)] private int costGrowth = 15;

    [Header("조건")]
    [Tooltip("이 항목들을 하나 이상 올려 두어야 열린다. 비워 두면 처음부터 열려 있다.")]
    [SerializeField] private MetaUpgradeNode[] requirements = Array.Empty<MetaUpgradeNode>();

    [Header("효과")]
    [Tooltip("단계마다 더해지는 변화들. 여러 줄을 넣으면 함께 오른다.")]
    [SerializeField] private MetaEffect[] effects = Array.Empty<MetaEffect>();

    public string Id => string.IsNullOrEmpty(id) ? name : id;
    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public ItemGrade Grade => grade;
    public int Order => order;
    public int MaxLevel => Mathf.Max(1, maxLevel);
    public MetaUpgradeNode[] Requirements => requirements;
    public MetaEffect[] Effects => effects;

    /// <summary>지금 level단계일 때 다음 한 단계를 올리는 값.</summary>
    public int CostToRaise(int level)
    {
        if (level >= MaxLevel) return 0;
        return Mathf.Max(0, baseCost + costGrowth * Mathf.Max(0, level));
    }

    /// <summary>선행 조건을 모두 채웠는가. levelOf는 다른 항목의 현재 단계를 돌려준다.</summary>
    public bool IsAvailable(Func<MetaUpgradeNode, int> levelOf)
    {
        if (requirements == null || requirements.Length == 0) return true;
        if (levelOf == null) return false;

        foreach (MetaUpgradeNode requirement in requirements)
        {
            if (requirement == null) continue;
            if (levelOf(requirement) <= 0) return false;
        }
        return true;
    }

    /// <summary>아직 열리지 않았을 때 무엇이 필요한지 알려 주는 문구.</summary>
    public string DescribeRequirements()
    {
        if (requirements == null || requirements.Length == 0) return string.Empty;

        StringBuilder builder = new StringBuilder();
        foreach (MetaUpgradeNode requirement in requirements)
        {
            if (requirement == null) continue;
            if (builder.Length > 0) builder.Append(", ");
            builder.Append(requirement.DisplayName);
        }
        return builder.Length > 0 ? builder + " 필요" : string.Empty;
    }

    /// <summary>level단계까지 올렸을 때의 효과 총량. 0단계면 한 단계치를 미리 보여 준다.</summary>
    public string DescribeEffects(int level)
    {
        if (effects == null || effects.Length == 0) return string.Empty;

        int shown = Mathf.Max(1, level);
        StringBuilder builder = new StringBuilder();
        foreach (MetaEffect effect in effects)
        {
            string line = effect.Describe(shown);
            if (string.IsNullOrEmpty(line)) continue;
            if (builder.Length > 0) builder.Append("  ");
            builder.Append(line);
        }
        return builder.ToString();
    }

    private void OnValidate()
    {
        maxLevel = Mathf.Max(1, maxLevel);
        baseCost = Mathf.Max(0, baseCost);
        costGrowth = Mathf.Max(0, costGrowth);

        // 자기 자신을 선행 조건으로 두면 영영 열리지 않는다.
        if (requirements == null) return;
        for (int index = 0; index < requirements.Length; index++)
        {
            if (requirements[index] != this) continue;

            requirements[index] = null;
            Debug.LogWarning(
                DisplayName + ": 자기 자신은 선행 조건이 될 수 없어 비웠습니다.", this);
        }
    }
}
