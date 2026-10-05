using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Spawning/Spawn Director")]
[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Header("진행 방식")]
    [Tooltip("켜면 시간 기반 자동 스폰과 타이머 보스를 끄고, 방(RoomInstance)이 " +
        "요청할 때만 적을 생성한다. 로그라이크 구성에서는 켜 둔다.")]
    [SerializeField] private bool roomDrivenMode = true;
    [Tooltip("방 기반 진행일 때 AI가 살아 있는 최소 거리. 방 대각선보다 커야 " +
        "방 반대편 적이 멈춰 서지 않는다.")]
    [SerializeField, Min(1f)] private float roomAiDistance = 70f;

    [Header("스폰 데이터")]
    [Tooltip("현재 활성 SpawnZone에 별도 테이블이 없을 때 사용하는 기본 소환 테이블입니다.")]
    [SerializeField] private EnemySpawnTable defaultSpawnTable;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.5f;
    [SerializeField, Min(1)] private int maxActiveEnemies = 80;
    [Header("생성 위치")]
    [SerializeField, Min(1f)] private float minimumSpawnDistance = 10f;
    [SerializeField, Min(1)] private int positionSearchAttempts = 12;
    [SerializeField, Min(0f)] private float spawnClearanceRadius = 0.6f;
    [SerializeField] private LayerMask blockedSpawnLayers;
    [SerializeField] private Collider2D optionalMapBounds;
    [SerializeField, Min(0f)] private float viewportPadding = 0.08f;
    [Header("AI 거리 단계")]
    [SerializeField, Min(1f)] private float reducedAiDistance = 22f;
    [SerializeField, Min(1f)] private float poolReturnDistance = 32f;
    [SerializeField, Min(0.1f)] private float activationCheckInterval = 0.5f;
    [Header("보스")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField, Min(0f)] private float bossSpawnDelay = 20f;

    private readonly Dictionary<GameObject, Queue<PooledEnemy>> pools =
        new Dictionary<GameObject, Queue<PooledEnemy>>();
    private readonly Dictionary<EnemySpawnTable, int> sequenceIndices =
        new Dictionary<EnemySpawnTable, int>();
    private PlayerLevel playerLevel;
    private Camera worldCamera;
    private Transform poolRoot;
    private float elapsedTime;
    private float spawnTimer;
    private int activeEnemyCount;
    private bool bossSpawned;

    private void Awake()
    {
        GameObject poolObject = new GameObject("EnemyPool");
        poolObject.transform.SetParent(transform, false);
        poolRoot = poolObject.transform;
        worldCamera = Camera.main;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player == null || (!roomDrivenMode && defaultSpawnTable == null))
        {
            Debug.LogError("SpawnDirector에 Player가 필요하며, 자동 스폰 모드에서는 Default Spawn Table도 필요합니다.", this);
            enabled = false;
            return;
        }

        playerLevel = player.GetComponent<PlayerLevel>();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        // 방 기반 진행에서는 방이 스폰을 주도하므로 시간 기반 로직을 돌리지 않는다.
        if (roomDrivenMode) return;

        TrySpawnBoss();
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f || activeEnemyCount >= maxActiveEnemies)
        {
            return;
        }

        SpawnZone zone = GetCurrentZone();
        if (zone == null)
        {
            spawnTimer = spawnInterval;
            return;
        }

        EnemySpawnTable table = zone.SpawnTable != null
            ? zone.SpawnTable : defaultSpawnTable;
        float density = zone.DensityMultiplier;
        float interval = table != null ? table.BatchInterval : spawnInterval;
        spawnTimer = interval / Mathf.Max(0.01f, density);
        if (table == null) return;

        int level = playerLevel != null ? playerLevel.GetCurrentLevel() : 1;
        switch (table.Mode)
        {
            case EnemySpawnTable.SpawnMode.FixedBatch:
                SpawnFixedBatch(table, zone, level);
                break;
            case EnemySpawnTable.SpawnMode.Sequence:
                SpawnSequence(table, zone, level);
                break;
            default:
                SpawnWeightedRandom(table, zone, level);
                break;
        }
    }

    private void SpawnWeightedRandom(EnemySpawnTable table, SpawnZone zone, int level)
    {
        if (table.TrySelectWeighted(elapsedTime, level, out EnemySpawnTable.Entry entry))
        {
            SpawnRandomGroup(entry, zone);
        }
    }

    private void SpawnFixedBatch(EnemySpawnTable table, SpawnZone zone, int level)
    {
        foreach (EnemySpawnTable.Entry entry in table.Entries)
        {
            if (activeEnemyCount >= maxActiveEnemies) break;
            if (!entry.IsAvailable(elapsedTime, level) || entry.fixedSpawnCount <= 0) continue;
            SpawnEntry(entry, zone, entry.fixedSpawnCount);
        }
    }

    private void SpawnSequence(EnemySpawnTable table, SpawnZone zone, int level)
    {
        sequenceIndices.TryGetValue(table, out int sequenceIndex);
        if (table.TrySelectSequence(elapsedTime, level, ref sequenceIndex,
            out EnemySpawnTable.Entry entry))
        {
            sequenceIndices[table] = sequenceIndex;
            SpawnRandomGroup(entry, zone);
        }
    }

    private void SpawnRandomGroup(EnemySpawnTable.Entry entry, SpawnZone zone)
    {
        int minimumGroup = Mathf.Max(1, entry.minimumGroupSize);
        int maximumGroup = Mathf.Max(minimumGroup, entry.maximumGroupSize);
        int groupSize = Random.Range(minimumGroup, maximumGroup + 1);
        SpawnEntry(entry, zone, groupSize);
    }

    private void SpawnEntry(EnemySpawnTable.Entry entry, SpawnZone zone, int count)
    {
        for (int index = 0; index < count && activeEnemyCount < maxActiveEnemies; index++)
        {
            if (TryFindSpawnPosition(zone, out Vector2 position))
            {
                Acquire(entry.prefab, position);
            }
        }
    }

    /// <summary>방(RoomInstance)이 특정 위치에 적 하나를 요청할 때 쓴다.</summary>
    public EnemyHealth SpawnForRoom(GameObject prefab, Vector2 position) =>
        SpawnForRoom(prefab, position, EnemyScaling.None);

    /// <summary>층 난이도 보정을 걸어 적 하나를 생성한다.</summary>
    public EnemyHealth SpawnForRoom(
        GameObject prefab, Vector2 position, EnemyScaling scaling, EnemyRank? encounterRank = null)
    {
        if (prefab == null) return null;
        PooledEnemy member = Acquire(prefab, position, scaling, encounterRank);
        return member != null ? member.GetComponent<EnemyHealth>() : null;
    }

    private PooledEnemy Acquire(GameObject prefab, Vector2 position) =>
        Acquire(prefab, position, EnemyScaling.None);

    private PooledEnemy Acquire(
        GameObject prefab, Vector2 position, EnemyScaling scaling, EnemyRank? encounterRank = null)
    {
        if (!pools.TryGetValue(prefab, out Queue<PooledEnemy> pool))
        {
            pool = new Queue<PooledEnemy>();
            pools.Add(prefab, pool);
        }

        PooledEnemy member;
        if (pool.Count > 0)
        {
            member = pool.Dequeue();
        }
        else
        {
            GameObject enemyObject = Instantiate(prefab, poolRoot);
            enemyObject.SetActive(false);
            member = enemyObject.AddComponent<PooledEnemy>();
            member.Initialize(this, prefab);
            EnemyActivationAgent activation = enemyObject.AddComponent<EnemyActivationAgent>();
            // 방 기반 진행에서는 거리로 풀에 회수해 버리면 그 방이 영원히 클리어되지 않는다.
            // 회수는 사실상 끄고, AI 감속 거리도 방 대각선을 덮도록 넉넉히 잡는다.
            float returnDistance = roomDrivenMode ? 100000f : poolReturnDistance;
            float aiDistance = roomDrivenMode
                ? Mathf.Max(reducedAiDistance, roomAiDistance)
                : reducedAiDistance;
            activation.Initialize(member, player, aiDistance,
                returnDistance, activationCheckInterval);
        }

        member.transform.SetParent(null, false);
        member.transform.position = position;
        // 활성화 순간 OnEnable이 체력을 되돌리므로, 보정은 그 전에 걸어야 한다.
        if (member.TryGetComponent(out EnemyHealth health))
        {
            // Preserve a prefab's higher rank, but promote ordinary room enemies to elite.
            EnemyRank baseRank = health.Profile != null ? health.Profile.Rank : EnemyRank.Normal;
            health.SetEncounterRank(encounterRank.HasValue && encounterRank.Value > baseRank
                ? encounterRank : null);
            health.ApplyScaling(scaling);
        }
        if (member.TryGetComponent(out ScenePlacedEnemy scenePlacedEnemy))
        {
            scenePlacedEnemy.MarkSpawnerManaged();
        }
        member.gameObject.SetActive(true);
        member.NotifySpawned();
        if (member.TryGetComponent(out EnemyLifecycleVisual lifecycleVisual))
        {
            lifecycleVisual.PlaySpawn(IsInsideCamera(position));
        }
        activeEnemyCount++;
        return member;
    }

    public void Release(PooledEnemy member)
    {
        if (member == null || !member.IsConfigured || !member.gameObject.activeSelf)
        {
            return;
        }

        GameObject sourcePrefab = member.SourcePrefab;
        if (!pools.TryGetValue(sourcePrefab, out Queue<PooledEnemy> pool))
        {
            pool = new Queue<PooledEnemy>();
            pools.Add(sourcePrefab, pool);
        }

        Rigidbody2D body = member.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        member.NotifyDespawned();
        member.gameObject.SetActive(false);
        member.transform.SetParent(poolRoot, false);
        pool.Enqueue(member);
        activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
    }

    private bool TryFindSpawnPosition(SpawnZone zone, out Vector2 position)
    {
        for (int attempt = 0; attempt < positionSearchAttempts; attempt++)
        {
            if (zone == null || !zone.TryGetRandomPoint(positionSearchAttempts, out Vector2 candidate))
            {
                break;
            }

            float distance = Vector2.Distance(candidate, player.position);
            if (distance < minimumSpawnDistance ||
                !IsOutsideCamera(candidate) ||
                (optionalMapBounds != null && !optionalMapBounds.OverlapPoint(candidate)) ||
                Physics2D.OverlapCircle(candidate, spawnClearanceRadius, blockedSpawnLayers) != null)
            {
                continue;
            }

            position = candidate;
            return true;
        }

        position = default;
        return false;
    }

    private bool IsOutsideCamera(Vector2 position)
    {
        if (worldCamera == null) return true;
        Vector3 viewport = worldCamera.WorldToViewportPoint(position);
        return viewport.x < -viewportPadding || viewport.x > 1f + viewportPadding ||
            viewport.y < -viewportPadding || viewport.y > 1f + viewportPadding;
    }

    private bool IsInsideCamera(Vector2 position)
    {
        if (worldCamera == null) return false;
        Vector3 viewport = worldCamera.WorldToViewportPoint(position);
        return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
            viewport.y >= 0f && viewport.y <= 1f;
    }

    private SpawnZone GetCurrentZone()
    {
        foreach (SpawnZone zone in SpawnZone.ActiveZones)
        {
            if (zone != null && zone.isActiveAndEnabled && zone.Contains(player.position))
            {
                return zone;
            }
        }
        return null;
    }

    private void TrySpawnBoss()
    {
        if (bossSpawned || bossPrefab == null || elapsedTime < bossSpawnDelay || player == null)
        {
            return;
        }
        SpawnZone zone = GetCurrentZone();
        if (zone != null && TryFindSpawnPosition(zone, out Vector2 position))
        {
            bossSpawned = true;
            GameObject boss = Instantiate(bossPrefab, position, Quaternion.identity);
            if (boss.TryGetComponent(out ScenePlacedEnemy scenePlacedEnemy))
            {
                scenePlacedEnemy.MarkSpawnerManaged();
            }
        }
    }
}
