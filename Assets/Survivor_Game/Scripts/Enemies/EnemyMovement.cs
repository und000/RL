using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[AddComponentMenu("EnemysMovement")]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField]
    [Min(0f)]
    private float moveSpeed = 2f;

    private Rigidbody2D body;
    private Transform target;

    private bool isPausing = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            target = player.transform;
            StartCoroutine(PauseTimerRoutine());
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        if (isPausing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = (Vector2)target.position;
        Vector2 direction = targetPosition - body.position;

        body.linearVelocity = direction.normalized * moveSpeed;
    }

    private System.Collections.IEnumerator PauseTimerRoutine()
    {
        while (true)
        {
            isPausing = false;
            yield return new WaitForSeconds(0.5f);

            isPausing = true;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnDisable()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
