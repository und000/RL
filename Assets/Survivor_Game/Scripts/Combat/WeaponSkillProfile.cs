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

    [Header("Equippable Special Attack")]
    [SerializeField] private bool equippableSpecialAttack;
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string description;
    [Tooltip("사용 가능한 무기군을 여러 개 선택합니다. Everything은 제한 없음, Nothing은 사용 불가입니다. 전용 기술에도 적용됩니다.")]
    [SerializeField] private WeaponFamily allowedWeaponFamilies = WeaponFamily.All;
    [Tooltip("Set for a unique weapon's bound skill. It can never become a transferable item.")]
    [SerializeField] private WeaponStatsProfile exclusiveWeapon;
    [SerializeField, Min(1)] private int mpCost = 5;
    [SerializeField] private AnimationClip specialAttackClip;
    [SerializeField] private WeaponAttackStep specialAttack = new WeaponAttackStep { animatorStateName = "SpecialAttack" };
    [Tooltip("Optional special-attack trail. Empty uses the weapon's trail prefab.")]
    [SerializeField] private WeaponSwingVFX specialAttackVfx;

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

    public WeaponFamily AllowedWeaponFamilies => allowedWeaponFamilies;
    public string AllowedWeaponFamiliesLabel => WeaponFamilyUtility.Label(allowedWeaponFamilies);
    public WeaponStatsProfile ExclusiveWeapon => exclusiveWeapon;
    public bool IsEquippableSpecialAttack => equippableSpecialAttack;
    public bool IsTransferable => equippableSpecialAttack && exclusiveWeapon == null;
    public Sprite Icon => icon;
    public string Description => description;
    public int MpCost => Mathf.Max(1, mpCost);
    public AnimationClip SpecialAttackClip => specialAttackClip;
    public WeaponAttackStep SpecialAttack => equippableSpecialAttack ? specialAttack : null;
    public WeaponSwingVFX SpecialAttackVfx => specialAttackVfx;

    public bool CanUseOn(WeaponStatsProfile weapon)
    {
        if (!equippableSpecialAttack || weapon == null || specialAttack == null || specialAttackClip == null) return false;
        if (!WeaponFamilyUtility.IsSingle(weapon.Family) || (allowedWeaponFamilies & weapon.Family) == 0) return false;
        if (exclusiveWeapon != null)
            return exclusiveWeapon == weapon && weapon.LockSpecialAttack && weapon.DefaultSpecialAttack == this;
        return true;
    }

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
        mpCost = Mathf.Max(1, mpCost);
        if (specialAttack == null) specialAttack = new WeaponAttackStep();
        specialAttack.animatorStateName = "SpecialAttack";
        specialAttack.Normalize(0);
        cooldown = Mathf.Max(0f, cooldown);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        if (animationSteps == null) animationSteps = Array.Empty<WeaponSkillMotionStep>();
        for (int index = 0; index < animationSteps.Length; index++)
        {
            animationSteps[index]?.Normalize(index);
        }
    }
}
