using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private HealthBarType healthBarType = HealthBarType.Normal;
    [SerializeField] private Vector3 barOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField, Min(0f)] private float visibleDuration = 2f;
    private HealthBarView activeView;
    private EnemyHealth targetHealth;
    private bool hasStarted;

    private void Awake()
    {
        targetHealth = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        hasStarted = true;
        Register();
    }

    private void OnEnable()
    {
        if (hasStarted)
        {
            Register();
        }
    }

    private void OnDisable()
    {
        if (activeView != null && EnemyHealthBarManager.Instance != null &&
            activeView.IsTracking(targetHealth) &&
            !activeView.IsHoldingDeathDisplayFor(targetHealth))
        {
            EnemyHealthBarManager.Instance.Release(activeView, healthBarType);
        }
        activeView = null;
    }

    private void Register()
    {
        if (EnemyHealthBarManager.Instance == null)
        {
            Debug.LogError("씬에 EnemyHealthBarManager가 없습니다.");
            return;
        }

        EnemyProfile profile = targetHealth.Profile;
        HealthBarType resolvedType = profile != null
            ? profile.HealthBarType : healthBarType;
        Vector3 resolvedOffset = profile != null
            ? profile.HealthBarOffset : barOffset;
        float resolvedDuration = profile != null
            ? profile.HealthBarVisibleDuration : visibleDuration;
        healthBarType = resolvedType;
        activeView = EnemyHealthBarManager.Instance.Register(
            targetHealth,
            resolvedType,
            resolvedOffset,
            resolvedDuration
        );
    }
}
