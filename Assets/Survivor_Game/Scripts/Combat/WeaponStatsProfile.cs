using System;
using UnityEngine;

public enum WeaponKnockbackDirection
{
    AwayFromAttackOrigin,
    AimDirection
}

[Serializable]
public class WeaponDissolveSettings
{
    [Header("재생")]
    [Min(0.01f)] public float duration = 0.18f;

    [Header("노이즈 텍스처")]
    public Texture2D noiseTexture;
    public Vector2 noiseTiling = Vector2.one;
    [Range(-180f, 180f)] public float noiseAngle;
    [Range(-150f, 150f)] public float noiseArcBendAngle = 60f;
    public Vector2 noiseArcBendCenter = new Vector2(0.5f, 0.5f);
    public Vector2 noiseScrollSpeed = new Vector2(0.8f, 0f);
    [Range(0.1f, 4f)] public float noiseContrast = 1.35f;
    public bool randomizeNoiseOffset;

    [Header("진행 형태")]
    [Range(0f, 1f)] public float shapeRoundness = 1f;
    public Vector2 dissolveDirection = Vector2.right;
    [Range(0f, 1f)] public float progressSpread = 0.7f;
    public bool reverseDirection;

    [Header("원형 진행")]
    public Vector2 radialCenter = new Vector2(0.5f, 0.5f);
    public Vector2 radialAspect = Vector2.one;
    public bool radialOutsideIn;

    [Header("디졸브 경계 부드러움")]
    [Range(0.001f, 0.2f)] public float softness = 0.055f;

    public void Normalize()
    {
        duration = Mathf.Max(0.01f, duration);
        noiseTiling.x = Mathf.Max(0.01f, Mathf.Abs(noiseTiling.x));
        noiseTiling.y = Mathf.Max(0.01f, Mathf.Abs(noiseTiling.y));
        noiseAngle = Mathf.Clamp(noiseAngle, -180f, 180f);
        noiseArcBendAngle = Mathf.Clamp(noiseArcBendAngle, -150f, 150f);
        noiseArcBendCenter.x = Mathf.Clamp01(noiseArcBendCenter.x);
        noiseArcBendCenter.y = Mathf.Clamp01(noiseArcBendCenter.y);
        noiseContrast = Mathf.Clamp(noiseContrast, 0.1f, 4f);
        shapeRoundness = Mathf.Clamp01(shapeRoundness);
        progressSpread = Mathf.Clamp01(progressSpread);
        radialCenter.x = Mathf.Clamp01(radialCenter.x);
        radialCenter.y = Mathf.Clamp01(radialCenter.y);
        radialAspect.x = Mathf.Max(0.01f, Mathf.Abs(radialAspect.x));
        radialAspect.y = Mathf.Max(0.01f, Mathf.Abs(radialAspect.y));
        softness = Mathf.Clamp(softness, 0.001f, 0.2f);
    }
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
    [Header("Forward Movement")]
    [Tooltip("Signed distance along the aim direction captured when the animation starts. Positive moves forward, negative moves backward, zero disables movement.")]
    public float movementDistance;
    [Tooltip("Movement time at weapon speed 1. Scales with weapon attack speed.")]
    [Min(0.01f)] public float movementDuration = 0.1f;
    [Tooltip("State name in the Animator Controller, such as Attack1.")]
    public string animatorStateName = "Attack1";
    [Tooltip("Delay before this attack animation starts, in base seconds.")]
    [Min(0f)] public float startDelay;
    [Tooltip("Reference timeline for hit/input timings, not playback duration. Times within this duration are mapped proportionally onto the actual clip. Playback duration is clip length / weapon speed.")]
    [Min(0.05f)] public float duration = 0.5f;
    [Tooltip("Delay after the animation before the next attack begins.")]
    [Min(0f)] public float recoveryDuration = 0.15f;
    [Tooltip("First animation time at which a click can queue the next attack.")]
    [Min(0f)] public float nextInputStartTime;
    [Tooltip("Last animation/recovery time at which a click can queue the next attack.")]
    [Min(0f)] public float nextInputEndTime = 0.65f;
    [Tooltip("Each entry is an independent hit. Multiple entries enable multi-hit attacks.")]
    public WeaponHitWindow[] hitWindows = { new WeaponHitWindow() };
    [Tooltip("Optional procedural swing visual for this attack step.")]
    public WeaponSwingArcSettings swingVfx = new WeaponSwingArcSettings();

    public float TotalDuration => startDelay + duration + recoveryDuration;

    public void Normalize(int index)
    {
        if (string.IsNullOrWhiteSpace(animatorStateName))
        {
            animatorStateName = $"Attack{index + 1}";
        }

        startDelay = Mathf.Max(0f, startDelay);
        movementDuration = Mathf.Max(0.01f, movementDuration);
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
        if (swingVfx == null) swingVfx = new WeaponSwingArcSettings();
        swingVfx.Normalize();
    }
}

