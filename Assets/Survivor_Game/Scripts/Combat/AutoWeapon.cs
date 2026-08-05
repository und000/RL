using UnityEngine;

[RequireComponent(typeof(PlayerCombatStats))]
public class AutoWeapon : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0.05f)] private float attackInterval = 0.8f;
    [SerializeField, Min(0f)] private float attackRange = 8f;
    [SerializeField, Min(0f)] private float projectileSpeed = 8f;

    private float attackTimer;
    private PlayerCombatStats combatStats;

    private void Awake()
    {
        combatStats = GetComponent<PlayerCombatStats>();
    }

    private void Update()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f)
        {
            return;
        }

        Transform target = FindNearestEnemy();
        if (target == null)
        {
            return;
        }

        Fire(target);
        attackTimer = attackInterval;
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearestEnemy = null;
        float nearestDistanceSqr = attackRange * attackRange;

        foreach (GameObject enemy in enemies)
        {
            Vector2 offset = enemy.transform.position - transform.position;
            float distanceSqr = offset.sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy.transform;
            }
        }

        return nearestEnemy;
    }

    private void Fire(Transform target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("AutoWeapon에 Projectile Prefab이 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        if (!projectileObject.TryGetComponent(out Projectile projectile))
        {
            Debug.LogError("Projectile 프리팹에 Projectile 스크립트가 없습니다.");
            Destroy(projectileObject);
            return;
        }

        projectile.Initialize(
            direction,
            projectileSpeed,
            combatStats.CreateDamageData()
        );
    }
}
