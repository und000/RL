using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField]
    [Min(1)]
    private int contactDamage = 1;

    [SerializeField]
    [Min(0.1f)]
    private float damageInterval = 1f;

    private float nextDamageTime;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextDamageTime)
        {
            return;
        }

        if (!collision.gameObject.TryGetComponent(
                out PlayerHealth playerHealth))
        {
            return;
        }

        playerHealth.TakeDamage(contactDamage);

        nextDamageTime = Time.time + damageInterval;
    }
}