[CreateAssetMenu(fileName = "WeaponStats_New", menuName = "Survivor/Weapons/Weapon Profile")]
public class WeaponStatsProfile : ScriptableObject
{
    [Header("Weapon Prefab")]
    [SerializeField] private GameObject weaponPrefab;
    [Tooltip("화면에 띄울 무기 이름. 비우면 에셋 이름을 쓴다.")]
    [SerializeField] private string displayName;
    [Tooltip("무기 등급. 바닥에 떨어져 있을 때의 연출 색이 여기서 나온다.")]
    [SerializeField] private ItemGrade grade = ItemGrade.Standard;

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
    [Tooltip("Fixed seconds used to blend from an attack or skill back to Idle.")]
    [SerializeField, Min(0f)] private float idleReturnBlendDuration = 0.1f;
    [Tooltip("Idle return progress: X = normalized time, Y = blend weight (0 attack pose, 1 idle pose).")]
    [SerializeField] private AnimationCurve idleReturnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Procedural Swing VFX")]
    [Tooltip("Pooled visual played at the enemy on each successful melee hit.")]
    [SerializeField] private ProjectileImpactVisual hitImpactPrefab;
    [Tooltip("Shared procedural arc VFX prefab. Shape and timing are set per attack step.")]
    [SerializeField] private WeaponSwingVFX swingVfxPrefab;

    [Header("Shared Dissolve VFX")]
    [Tooltip("Applied through MaterialPropertyBlock; no per-weapon material is required.")]
    [SerializeField] private WeaponDissolveSettings dissolveVfx =
        new WeaponDissolveSettings();

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

    [Header("Heavy Attack (Shift + Left Click)")]
    [Tooltip("Shift를 누른 채 좌클릭하면 나가는 강공격을 이 무기가 쓰는지 여부.")]
    [SerializeField] private bool useHeavyAttack;
    [Tooltip("강공격 1회에 소모하는 플레이어 에너지. 0이면 소모하지 않는다.")]
    [SerializeField, Min(0)] private int heavyAttackEnergyCost = 10;
    [Tooltip("강공격 한 방의 모션·판정·연출. 콤보에는 참여하지 않는 단독 공격이다.")]
    [SerializeField] private WeaponAttackStep heavyAttack = new WeaponAttackStep();

    [Header("Special Attack (Right Click)")]
    [SerializeField] private bool useSpecialAttack;
    [SerializeField] private WeaponAttackStep specialAttack = new WeaponAttackStep { animatorStateName = "SpecialAttack" };

    [Header("Weapon Skills")]
    [Tooltip("Optional weapon-specific skills. Each skill owns its own animation step list.")]
    [SerializeField] private WeaponSkillProfile[] weaponSkills = Array.Empty<WeaponSkillProfile>();

    public float BaseDamage => baseDamage;
    public float AttackSpeedMultiplier => Mathf.Max(0.01f, attackSpeedMultiplier);
    public float DamageMultiplier => damageMultiplier;
    public GameObject WeaponPrefab => weaponPrefab;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public ItemGrade Grade => grade;
    public string IdleStateName => idleStateName;
    public float IdleReturnBlendDuration => Mathf.Max(0f, idleReturnBlendDuration);
    public AnimationCurve IdleReturnCurve => idleReturnCurve;
    public WeaponSwingVFX SwingVfxPrefab => swingVfxPrefab;
    public ProjectileImpactVisual HitImpactPrefab => hitImpactPrefab;
    public WeaponDissolveSettings DissolveVfx => dissolveVfx;
    public bool RepeatWhileHeld => repeatWhileHeld;
    public float ComboResetWindow => comboResetWindow;
    public float InputBufferDuration => inputBufferDuration;
    /// <summary>Use Heavy Attack이 꺼져 있거나 상태 이름이 비면 강공격이 없는 무기로 취급한다.</summary>
    public WeaponAttackStep HeavyAttack =>
        useHeavyAttack && heavyAttack != null &&
        !string.IsNullOrWhiteSpace(heavyAttack.animatorStateName)
            ? heavyAttack
            : null;
    public bool HasHeavyAttack => HeavyAttack != null;
    public WeaponAttackStep SpecialAttack => useSpecialAttack ? specialAttack : null;
    public bool HasSpecialAttack => SpecialAttack != null;
    public int HeavyAttackEnergyCost => Mathf.Max(0, heavyAttackEnergyCost);

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
        idleReturnBlendDuration = Mathf.Max(0f, idleReturnBlendDuration);
        if (dissolveVfx == null) dissolveVfx = new WeaponDissolveSettings();
        dissolveVfx.Normalize();
        comboResetWindow = Mathf.Max(0f, comboResetWindow);
        inputBufferDuration = Mathf.Max(0f, inputBufferDuration);
        if (basicAttackSteps == null) basicAttackSteps = Array.Empty<WeaponAttackStep>();
        for (int index = 0; index < basicAttackSteps.Length; index++)
        {
            basicAttackSteps[index]?.Normalize(index);
        }
        heavyAttackEnergyCost = Mathf.Max(0, heavyAttackEnergyCost);
        if (heavyAttack == null) heavyAttack = new WeaponAttackStep();
        if (useHeavyAttack) heavyAttack.Normalize(0);
        if (specialAttack == null) specialAttack = new WeaponAttackStep { animatorStateName = "SpecialAttack" };
        if (useSpecialAttack) specialAttack.Normalize(0);
        if (weaponSkills == null) weaponSkills = Array.Empty<WeaponSkillProfile>();
    }
}
