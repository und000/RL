using System;
using UnityEngine;

/// <summary>
/// 칩이 건드릴 수 있는 스탯. 이름은 기존 PlayerCombatStats·PlayerEnergy의
/// 필드와 1:1로 맞춰 두었다.
/// </summary>
public enum ChipStatKind
{
    /// <summary>PlayerCombatStats.equipmentFlatAttack에 더한다.</summary>
    FlatAttack,
    /// <summary>PlayerCombatStats.damageIncreaseRate에 더한다. 0.1 = +10%.</summary>
    DamageIncreaseRate,
    /// <summary>PlayerCombatStats.armorPenetrationRate에 더한다.</summary>
    ArmorPenetrationRate,
    /// <summary>PlayerCombatStats.flatArmorPenetration에 더한다.</summary>
    FlatArmorPenetration,
    /// <summary>PlayerCombatStats.trueDamageRate에 더한다.</summary>
    TrueDamageRate,
    MaxHealth,
    /// <summary>이동 속도 증가율. 0.05 = +5%.</summary>
    MoveSpeedRate,
    /// <summary>PlayerEnergy.maxEnergy에 더한다.</summary>
    MaxEnergy,
    /// <summary>PlayerEnergy.regenerationPerSecond에 더한다.</summary>
    EnergyRegeneration,
    PickupRange,
    /// <summary>재사용 대기시간 감소율. 0.1 = 10% 감소.</summary>
    CooldownReductionRate
}

/// <summary>칩 하나가 주는 스탯 변화 한 줄.</summary>
[Serializable]
public struct ChipStatModifier
{
    public ChipStatKind stat;
    public float value;
}

/// <summary>보드 전체를 훑어 합산한 결과. 재배치할 때마다 다시 만든다.</summary>
public sealed class CoreBoardStats
{
    private static readonly int StatCount = Enum.GetValues(typeof(ChipStatKind)).Length;

    private readonly float[] values = new float[StatCount];

    public float Get(ChipStatKind stat) => values[(int)stat];

    public int GetInt(ChipStatKind stat) => Mathf.RoundToInt(values[(int)stat]);

    public void Add(ChipStatKind stat, float amount) => values[(int)stat] += amount;

    public void Add(ChipStatModifier modifier) => Add(modifier.stat, modifier.value);

    public void Clear() => Array.Clear(values, 0, values.Length);

    /// <summary>디버그용. 0이 아닌 항목만 한 줄로 뽑는다.</summary>
    public override string ToString()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int index = 0; index < values.Length; index++)
        {
            if (Mathf.Approximately(values[index], 0f)) continue;
            if (builder.Length > 0) builder.Append(", ");
            builder.Append((ChipStatKind)index).Append(' ').Append(values[index]);
        }
        return builder.Length > 0 ? builder.ToString() : "(변화 없음)";
    }
}
