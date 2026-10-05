using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStagger : MonoBehaviour, IEnemyPoolLifecycle
{
    [Header("붕괴 연출")]
    [Tooltip("무너지는 자세는 이 애니메이션 클립에서 편집합니다. 루트 물리/판정은 움직이지 않습니다.")]
    [SerializeField, InspectorName("붕괴 모션")] private AnimationClip collapseClip;
    [Tooltip("일반 적의 붕괴를 발동시킨 타격에만 생성하는 피격 프리팹입니다.")]
    [SerializeField, InspectorName("일반 붕괴 피격 효과")] private RadialImpactVisual staggerImpactPrefab;
    [Tooltip("엘리트와 보스의 붕괴 피격 프리팹입니다. 비워 두면 일반 효과를 사용합니다.")]
    [SerializeField, InspectorName("엘리트 / 보스 붕괴 피격 효과")] private RadialImpactVisual majorStaggerImpactPrefab;
    [SerializeField, InspectorName("붕괴 피격 효과 위치 보정")] private Vector2 staggerImpactOffset;
    private readonly StaggerMeter meter = new StaggerMeter();
    private EnemyHealth health;
    private EnemyHitEffect hitEffect;
    private EnemyPatternController patterns;
    private EnemyKnockback knockback;
    private Rigidbody2D body;
    private Transform poseRoot;
    private float heldDuration;

    public bool IsStaggered => isActiveAndEnabled && meter.IsStaggered;
    public float Ratio => meter.Ratio;
    public float Current => meter.Current;
    public float Maximum => meter.Maximum;
    public float RemainingDuration => meter.RemainingDuration;
    public event Action OnChanged;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        hitEffect = GetComponent<EnemyHitEffect>();
        patterns = GetComponent<EnemyPatternController>();
        knockback = GetComponent<EnemyKnockback>();
        body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        health.OnDied += HandleDeath;
        Configure();
    }

    private void Configure()
    {
        EnemyProfile profile = health.Profile;
        if (profile == null) return;
        float rankMultiplier = health.Rank == EnemyRank.Elite && profile.Rank == EnemyRank.Normal
            ? profile.EliteStaggerMultiplier : 1f;
        meter.Configure(profile.StaggerThreshold * rankMultiplier, profile.StaggerDuration,
            profile.StaggerDecayDelay, profile.StaggerDecayPerSecond, profile.StaggerRecoveryImmunity);
        RestorePose();
        OnChanged?.Invoke();
    }

    public void ApplyImpact(float impact)
    {
        if (!isActiveAndEnabled || health.GetCurrentHealth() <= 0 || impact <= 0f) return;
        bool began = meter.AddImpact(impact);
        if (began)
        {
            heldDuration = meter.RemainingDuration;
            patterns?.InterruptForStagger();
            foreach (EnemyMovementPatternBase movement in GetComponents<EnemyMovementPatternBase>())
                movement.InterruptForStagger();
            knockback?.ClearKnockback();
            if (body != null) body.linearVelocity = Vector2.zero;
            StaggerImpactFeedback.Play(health.Rank);
            RadialImpactVisual impactPrefab = health.Rank != EnemyRank.Normal && majorStaggerImpactPrefab != null
                ? majorStaggerImpactPrefab : staggerImpactPrefab;
            if (impactPrefab != null)
            {
                Vector3 center = transform.position + (Vector3)staggerImpactOffset;
                // Keep the burst independent of enemy collapse, death and pool reuse.
                RadialImpactVisual visual = Instantiate(impactPrefab, center, Quaternion.identity);
                visual.Play(center, 0f, null);
            }
            ApplyPose();
        }
        OnChanged?.Invoke();
    }

    private void Update()
    {
        bool wasStaggered = meter.IsStaggered;
        float previous = meter.Current;
        meter.Tick(Time.deltaTime);
        if (wasStaggered && !meter.IsStaggered)
        {
            RestorePose();
            knockback?.ClearKnockback();
        }
        if (previous != meter.Current || wasStaggered != meter.IsStaggered) OnChanged?.Invoke();
    }

    private void LateUpdate()
    {
        if (meter.IsStaggered) ApplyPose();
    }

    private void ApplyPose()
    {
        if (poseRoot == null && hitEffect != null) poseRoot = hitEffect.StaggerPoseRoot;
        if (poseRoot == null || collapseClip == null) return;
        // The last pose remains held until the gameplay timer expires.
        float elapsed = heldDuration - meter.RemainingDuration;
        collapseClip.SampleAnimation(poseRoot.gameObject, Mathf.Min(elapsed, collapseClip.length));
    }

    private void RestorePose()
    {
        if (poseRoot == null) return;
        poseRoot.localPosition = Vector3.zero;
        poseRoot.localRotation = Quaternion.identity;
        poseRoot.localScale = Vector3.one;
    }

    private void HandleDeath()
    {
        meter.Reset();
        RestorePose();
        OnChanged?.Invoke();
    }

    private void OnDisable()
    {
        health.OnDied -= HandleDeath;
        HandleDeath();
    }

    public void OnEnemySpawned() => Configure();
    public void OnEnemyDespawned() => HandleDeath();
}
