using UnityEngine;

[AddComponentMenu("Enemies/Projectile Modules/Spawn On Finish")]
public class EnemyProjectileSpawnOnFinish : MonoBehaviour, IEnemyProjectileLifecycle
{
    [Header("발동 조건")]
    [SerializeField] private bool spawnOnHit;
    [SerializeField] private bool spawnOnExpired = true;
    [SerializeField] private bool spawnOnCancelled;
    [SerializeField, Min(0f)] private float spawnDelay;

    [Header("자식 투사체")]
    [SerializeField] private GameObject childProjectilePrefab;
    [SerializeField, Min(1)] private int projectileCount = 2;
    [SerializeField, Range(0f, 360f)] private float spreadAngle = 90f;
    [SerializeField] private float angleOffset;
    [SerializeField] private bool inheritParentDirection = true;
    [SerializeField] private Vector2 fixedDirection = Vector2.down;
    [SerializeField] private bool alignProjectileToDirection;

    [Header("자식 투사체 수치")]
    [SerializeField, Min(0f)] private float projectileSpeed = 5f;
    [SerializeField, Min(1)] private int projectileDamage = 1;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 3f;

    private Vector2 launchDirection = Vector2.down;

    public GameObject ChildProjectilePrefab => childProjectilePrefab;

    public void OnProjectileLaunched(EnemyProjectile projectile, Vector2 direction)
    {
        launchDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized : Vector2.down;
    }

    public void OnProjectileFinished(
        EnemyProjectile projectile,
        EnemyProjectileFinishReason reason)
    {
        if (childProjectilePrefab == null || projectile.SuppressChildEmission ||
            (projectile.AttackContext != null && projectile.AttackContext.IsCancelled) || !ShouldSpawn(reason)) return;
        Vector2 direction = inheritParentDirection
            ? launchDirection : NormalizeOrFallback(fixedDirection);
        EnemyProjectileEmissionScheduler.Schedule(
            spawnDelay,
            childProjectilePrefab,
            transform.position,
            direction,
            projectileCount,
            spreadAngle,
            angleOffset,
            projectileSpeed,
            projectileDamage,
            projectileLifetime,
            alignProjectileToDirection,
            projectile.AttackContext);
    }

    private bool ShouldSpawn(EnemyProjectileFinishReason reason)
    {
        switch (reason)
        {
            case EnemyProjectileFinishReason.Hit:
                return spawnOnHit;
            case EnemyProjectileFinishReason.Expired:
                return spawnOnExpired;
            default:
                return spawnOnCancelled;
        }
    }

    private static Vector2 NormalizeOrFallback(Vector2 direction)
    {
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
    }

    private void OnValidate()
    {
        spawnDelay = Mathf.Max(0f, spawnDelay);
        projectileCount = Mathf.Max(1, projectileCount);
        spreadAngle = Mathf.Clamp(spreadAngle, 0f, 360f);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileDamage = Mathf.Max(1, projectileDamage);
        projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
    }
}
