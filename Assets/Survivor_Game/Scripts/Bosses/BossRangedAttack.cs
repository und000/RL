using System.Collections;
using UnityEngine;

public class BossRangedAttack : EnemyAttackPattern
{
    [Header("발사 주기")]
    [SerializeField, Min(0.1f)] private float attackInterval = 2f;
    [SerializeField, Min(0f)] private float firstAttackDelay = 1f;

    [Header("투사체")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 5f;
    [SerializeField, Min(1)] private int projectileDamage = 1;
    [SerializeField, Min(0.1f)] private float projectileLifeTime = 6f;

    [Header("공격 시퀀스")]
    [SerializeField, Min(0f)] private float windupDuration;
    [SerializeField, Min(1)] private int shotCount = 1;
    [SerializeField, Min(0f)] private float shotInterval;
    [SerializeField, Min(0f)] private float recoveryDuration;

    protected override float Cooldown => attackInterval;
    protected override float InitialDelay => firstAttackDelay;

    protected override bool CanExecutePattern(Transform target)
    {
        return projectilePrefab != null;
    }

    protected override IEnumerator ExecutePattern(Transform target)
    {
        if (windupDuration > 0f)
        {
            yield return new WaitForSeconds(windupDuration);
        }

        int validShotCount = Mathf.Max(1, shotCount);
        for (int shotIndex = 0; shotIndex < validShotCount; shotIndex++)
        {
            if (target == null)
            {
                yield break;
            }

            Vector2 direction = (target.position - transform.position).normalized;
            FireProjectile(direction);

            if (shotIndex < validShotCount - 1 && shotInterval > 0f)
            {
                yield return new WaitForSeconds(shotInterval);
            }
        }

        if (recoveryDuration > 0f)
        {
            yield return new WaitForSeconds(recoveryDuration);
        }
    }

    public void FireProjectile(Vector2 direction)
    {
        if (direction == Vector2.zero || projectilePrefab == null)
        {
            return;
        }

        EnemyProjectile projectile = EnemyProjectile.Spawn(
            projectilePrefab,
            transform.position,
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime);
        if (projectile == null)
        {
            Debug.LogError($"{projectilePrefab.name}에 EnemyProjectile 스크립트가 없습니다.");
        }
    }
}
