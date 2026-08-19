using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Spawning/Scene Placed Enemy")]
public class ScenePlacedEnemy : MonoBehaviour
{
    [Header("거리 비활성화")]
    [SerializeField, Min(1f)] private float activationRange = 35f;
    [SerializeField, Min(0.1f)] private float checkInterval = 0.5f;

    internal float ActivationRangeSquared => activationRange * activationRange;
    internal float CheckInterval => checkInterval;
    internal float NextCheckTime { get; set; }
    internal bool IsSpawnerManaged { get; private set; }

    public void MarkSpawnerManaged()
    {
        IsSpawnerManaged = true;
    }

    private void Awake()
    {
        ScenePlacedEnemyManager.Register(this);
    }

    private void OnDestroy()
    {
        ScenePlacedEnemyManager.Unregister(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }
}
