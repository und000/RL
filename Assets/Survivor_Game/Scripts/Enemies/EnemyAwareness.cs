using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Enemies/Enemy Awareness")]
public class EnemyAwareness : MonoBehaviour, IEnemyPoolLifecycle
{
    [Header("플레이어 인식")]
    [SerializeField, Min(0f)] private float detectionRange = 8f;
    [SerializeField, Min(0f)] private float disengageRange = 12f;
    [SerializeField, Min(0.05f)] private float checkInterval = 0.2f;
    [Header("피격 시 경계")]
    [SerializeField, Min(0f)] private float damagedAlertDuration = 5f;

    private EnemyHealth health;
    private Transform player;
    private float nextCheckTime;
    private float alertUntil;
    public bool CanAct { get; private set; }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        ResolvePlayer();
    }

    private void OnEnable()
    {
        ResetAwareness();
        if (health != null) health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamaged -= HandleDamaged;
    }

    private void Update()
    {
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;
        if (player == null) ResolvePlayer();
        if (player == null)
        {
            CanAct = false;
            return;
        }

        float distanceSquared = ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude;
        float detect = detectionRange * detectionRange;
        float disengage = Mathf.Max(detectionRange, disengageRange);
        if (distanceSquared <= detect)
        {
            CanAct = true;
        }
        else if (Time.time >= alertUntil && distanceSquared > disengage * disengage)
        {
            CanAct = false;
        }
    }

    private void HandleDamaged()
    {
        CanAct = true;
        alertUntil = Time.time + damagedAlertDuration;
    }

    private void ResolvePlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    public void OnEnemySpawned()
    {
        ResetAwareness();
    }

    public void OnEnemyDespawned()
    {
        CanAct = false;
        alertUntil = 0f;
    }

    private void ResetAwareness()
    {
        CanAct = false;
        alertUntil = 0f;
        nextCheckTime = Time.time;
        if (player == null) ResolvePlayer();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.45f, 0f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(detectionRange, disengageRange));
    }
}
