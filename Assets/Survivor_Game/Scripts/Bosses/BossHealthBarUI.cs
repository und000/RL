using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossHealthBarUI : MonoBehaviour
{
    private void Start()
    {
        if (EnemyHealthBarManager.Instance == null)
        {
            Debug.LogError("씬에 EnemyHealthBarManager가 없습니다.");
            return;
        }

        EnemyHealth health = GetComponent<EnemyHealth>();
        EnemyProfile profile = health.Profile;
        EnemyHealthBarManager.Instance.Register(
            health,
            profile != null ? profile.HealthBarType : HealthBarType.Boss,
            profile != null ? profile.HealthBarOffset : Vector3.zero,
            profile != null ? profile.HealthBarVisibleDuration : 0f);
    }
}
