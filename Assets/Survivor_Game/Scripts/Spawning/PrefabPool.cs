using System.Collections.Generic;
using UnityEngine;

public interface IPrefabPoolLifecycle
{
    void OnPrefabSpawned();
    void OnPrefabDespawned();
}

[DisallowMultipleComponent]
public sealed class PooledPrefabInstance : MonoBehaviour
{
    private IPrefabPoolLifecycle[] listeners;
    public GameObject SourcePrefab { get; private set; }
    public bool IsReleased { get; private set; }

    internal void Configure(GameObject sourcePrefab)
    {
        SourcePrefab = sourcePrefab;
        CacheListeners();
    }

    internal void NotifySpawned()
    {
        IsReleased = false;
        EnsureListeners();
        foreach (IPrefabPoolLifecycle listener in listeners) listener.OnPrefabSpawned();
    }

    internal void NotifyDespawned()
    {
        if (IsReleased) return;
        IsReleased = true;
        EnsureListeners();
        foreach (IPrefabPoolLifecycle listener in listeners) listener.OnPrefabDespawned();
    }

    private void EnsureListeners()
    {
        if (listeners == null) CacheListeners();
    }

    private void CacheListeners()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        List<IPrefabPoolLifecycle> results =
            new List<IPrefabPoolLifecycle>(behaviours.Length);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IPrefabPoolLifecycle listener) results.Add(listener);
        }
        listeners = results.ToArray();
    }
}

public sealed class PrefabPool : MonoBehaviour
{
    private static PrefabPool instance;
    private readonly Dictionary<GameObject, Queue<PooledPrefabInstance>> pools =
        new Dictionary<GameObject, Queue<PooledPrefabInstance>>();
    private Transform poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        PrefabPool owner = GetOrCreate();
        if (!owner.pools.TryGetValue(prefab, out Queue<PooledPrefabInstance> pool))
        {
            pool = new Queue<PooledPrefabInstance>();
            owner.pools.Add(prefab, pool);
        }

        PooledPrefabInstance member = null;
        while (pool.Count > 0 && member == null) member = pool.Dequeue();
        if (member == null)
        {
            GameObject created = Instantiate(prefab, owner.poolRoot);
            created.SetActive(false);
            member = created.GetComponent<PooledPrefabInstance>();
            if (member == null) member = created.AddComponent<PooledPrefabInstance>();
            member.Configure(prefab);
        }

        member.transform.SetParent(null, false);
        member.transform.SetPositionAndRotation(position, rotation);
        member.gameObject.SetActive(true);
        member.NotifySpawned();
        return member.gameObject;
    }

    public static void Release(GameObject instanceObject)
    {
        if (instanceObject == null) return;
        PooledPrefabInstance member = instanceObject.GetComponent<PooledPrefabInstance>();
        if (member == null || member.SourcePrefab == null || instance == null)
        {
            Destroy(instanceObject);
            return;
        }
        if (member.IsReleased) return;

        member.NotifyDespawned();
        instanceObject.SetActive(false);
        member.transform.SetParent(instance.poolRoot, false);
        if (!instance.pools.TryGetValue(
            member.SourcePrefab, out Queue<PooledPrefabInstance> pool))
        {
            pool = new Queue<PooledPrefabInstance>();
            instance.pools.Add(member.SourcePrefab, pool);
        }
        pool.Enqueue(member);
    }

    private static PrefabPool GetOrCreate()
    {
        if (instance != null) return instance;
        GameObject poolObject = new GameObject("RuntimePrefabPool");
        instance = poolObject.AddComponent<PrefabPool>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        poolRoot = transform;
    }
}
