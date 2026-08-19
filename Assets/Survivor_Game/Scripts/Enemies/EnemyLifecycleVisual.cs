using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Enemies/Enemy Lifecycle Visual")]
public class EnemyLifecycleVisual : MonoBehaviour, IEnemyPoolLifecycle
{
    [Header("Spawn")]
    [SerializeField, Min(0.01f)] private float spawnDuration = 0.3f;
    [SerializeField, Range(0f, 1f)] private float spawnStartScale = 0.15f;
    [Header("Death")]
    [SerializeField, Min(0.01f)] private float deathDuration = 0.25f;
    [SerializeField, Min(0f)] private float deathEndScale = 1.35f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Vector3 baseScale;
    private Coroutine routine;
    private Collider2D[] colliders;
    private bool[] colliderStates;
    private Rigidbody2D body;
    private bool bodyWasSimulated;

    private void Awake()
    {
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        colliders = GetComponentsInChildren<Collider2D>(true);
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderStates[i] = colliders[i].enabled;
        body = GetComponent<Rigidbody2D>();
        bodyWasSimulated = body != null && body.simulated;
    }

    private void OnEnable()
    {
        RestoreVisuals();
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = colliderStates[i];
        if (body != null) body.simulated = bodyWasSimulated;
    }

    public void PlaySpawn(bool shouldPlay)
    {
        if (!shouldPlay) return;
        StartVisualRoutine(SpawnRoutine());
    }

    public bool TryPlayDeath(Action onComplete)
    {
        if (!isActiveAndEnabled || deathDuration <= 0f) return false;
        StartVisualRoutine(DeathRoutine(onComplete));
        return true;
    }

    private void StartVisualRoutine(IEnumerator nextRoutine)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(nextRoutine);
    }

    private IEnumerator SpawnRoutine()
    {
        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spawnDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = baseScale * Mathf.Lerp(spawnStartScale, 1f, eased);
            SetAlpha(eased);
            yield return null;
        }
        RestoreVisuals();
        routine = null;
    }

    private IEnumerator DeathRoutine(Action onComplete)
    {
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
        float elapsed = 0f;
        while (elapsed < deathDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathDuration);
            transform.localScale = baseScale * Mathf.Lerp(1f, deathEndScale, t);
            SetAlpha(1f - t);
            yield return null;
        }
        routine = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float multiplier)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = baseColors[i];
            color.a *= multiplier;
            renderers[i].color = color;
        }
    }

    private void RestoreVisuals()
    {
        transform.localScale = baseScale;
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = baseColors[i];
    }

    public void OnEnemySpawned()
    {
        ResetLifecycleState();
    }

    public void OnEnemyDespawned()
    {
        ResetLifecycleState();
    }

    private void ResetLifecycleState()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        RestoreVisuals();
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = colliderStates[i];
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = bodyWasSimulated;
        }
    }
}
