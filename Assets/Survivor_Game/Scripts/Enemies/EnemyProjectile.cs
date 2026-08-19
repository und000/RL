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
        bool alignToDirection = false)
    {
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
        projectile.Initialize(direction, speed, damage, lifetime);
        return projectile;
    }

    public void Initialize(Vector2 direction, float speed, int newDamage, float lifetime)
    {
        Vector2 normalizedDirection = direction.normalized;
        damage = Mathf.Max(1, newDamage);
        expiresAt = Time.time + Mathf.Max(0.01f, lifetime);
        initialized = true;
        finishing = false;
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
        playerHealth.TakeDamage(damage);
        Finish(EnemyProjectileFinishReason.Hit, true);
    }

    public void Cancel()
    {
        if (initialized && !finishing) Finish(EnemyProjectileFinishReason.Cancelled, false);
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
        initialized = false;
        finishing = false;
        expiresAt = 0f;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    public void OnPrefabDespawned()
    {
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
