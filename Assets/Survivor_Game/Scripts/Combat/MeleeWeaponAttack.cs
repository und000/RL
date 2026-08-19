using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class WeaponHitboxGroup
{
    public string id = "Main";
    public Collider2D[] colliders = Array.Empty<Collider2D>();
}

[DisallowMultipleComponent]
[RequireComponent(typeof(WeaponAimController))]
[AddComponentMenu("Combat/Melee Weapon Attack")]
public class MeleeWeaponAttack : MonoBehaviour
{
    [Header("Weapon Profile")]
    [SerializeField] private WeaponStatsProfile weaponStats;

    [Header("Animation")]
    [SerializeField] private Animator swingAnimator;

    [Header("Hitbox Groups")]
    [Tooltip("IDs must match Hitbox Group ID values in the weapon profile.")]
    [SerializeField] private WeaponHitboxGroup[] hitboxGroups =
        Array.Empty<WeaponHitboxGroup>();
    [SerializeField] private Transform attackOrigin;

    private readonly List<Collider2D> overlapResults = new List<Collider2D>(32);
    private readonly List<HashSet<EnemyHealth>> hitEnemiesByWindow =
        new List<HashSet<EnemyHealth>>();
    private WeaponAimController aimController;
    private PlayerCombatStats combatStats;
    private ContactFilter2D enemyFilter;
    private float stepElapsed;
    private float nextAttackTime;
    private float lastAttackCompletedTime = float.NegativeInfinity;
    private float bufferedAttackUntil = float.NegativeInfinity;
    private int currentComboIndex = -1;
    private bool attacking;
    private bool animatorStarted;
    private bool externalActionLocked;
    private bool queuedNextAttack;

    public int AttackCount => weaponStats != null ? weaponStats.BasicAttackCount : 0;
    public int CurrentComboIndex => currentComboIndex;
    public bool IsAttacking => attacking;
    public bool IsExternalActionActive => externalActionLocked;
    public WeaponStatsProfile WeaponProfile => weaponStats;
    public Animator SwingAnimator => swingAnimator;

    private void Awake()
    {
        aimController = GetComponent<WeaponAimController>();
        combatStats = GetComponentInParent<PlayerCombatStats>();
        if (attackOrigin == null) attackOrigin = transform;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyFilter = new ContactFilter2D();
        if (enemyLayer >= 0)
        {
            enemyFilter.SetLayerMask(1 << enemyLayer);
        }
        enemyFilter.useTriggers = false;
        SetAllHitboxesEnabled(false);
    }

    private void Start()
    {
        PlayIdle();
    }

    private void Update()
    {
        if (externalActionLocked) return;
        Mouse mouse = Mouse.current;
        if (mouse == null || weaponStats == null) return;
        bool pressedThisFrame = mouse.leftButton.wasPressedThisFrame;

        if (attacking)
        {
            if (pressedThisFrame)
            {
                bufferedAttackUntil = Time.time + weaponStats.InputBufferDuration;
            }
            UpdateAttack();
            return;
        }

        if (LevelUpUI.IsPopupOpen) return;
        if (pressedThisFrame)
        {
            bufferedAttackUntil = Time.time + weaponStats.InputBufferDuration;
        }
        if (Time.time < nextAttackTime) return;

        bool requested = weaponStats.RepeatWhileHeld
            ? mouse.leftButton.isPressed
            : pressedThisFrame || Time.time <= bufferedAttackUntil;
        if (!requested) return;

        bufferedAttackUntil = float.NegativeInfinity;
        bool continueCombo = Time.time - lastAttackCompletedTime <=
            weaponStats.ComboResetWindow;
        BeginAttack(continueCombo ? GetNextComboIndex() : 0);
    }

    public void BeginAttack()
    {
        if (weaponStats == null) return;
        bool continueCombo = Time.time - lastAttackCompletedTime <=
            weaponStats.ComboResetWindow;
        BeginAttack(continueCombo ? GetNextComboIndex() : 0);
    }

