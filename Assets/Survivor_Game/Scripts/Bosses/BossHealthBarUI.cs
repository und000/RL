using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossHealthBarUI : MonoBehaviour
{
    private void Start()
    {
        EnemyHealthBarManager manager = EnemyHealthBarManager.Instance;
        if (manager == null)
        {
            // 플레이 종료·씬 언로드 중에는 매니저가 이미 사라졌을 수 있다.
            if (gameObject.scene.isLoaded)
            {
                Debug.LogError("씬에 EnemyHealthBarManager가 없습니다.", this);
            }
            return;
        }

        EnemyHealth health = GetComponent<EnemyHealth>();
        EnemyProfile profile = health.Profile;
        manager.Register(
            health,
            profile != null ? profile.HealthBarType : HealthBarType.Boss,
            profile != null ? profile.HealthBarOffset : Vector3.zero,
            profile != null ? profile.HealthBarVisibleDuration : 0f);
    }
}
