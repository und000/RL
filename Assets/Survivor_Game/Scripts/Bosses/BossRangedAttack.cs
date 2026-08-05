using UnityEngine;

public class BossRangedAttack : MonoBehaviour
{
    [Header("발사 주기")]
    [SerializeField, Min(0.1f)] private float attackInterval = 2f;
    [SerializeField, Min(0f)] private float firstAttackDelay = 1f;

    [Header("탄환")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 5f;
    [SerializeField, Min(1)] private int projectileDamage = 1;
    [SerializeField, Min(0.1f)] private float projectileLifeTime = 6f;

    private Transform target;
    private float nextAttackTime;

    private void Start()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError($"{name}에 투사체 프리팹이 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }

        nextAttackTime = Time.time + firstAttackDelay;
    }

    private void Update()
    {
        if (target == null || Time.time < nextAttackTime)
        {
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        FireProjectile(direction);
        nextAttackTime = Time.time + attackInterval;
    }

    public void FireProjectile(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return;
        }

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        if (!projectileObject.TryGetComponent(out BossProjectile projectile))
        {
            Debug.LogError($"{projectilePrefab.name}에 BossProjectile 스크립트가 없습니다.");
            Destroy(projectileObject);
            return;
        }

        projectile.Initialize(
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime
        );
    }
}
