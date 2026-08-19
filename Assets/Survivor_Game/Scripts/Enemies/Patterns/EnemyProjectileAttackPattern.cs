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
    [SerializeField] private EnemyProjectileAimMode aimMode =
        EnemyProjectileAimMode.CurrentTarget;
    [SerializeField] private Vector2 fixedWorldDirection = Vector2.down;

    [Header("공격 시퀀스")]
    [SerializeField, Min(0f)] private float windupDuration;
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

    public GameObject ProjectilePrefab => projectilePrefab;
    public bool RequiresAttackGate => requireAttackGate;
    public bool HasAttackGate => attackGate != null;
    protected override float Cooldown => attackInterval;
    protected override float InitialDelay => firstAttackDelay;

    protected override void Awake()
    {
        base.Awake();
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
        return projectilePrefab != null &&
            (!requireAttackGate || (attackGate != null && attackGate.CanUseEnemyAttacks));
    }

    protected override IEnumerator ExecutePattern(Transform target)
    {
        Vector2 snapshotDirection = DirectionToTarget(target);
        if (windupDuration > 0f) yield return new WaitForSeconds(windupDuration);

        int validShotCount = Mathf.Max(1, shotCount);
        for (int shotIndex = 0; shotIndex < validShotCount; shotIndex++)
        {
            if (target == null) yield break;
            Vector2 direction = ResolveAimDirection(target, snapshotDirection);
            FireVolley(direction);

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

    private void FireVolley(Vector2 baseDirection)
    {
        int count = Mathf.Max(1, projectilesPerShot);
        for (int index = 0; index < count; index++)
        {
            float spreadOffset = count == 1
                ? 0f
                : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f,
                    (float)index / (count - 1));
            Vector2 direction = Rotate(baseDirection, angleOffset + spreadOffset);
            EnemyProjectile projectile = EnemyProjectile.Spawn(
                projectilePrefab,
                transform.position,
                direction,
                projectileSpeed,
                projectileDamage,
                projectileLifeTime,
                alignProjectileToDirection);
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

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);
        return new Vector2(
            direction.x * cosine - direction.y * sine,
            direction.x * sine + direction.y * cosine).normalized;
    }

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
