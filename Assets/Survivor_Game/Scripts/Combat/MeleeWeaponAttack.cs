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
[DefaultExecutionOrder(300)]
[RequireComponent(typeof(WeaponAimController))]
[AddComponentMenu("Combat/Melee Weapon Attack")]
public class MeleeWeaponAttack : MonoBehaviour
{
    [Header("Weapon Profile")]
    [SerializeField] private WeaponStatsProfile weaponStats;

    [Header("Animation")]
    [SerializeField] private Animator swingAnimator;
    [SerializeField] private Transform swingVfxRoot;
    [SerializeField] private Transform swingVfxMotionPoint;

    [Header("Hitbox Groups")]
    [Tooltip("IDs must match Hitbox Group ID values in the weapon profile. Add Polygon, Box, Circle or Capsule colliders from the SwingEffect hierarchy; one group may contain several shapes.")]
    [SerializeField] private WeaponHitboxGroup[] hitboxGroups =
        Array.Empty<WeaponHitboxGroup>();
    [SerializeField] private Transform attackOrigin;

    private readonly HashSet<int> firedProjectileWindows = new HashSet<int>();
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(32);
    private readonly List<HashSet<EnemyHealth>> hitEnemiesByWindow =
        new List<HashSet<EnemyHealth>>();
    private WeaponAimController aimController;
    private PlayerCombatStats combatStats;
    private PlayerEnergy playerEnergy;
    private PlayerMovement playerMovement;
    private bool specialAttackActive;
    private bool specialAttackConfigured;
    private WeaponSkillProfile equippedSpecialAttack;
    private RuntimeAnimatorController originalAnimatorController;
    private AnimatorOverrideController specialAttackOverride;
    public WeaponSkillProfile EquippedSpecialAttack => equippedSpecialAttack;
    public bool CanChangeSpecialAttack => CanStartAttack && !attacking;
    public bool HasEquippedSpecialAttack => equippedSpecialAttack != null && equippedSpecialAttack.CanUseOn(weaponStats);
    private ContactFilter2D enemyFilter;
    private float stepElapsed;
    private float motionDuration;
    private float timingScale = 1f;
    private float stepSpeed = 1f;
    private float lastAttackCompletedTime = float.NegativeInfinity;
    private float bufferedAttackUntil = float.NegativeInfinity;
    private int currentComboIndex = -1;
    private bool attacking;
    private bool animatorStarted;
    private bool attackMovementStarted;
    private Vector2 attackMovementDirection;
    private bool externalActionLocked;
    private bool queuedNextAttack;
    private bool heavyAttackActive;
    private Transform[] returnTransforms;
    private Vector3[] returnPositions, returnScales;
    private Quaternion[] returnRotations;
    private bool returningToIdle;
    private float returnElapsed;
    // 콤보로 다음 공격이 시작돼도 이전 궤적은 남아 자연스럽게 페이드된다.
    // 그 동안에도 취소/비활성화 시 전부 정리할 수 있게 겹치는 궤적을 모두 추적한다.
    private readonly List<WeaponSwingVfxHandle> activeSwingVfxHandles =
        new List<WeaponSwingVfxHandle>(4);

    public int AttackCount => weaponStats != null ? weaponStats.BasicAttackCount : 0;
    public int CurrentComboIndex => currentComboIndex;
    public bool IsAttacking => attacking;
    public bool IsExternalActionActive => externalActionLocked;
    public WeaponStatsProfile WeaponProfile => weaponStats;
    public Animator SwingAnimator => swingAnimator;

    private bool CanStartAttack => isActiveAndEnabled && !externalActionLocked && !GameInputKeys.IsGameplayBlocked &&
        (playerMovement == null || playerMovement.CanAttack);

    /// <summary>장착한 프로필을 적용한다. 프리팹 변형도 실제 장비의 수치를 사용한다.</summary>
    public void Configure(WeaponStatsProfile profile)
    {
        CancelAttack(true);
        weaponStats = profile;
        SetSpecialAttack(profile != null ? profile.DefaultSpecialAttack : null);
        currentComboIndex = -1;
        lastAttackCompletedTime = float.NegativeInfinity;
        PlayIdle(true);
    }

