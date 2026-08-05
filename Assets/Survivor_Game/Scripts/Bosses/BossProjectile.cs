using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossProjectile : MonoBehaviour
{
    private int damage;

    public void Initialize(Vector2 direction, float speed, int newDamage, float lifeTime)
    {
        damage = Mathf.Max(1, newDamage);
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerHealth playerHealth))
        {
            return;
        }

        playerHealth.TakeDamage(damage);
        Destroy(gameObject);
    }
}
