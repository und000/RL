using System;
using UnityEngine;

[Serializable]
public class WeaponSkillMotionStep
{
    [Tooltip("State name in the weapon Animator Controller.")]
    public string animatorStateName = "Skill1";
    [Min(0f)] public float startDelay;
    [Min(0.05f)] public float duration = 0.5f;

    public void Normalize(int index)
    {
        if (string.IsNullOrWhiteSpace(animatorStateName))
        {
            animatorStateName = $"Skill{index + 1}";
        }
        startDelay = Mathf.Max(0f, startDelay);
        duration = Mathf.Max(0.05f, duration);
    }
}

[CreateAssetMenu(fileName = "WeaponSkill_New", menuName = "Survivor/Weapons/Weapon Skill Profile")]
public class WeaponSkillProfile : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string skillId = "Skill_New";
    [SerializeField] private string displayName = "New Skill";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.15f;
    [SerializeField] private bool useWeaponAttackSpeed = true;
    [SerializeField] private bool canInterruptBasicAttack;

    [Header("Animation Steps")]
    [Tooltip("The array size is this skill's animation count.")]
    [SerializeField] private WeaponSkillMotionStep[] animationSteps =
    {
        new WeaponSkillMotionStep()
    };

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public float Cooldown => cooldown;
    public float RecoveryDuration => recoveryDuration;
    public bool UseWeaponAttackSpeed => useWeaponAttackSpeed;
    public bool CanInterruptBasicAttack => canInterruptBasicAttack;
    public int AnimationCount => animationSteps != null ? animationSteps.Length : 0;
    public WeaponSkillMotionStep[] AnimationSteps => animationSteps;

    private void OnValidate()
    {
        cooldown = Mathf.Max(0f, cooldown);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        if (animationSteps == null) animationSteps = Array.Empty<WeaponSkillMotionStep>();
        for (int index = 0; index < animationSteps.Length; index++)
        {
            animationSteps[index]?.Normalize(index);
        }
    }
}
