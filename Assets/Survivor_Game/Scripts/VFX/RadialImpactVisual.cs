using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class RadialImpactVisual : MonoBehaviour
{
    public enum ImpactTiming { Expanding, Instant, DelayedInstant }
    [Header("Prefab Visual")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float visualLifetime = 0.35f;
    [Tooltip("끄면 재생, 확산, 지연, 소멸이 게임 배속과 일시정지를 따릅니다. 자식 Animator/Particle System도 같은 시간 모드로 설정하세요.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Impact Timing")]
    [FormerlySerializedAs("duration")]
    [SerializeField, Min(0.01f)] private float impactDuration = 0.1f;
    [Tooltip("Used only when Impact Timing is Expanding.")]
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private ImpactTiming impactTiming = ImpactTiming.Expanding;
    [SerializeField, Min(0f)] private float impactDelay;

    [Header("Editor Preview Only")]
    [SerializeField] private bool showRangePreview = true;
    [SerializeField, Min(0.01f)] private float previewRadius = 3f;
    [SerializeField] private Color previewColor = new Color(0.1f, 0.8f, 1f, 0.18f);
    [SerializeField] private bool showPreviewLabel = true;

    public void Play(Vector2 center, float radius, Action<float> onRadiusReached,
        float impactDurationOverride = -1f)
    {
        StartCoroutine(PlayRoutine(center, radius, onRadiusReached,
            impactDurationOverride > 0f ? impactDurationOverride : impactDuration));
    }

    private IEnumerator PlayRoutine(Vector2 center, float radius,
        Action<float> onRadiusReached, float playDuration)
    {
        transform.position = center;
        float startedAt = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (animator != null)
        {
            animator.updateMode = useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
            animator.speed = 1f / Mathf.Max(0.01f, visualLifetime);
            animator.Play(0, 0, 0f);
        }
        if (impactTiming == ImpactTiming.Instant) onRadiusReached?.Invoke(radius);
        else if (impactTiming == ImpactTiming.DelayedInstant)
        {
            if (impactDelay > 0f)
                yield return useUnscaledTime ? (object)new WaitForSecondsRealtime(impactDelay) : new WaitForSeconds(impactDelay);
            onRadiusReached?.Invoke(radius);
        }
        float elapsed = 0f;
        while (elapsed < playDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / playDuration);
            float expansion = Mathf.Clamp01(expansionCurve.Evaluate(progress));
            if (impactTiming == ImpactTiming.Expanding) onRadiusReached?.Invoke(radius * expansion);
            yield return null;
        }
        if (impactTiming == ImpactTiming.Expanding) onRadiusReached?.Invoke(radius);

        float remainingVisualTime = visualLifetime - ((useUnscaledTime ? Time.unscaledTime : Time.time) - startedAt);
        if (remainingVisualTime > 0f)
            yield return useUnscaledTime ? (object)new WaitForSecondsRealtime(remainingVisualTime) : new WaitForSeconds(remainingVisualTime);

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showRangePreview || previewRadius <= 0f) return;

        Vector3 center = transform.position;
        Color fillColor = previewColor;
        UnityEditor.Handles.color = fillColor;
        UnityEditor.Handles.DrawSolidDisc(center, Vector3.forward, previewRadius);

        Color outlineColor = previewColor;
        outlineColor.a = 1f;
        UnityEditor.Handles.color = outlineColor;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, previewRadius);

        if (showPreviewLabel)
        {
            GUIStyle labelStyle = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
            labelStyle.normal.textColor = outlineColor;
            UnityEditor.Handles.Label(
                center + Vector3.up * previewRadius,
                $"Preview Radius: {previewRadius:0.##}",
                labelStyle);
        }
    }
#endif
}
