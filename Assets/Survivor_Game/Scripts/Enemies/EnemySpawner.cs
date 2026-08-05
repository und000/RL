using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnCenter;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.5f;
    [SerializeField, Min(1f)] private float spawnRadius = 10f;
    [SerializeField, Min(1)] private int maxEnemies = 20;

    private float spawnTimer;

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

    private void SpawnEnemy()
    {
        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        Vector2 spawnPosition = (Vector2)spawnCenter.position +
            randomDirection.normalized * spawnRadius;
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}
