using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class EnemyHealthBarManager : MonoBehaviour
{
    public static EnemyHealthBarManager Instance { get; private set; }

    [Header("체력바 프리팹")]
    [SerializeField] private HealthBarView normalHealthBarPrefab;
    [SerializeField] private HealthBarView eliteHealthBarPrefab;
    [SerializeField] private HealthBarView bossHealthBarPrefab;

    private readonly Dictionary<HealthBarType, Stack<HealthBarView>> pools =
        new Dictionary<HealthBarType, Stack<HealthBarView>>();
    private readonly Dictionary<HealthBarType, RectTransform> containers =
        new Dictionary<HealthBarType, RectTransform>();
    private Camera worldCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("EnemyHealthBarManager가 씬에 두 개 이상 있습니다.");
            enabled = false;
            return;
        }

        Instance = this;
        worldCamera = Camera.main;

        CreateContainer(HealthBarType.Normal, "NormalHealthBars");
        CreateContainer(HealthBarType.Elite, "EliteHealthBars");
        CreateContainer(HealthBarType.Boss, "BossHealthBars");
    }

    public void Register(
        EnemyHealth target,
        HealthBarType type,
        Vector3 worldOffset,
        float visibleDuration)
    {
        HealthBarView view = Acquire(type);
        if (view == null)
        {
            return;
        }

        view.Initialize(
            target,
            type,
            worldOffset,
            visibleDuration,
            this,
            worldCamera
        );
    }

    public void Release(HealthBarView view, HealthBarType type)
    {
        view.ResetView();
        view.gameObject.SetActive(false);
        view.transform.SetParent(containers[type], false);
        pools[type].Push(view);
    }

    private HealthBarView Acquire(HealthBarType type)
    {
        Stack<HealthBarView> pool = pools[type];
        HealthBarView view;

        if (pool.Count > 0)
        {
            view = pool.Pop();
        }
        else
        {
            HealthBarView prefab = GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"{type} 체력바 프리팹이 연결되지 않았습니다.");
                return null;
            }

            view = Instantiate(prefab, containers[type]);
        }

        view.transform.SetParent(containers[type], false);
        view.gameObject.SetActive(true);
        return view;
    }

    private HealthBarView GetPrefab(HealthBarType type)
    {
        switch (type)
        {
            case HealthBarType.Elite:
                return eliteHealthBarPrefab;
            case HealthBarType.Boss:
                return bossHealthBarPrefab;
            default:
                return normalHealthBarPrefab;
        }
    }

    private void CreateContainer(HealthBarType type, string objectName)
    {
        GameObject containerObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform container = containerObject.GetComponent<RectTransform>();
        container.SetParent(transform, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;

        containers[type] = container;
        pools[type] = new Stack<HealthBarView>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
