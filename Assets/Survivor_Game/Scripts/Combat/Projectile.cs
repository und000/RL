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

    private Rigidbody2D body;
    private int damage;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, float weaponSpeed, int newDamage)
    {
        damage = newDamage;
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

        enemyHealth.TakeDamage(damage);
        Destroy(gameObject);
    }
}