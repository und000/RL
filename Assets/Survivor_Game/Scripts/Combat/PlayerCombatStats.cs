using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LevelGrowthOverride
{
    [Min(1)] public int startLevel;
    [Min(1)] public int endLevel;
    [Range(0f, 1f)] public float growthRate;

    public bool Contains(int level)
    {
        return level >= startLevel && level <= Mathf.Max(startLevel, endLevel);
    }
}

public readonly struct DamageData
{
    public readonly float NormalDamage;
    public readonly float ArmorPenetrationRate;
    public readonly int FlatArmorPenetration;
    public readonly float TrueDamage;
    public readonly int MinimumDamage;

    public DamageData(
        float normalDamage,
        float armorPenetrationRate,
        int flatArmorPenetration,
        float trueDamage,
        int minimumDamage)
    {
        NormalDamage = Mathf.Max(0f, normalDamage);
        ArmorPenetrationRate = Mathf.Clamp01(armorPenetrationRate);
        FlatArmorPenetration = Mathf.Max(0, flatArmorPenetration);
        TrueDamage = Mathf.Max(0f, trueDamage);
        MinimumDamage = Mathf.Max(1, minimumDamage);
    }
}

[RequireComponent(typeof(PlayerLevel))]
public class PlayerCombatStats : MonoBehaviour
{
    [Header("기준 공격력")]
    [SerializeField, Min(0)] private int characterBaseAttack = 20;
    [SerializeField, Min(0)] private int weaponAttack;
    [SerializeField, Min(0)] private int equipmentFlatAttack;

    [Header("레벨 성장")]
    [SerializeField, Range(0f, 1f)] private float defaultGrowthRate = 0.03f;
    [SerializeField] private List<LevelGrowthOverride> growthOverrides =
        new List<LevelGrowthOverride>();

    [Header("피해 증가")]
    [Tooltip("기준 공격력을 기준으로 합연산되는 주는 피해 증가율입니다.")]
    [SerializeField, Min(0f)] private float damageIncreaseRate;

    [Header("방어 관통")]
    [SerializeField, Range(0f, 1f)] private float armorPenetrationRate;
    [SerializeField, Min(0)] private int flatArmorPenetration;

    [Header("방어 무시 피해")]
    [Tooltip("기준 공격력에서 계산되며 레벨 배율과 피해 증가율을 받지 않습니다.")]
    [SerializeField, Min(0f)] private float trueDamageRate;

    [Header("10배 스케일")]
    [SerializeField, Min(1)] private int minimumNormalDamage = 10;

    private PlayerLevel playerLevel;

    private void Awake()
    {
        playerLevel = GetComponent<PlayerLevel>();
    }

    public DamageData CreateDamageData(float conditionalDamageIncreaseRate = 0f)
    {
        float baseAttack = GetBaseAttack();
        float normalDamage = baseAttack * (
            GetLevelGrowthMultiplier() +
            damageIncreaseRate +
            Mathf.Max(0f, conditionalDamageIncreaseRate)
        );
        float trueDamage = baseAttack * trueDamageRate;

        return new DamageData(
            normalDamage,
            armorPenetrationRate,
            flatArmorPenetration,
            trueDamage,
            minimumNormalDamage
        );
    }

    public int GetBaseAttack()
    {
        return characterBaseAttack + weaponAttack + equipmentFlatAttack;
    }

    public float GetLevelGrowthMultiplier()
    {
        int currentLevel = playerLevel != null ? playerLevel.GetCurrentLevel() : 1;
        float multiplier = 1f;

        for (int reachedLevel = 2; reachedLevel <= currentLevel; reachedLevel++)
        {
            multiplier *= 1f + GetGrowthRate(reachedLevel);
        }

        return multiplier;
    }

    public float GetDamageIncreaseRate() => damageIncreaseRate;
    public float GetTrueDamageRate() => trueDamageRate;

    public void SetWeaponAttack(int value)
    {
        weaponAttack = Mathf.Max(0, value);
    }

    public void SetEquipmentFlatAttack(int value)
    {
        equipmentFlatAttack = Mathf.Max(0, value);
    }

    public void SetDamageIncreaseRate(float value)
    {
        damageIncreaseRate = Mathf.Max(0f, value);
    }

    public void SetArmorPenetrationRate(float value)
    {
        armorPenetrationRate = Mathf.Clamp01(value);
    }

    public void SetFlatArmorPenetration(int value)
    {
        flatArmorPenetration = Mathf.Max(0, value);
    }

    public void SetTrueDamageRate(float value)
    {
        trueDamageRate = Mathf.Max(0f, value);
    }

    private float GetGrowthRate(int reachedLevel)
    {
        float selectedRate = defaultGrowthRate;

        foreach (LevelGrowthOverride growthOverride in growthOverrides)
        {
            if (growthOverride.Contains(reachedLevel))
            {
                selectedRate = growthOverride.growthRate;
            }
        }

        return selectedRate;
    }
}
