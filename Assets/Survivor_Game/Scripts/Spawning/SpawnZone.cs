using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[AddComponentMenu("Spawning/Spawn Zone")]
public class SpawnZone : MonoBehaviour
{
    private static readonly List<SpawnZone> activeZones = new List<SpawnZone>();
    public static IReadOnlyList<SpawnZone> ActiveZones => activeZones;
    [SerializeField] private EnemySpawnTable spawnTable;
    [SerializeField, Min(0f)] private float densityMultiplier = 1f;
    private Collider2D zoneCollider;

    public EnemySpawnTable SpawnTable => spawnTable;
    public float DensityMultiplier => densityMultiplier;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (!activeZones.Contains(this))
        {
            activeZones.Add(this);
        }
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    public bool Contains(Vector2 worldPosition)
    {
        return zoneCollider != null && zoneCollider.OverlapPoint(worldPosition);
    }

    public bool TryGetRandomPoint(int attempts, out Vector2 point)
    {
        if (zoneCollider == null)
        {
            point = default;
            return false;
        }

        Bounds bounds = zoneCollider.bounds;
        for (int index = 0; index < Mathf.Max(1, attempts); index++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y));
            if (zoneCollider.OverlapPoint(candidate))
            {
                point = candidate;
                return true;
            }
        }

        point = default;
        return false;
    }
}
