using System.Collections;
using UnityEngine;

public enum EnemyProjectileAimMode
{
    CurrentTarget,
    SnapshotAtPatternStart,
    FixedWorldDirection,
    RandomDirection
}

[AddComponentMenu("Enemies/Patterns/Enemy Projectile Attack Pattern")]
public class EnemyProjectileAttackPattern : EnemyAttackPattern
{
    [System.Serializable]
    public struct PhaseVolley
    {
        [Min(1)] public int minimumPhase;
        [Min(1)] public int projectileCount;
        [Range(0f, 360f)] public float spread;
        [Min(1)] public int shots;
        public float angleStep;
    }
    [Header("페이즈별 사격 구성")]
    [Tooltip("현재 페이즈 이하에서 가장 높은 Minimum Phase를 선택합니다. 비우면 기본 사격을 유지합니다.")]
    [SerializeField] private PhaseVolley[] phaseVolleys = System.Array.Empty<PhaseVolley>();
    private EnemyPhaseController phaseController;

    public PhaseVolley ResolveVolley(int phase)
    {
        var selected = new PhaseVolley { minimumPhase = 1, projectileCount = projectilesPerShot,
            spread = spreadAngle, shots = shotCount };
        if (phaseVolleys != null)
            foreach (PhaseVolley entry in phaseVolleys)
                if (entry.minimumPhase <= phase && entry.minimumPhase >= selected.minimumPhase) selected = entry;
        selected.projectileCount = Mathf.Max(1, selected.projectileCount);
        selected.shots = Mathf.Max(1, selected.shots);
        selected.spread = Mathf.Clamp(selected.spread, 0f, 360f);
        return selected;
    }
    [Header("발사 주기")]
    [SerializeField, Min(0.05f)] private float attackInterval = 2f;
    [SerializeField, Min(0f)] private float firstAttackDelay = 1f;