    public bool CanSetSpecialAttack(WeaponSkillProfile skill)
    {
        if (attacking || externalActionLocked || swingAnimator == null || weaponStats == null) return false;
        if (weaponStats.LockSpecialAttack && skill != weaponStats.DefaultSpecialAttack) return false;
        if (skill == null) return true;
        if (!skill.CanUseOn(weaponStats) || weaponStats.SpecialAttackSlotClip == null) return false;
        RuntimeAnimatorController source = originalAnimatorController != null
            ? originalAnimatorController : swingAnimator.runtimeAnimatorController;
        if (source == null || !swingAnimator.HasState(0, Animator.StringToHash(weaponStats.SpecialAttackStateName))) return false;
        bool hasSlot = false;
        foreach (AnimationClip clip in source.animationClips)
            if (clip == weaponStats.SpecialAttackSlotClip) hasSlot = true;
        if (!hasSlot) return false;
        if (skill.SpecialAttack.fireProjectile) return weaponStats.ProjectilePrefab != null;
        if (skill.SpecialAttack.hitWindows != null)
            foreach (WeaponHitWindow hit in skill.SpecialAttack.hitWindows)
                if (hit != null && FindHitboxGroup(hit.hitboxGroupId) == null) return false;
        return true;
    }

    public bool SetSpecialAttack(WeaponSkillProfile skill)
    {
        if (!CanSetSpecialAttack(skill)) return false;
        if (originalAnimatorController == null) originalAnimatorController = swingAnimator.runtimeAnimatorController;
        if (specialAttackOverride != null)
        {
            swingAnimator.runtimeAnimatorController = originalAnimatorController;
            Destroy(specialAttackOverride);
            specialAttackOverride = null;
        }
        specialAttackConfigured = true;
        equippedSpecialAttack = skill;
        if (skill != null)
        {
            specialAttackOverride = new AnimatorOverrideController(originalAnimatorController);
            specialAttackOverride[weaponStats.SpecialAttackSlotClip] = skill.SpecialAttackClip;
            swingAnimator.runtimeAnimatorController = specialAttackOverride;
        }
        return true;
    }

    private void OnDestroy()
    {
        if (specialAttackOverride != null) Destroy(specialAttackOverride);
    }

    private void Awake()
    {
        aimController = GetComponent<WeaponAimController>();
        combatStats = GetComponentInParent<PlayerCombatStats>();
        playerEnergy = GetComponentInParent<PlayerEnergy>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (attackOrigin == null) attackOrigin = transform;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyFilter = new ContactFilter2D();
        if (enemyLayer >= 0)
        {
            enemyFilter.SetLayerMask(1 << enemyLayer);
        }
        enemyFilter.useTriggers = false;
        SetAllHitboxesEnabled(false);
        if (swingAnimator != null)
        {
            // The Animator can live on the weapon root. Only blend the aiming
            // hierarchy's children, leaving cursor aiming and sibling VFX alone.
            Transform poseRoot = aimController.AimRoot;
            var nodes = new List<Transform>(poseRoot.GetComponentsInChildren<Transform>(true));
            nodes.Remove(poseRoot);
            nodes.Remove(swingAnimator.transform);
            returnTransforms = nodes.ToArray();
            returnPositions = new Vector3[nodes.Count];
            returnScales = new Vector3[nodes.Count];
            returnRotations = new Quaternion[nodes.Count];
        }
    }

    private void Start()
    {
        // Standalone prefab instances also receive their weapon default. Equipment can explicitly leave the slot empty.
        if (!specialAttackConfigured) Configure(weaponStats);
        PlayIdle(true);
    }

