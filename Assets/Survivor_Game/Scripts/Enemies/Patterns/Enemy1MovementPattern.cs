using UnityEngine;

[AddComponentMenu("Enemies/Patterns/Enemy1 Movement Pattern")]
public class Enemy1MovementPattern : EnemyMovementPatternBase
{
    private enum State { FirstChase, Stop, SecondChase }

    [Header("첫 번째 추적")]
    [SerializeField, Min(0f)] private float firstChaseDuration = 2f;
    [SerializeField, Min(0f)] private float firstChaseSpeed = 2f;
    [Header("정지")]
    [SerializeField, Min(0f)] private float stopDuration = 0.5f;
    [Header("두 번째 추적")]
    [SerializeField, Min(0f)] private float secondChaseDuration = 1.5f;
    [SerializeField, Min(0f)] private float secondChaseSpeed = 4f;

    private State state;
    private float stateElapsedTime;

    protected override void ResetPattern()
    {
        state = State.FirstChase;
        stateElapsedTime = 0f;
    }

    protected override void UpdatePattern(float deltaTime)
    {
        stateElapsedTime += deltaTime;
        switch (state)
        {
            case State.FirstChase:
                Body.linearVelocity = DirectionToTarget() * firstChaseSpeed;
                TryAdvance(firstChaseDuration, State.Stop);
                break;
            case State.Stop:
                Body.linearVelocity = Vector2.zero;
                TryAdvance(stopDuration, State.SecondChase);
                break;
            default:
                Body.linearVelocity = DirectionToTarget() * secondChaseSpeed;
                TryAdvance(secondChaseDuration, State.FirstChase);
                break;
        }
    }

    private void TryAdvance(float duration, State nextState)
    {
        if (stateElapsedTime < duration)
        {
            return;
        }

        state = nextState;
        stateElapsedTime = 0f;
    }
}
