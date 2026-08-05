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

        EnemyHealthBarManager.Instance.Register(
            GetComponent<EnemyHealth>(),
            HealthBarType.Boss,
            Vector3.zero,
            0f
        );
    }
}