    private void Update()
    {
        if (playerMovement != null && !playerMovement.CanAttack)
        {
            if (attacking || externalActionLocked) CancelAttack();
            return;
        }
        if (externalActionLocked || GameInputKeys.IsGameplayBlocked) return;
        Mouse mouse = Mouse.current;
        if (mouse == null || weaponStats == null) return;

        Keyboard keyboard = Keyboard.current;
        bool heavyModifierHeld = keyboard != null && keyboard.shiftKey.isPressed;
        // Shift를 누른 채로 한 클릭은 강공격 전용이라 기본 콤보 버퍼에 넣지 않는다.
        bool pressedThisFrame = mouse.leftButton.wasPressedThisFrame && !heavyModifierHeld;
        bool heavyPressedThisFrame =
            mouse.leftButton.isPressed && heavyModifierHeld;

        if (attacking)
        {
            if (pressedThisFrame)
            {
                bufferedAttackUntil = Time.time + weaponStats.InputBufferDuration;
            }
            return;
        }

        if ((weaponStats.RepeatWhileHeld ? mouse.rightButton.isPressed : mouse.rightButton.wasPressedThisFrame)
            && HasEquippedSpecialAttack)
        {
            BeginSpecialAttack();
            return;
        }
        if (heavyPressedThisFrame && weaponStats.HasHeavyAttack)
        {
            bufferedAttackUntil = float.NegativeInfinity;
            BeginHeavyAttack();
            return;
        }
        if (heavyModifierHeld) return;

        if (pressedThisFrame)
        {
            bufferedAttackUntil = Time.time + weaponStats.InputBufferDuration;
        }

        bool requested = weaponStats.RepeatWhileHeld
            ? mouse.leftButton.isPressed
            : pressedThisFrame || Time.time <= bufferedAttackUntil;
        if (!requested) return;

        bufferedAttackUntil = float.NegativeInfinity;
        bool continueCombo = Time.time - lastAttackCompletedTime <=
            weaponStats.ComboResetWindow;
        BeginAttack(continueCombo ? GetNextComboIndex() : 0);
    }

    private void LateUpdate()
    {
        if (GameInputKeys.IsGameplayBlocked) return;
        // Query colliders after Animator has applied this frame's weapon pose.
        // 회피나 사망이 이 프레임의 Update 뒤에 일어났어도 타격 전에 취소한다.
        if ((attacking || externalActionLocked) && playerMovement != null && !playerMovement.CanAttack) CancelAttack();
        if (attacking && !externalActionLocked) UpdateAttack();
        UpdateIdleReturn();
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
        if (!CanStartAttack) return;
        if (attacking || AttackCount == 0) return;

        heavyAttackActive = false;
        specialAttackActive = false;
        currentComboIndex = Mathf.Clamp(comboIndex, 0, AttackCount - 1);
        BeginStep(GetCurrentStep());
    }

    /// <summary>Shift + 좌클릭 강공격. 콤보에 참여하지 않는 단독 공격이다.</summary>
    public void BeginHeavyAttack()
    {
        if (!CanStartAttack) return;
        if (weaponStats == null || attacking || !weaponStats.HasHeavyAttack) return;

        // 에너지 컴포넌트가 아예 없는 캐릭터라면 자원 제약 없이 사용한다.
        int energyCost = weaponStats.HeavyAttackEnergyCost;
        if (energyCost > 0 && playerEnergy != null &&
            !playerEnergy.TryConsume(energyCost))
        {
            return;
        }

        heavyAttackActive = true;
        specialAttackActive = false;
        BeginStep(weaponStats.HeavyAttack);
    }

    public void BeginSpecialAttack()
    {
        if (!CanStartAttack) return;
        if (weaponStats == null || !HasEquippedSpecialAttack || attacking) return;
        // Validate the receiver before charging MP. Missing MP never means a free skill.
        if (swingAnimator == null || !swingAnimator.isActiveAndEnabled ||
            !swingAnimator.HasState(0, Animator.StringToHash(weaponStats.SpecialAttackStateName))) return;
        if (playerEnergy == null || !playerEnergy.TryConsume(equippedSpecialAttack.MpCost)) return;
        heavyAttackActive = false;
        specialAttackActive = true;
        bufferedAttackUntil = float.NegativeInfinity;
        BeginStep(equippedSpecialAttack.SpecialAttack);
    }

