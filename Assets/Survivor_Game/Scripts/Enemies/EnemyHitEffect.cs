using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyHitEffect : MonoBehaviour, IEnemyPoolLifecycle
{
    [Header("반짝임")]
    [SerializeField]
    private Color flashColor = Color.white;

    [SerializeField]
    [Min(0f)]
    private float flashDuration = 0.08f;

    [Header("흔들림")]
    [SerializeField]
    [Min(0f)]
    private float shakeStrength = 0.12f;

    [SerializeField]
    [Min(0f)]
    private float shakeDuration = 0.12f;

    [SerializeField]
    [Min(0.001f)]
    private float shakeInterval = 0.015f;

    [Header("재생 제한")]
    [SerializeField]
    [Min(0f)]
    private float effectCooldown = 0.05f;

    private SpriteRenderer visualRenderer;
    private Transform visualTransform;
    public Transform StaggerPoseRoot { get; private set; }
    private Color originalColor;
    private Coroutine effectRoutine;
    private float nextEffectTime;

    private void Awake()
    {
        CreateVisualObject();
    }

    public void Play()
    {
        if (Time.time < nextEffectTime)
        {
            return;
        }

        nextEffectTime = Time.time + effectCooldown;

        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            ResetVisual();
        }

        effectRoutine = StartCoroutine(PlayEffectRoutine());
    }

    private IEnumerator PlayEffectRoutine()
    {
        float elapsedTime = 0f;
        float totalDuration = Mathf.Max(flashDuration, shakeDuration);
        float nextShakeTime = 0f;

        while (elapsedTime < totalDuration)
        {
            if (elapsedTime < flashDuration)
            {
                visualRenderer.color = flashColor;
            }
            else
            {
                visualRenderer.color = originalColor;
            }

            if (elapsedTime < shakeDuration && elapsedTime >= nextShakeTime)
            {
                Vector2 shakeOffset = Random.insideUnitCircle * shakeStrength;
                visualTransform.localPosition = shakeOffset;
                nextShakeTime = elapsedTime + shakeInterval;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ResetVisual();
        effectRoutine = null;
    }

    private void CreateVisualObject()
    {
        SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
        StaggerPoseRoot = new GameObject("StaggerPose").transform;
        StaggerPoseRoot.SetParent(transform, false);
        GameObject visualObject = new GameObject("Visual");
        visualTransform = visualObject.transform;
        visualTransform.SetParent(StaggerPoseRoot, false);

        visualRenderer = visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = originalRenderer.sprite;
        visualRenderer.sharedMaterial = originalRenderer.sharedMaterial;
        visualRenderer.color = originalRenderer.color;
        visualRenderer.flipX = originalRenderer.flipX;
        visualRenderer.flipY = originalRenderer.flipY;
        visualRenderer.drawMode = originalRenderer.drawMode;
        visualRenderer.size = originalRenderer.size;
        visualRenderer.maskInteraction = originalRenderer.maskInteraction;
        visualRenderer.spriteSortPoint = originalRenderer.spriteSortPoint;
        visualRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        visualRenderer.sortingOrder = originalRenderer.sortingOrder;

        originalColor = originalRenderer.color;
        originalRenderer.enabled = false;
    }

    private void ResetVisual()
    {
        if (visualRenderer == null || visualTransform == null) return;
        visualRenderer.color = originalColor;
        visualTransform.localPosition = Vector3.zero;
    }

    public void OnEnemySpawned()
    {
        ResetEffect();
    }

    public void OnEnemyDespawned()
    {
        ResetEffect();
    }

    private void ResetEffect()
    {
        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }
        nextEffectTime = 0f;
        ResetVisual();
    }
}
