using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyMovementPatternBase : MonoBehaviour
{
    protected Rigidbody2D Body { get; private set; }
    protected Transform Target { get; private set; }
    private EnemyKnockback enemyKnockback;
    private EnemyAwareness awareness;

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        enemyKnockback = GetComponent<EnemyKnockback>();
        awareness = GetComponent<EnemyAwareness>();
    }

    protected virtual void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Target = player.transform;
        }
    }

    protected virtual void OnEnable()
    {
        ResetPattern();
    }

    protected void FixedUpdate()
    {
        if (enemyKnockback != null &&
            enemyKnockback.TryGetKnockbackVelocity(out Vector2 knockbackVelocity))
        {
            Body.linearVelocity = knockbackVelocity;
            return;
        }

        if (awareness != null && !awareness.CanAct)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        if (Target == null)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        UpdatePattern(Time.fixedDeltaTime);
    }

    protected Vector2 DirectionToTarget()
    {
        return ((Vector2)Target.position - Body.position).normalized;
    }

    protected abstract void UpdatePattern(float deltaTime);
    protected abstract void ResetPattern();

    protected virtual void OnDisable()
    {
        if (Body != null)
        {
            Body.linearVelocity = Vector2.zero;
        }
    }
}
