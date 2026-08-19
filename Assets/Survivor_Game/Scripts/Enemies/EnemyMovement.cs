using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[AddComponentMenu("Enemies/Enemy Movement")]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 2f;

    [Header("적 사이 간격")]
    [SerializeField, Min(0f)] private float separationRadius = 1.2f;
    [SerializeField, Min(0f)] private float separationStrength = 1.5f;

    [Header("미세 움직임 방지")]
    [Tooltip("최종 이동 벡터가 이 값 이하이면 정지합니다.")]
    [SerializeField, Min(0f)] private float movementDeadZone = 0.08f;

    private Rigidbody2D body;
    private EnemyKnockback enemyKnockback;
    private EnemyAwareness awareness;
    private Transform target;
    private ContactFilter2D separationFilter;
    private readonly List<Collider2D> nearbyEnemies = new List<Collider2D>(16);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        enemyKnockback = GetComponent<EnemyKnockback>();
        awareness = GetComponent<EnemyAwareness>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        separationFilter = new ContactFilter2D();
        separationFilter.SetLayerMask(1 << enemyLayer);
        separationFilter.useTriggers = false;
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    private void FixedUpdate()
    {
        if (enemyKnockback != null &&
            enemyKnockback.TryGetKnockbackVelocity(out Vector2 knockbackVelocity))
        {
            body.linearVelocity = knockbackVelocity;
            return;
        }

        if (awareness != null && !awareness.CanAct)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = target.position;
        Vector2 pursuitDirection = (targetPosition - body.position).normalized;
        Vector2 separationDirection = CalculateSeparationDirection();
        Vector2 moveDirection = pursuitDirection + separationDirection * separationStrength;

        if (moveDirection.sqrMagnitude <= movementDeadZone * movementDeadZone)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        body.linearVelocity = moveDirection.normalized * moveSpeed;
    }

    private Vector2 CalculateSeparationDirection()
    {
        if (separationRadius <= 0f)
        {
            return Vector2.zero;
        }

        nearbyEnemies.Clear();
        Physics2D.OverlapCircle(
            body.position,
            separationRadius,
            separationFilter,
            nearbyEnemies
        );

        Vector2 separationDirection = Vector2.zero;
        foreach (Collider2D nearbyCollider in nearbyEnemies)
        {
            Rigidbody2D nearbyBody = nearbyCollider.attachedRigidbody;
            if (nearbyBody == null || nearbyBody == body)
            {
                continue;
            }

            Vector2 offset = body.position - nearbyBody.position;
            float distance = offset.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                float angle = GetInstanceID() * 137.5f * Mathf.Deg2Rad;
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                distance = 0f;
            }

            float closeness = 1f - distance / separationRadius;
            separationDirection += offset.normalized * Mathf.Max(closeness, 0f);
        }

        return separationDirection;
    }

    private void OnDisable()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
