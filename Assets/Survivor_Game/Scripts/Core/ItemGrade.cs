using UnityEngine;

/// <summary>
/// 무기·장비·아이템에 공통으로 붙는 등급. 낮은 것부터 높은 순서이며,
/// 직렬화는 이름이 아니라 순서 번호로 되므로 중간에 끼워 넣지 말고 뒤에 붙인다.
/// </summary>
public enum ItemGrade
{
    /// <summary>공장 규격에 맞춰 찍어낸 것. 흰색.</summary>
    Standard,
    /// <summary>부품을 덧대 성능을 끌어올린 것. 초록색.</summary>
    Reinforced,
    /// <summary>흔치 않은 설계로 만들어진 것. 푸른색.</summary>
    Rare,
    /// <summary>설계도가 봉인된 군용 규격. 보라색.</summary>
    Classified,
    /// <summary>존재 자체가 감춰진 것. 노란색.</summary>
    TopSecret,
    /// <summary>작동 원리를 알 수 없는 것. 빨간색.</summary>
    Anomaly
}

/// <summary>등급마다 정해진 이름과 색. 화면에 보이는 모든 등급 표현이 여기서 나온다.</summary>
public static class ItemGradeInfo
{
    /// <summary>가장 높은 등급의 티어 번호. 연출 강도를 이 값으로 나눠 쓴다.</summary>
    public static readonly int MaxTier = (int)ItemGrade.Anomaly;

    private static readonly string[] Names =
    {
        "표준", "강화", "희귀", "기밀", "극비", "특이"
    };

    private static readonly string[] Descriptions =
    {
        "공장 규격에 맞춰 찍어낸 것",
        "부품을 덧대 성능을 끌어올린 것",
        "흔치 않은 설계로 만들어진 것",
        "설계도가 봉인된 군용 규격",
        "존재 자체가 감춰진 것",
        "작동 원리를 알 수 없는 것"
    };

    private static readonly Color[] Colors =
    {
        new Color(0.90f, 0.92f, 0.95f),
        new Color(0.35f, 0.85f, 0.40f),
        new Color(0.32f, 0.62f, 1.00f),
        new Color(0.66f, 0.42f, 0.95f),
        new Color(1.00f, 0.82f, 0.25f),
        new Color(1.00f, 0.30f, 0.28f)
    };

    public static int Tier(ItemGrade grade) =>
        Mathf.Clamp((int)grade, 0, MaxTier);

    public static string Name(ItemGrade grade) => Names[Tier(grade)];

    public static string Description(ItemGrade grade) => Descriptions[Tier(grade)];

    public static Color Color(ItemGrade grade) => Colors[Tier(grade)];

    /// <summary>0(가장 낮음)에서 1(가장 높음) 사이로 옮긴 값. 연출 강도에 쓴다.</summary>
    public static float Strength(ItemGrade grade) =>
        MaxTier <= 0 ? 1f : Tier(grade) / (float)MaxTier;

    /// <summary>"희귀" 같은 이름에 색을 입힌 TMP 태그 문구.</summary>
    public static string ColoredName(ItemGrade grade)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(Color(grade)) + ">" +
            Name(grade) + "</color>";
    }
}
