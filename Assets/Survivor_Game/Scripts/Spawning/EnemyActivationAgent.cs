using UnityEngine;

[DisallowMultipleComponent]
public class EnemyActivationAgent : MonoBehaviour
{
    private PooledEnemy pooledEnemy;
    private Transform player;
    private Behaviour[] aiBehaviours;
    private float reducedAiDistance;
    private float returnDistance;
    private float checkInterval;
    private float nextCheckTime;
    private bool aiEnabled = true;

    public void Initialize(
        PooledEnemy member,
        Transform playerTransform,
        float newReducedAiDistance,
        float newReturnDistance,
        float newCheckInterval)
    {
        pooledEnemy = member;
        player = playerTransform;
        reducedAiDistance = newReducedAiDistance;
        returnDistance = Mathf.Max(newReturnDistance, reducedAiDistance);
        checkInterval = Mathf.Max(0.1f, newCheckInterval);
        aiBehaviours = GetComponents<Behaviour>();
    }

    private void OnEnable()
    {
        nextCheckTime = Time.time;
        SetAiEnabled(true);
    }

    private void Update()
    {
        if (player == null || pooledEnemy == null || Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance >= returnDistance)
        {
            pooledEnemy.ReturnToPool();
            return;
        }

        SetAiEnabled(distance < reducedAiDistance);
    }

    private void SetAiEnabled(bool enabledState)
    {
        if (aiBehaviours == null || aiEnabled == enabledState)
        {
            return;
        }

        aiEnabled = enabledState;
        foreach (Behaviour behaviour in aiBehaviours)
        {
            if (behaviour == null || behaviour == this ||
                behaviour is EnemyHealth || behaviour is EnemyHealthBar ||
                behaviour is EnemyHitEffect || behaviour is EnemyKnockback)
            {
                continue;
            }

            if (behaviour is EnemyMovement ||
                behaviour is EnemyMovementPatternBase ||
                behaviour is EnemyPatternController)
            {
                behaviour.enabled = enabledState;
            }
        }
    }
}
