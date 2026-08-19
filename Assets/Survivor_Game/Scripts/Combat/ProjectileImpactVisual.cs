using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Combat/Projectile Impact Visual")]
public class ProjectileImpactVisual : MonoBehaviour, IPrefabPoolLifecycle
{
    [SerializeField, Min(0.01f)] private float duration = 0.2f;

    public void Play()
    {
        StartCoroutine(DestroyAfterDuration());
    }

    private IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSecondsRealtime(duration);
        PrefabPool.Release(gameObject);
    }

    public void OnPrefabSpawned()
    {
        StopAllCoroutines();
    }

    public void OnPrefabDespawned()
    {
        StopAllCoroutines();
    }
}
