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
        if (activeView != null)
        {
            EnemyHealthBarManager manager = EnemyHealthBarManager.Instance;
            if (manager != null &&
                activeView.IsTracking(targetHealth) &&
                !activeView.IsHoldingDeathDisplayFor(targetHealth))
            {
                manager.Release(activeView, healthBarType);
            }
        }
        activeView = null;
    }

    private void Register()
    {
        EnemyHealthBarManager manager = EnemyHealthBarManager.Instance;
        if (manager == null)
        {
            // 플레이 종료·씬 언로드 중에는 매니저가 이미 사라졌을 수 있다.
            // 그때는 진짜 설정 누락이 아니므로 조용히 넘어간다.
            if (gameObject.scene.isLoaded)
            {
                Debug.LogError("씬에 EnemyHealthBarManager가 없습니다.", this);
            }
            return;
        }

        EnemyProfile profile = targetHealth.Profile;
        HealthBarType resolvedType = profile != null
            ? profile.HealthBarType : healthBarType;
        if (targetHealth.Rank == EnemyRank.Elite) resolvedType = HealthBarType.Elite;
        else if (targetHealth.Rank == EnemyRank.Boss) resolvedType = HealthBarType.Boss;
        Vector3 resolvedOffset = profile != null
            ? profile.HealthBarOffset : barOffset;
        float resolvedDuration = profile != null
            ? profile.HealthBarVisibleDuration : visibleDuration;
        healthBarType = resolvedType;
        activeView = manager.Register(
            targetHealth,
            resolvedType,
            resolvedOffset,
            resolvedDuration
        );
    }
}
