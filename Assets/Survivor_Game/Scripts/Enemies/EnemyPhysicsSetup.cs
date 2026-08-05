using UnityEngine;

public static class EnemyPhysicsSetup
{
    private const string EnemyLayerName = "Enemy";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void DisableEnemyToEnemyCollision()
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);

        if (enemyLayer < 0)
        {
            Debug.LogError("Enemy 레이어를 찾을 수 없습니다.");
            return;
        }

        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }
}
