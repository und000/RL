using UnityEngine;

[AddComponentMenu("Enemies/Patterns/Enemy3 Movement Pattern")]
public class Enemy3MovementPattern : EnemyMovementPatternBase
{
    private enum State { Chase, RangedRoam }

    [Header("추적")]
    [SerializeField, Min(0f)] private float chaseDuration = 2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 2f;
    [Header("감지 및 발사")]
    [SerializeField, Min(0f)] private float detectionRange = 6f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0.05f)] private float fireInterval = 0.8f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 6f;
    [SerializeField, Min(1)] private int projectileDamage = 10;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 5f;
    [Header("랜덤 이동")]
    [SerializeField, Min(0f)] private float randomMoveDuration = 2f;
    [SerializeField, Min(0f)] private float randomMoveSpeed = 2.5f;

    private State state;
    private float stateElapsedTime;
    private float nextFireTime;
    private Vector2 randomDirection;

    protected override void ResetPattern()
    {
        state = State.Chase;
        stateElapsedTime = 0f;
        nextFireTime = 0f;
        randomDirection = Vector2.zero;
    }

    protected override void UpdatePattern(float deltaTime)
    {
        stateElapsedTime += deltaTime;
        if (state == State.Chase)
        {
            Body.linearVelocity = DirectionToTarget() * chaseSpeed;
            if (stateElapsedTime >= chaseDuration)
            {
                stateElapsedTime = 0f;
                if (Vector2.Distance(Body.position, Target.position) <= detectionRange)
                {
                    BeginRangedRoam();
                }
            }
            return;
        }

        Body.linearVelocity = randomDirection * randomMoveSpeed;
        if (Time.time >= nextFireTime)
        {
            FireProjectile();
            nextFireTime = Time.time + fireInterval;
        }

        if (stateElapsedTime >= randomMoveDuration)
        {
            state = State.Chase;
            stateElapsedTime = 0f;
        }
    }

    private void BeginRangedRoam()
    {
        state = State.RangedRoam;
        randomDirection = Random.insideUnitCircle.normalized;
        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }
        nextFireTime = Time.time;
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        EnemyProjectile projectile = EnemyProjectile.Spawn(
            projectilePrefab,
            transform.position,
            DirectionToTarget(),
            projectileSpeed,
            projectileDamage,
            projectileLifetime);
        if (projectile == null)
        {
            Debug.LogError($"{projectilePrefab.name}에 EnemyProjectile 스크립트가 없습니다.", this);
        }
    }
}
