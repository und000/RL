using UnityEngine;

public enum KnockbackDirectionMode
{
    ProjectileDirection,
    AwayFromAttacker,
    CustomDirection
}

public class EnemyKnockback : MonoBehaviour, IEnemyPoolLifecycle
{
    [Header("넉백 저항")]
    [SerializeField]
    [Range(0f, 1f)]
    private float knockbackResistance;

    [Header("넉백 후 정지")]
    [Tooltip("넉백이 끝난 뒤 이동을 멈추는 시간입니다. 0이면 즉시 이동합니다.")]
    [SerializeField]
    [Min(0f)]
    private float postKnockbackStopDuration = 0.2f;

    private Vector2 knockbackDirection;
    private float knockbackStrength;
    private float knockbackDuration;
    private float remainingTime;
    private float remainingStopTime;

    public void ApplyKnockback(
        Vector2 direction,
        float strength,
        float duration)
    {
        float resistanceMultiplier = 1f - knockbackResistance;
        float finalStrength = Mathf.Max(0f, strength) * resistanceMultiplier;
        float finalDuration = Mathf.Max(0f, duration) * resistanceMultiplier;

        if (direction == Vector2.zero ||
            finalStrength <= 0f ||
            finalDuration <= 0f)
        {
            return;
        }

        knockbackDirection = direction.normalized;
        knockbackStrength = finalStrength;
        knockbackDuration = finalDuration;
        remainingTime = finalDuration;
        remainingStopTime = 0f;
    }

    public bool TryGetKnockbackVelocity(out Vector2 velocity)
    {
        if (remainingTime > 0f)
        {
            float remainingRatio = remainingTime / knockbackDuration;
            velocity = knockbackDirection * knockbackStrength * remainingRatio;
            remainingTime = Mathf.Max(remainingTime - Time.fixedDeltaTime, 0f);

            if (remainingTime <= 0f)
            {
                remainingStopTime = postKnockbackStopDuration;
            }

            return true;
        }

        if (remainingStopTime > 0f)
        {
            remainingStopTime = Mathf.Max(
                remainingStopTime - Time.fixedDeltaTime,
                0f
            );
            velocity = Vector2.zero;
            return true;
        }

        velocity = Vector2.zero;
        return false;
    }

    public void OnEnemySpawned()
    {
        ResetKnockback();
    }

    public void OnEnemyDespawned()
    {
        ResetKnockback();
    }

    private void ResetKnockback()
    {
        knockbackDirection = Vector2.zero;
        knockbackStrength = 0f;
        knockbackDuration = 0f;
        remainingTime = 0f;
        remainingStopTime = 0f;
    }
}
