using UnityEngine;

[AddComponentMenu("Enemies/Patterns/Enemy2 Movement Pattern")]
public class Enemy2MovementPattern : EnemyMovementPatternBase
{
    private enum State { Chase, Stop, Dash }

    [Header("추적")]
    [SerializeField, Min(0f)] private float chaseDuration = 2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 2f;
    [Header("정지")]
    [SerializeField, Min(0f)] private float stopDuration = 0.5f;
    [Header("돌진")]
    [SerializeField, Min(0f)] private float dashDuration = 0.6f;
    [SerializeField, Min(0f)] private float dashSpeed = 7f;
    [SerializeField] private EnemyAttackTelegraph telegraphPrefab;
    private EnemyAttackTelegraph telegraph;

    private State state;
    private float stateElapsedTime;
    private Vector2 dashDirection;

    protected override void ResetPattern()
    {
        if (telegraph != null) telegraph.Hide();
        state = State.Chase;
        stateElapsedTime = 0f;
        dashDirection = Vector2.zero;
    }

    protected override void UpdatePattern(float deltaTime)
    {
        stateElapsedTime += deltaTime;
        switch (state)
        {
            case State.Chase:
                Body.linearVelocity = DirectionToTarget() * chaseSpeed;
                if (TryAdvance(chaseDuration, State.Stop))
                {
                    Body.linearVelocity = Vector2.zero;
                    dashDirection = DirectionToTarget();
                    if (telegraph == null && telegraphPrefab != null)
                        telegraph = Instantiate(telegraphPrefab, transform);
                }
                break;
            case State.Stop:
                Body.linearVelocity = Vector2.zero;
                if (telegraph != null)
                    telegraph.Show(transform.position, dashDirection, 1, 0f, 0f,
                        dashSpeed * dashDuration, stopDuration > 0f ? stateElapsedTime / stopDuration : 1f);
                if (TryAdvance(stopDuration, State.Dash))
                {
                    if (telegraph != null) telegraph.Hide();
                }
                break;
            default:
                Body.linearVelocity = dashDirection * dashSpeed;
                TryAdvance(dashDuration, State.Chase);
                break;
        }
    }

    private bool TryAdvance(float duration, State nextState)
    {
        if (stateElapsedTime < duration)
        {
            return false;
        }

        state = nextState;
        stateElapsedTime = 0f;
        return true;
    }

    protected override void OnDisable()
    {
        ResetPattern();
        base.OnDisable();
    }
}
