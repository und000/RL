using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private HealthBarType healthBarType = HealthBarType.Normal;
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField, Min(0f)] private float visibleDuration = 2f;

    private void Start()
    {
        if (EnemyHealthBarManager.Instance == null)
        {
            Debug.LogError("씬에 EnemyHealthBarManager가 없습니다.");
            return;
        }

        EnemyHealthBarManager.Instance.Register(
            GetComponent<EnemyHealth>(),
            healthBarType,
            barOffset,
            visibleDuration
        );
    }
}