    private void BeginAttack(int comboIndex)
    {
        if (externalActionLocked || attacking || Time.time < nextAttackTime ||
            AttackCount == 0) return;

        currentComboIndex = Mathf.Clamp(comboIndex, 0, AttackCount - 1);
        WeaponAttackStep step = GetCurrentStep();
        if (step == null) return;

        attacking = true;
        animatorStarted = false;
        queuedNextAttack = false;
        stepElapsed = 0f;
        PrepareHitWindowCaches(step);
        SetAllHitboxesEnabled(false);
        aimController.BeginAttack();

        if (step.startDelay <= 0f)
        {
            StartAttackAnimation(step);
        }
    }

    private void UpdateAttack()
    {
        WeaponAttackStep step = GetCurrentStep();
        if (step == null)
        {
            CancelAttack();
            return;
        }

        stepElapsed += Time.deltaTime * GetAttackSpeedMultiplier();
        float motionElapsed = stepElapsed - step.startDelay;

        if (!animatorStarted && motionElapsed >= 0f)
        {
            StartAttackAnimation(step);
        }

        TryConsumeBufferedComboInput(step, motionElapsed);
        if (motionElapsed >= 0f && motionElapsed <= step.duration)
        {
            QueryActiveHitWindows(step, motionElapsed);
        }

        if (stepElapsed >= step.TotalDuration)
        {
            CompleteCurrentAttack();
        }
    }

    private void TryConsumeBufferedComboInput(WeaponAttackStep step, float motionElapsed)
    {
        if (queuedNextAttack || Time.time > bufferedAttackUntil) return;
        if (motionElapsed < step.nextInputStartTime ||
            motionElapsed > step.nextInputEndTime) return;

        queuedNextAttack = true;
        bufferedAttackUntil = float.NegativeInfinity;
    }

    private void QueryActiveHitWindows(WeaponAttackStep step, float motionElapsed)
    {
        WeaponHitWindow[] windows = step.hitWindows;
        if (windows == null || windows.Length == 0) return;

        Physics2D.SyncTransforms();
        for (int index = 0; index < windows.Length; index++)
        {
            WeaponHitWindow window = windows[index];
            if (window == null || motionElapsed < window.startTime ||
                motionElapsed > window.endTime) continue;

            QueryHitWindow(window, index);
        }
    }

    private void QueryHitWindow(WeaponHitWindow window, int windowIndex)
    {
        WeaponHitboxGroup group = FindHitboxGroup(window.hitboxGroupId);
        if (group == null || group.colliders == null) return;

        HashSet<EnemyHealth> hitEnemies = hitEnemiesByWindow[windowIndex];
        foreach (Collider2D hitbox in group.colliders)
        {
            if (hitbox == null) continue;
            hitbox.enabled = true;
            overlapResults.Clear();
            hitbox.Overlap(enemyFilter, overlapResults);
            hitbox.enabled = false;

            foreach (Collider2D targetCollider in overlapResults)
            {
                EnemyHealth health = targetCollider.GetComponentInParent<EnemyHealth>();
                if (health == null || !hitEnemies.Add(health)) continue;
                ApplyHit(health, window);
            }
        }
    }

    private void ApplyHit(EnemyHealth health, WeaponHitWindow window)
    {
        if (combatStats != null)
        {
            health.TakeDamage(combatStats.CreateWeaponDamageData(
                weaponStats.BaseDamage,
                weaponStats.DamageMultiplier * window.damageMultiplier));
        }

        Vector2 direction = window.knockbackDirection ==
            WeaponKnockbackDirection.AimDirection
            ? aimController.AimDirection
            : ((Vector2)health.transform.position - (Vector2)attackOrigin.position).normalized;
        if (direction == Vector2.zero) direction = aimController.AimDirection;

        if (health.TryGetComponent(out EnemyKnockback knockback))
        {
            knockback.ApplyKnockback(
                direction,
                window.knockbackStrength,
                window.knockbackDuration);
        }
        if (health.TryGetComponent(out EnemyHitEffect hitEffect)) hitEffect.Play();
    }

    private void CompleteCurrentAttack()
    {
        attacking = false;
        animatorStarted = false;
        SetAllHitboxesEnabled(false);
        aimController.EndAttack();
        lastAttackCompletedTime = Time.time;
        nextAttackTime = Time.time;
        PlayIdle();

        if (queuedNextAttack)
        {
            queuedNextAttack = false;
            BeginAttack(GetNextComboIndex());
        }
        else
        {
            bufferedAttackUntil = float.NegativeInfinity;
        }
    }

