using System.Collections.Generic;
using UnityEngine;

// 적보다 먼저 Awake가 돌도록 실행 순서를 앞당긴다.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Canvas))]
public class EnemyHealthBarManager : MonoBehaviour
{
    private static EnemyHealthBarManager instance;
    private static bool isQuitting;

    /// <summary>
    /// Awake 순서에 의존하지 않도록, 정적 참조가 비어 있으면 씬에서 한 번 찾아 캐시한다.
    /// 종료·씬 언로드 중에는 찾지 않는다.
    /// </summary>
    public static EnemyHealthBarManager Instance
    {
        get
        {
            if (instance == null && !isQuitting)
            {
                instance = FindFirstObjectByType<EnemyHealthBarManager>(
                    FindObjectsInactive.Exclude);
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

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
        if (instance != null && instance != this)
        {
            Debug.LogError("EnemyHealthBarManager가 씬에 두 개 이상 있습니다.", this);
            enabled = false;
            return;
        }

        instance = this;
        worldCamera = Camera.main;

        CreateContainer(HealthBarType.Normal, "NormalHealthBars");
        CreateContainer(HealthBarType.Elite, "EliteHealthBars");
        CreateContainer(HealthBarType.Boss, "BossHealthBars");
    }

    public HealthBarView Register(
        EnemyHealth target,
        HealthBarType type,
        Vector3 worldOffset,
        float visibleDuration)
    {
        HealthBarView view = Acquire(type);
        if (view == null)
        {
            return null;
        }

        view.Initialize(
            target,
            type,
            worldOffset,
            visibleDuration,
            this,
            worldCamera
        );
        return view;
    }

    public void Release(HealthBarView view, HealthBarType type)
    {
        if (view == null || !view.gameObject.activeSelf)
        {
            return;
        }

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

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
