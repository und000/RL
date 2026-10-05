using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeleeWeaponAttack))]
[AddComponentMenu("Combat/Weapon Skill Controller")]
public class WeaponSkillController : MonoBehaviour
{
    private readonly Dictionary<WeaponSkillProfile, float> readyTimes =
        new Dictionary<WeaponSkillProfile, float>();
    private MeleeWeaponAttack meleeAttack;
    private Coroutine activeRoutine;

    public WeaponSkillProfile ActiveSkill { get; private set; }
    public bool IsUsingSkill => ActiveSkill != null;

    public event Action<WeaponSkillProfile> SkillStarted;
    public event Action<WeaponSkillProfile, int> MotionStarted;
    public event Action<WeaponSkillProfile> SkillCompleted;

    private void Awake()
    {
        meleeAttack = GetComponent<MeleeWeaponAttack>();
    }

    public bool TryUseSkill(int skillIndex)
    {
        WeaponStatsProfile weaponProfile = meleeAttack.WeaponProfile;
        return weaponProfile != null && TryUseSkill(weaponProfile.GetSkill(skillIndex));
    }

    public bool TryUseSkill(string skillId)
    {
        WeaponStatsProfile weaponProfile = meleeAttack.WeaponProfile;
        if (weaponProfile?.WeaponSkills == null || string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        foreach (WeaponSkillProfile skill in weaponProfile.WeaponSkills)
        {
            if (skill != null && string.Equals(
                skill.SkillId,
                skillId,
                StringComparison.Ordinal))
            {
                return TryUseSkill(skill);
            }
        }
        return false;
    }

    public bool TryUseSkill(WeaponSkillProfile skill)
    {
        if (!isActiveAndEnabled || meleeAttack == null || meleeAttack.WeaponProfile == null ||
            skill == null || skill.IsEquippableSpecialAttack || skill.AnimationCount == 0 || IsUsingSkill ||
            GetRemainingCooldown(skill) > 0f) return false;
        if (!meleeAttack.TryBeginExternalAction(skill.CanInterruptBasicAttack)) return false;

        ActiveSkill = skill;
        readyTimes[skill] = Time.time + skill.Cooldown;
        SkillStarted?.Invoke(skill);
        activeRoutine = StartCoroutine(PlaySkillSequence(skill));
        return true;
    }

    public float GetRemainingCooldown(int skillIndex)
    {
        WeaponStatsProfile weaponProfile = meleeAttack.WeaponProfile;
        return weaponProfile == null
            ? 0f
            : GetRemainingCooldown(weaponProfile.GetSkill(skillIndex));
    }

    public float GetRemainingCooldown(WeaponSkillProfile skill)
    {
        if (skill == null || !readyTimes.TryGetValue(skill, out float readyTime)) return 0f;
        return Mathf.Max(0f, readyTime - Time.time);
    }

    private IEnumerator PlaySkillSequence(WeaponSkillProfile skill)
    {
        float speedMultiplier = skill.UseWeaponAttackSpeed
            ? meleeAttack.WeaponProfile.AttackSpeedMultiplier
            : 1f;
        WeaponSkillMotionStep[] steps = skill.AnimationSteps;

        for (int index = 0; index < steps.Length; index++)
        {
            WeaponSkillMotionStep step = steps[index];
            if (step == null) continue;

            yield return WaitForBaseSeconds(step.startDelay, speedMultiplier);
            PlayMotion(step, speedMultiplier);
            MotionStarted?.Invoke(skill, index);
            yield return WaitForBaseSeconds(step.duration, speedMultiplier);
        }

        yield return WaitForBaseSeconds(skill.RecoveryDuration, speedMultiplier);
        FinishSkill(skill, true);
    }

    private void PlayMotion(WeaponSkillMotionStep step, float speedMultiplier)
    {
        Animator animator = meleeAttack.SwingAnimator;
        if (animator == null) return;

        animator.speed = 1f;
        animator.Play(step.animatorStateName, 0, 0f);
        animator.Update(0f);
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0 || clips[0].clip == null) return;

        float actualDuration = step.duration / Mathf.Max(0.01f, speedMultiplier);
        animator.speed = Mathf.Max(
            0.01f,
            clips[0].clip.length / Mathf.Max(0.05f, actualDuration));
    }

    private static IEnumerator WaitForBaseSeconds(float duration, float speedMultiplier)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * Mathf.Max(0.01f, speedMultiplier);
            yield return null;
        }
    }

    private void FinishSkill(WeaponSkillProfile skill, bool invokeCompleted)
    {
        activeRoutine = null;
        ActiveSkill = null;
        meleeAttack.CompleteExternalAction();
        if (invokeCompleted) SkillCompleted?.Invoke(skill);
    }

    /// <summary>무기 교체·회피·사망 시 동작과 외부 공격 잠금을 함께 해제한다.</summary>
    public void CancelSkill()
    {
        if (IsUsingSkill)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            WeaponSkillProfile interruptedSkill = ActiveSkill;
            FinishSkill(interruptedSkill, false);
        }
    }

    private void OnDisable()
    {
        CancelSkill();
    }
}
