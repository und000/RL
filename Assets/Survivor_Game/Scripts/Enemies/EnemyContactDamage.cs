using UnityEngine;

public class EnemyContactDamage : MonoBehaviour, IEnemyPoolLifecycle
{
    [SerializeField]
    [Min(1)]
    private int contactDamage = 1;

    [SerializeField]
    [Min(0.1f)]
    private float damageInterval = 1f;

    private float nextDamageTime;
    private EnemyStagger stagger;
    private void Awake() => stagger = GetComponent<EnemyStagger>();

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (stagger != null && stagger.IsStaggered) return;
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

    public void OnEnemySpawned()
    {
        nextDamageTime = 0f;
    }

    public void OnEnemyDespawned()
    {
        nextDamageTime = 0f;
    }
}
