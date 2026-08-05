using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField]
    [Min(0.1f)]
    private float lifeTime = 3f;

    [SerializeField]
    [Min(0.1f)]
    private float speedMultiplier = 1.0f;

    [Header("넉백")]
    [SerializeField]
    [Min(0f)]
    private float knockbackStrength = 3f;

    [SerializeField]
    [Min(0f)]
    private float knockbackDuration = 0.15f;

    [SerializeField]
    private KnockbackDirectionMode knockbackDirectionMode =
        KnockbackDirectionMode.ProjectileDirection;

    [SerializeField]
    private Vector2 customKnockbackDirection = Vector2.up;

    private Rigidbody2D body;
    private DamageData damageData;
    private Vector2 travelDirection;
    private Vector2 attackOrigin;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Initialize(
        Vector2 direction,
        float weaponSpeed,
        DamageData newDamageData)
    {
        damageData = newDamageData;
        travelDirection = direction.normalized;
        attackOrigin = transform.position;
        float finalSpeed = weaponSpeed * speedMultiplier;
        body.linearVelocity = direction * finalSpeed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out EnemyHealth enemyHealth))
        {
            return;
        }

        enemyHealth.TakeDamage(damageData);

        if (other.TryGetComponent(out EnemyKnockback enemyKnockback))
        {
            Vector2 knockbackDirection = CalculateKnockbackDirection(other);

            enemyKnockback.ApplyKnockback(
                knockbackDirection,
                knockbackStrength,
                knockbackDuration
            );
        }

        if (other.TryGetComponent(out EnemyHitEffect hitEffect))
        {
            hitEffect.Play();
        }

        Destroy(gameObject);
    }

    private Vector2 CalculateKnockbackDirection(Collider2D targetCollider)
    {
        switch (knockbackDirectionMode)
        {
            case KnockbackDirectionMode.AwayFromAttacker:
                return (Vector2)targetCollider.transform.position - attackOrigin;

            case KnockbackDirectionMode.CustomDirection:
                return customKnockbackDirection;

            default:
                return travelDirection;
        }
    }
}
