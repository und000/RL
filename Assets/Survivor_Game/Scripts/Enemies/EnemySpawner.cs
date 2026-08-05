using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnCenter;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.5f;
    [SerializeField, Min(1f)] private float spawnRadius = 10f;
    [SerializeField, Min(1)] private int maxEnemies = 20;

    [Header("임시 보스")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField, Min(0f)] private float bossSpawnDelay = 20f;

    private float spawnTimer;
    private float elapsedTime;
    private bool bossSpawned;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Spawner에 Enemy Prefab이 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        if (spawnCenter == null)
        {
            Debug.LogError("Enemy Spawner에 Spawn Center가 연결되지 않았습니다.");
            enabled = false;
        }
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        TrySpawnBoss();

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer = spawnInterval;
        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies)
        {
            return;
        }

        SpawnEnemy();
    }

    private void TrySpawnBoss()
    {
        if (bossSpawned || bossPrefab == null || elapsedTime < bossSpawnDelay)
        {
            return;
        }

        bossSpawned = true;
        SpawnAtRandomEdge(bossPrefab);
    }

    private void SpawnEnemy()
    {
        SpawnAtRandomEdge(enemyPrefab);
    }

    private void SpawnAtRandomEdge(GameObject prefab)
    {
        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        Vector2 spawnPosition = (Vector2)spawnCenter.position +
            randomDirection.normalized * spawnRadius;
        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }
}