    private void StartAttackAnimation(WeaponAttackStep step)
    {
        animatorStarted = true;
        if (swingAnimator == null) return;

        swingAnimator.speed = 1f;
        swingAnimator.Play(step.animatorStateName, 0, 0f);
        swingAnimator.Update(0f);
        swingAnimator.speed = GetAnimatorPlaybackSpeed(step);
    }

    private float GetAnimatorPlaybackSpeed(WeaponAttackStep step)
    {
        AnimatorClipInfo[] clips = swingAnimator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0 || clips[0].clip == null) return 1f;

        float actualDuration = step.duration / GetAttackSpeedMultiplier();
        return Mathf.Max(0.01f, clips[0].clip.length / Mathf.Max(0.05f, actualDuration));
    }

    private void PlayIdle()
    {
        if (swingAnimator == null || weaponStats == null ||
            string.IsNullOrWhiteSpace(weaponStats.IdleStateName)) return;

        swingAnimator.speed = 1f;
        swingAnimator.Play(weaponStats.IdleStateName, 0, 0f);
    }

    public bool TryBeginExternalAction(bool interruptBasicAttack)
    {
        if (externalActionLocked) return false;
        if (attacking)
        {
            if (!interruptBasicAttack) return false;
            CancelAttack();
        }

        externalActionLocked = true;
        SetAllHitboxesEnabled(false);
        aimController.BeginAttack();
        return true;
    }

    public void CompleteExternalAction()
    {
        if (!externalActionLocked) return;
        externalActionLocked = false;
        aimController.EndAttack();
        PlayIdle();
    }

    private void PrepareHitWindowCaches(WeaponAttackStep step)
    {
        int count = step.hitWindows != null ? step.hitWindows.Length : 0;
        while (hitEnemiesByWindow.Count < count)
        {
            hitEnemiesByWindow.Add(new HashSet<EnemyHealth>());
        }
        foreach (HashSet<EnemyHealth> cache in hitEnemiesByWindow) cache.Clear();
    }

    private WeaponHitboxGroup FindHitboxGroup(string id)
    {
        if (hitboxGroups == null) return null;
        foreach (WeaponHitboxGroup group in hitboxGroups)
        {
            if (group != null && string.Equals(group.id, id, StringComparison.Ordinal))
            {
                return group;
            }
        }
        return null;
    }

    private void SetAllHitboxesEnabled(bool enabledState)
    {
        if (hitboxGroups == null) return;
        foreach (WeaponHitboxGroup group in hitboxGroups)
        {
            if (group?.colliders == null) continue;
            foreach (Collider2D hitbox in group.colliders)
            {
                if (hitbox != null) hitbox.enabled = enabledState;
            }
        }
    }

    private WeaponAttackStep GetCurrentStep() =>
        weaponStats != null ? weaponStats.GetBasicAttackStep(currentComboIndex) : null;
    private int GetNextComboIndex() =>
        AttackCount > 0 ? (currentComboIndex + 1) % AttackCount : 0;
    private float GetAttackSpeedMultiplier() =>
        weaponStats != null ? weaponStats.AttackSpeedMultiplier : 1f;

    private void CancelAttack()
    {
        attacking = false;
        animatorStarted = false;
        externalActionLocked = false;
        queuedNextAttack = false;
        bufferedAttackUntil = float.NegativeInfinity;
        SetAllHitboxesEnabled(false);
        aimController?.CancelAttack();
        PlayIdle();
    }

    private void OnDisable()
    {
        CancelAttack();
    }

    private void OnValidate()
    {
        if (hitboxGroups == null) hitboxGroups = Array.Empty<WeaponHitboxGroup>();
        for (int index = 0; index < hitboxGroups.Length; index++)
        {
            WeaponHitboxGroup group = hitboxGroups[index];
            if (group == null) continue;
            if (string.IsNullOrWhiteSpace(group.id))
            {
                group.id = index == 0 ? "Main" : $"Group{index + 1}";
            }
            if (group.colliders == null) group.colliders = Array.Empty<Collider2D>();
        }
    }
}