    [Header("투사체")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0f)] private float projectileSpeed = 5f;
    [SerializeField, Min(1)] private int projectileDamage = 1;
    [SerializeField, Min(0.01f)] private float projectileLifeTime = 6f;
    [SerializeField] private bool alignProjectileToDirection;

    [Header("조준")]
    [Tooltip("Current Target도 발사 직전 재조준하지 않고 해당 발사의 예고 시작 시 방향을 고정합니다.")]
    [SerializeField] private EnemyProjectileAimMode aimMode =
        EnemyProjectileAimMode.CurrentTarget;
    [SerializeField] private Vector2 fixedWorldDirection = Vector2.down;

    [Header("공격 시퀀스")]
    [Tooltip("매 발사 전 예고 시간. 연사의 실제 발사 간격은 이 시간과 Shot Interval의 합입니다.")]
    [SerializeField, Min(0f)] private float windupDuration;
    [Tooltip("매 발사 전 방향을 고정하고 준비 시간 동안 표시합니다. 실제 피격 폭이 아닌 방향 안내입니다.")]
    [SerializeField] private EnemyAttackTelegraph telegraphPrefab;
    private EnemyAttackTelegraph telegraph;
    [Tooltip("한 패턴 실행에서 반복하는 발사 횟수입니다.")]
    [SerializeField, Min(1)] private int shotCount = 1;
    [SerializeField, Min(0f)] private float shotInterval;
    [SerializeField, Min(0f)] private float recoveryDuration;

    [Header("발사당 투사체")]
    [Tooltip("한 번 발사할 때 동시에 생성하는 투사체 수입니다.")]
    [SerializeField, Min(1)] private int projectilesPerShot = 1;
    [Tooltip("첫 투사체부터 마지막 투사체까지 펼쳐지는 전체 각도입니다.")]
    [SerializeField, Range(0f, 360f)] private float spreadAngle;
    [SerializeField] private float angleOffset;

    [Header("선택적 이동 상태 연동")]
    [Tooltip("IEnemyAttackGate가 허용하는 상태에서만 이 패턴을 사용합니다.")]
    [SerializeField] private bool requireAttackGate;

    private IEnemyAttackGate attackGate;
    private EnemyHealth owner;
    private bool OwnerCancelled => owner != null && owner.AttackContext.IsCancelled;

    public GameObject ProjectilePrefab => projectilePrefab;
    public bool RequiresAttackGate => requireAttackGate;
    public bool HasAttackGate => attackGate != null;
    protected override float Cooldown => attackInterval;
    protected override float InitialDelay => firstAttackDelay;

    public string DescribeAttack()
    {
        PhaseVolley first = ResolveVolley(1);
        string volley = first.projectileCount > 1 ? first.projectileCount + "방향 산탄" : "단발 사격";
        if (first.shots > 1) volley += " · " + first.shots + "연사";
        return volley;
    }

    protected override void Awake()
    {
        base.Awake();
        owner = GetComponent<EnemyHealth>();
        phaseController = GetComponent<EnemyPhaseController>();
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour is IEnemyAttackGate gate)
            {
                attackGate = gate;
                break;
            }
        }
    }

    protected override bool CanExecutePattern(Transform target)
    {
        return projectilePrefab != null && !OwnerCancelled &&
            (!requireAttackGate || (attackGate != null && attackGate.CanUseEnemyAttacks));
    }

    protected override IEnumerator ExecutePattern(Transform target)
    {
        Vector2 snapshotDirection = DirectionToTarget(target);
        PhaseVolley volley = ResolveVolley(phaseController != null ? phaseController.CurrentPhase : 1);

        int validShotCount = volley.shots;
        for (int shotIndex = 0; shotIndex < validShotCount; shotIndex++)
        {
            if (target == null || !isActiveAndEnabled || OwnerCancelled) { CancelPresentation(); yield break; }
            Vector2 direction = ResolveAimDirection(target, snapshotDirection);
            float shotAngle = angleOffset + volley.angleStep * shotIndex;
            float elapsed = 0f;
            if (telegraph == null && telegraphPrefab != null)
                telegraph = Instantiate(telegraphPrefab, transform);
            while (elapsed < windupDuration)
            {
                if (target == null || !isActiveAndEnabled || OwnerCancelled) { CancelPresentation(); yield break; }
                if (telegraph != null)
                    telegraph.Show(transform.position, direction, volley.projectileCount, volley.spread,
                        shotAngle, projectileSpeed * projectileLifeTime, elapsed / windupDuration);
                yield return null;
                elapsed += Time.deltaTime;
            }
            CancelPresentation();
            if (target == null || !isActiveAndEnabled || OwnerCancelled) yield break;
            FireVolley(direction, volley, shotAngle);

            if (shotIndex < validShotCount - 1 && shotInterval > 0f)
            {
                yield return new WaitForSeconds(shotInterval);
            }
        }

        if (recoveryDuration > 0f) yield return new WaitForSeconds(recoveryDuration);
    }

    private Vector2 ResolveAimDirection(Transform target, Vector2 snapshotDirection)
    {
        switch (aimMode)
        {
            case EnemyProjectileAimMode.SnapshotAtPatternStart:
                return snapshotDirection;
            case EnemyProjectileAimMode.FixedWorldDirection:
                return NormalizeOrFallback(fixedWorldDirection);
            case EnemyProjectileAimMode.RandomDirection:
                return NormalizeOrFallback(Random.insideUnitCircle);
            default:
                return DirectionToTarget(target);
        }
    }

    private void FireVolley(Vector2 baseDirection, PhaseVolley volley, float shotAngle)
    {
        int count = volley.projectileCount;
        for (int index = 0; index < count; index++)
        {
            Vector2 direction = EnemyAttackGeometry.VolleyDirection(
                baseDirection, index, count, volley.spread, shotAngle);
            EnemyProjectile projectile = EnemyProjectile.Spawn(
                projectilePrefab,
                transform.position,
                direction,
                projectileSpeed,
                projectileDamage,
                projectileLifeTime,
                alignProjectileToDirection,
                owner != null ? owner.AttackContext : null);
            if (projectile == null)
            {
                Debug.LogError(
                    $"{projectilePrefab.name}에 EnemyProjectile 스크립트가 없습니다.",
                    this);
                return;
            }
        }
    }

    private Vector2 DirectionToTarget(Transform target)
    {
        if (target == null) return Vector2.down;
        return NormalizeOrFallback((Vector2)target.position - (Vector2)transform.position);
    }

    private static Vector2 NormalizeOrFallback(Vector2 direction)
    {
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
    }

    public override void CancelPresentation() { if (telegraph != null) telegraph.Hide(); }
    private void OnDisable() => CancelPresentation();

    private void OnValidate()
    {
        attackInterval = Mathf.Max(0.05f, attackInterval);
        firstAttackDelay = Mathf.Max(0f, firstAttackDelay);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileDamage = Mathf.Max(1, projectileDamage);
        projectileLifeTime = Mathf.Max(0.01f, projectileLifeTime);
        windupDuration = Mathf.Max(0f, windupDuration);
        shotCount = Mathf.Max(1, shotCount);
        shotInterval = Mathf.Max(0f, shotInterval);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        projectilesPerShot = Mathf.Max(1, projectilesPerShot);
        spreadAngle = Mathf.Clamp(spreadAngle, 0f, 360f);
    }
}
