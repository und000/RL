using System;
using UnityEngine;

public enum WeaponKnockbackDirection
{
    AwayFromAttackOrigin,
    AimDirection
}

[Serializable]
public class WeaponHitWindow
{
    [Tooltip("ID of the collider group configured on the weapon prefab.")]
    public string hitboxGroupId = "Main";
    [Tooltip("Hit detection start time in base animation seconds.")]
    [Min(0f)] public float startTime = 0.1f;
    [Tooltip("Hit detection end time in base animation seconds.")]
    [Min(0f)] public float endTime = 0.35f;
    [Min(0f)] public float damageMultiplier = 1f;
    [Min(0f)] public float knockbackStrength = 3f;
    [Min(0f)] public float knockbackDuration = 0.2f;
    public WeaponKnockbackDirection knockbackDirection =
        WeaponKnockbackDirection.AwayFromAttackOrigin;

    public void Normalize(float animationDuration)
    {
        if (string.IsNullOrWhiteSpace(hitboxGroupId)) hitboxGroupId = "Main";
        startTime = Mathf.Clamp(startTime, 0f, animationDuration);
        endTime = Mathf.Clamp(Mathf.Max(startTime, endTime), startTime, animationDuration);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        knockbackStrength = Mathf.Max(0f, knockbackStrength);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
    }
}

[Serializable]
public class WeaponAttackStep
{
    [Tooltip("State name in the Animator Controller, such as Attack1.")]
    public string animatorStateName = "Attack1";
    [Tooltip("Delay before this attack animation starts, in base seconds.")]
    [Min(0f)] public float startDelay;
    [Tooltip("Desired base duration of the attack animation.")]
    [Min(0.05f)] public float duration = 0.5f;
    [Tooltip("Delay after the animation before the next attack begins.")]
    [Min(0f)] public float recoveryDuration = 0.15f;
    [Tooltip("First animation time at which a click can queue the next attack.")]
    [Min(0f)] public float nextInputStartTime;
    [Tooltip("Last animation/recovery time at which a click can queue the next attack.")]
    [Min(0f)] public float nextInputEndTime = 0.65f;
    [Tooltip("Each entry is an independent hit. Multiple entries enable multi-hit attacks.")]
    public WeaponHitWindow[] hitWindows = { new WeaponHitWindow() };

    public float TotalDuration => startDelay + duration + recoveryDuration;

    public void Normalize(int index)
    {
        if (string.IsNullOrWhiteSpace(animatorStateName))
        {
            animatorStateName = $"Attack{index + 1}";
        }

        startDelay = Mathf.Max(0f, startDelay);
        duration = Mathf.Max(0.05f, duration);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        nextInputStartTime = Mathf.Clamp(nextInputStartTime, 0f, duration + recoveryDuration);
        nextInputEndTime = Mathf.Clamp(
            Mathf.Max(nextInputStartTime, nextInputEndTime),
            nextInputStartTime,
            duration + recoveryDuration);

        if (hitWindows == null) hitWindows = Array.Empty<WeaponHitWindow>();
        foreach (WeaponHitWindow hitWindow in hitWindows)
        {
            hitWindow?.Normalize(duration);
        }
    }
}

[CreateAssetMenu(fileName = "WeaponStats_New", menuName = "Survivor/Weapons/Weapon Profile")]
public class WeaponStatsProfile : ScriptableObject
{
    [Header("Weapon Prefab")]
    [SerializeField] private GameObject weaponPrefab;

    [Header("Core Attack Stats")]
    [Tooltip("Weapon damage added to the character and equipment base attack.")]
    [SerializeField, Min(0f)] private float baseDamage = 20f;
    [Tooltip("1 = normal speed, 1.5 = 50% faster, 0.5 = 50% slower.")]
    [SerializeField, Min(0.01f)] private float attackSpeedMultiplier = 1f;
    [Tooltip("Multiplier applied to this weapon's final normal attack damage.")]
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;

    [Header("Required Motion")]
    [Tooltip("Every weapon must have an Idle state in its Animator Controller.")]
    [SerializeField] private string idleStateName = "Idle";

    [Header("Basic Attack Input")]
    [SerializeField] private bool repeatWhileHeld;
    [SerializeField, Min(0f)] private float comboResetWindow = 0.6f;
    [SerializeField, Min(0f)] private float inputBufferDuration = 0.2f;

    [Header("Basic Attack Steps")]
    [Tooltip("The array size is the basic combo count. Each Hit Windows size is that step's hit count.")]
    [SerializeField] private WeaponAttackStep[] basicAttackSteps =
    {
        new WeaponAttackStep()
    };

    [Header("Weapon Skills")]
    [Tooltip("Optional weapon-specific skills. Each skill owns its own animation step list.")]
    [SerializeField] private WeaponSkillProfile[] weaponSkills = Array.Empty<WeaponSkillProfile>();

    public float BaseDamage => baseDamage;
    public float AttackSpeedMultiplier => Mathf.Max(0.01f, attackSpeedMultiplier);
    public float DamageMultiplier => damageMultiplier;
    public GameObject WeaponPrefab => weaponPrefab;
    public string IdleStateName => idleStateName;
    public bool RepeatWhileHeld => repeatWhileHeld;
    public float ComboResetWindow => comboResetWindow;
    public float InputBufferDuration => inputBufferDuration;
    public int BasicAttackCount => basicAttackSteps != null ? basicAttackSteps.Length : 0;
    public int SkillCount => weaponSkills != null ? weaponSkills.Length : 0;
    public WeaponSkillProfile[] WeaponSkills => weaponSkills;

    public WeaponAttackStep GetBasicAttackStep(int index)
    {
        if (basicAttackSteps == null || basicAttackSteps.Length == 0) return null;
        return basicAttackSteps[Mathf.Clamp(index, 0, basicAttackSteps.Length - 1)];
    }

    public WeaponSkillProfile GetSkill(int index)
    {
        if (weaponSkills == null || index < 0 || index >= weaponSkills.Length) return null;
        return weaponSkills[index];
    }

    private void OnValidate()
    {
        baseDamage = Mathf.Max(0f, baseDamage);
        attackSpeedMultiplier = Mathf.Max(0.01f, attackSpeedMultiplier);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        comboResetWindow = Mathf.Max(0f, comboResetWindow);
        inputBufferDuration = Mathf.Max(0f, inputBufferDuration);
        if (basicAttackSteps == null) basicAttackSteps = Array.Empty<WeaponAttackStep>();
        for (int index = 0; index < basicAttackSteps.Length; index++)
        {
            basicAttackSteps[index]?.Normalize(index);
        }
        if (weaponSkills == null) weaponSkills = Array.Empty<WeaponSkillProfile>();
    }
}
