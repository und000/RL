using UnityEngine;

public enum EnemyProjectileFinishReason
{
    Hit,
    Expired,
    Cancelled
}

public interface IEnemyProjectileLifecycle
{
    void OnProjectileLaunched(EnemyProjectile projectile, Vector2 direction);
    void OnProjectileFinished(EnemyProjectile projectile, EnemyProjectileFinishReason reason);
}

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[DisallowMultipleComponent]
[AddComponentMenu("Enemies/Enemy Projectile")]
public class EnemyProjectile : MonoBehaviour, IPrefabPoolLifecycle
{
    [SerializeField] private ProjectileImpactVisual impactPrefab;

    private Rigidbody2D body;
    private IEnemyProjectileLifecycle[] modules;
    private float expiresAt;
    private int damage;
    private bool initialized;
    private bool finishing;
    public EnemyAttackContext AttackContext { get; private set; }
    public bool SuppressChildEmission { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        CacheModules();
    }

    public static EnemyProjectile Spawn(
        GameObject prefab,
        Vector3 position,
        Vector2 direction,
        float speed,
        int damage,
        float lifetime,
        bool alignToDirection = false,
        EnemyAttackContext attackContext = null)
    {
        if (attackContext != null && attackContext.IsCancelled) return null;
        GameObject instance = PrefabPool.Spawn(prefab, position, Quaternion.identity);
        if (instance == null || !instance.TryGetComponent(out EnemyProjectile projectile))
        {
            if (instance != null) PrefabPool.Release(instance);
            return null;
        }
        if (alignToDirection && direction.sqrMagnitude > 0.0001f)
        {
            projectile.transform.right = direction.normalized;
        }
        projectile.Initialize(direction, speed, damage, lifetime, attackContext);
        return projectile;
    }

    public void Initialize(Vector2 direction, float speed, int newDamage, float lifetime,
        EnemyAttackContext attackContext = null)
    {
        DetachContext();
        AttackContext = attackContext;
        SuppressChildEmission = false;
        if (AttackContext != null) AttackContext.OnCancelled += CancelOwnedAttack;
        Vector2 normalizedDirection = direction.normalized;
        damage = Mathf.Max(1, newDamage);
        expiresAt = Time.time + Mathf.Max(0.01f, lifetime);
        initialized = true;
        finishing = false;
        if (AttackContext != null && AttackContext.IsCancelled) { CancelOwnedAttack(); return; }
        body.linearVelocity = normalizedDirection * Mathf.Max(0f, speed);
        foreach (IEnemyProjectileLifecycle module in modules)
        {
            module.OnProjectileLaunched(this, normalizedDirection);
        }
    }

    private void Update()
    {
        if (initialized && !finishing && Time.time >= expiresAt)
        {
            Finish(EnemyProjectileFinishReason.Expired, false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized || finishing || !other.TryGetComponent(out PlayerHealth playerHealth))
        {
            return;
        }
        playerHealth.TakeDamage(damage, AttackContext != null ? AttackContext.SourceName : "알 수 없는 적",
            PlayerDamageKind.Projectile);
        // 사망 콜백이 런의 탄을 회수했으면 중복 반환/자식 발사를 하지 않는다.
        if (initialized && !finishing) Finish(EnemyProjectileFinishReason.Hit, true);
    }

    public void Cancel(bool suppressChildren = false)
    {
        SuppressChildEmission |= suppressChildren;
        if (initialized && !finishing) Finish(EnemyProjectileFinishReason.Cancelled, false);
    }

    private void CancelOwnedAttack() => Cancel(true);

    private void DetachContext()
    {
        if (AttackContext != null) AttackContext.OnCancelled -= CancelOwnedAttack;
        AttackContext = null;
    }

    private void OnDisable()
    {
        initialized = false;
        DetachContext();
    }

    private void Finish(EnemyProjectileFinishReason reason, bool playImpact)
    {
        finishing = true;
        if (body != null) body.linearVelocity = Vector2.zero;
        foreach (IEnemyProjectileLifecycle module in modules)
        {
            module.OnProjectileFinished(this, reason);
        }
        if (playImpact && impactPrefab != null)
        {
            GameObject impactObject = PrefabPool.Spawn(
                impactPrefab.gameObject, transform.position, Quaternion.identity);
            if (impactObject != null &&
                impactObject.TryGetComponent(out ProjectileImpactVisual impact))
            {
                impact.Play();
            }
        }
        PrefabPool.Release(gameObject);
    }

    public void OnPrefabSpawned()
    {
        DetachContext();
        SuppressChildEmission = false;
        initialized = false;
        finishing = false;
        expiresAt = 0f;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    public void OnPrefabDespawned()
    {
        DetachContext();
        initialized = false;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void CacheModules()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        System.Collections.Generic.List<IEnemyProjectileLifecycle> results =
            new System.Collections.Generic.List<IEnemyProjectileLifecycle>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IEnemyProjectileLifecycle module) results.Add(module);
        }
        modules = results.ToArray();
    }
}