    private void BeginStep(WeaponAttackStep step)
    {
        if (step == null)
        {
            heavyAttackActive = false;
            return;
        }

        attacking = true;
        attackMovementStarted = false;
        returningToIdle = false;
        animatorStarted = false;
        queuedNextAttack = false;
        stepElapsed = 0f;
        motionDuration = step.duration;
        timingScale = 1f;
        stepSpeed = GetAttackSpeedMultiplier();
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

        stepElapsed += Time.deltaTime * stepSpeed;
        float motionElapsed = stepElapsed - step.startDelay;

        if (!animatorStarted && motionElapsed >= 0f)
        {
            StartAttackAnimation(step);
        }

        if (animatorStarted && swingAnimator != null)
        {
            // Animator progress is the common clock for motion, hits and recovery.
            motionElapsed = swingAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime * motionDuration;
            stepElapsed = step.startDelay + motionElapsed;
        }

        float profileTime = motionElapsed <= motionDuration
            ? motionElapsed / timingScale
            : step.duration + motionElapsed - motionDuration;
        TryConsumeBufferedComboInput(step, profileTime);
        if (motionElapsed >= 0f && motionElapsed <= motionDuration)
        {
            QueryActiveHitWindows(step, profileTime);
        }

        if (stepElapsed >= step.startDelay + motionDuration + step.recoveryDuration)
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
                (!step.fireProjectile && motionElapsed > window.endTime)) continue;

