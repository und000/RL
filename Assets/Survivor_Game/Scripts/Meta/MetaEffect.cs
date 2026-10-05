using System;
using UnityEngine;

/// <summary>
/// 영구 개조 한 줄이 무엇을 바꾸는가. 전투 능력치는 코어 보드·장비와
/// 같은 통로(ChipStatKind)를 쓰고, 런 진행에만 관여하는 것들은 따로 둔다.
/// </summary>
public enum MetaEffectKind
{
    /// <summary>ChipStatKind 하나를 올린다. 보드·장비 보너스와 같은 자리에 더해진다.</summary>
    Stat,
    /// <summary>런을 시작할 때 들고 있는 크레딧.</summary>
    StartingCredits,
    /// <summary>상점 가격 할인율. 0.1 = 10% 싸게.</summary>
    ShopDiscountRate,
    /// <summary>방 보상 받침대 수. 고를 수 있는 선택지가 늘어난다.</summary>
    RewardChoiceCount,
    /// <summary>경험치 획득 배율 가산. 0.1 = +10%.</summary>
    ExperienceRate,
    /// <summary>잔존 코드 획득 배율 가산. 0.1 = +10%.</summary>
    SalvageRate,
    /// <summary>기체가 멈췄을 때 그 자리에서 다시 일어나는 횟수.</summary>
    Revive
}

/// <summary>개조 한 단계가 주는 변화 한 줄.</summary>
[Serializable]
public struct MetaEffect
{
    public MetaEffectKind kind;
    [Tooltip("kind가 Stat일 때만 쓴다.")]
    public ChipStatKind stat;
    [Tooltip("한 단계마다 더해지는 값.")]
    public float valuePerLevel;

    /// <summary>UI에 그대로 띄울 수 있는 한 줄. level 단계까지 올렸을 때의 총량이다.</summary>
    public string Describe(int level)
    {
        float total = valuePerLevel * Mathf.Max(0, level);
        switch (kind)
        {
            case MetaEffectKind.Stat:
                return MetaEffectFormat.StatName(stat) + " " +
                    MetaEffectFormat.StatValue(stat, total);
            case MetaEffectKind.StartingCredits:
                return "시작 크레딧 +" + Mathf.RoundToInt(total);
            case MetaEffectKind.ShopDiscountRate:
                return "상점 가격 -" + MetaEffectFormat.Percent(total);
            case MetaEffectKind.RewardChoiceCount:
                return "보상 선택지 +" + Mathf.RoundToInt(total);
            case MetaEffectKind.ExperienceRate:
                return "경험치 +" + MetaEffectFormat.Percent(total);
            case MetaEffectKind.SalvageRate:
                return "잔존 코드 +" + MetaEffectFormat.Percent(total);
            case MetaEffectKind.Revive:
                return "재기동 " + Mathf.RoundToInt(total) + "회";
            default:
                return string.Empty;
        }
    }
}

/// <summary>개조 문구를 만드는 곳. 표기가 한 군데에 모여 있어야 손보기 쉽다.</summary>
public static class MetaEffectFormat
{
    /// <summary>비율로 읽어야 하는 스탯인가. 0.1을 "10%"로 적을지 "0.1"로 적을지 가른다.</summary>
    public static bool IsRate(ChipStatKind stat)
    {
        switch (stat)
        {
            case ChipStatKind.DamageIncreaseRate:
            case ChipStatKind.ArmorPenetrationRate:
            case ChipStatKind.TrueDamageRate:
            case ChipStatKind.MoveSpeedRate:
            case ChipStatKind.CooldownReductionRate:
                return true;
            default:
                return false;
        }
    }

    public static string StatName(ChipStatKind stat)
    {
        switch (stat)
        {
            case ChipStatKind.FlatAttack: return "공격력";
            case ChipStatKind.DamageIncreaseRate: return "피해 증가";
            case ChipStatKind.ArmorPenetrationRate: return "방어 관통";
            case ChipStatKind.FlatArmorPenetration: return "고정 관통";
            case ChipStatKind.TrueDamageRate: return "고정 피해";
            case ChipStatKind.MaxHealth: return "최대 체력";
            case ChipStatKind.MoveSpeedRate: return "이동 속도";
            case ChipStatKind.MaxEnergy: return "최대 에너지";
            case ChipStatKind.EnergyRegeneration: return "에너지 회복";
            case ChipStatKind.PickupRange: return "획득 범위";
            case ChipStatKind.CooldownReductionRate: return "재사용 감소";
            default: return stat.ToString();
        }
    }

    public static string StatValue(ChipStatKind stat, float total)
    {
        string sign = total >= 0f ? "+" : "";
        if (IsRate(stat)) return sign + Percent(total);
        // 소수를 쓰는 값과 정수로만 오르는 값을 나눠 적는다.
        if (stat == ChipStatKind.EnergyRegeneration || stat == ChipStatKind.PickupRange)
        {
            return sign + total.ToString("0.##");
        }
        return sign + Mathf.RoundToInt(total);
    }

    public static string Percent(float rate) =>
        Mathf.RoundToInt(rate * 100f).ToString() + "%";
}
