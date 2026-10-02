using UnityEngine;

/// <summary>
/// 층 난이도에 따라 적 하나에 적용되는 배율 묶음.
/// 스폰 직전에 적용되므로, 같은 프리팹이라도 층마다 다른 강도로 나온다.
/// </summary>
public readonly struct EnemyScaling
{
    public readonly float HealthMultiplier;
    public readonly int DefenseBonus;
    public readonly float ExperienceMultiplier;

    public EnemyScaling(
        float healthMultiplier, int defenseBonus, float experienceMultiplier)
    {
        HealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
        DefenseBonus = Mathf.Max(0, defenseBonus);
        ExperienceMultiplier = Mathf.Max(0f, experienceMultiplier);
    }

    /// <summary>아무 보정도 하지 않는 기본값. 씬에 직접 놓인 적이 쓴다.</summary>
    public static EnemyScaling None => new EnemyScaling(1f, 0, 1f);

    public bool IsNone =>
        Mathf.Approximately(HealthMultiplier, 1f) &&
        DefenseBonus == 0 &&
        Mathf.Approximately(ExperienceMultiplier, 1f);

    public override string ToString() =>
        $"체력 ×{HealthMultiplier:0.##}, 방어 +{DefenseBonus}, 경험치 ×{ExperienceMultiplier:0.##}";
}