            if (step.fireProjectile) FireProjectileWindow(step, window, index);
            else QueryHitWindow(window, index);
        }
    }

    private void FireProjectileWindow(WeaponAttackStep step, WeaponHitWindow window, int windowIndex)
    {
        if (weaponStats.ProjectilePrefab == null || combatStats == null || !firedProjectileWindows.Add(windowIndex)) return;
        Vector2 direction = aimController.AimDirection.normalized;
        Vector2 side = new Vector2(-direction.y, direction.x);
        Vector2 offset = weaponStats.ProjectileSpawnOffset;
        Vector3 origin = attackOrigin.position + (Vector3)(direction * offset.x + side * offset.y);
        int count = Mathf.Clamp(step.projectilesPerShot, 1, 12);
        for (int i = 0; i < count; i++)
        {
            float spread = count > 1 ? Mathf.Lerp(-step.projectileSpreadAngle * 0.5f, step.projectileSpreadAngle * 0.5f, (float)i / (count - 1)) : 0f;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, spread) * direction;
            float angle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
            Projectile projectile = Instantiate(weaponStats.ProjectilePrefab, origin, Quaternion.Euler(0f, 0f, angle));
            projectile.Initialize(shotDirection, weaponStats.ProjectileSpeed,
                combatStats.CreateWeaponDamageData(weaponStats.BaseDamage, weaponStats.DamageMultiplier * window.damageMultiplier)
                    .WithStaggerImpact(window.staggerImpact));
        }
    }

    private void QueryHitWindow(WeaponHitWindow window, int windowIndex)
    {
        WeaponHitboxGroup group = FindHitboxGroup(window.hitboxGroupId);
        if (group == null || group.colliders == null) return;

        HashSet<EnemyHealth> hitEnemies = hitEnemiesByWindow[windowIndex];
        foreach (Collider2D hitbox in group.colliders)
        {
            if (hitbox == null || !hitbox.gameObject.activeInHierarchy) continue;
            hitbox.enabled = true;
            overlapResults.Clear();
            hitbox.Overlap(enemyFilter, overlapResults);
            hitbox.enabled = false;

            foreach (Collider2D targetCollider in overlapResults)
            {
                EnemyHealth health = targetCollider.GetComponentInParent<EnemyHealth>();
                if (health == null || health.GetCurrentHealth() <= 0 || !hitEnemies.Add(health)) continue;
                ApplyHit(health, window);
            }
        }
    }

    private void ApplyHit(EnemyHealth health, WeaponHitWindow window)
    {
        if (combatStats != null)
        {
            Vector3 hitPosition = health.transform.position;
            health.TakeDamage(combatStats.CreateWeaponDamageData(
                weaponStats.BaseDamage,
                weaponStats.DamageMultiplier * window.damageMultiplier).WithStaggerImpact(window.staggerImpact));
            if (weaponStats.HitImpactPrefab != null)
            {
                GameObject impact = PrefabPool.Spawn(weaponStats.HitImpactPrefab.gameObject,
                    hitPosition, Quaternion.identity);
                if (impact != null) impact.GetComponent<ProjectileImpactVisual>().Play();
            }
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
        playerMovement?.StopAttackMovement();
        attacking = false;
        animatorStarted = false;
        SetAllHitboxesEnabled(false);
        aimController.EndAttack();
        lastAttackCompletedTime = Time.time;

        if (heavyAttackActive || specialAttackActive)
        {
            // 강공격은 콤보로 이어지지 않는다. 다음 기본 공격은 1타부터 시작한다.
            heavyAttackActive = false;
            specialAttackActive = false;
            queuedNextAttack = false;
            currentComboIndex = -1;
            bufferedAttackUntil = float.NegativeInfinity;
            PlayIdle();
            return;
        }

        // A held button must still be down when this attack ends. Do not buffer
        // the initial click's held frames as a second attack after release.
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        bool repeatHeldAttack = weaponStats.RepeatWhileHeld && mouse != null &&
            mouse.leftButton.isPressed && !(keyboard != null && keyboard.shiftKey.isPressed);
        if (queuedNextAttack || repeatHeldAttack)
        {
            queuedNextAttack = false;
            BeginAttack(GetNextComboIndex());
            // 콤보가 실제로 이어졌으면 Idle을 거치지 않는다.
            if (attacking) return;
        }

        bufferedAttackUntil = float.NegativeInfinity;
        PlayIdle();
    }

    // Animation Event on the weapon Animator: place this at the desired movement frame.
    public void StartAttackMovement()
    {
        if (!CanStartAttack || !attacking || !animatorStarted || attackMovementStarted ||
            playerMovement == null) return;

        WeaponAttackStep step = GetCurrentStep();
        if (step == null) return;

        attackMovementStarted = true;
        playerMovement.BeginAttackMovement(attackMovementDirection, step.movementDistance,
            step.movementDuration / stepSpeed);
    }

    private void StartAttackAnimation(WeaponAttackStep step)
    {
        animatorStarted = true;
        attackMovementDirection = aimController.AimDirection;
        if (swingAnimator == null) return;

        swingAnimator.speed = 1f;
        swingAnimator.Play(specialAttackActive ? weaponStats.SpecialAttackStateName : step.animatorStateName, 0, 0f);
        swingAnimator.Update(0f);
        AnimatorClipInfo[] clips = swingAnimator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length > 0 && clips[0].clip != null)
        {
            motionDuration = Mathf.Max(0.001f, clips[0].clip.length);
            timingScale = motionDuration / Mathf.Max(0.05f, step.duration);
        }
        swingAnimator.speed = stepSpeed;
        Transform vfxParent = swingVfxRoot != null
            ? swingVfxRoot
            : swingAnimator.transform.parent != null
                ? swingAnimator.transform.parent
                : swingAnimator.transform;
        TrackSwingVfx(WeaponSwingVFX.SpawnAttached(
            specialAttackActive && equippedSpecialAttack != null && equippedSpecialAttack.SpecialAttackVfx != null
                ? equippedSpecialAttack.SpecialAttackVfx : weaponStats.SwingVfxPrefab,
            vfxParent,
            swingVfxMotionPoint,
            step.swingVfx,
            GetAttackSpeedMultiplier()));
    }

    private void TrackSwingVfx(WeaponSwingVfxHandle handle)
    {
        // 이미 자연 종료된 궤적은 목록에서 걷어낸다.
        for (int index = activeSwingVfxHandles.Count - 1; index >= 0; index--)
        {
            if (!activeSwingVfxHandles[index].IsActive) activeSwingVfxHandles.RemoveAt(index);
        }
        if (handle.IsActive) activeSwingVfxHandles.Add(handle);
    }

    private void StopAllSwingVfx()
    {
        for (int index = 0; index < activeSwingVfxHandles.Count; index++)
        {
            activeSwingVfxHandles[index].Stop();
        }
        activeSwingVfxHandles.Clear();
    }

    private void PlayIdle(bool immediate = false)
    {
        returningToIdle = false;
        if (swingAnimator == null || weaponStats == null ||
            string.IsNullOrWhiteSpace(weaponStats.IdleStateName)) return;
        // 오브젝트가 꺼지는 중이거나 컨트롤러가 이미 해제됐으면
        // Animator 호출이 무시되고 경고만 남는다.
        // (플레이 모드 종료, 무기 해제 시 OnDisable -> CancelAttack 경로)
        // isInitialized까지 봐야 플레이 모드 종료 순서에서 나는 경고가 사라진다.
        if (!swingAnimator.gameObject.activeInHierarchy ||
            !swingAnimator.isActiveAndEnabled ||
            !swingAnimator.isInitialized ||
            swingAnimator.runtimeAnimatorController == null) return;

        swingAnimator.speed = 1f;
        int idleStateHash = Animator.StringToHash(weaponStats.IdleStateName);
        float blendDuration = weaponStats.IdleReturnBlendDuration;
        if (immediate || blendDuration <= 0f)
        {
            swingAnimator.Play(idleStateHash, 0, 0f);
            return;
        }

        if (returnTransforms == null) return;
        for (int i = 0; i < returnTransforms.Length; i++)
        {
            Transform node = returnTransforms[i];
            if (node == null) continue;
            returnPositions[i] = node.localPosition;
            returnRotations[i] = node.localRotation;
            returnScales[i] = node.localScale;
        }
        swingAnimator.Play(idleStateHash, 0, 0f);
        swingAnimator.Update(0f);
        returnElapsed = 0f;
        returningToIdle = true;
        BlendIdlePose(0f);
    }

    private void UpdateIdleReturn()
    {
        if (!returningToIdle || attacking || externalActionLocked) return;
        returnElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(returnElapsed / Mathf.Max(.001f, weaponStats.IdleReturnBlendDuration));
        AnimationCurve curve = weaponStats.IdleReturnCurve;
        float weight = t >= 1f ? 1f : curve == null || curve.length == 0 ? t : Mathf.Clamp01(curve.Evaluate(t));
        BlendIdlePose(weight);
        if (t >= 1f) returningToIdle = false;
    }

    private void BlendIdlePose(float weight)
    {
        for (int i = 0; i < returnTransforms.Length; i++)
        {
            Transform node = returnTransforms[i];
            if (node == null) continue;
            node.localPosition = Vector3.LerpUnclamped(returnPositions[i], node.localPosition, weight);
            node.localRotation = Quaternion.SlerpUnclamped(returnRotations[i], node.localRotation, weight);
            node.localScale = Vector3.LerpUnclamped(returnScales[i], node.localScale, weight);
        }
    }

    public bool TryBeginExternalAction(bool interruptBasicAttack)
    {
        if (!CanStartAttack || weaponStats == null) return false;
        if (attacking)
        {
            if (!interruptBasicAttack) return false;
            CancelAttack();
        }

        externalActionLocked = true;
        returningToIdle = false;
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
        firedProjectileWindows.Clear();
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

    private WeaponAttackStep GetCurrentStep()
    {
        if (weaponStats == null) return null;
        if (specialAttackActive) return equippedSpecialAttack != null ? equippedSpecialAttack.SpecialAttack : null;
        return heavyAttackActive
            ? weaponStats.HeavyAttack
            : weaponStats.GetBasicAttackStep(currentComboIndex);
    }
    private int GetNextComboIndex() =>
        AttackCount > 0 ? (currentComboIndex + 1) % AttackCount : 0;
    private float GetAttackSpeedMultiplier() =>
        weaponStats != null ? weaponStats.AttackSpeedMultiplier : 1f;

    public void CancelForStageTransition() { CancelAttack(true); }

    private void CancelAttack(bool returnToIdleImmediately = false)
    {
        GetComponent<WeaponSkillController>()?.CancelSkill();
        externalActionLocked = false;
        playerMovement?.StopAttackMovement();
        specialAttackActive = false;
        attacking = false;
        animatorStarted = false;
        queuedNextAttack = false;
        heavyAttackActive = false;
        bufferedAttackUntil = float.NegativeInfinity;
        SetAllHitboxesEnabled(false);
        StopAllSwingVfx();
        aimController?.CancelAttack();
        PlayIdle(returnToIdleImmediately);
    }

    private void OnDisable()
    {
        CancelAttack(true);
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
