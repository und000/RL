using UnityEngine;

[AddComponentMenu("Enemies/Patterns/Enemy3 Movement Pattern")]
public class Enemy3MovementPattern : EnemyMovementPatternBase, IEnemyAttackGate
{
    private enum State { Chase, RangedRoam }

    [Header("추적")]
    [SerializeField, Min(0f)] private float chaseDuration = 2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 2f;
    [Header("감지")]
    [SerializeField, Min(0f)] private float detectionRange = 6f;
    [Header("랜덤 이동")]
    [SerializeField, Min(0f)] private float randomMoveDuration = 2f;
    [SerializeField, Min(0f)] private float randomMoveSpeed = 2.5f;

    private State state;
    private float stateElapsedTime;
    private Vector2 randomDirection;

    public bool CanUseEnemyAttacks => state == State.RangedRoam;

    protected override void ResetPattern()
    {
        state = State.Chase;
        stateElapsedTime = 0f;
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
    }
}
