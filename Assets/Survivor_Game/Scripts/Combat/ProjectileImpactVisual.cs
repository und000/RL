using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Combat/Projectile Impact Visual")]
public class ProjectileImpactVisual : MonoBehaviour, IPrefabPoolLifecycle
{
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [Tooltip("Optional animator. When assigned, replay from the start on every pooled hit.")]
    [SerializeField] private Animator impactAnimator;
    [SerializeField] private bool useAnimationDuration;

    public void Play()
    {
        StopAllCoroutines();
        float lifetime = duration;
        if (impactAnimator != null && impactAnimator.runtimeAnimatorController != null)
        {
            impactAnimator.Rebind();
            impactAnimator.Update(0f);
            if (useAnimationDuration)
                lifetime = impactAnimator.GetCurrentAnimatorStateInfo(0).length /
                    Mathf.Max(.01f, impactAnimator.speed);
        }
        StartCoroutine(DestroyAfterDuration(Mathf.Max(.01f, lifetime)));
    }

    private IEnumerator DestroyAfterDuration(float lifetime)
    {
        if (impactAnimator != null && impactAnimator.updateMode != AnimatorUpdateMode.UnscaledTime)
            yield return new WaitForSeconds(lifetime);
        else
            yield return new WaitForSecondsRealtime(lifetime);
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
