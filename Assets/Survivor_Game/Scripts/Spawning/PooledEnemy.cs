using UnityEngine;

[DisallowMultipleComponent]
public class PooledEnemy : MonoBehaviour
{
    private EnemySpawner owner;
    private GameObject sourcePrefab;
    private IEnemyPoolLifecycle[] lifecycleListeners;
    public bool IsConfigured => owner != null && sourcePrefab != null;
    public GameObject SourcePrefab => sourcePrefab;

    public void Initialize(EnemySpawner newOwner, GameObject newSourcePrefab)
    {
        owner = newOwner;
        sourcePrefab = newSourcePrefab;
        CacheLifecycleListeners();
    }

    public void NotifySpawned()
    {
        EnsureLifecycleListeners();
        foreach (IEnemyPoolLifecycle listener in lifecycleListeners)
        {
            listener.OnEnemySpawned();
        }
    }

    public void NotifyDespawned()
    {
        EnsureLifecycleListeners();
        foreach (IEnemyPoolLifecycle listener in lifecycleListeners)
        {
            listener.OnEnemyDespawned();
        }
    }

    public void ReturnToPool()
    {
        if (owner != null)
        {
            owner.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void EnsureLifecycleListeners()
    {
        if (lifecycleListeners == null) CacheLifecycleListeners();
    }

    private void CacheLifecycleListeners()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        System.Collections.Generic.List<IEnemyPoolLifecycle> listeners =
            new System.Collections.Generic.List<IEnemyPoolLifecycle>(behaviours.Length);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IEnemyPoolLifecycle listener) listeners.Add(listener);
        }
        lifecycleListeners = listeners.ToArray();
    }
}
