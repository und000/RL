using UnityEngine;

/// <summary>One camera-wide impact sequence; simultaneous staggers share the same 0.6 second beat.</summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class StaggerImpactFeedback : MonoBehaviour
{
    [Header("붕괴 효과음")]
    [SerializeField] private AudioClip normalSound;
    [SerializeField] private AudioClip majorSound;
    [SerializeField, Range(0f, 1f)] private float normalVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float majorVolume = 0.95f;
    [SerializeField, Min(0f)] private float normalSoundInterval = 0.05f;

    [Header("엘리트 / 보스 붕괴 연출 (실제 시간)")]
    [SerializeField, Range(0.01f, 1f)] private float impactTimeScale = 0.1f;
    [SerializeField, Min(0f)] private float holdDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float recoveryDuration = 0.3f;
    [SerializeField, Range(0f, 0.3f)] private float zoomAmount = 0.08f;
    [SerializeField, Min(0f)] private float shakeStrength = 0.45f;
    [SerializeField, Min(1f)] private float shakeFrequency = 38f;
    [SerializeField] private AnimationCurve shakeEnvelope = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private static StaggerImpactFeedback instance;
    private Camera worldCamera;
    private AudioSource audioSource;
    private PlayerTeleportSkill teleport;
    private RunManager runManager;
    private bool active;
    private float startedAt;
    private float originalTimeScale;
    private float originalFixedDelta;
    private float originalSize;
    private float originalFov;
    private float lastTimeScale;
    private float lastFixedDelta;
    private float nextNormalSoundTime;
    private Vector3 appliedShake;

    public bool IsPlaying => active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private void Awake()
    {
        worldCamera = GetComponent<Camera>();
        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.pitch = 1f;
        teleport = FindFirstObjectByType<PlayerTeleportSkill>();
        runManager = FindFirstObjectByType<RunManager>();
    }

    public static void Play(EnemyRank rank)
    {
        if (instance == null && Camera.main != null)
            instance = Camera.main.GetComponent<StaggerImpactFeedback>();
        if (instance != null && instance.isActiveAndEnabled) instance.PlayImpact(rank);
    }

    private void PlayImpact(EnemyRank rank)
    {
        if (Time.timeScale <= 0f || GameInputKeys.IsGameplayBlocked ||
            (runManager != null && runManager.IsRunOver)) return;
        if (rank == EnemyRank.Normal)
        {
            if (normalSound != null && Time.unscaledTime >= nextNormalSoundTime)
            {
                audioSource.PlayOneShot(normalSound, normalVolume);
                nextNormalSoundTime = Time.unscaledTime + normalSoundInterval;
            }
            return;
        }

        // Multiple enemies can break on the same swing. Do not stack shake/zoom/audio or extend time.
        if (active) return;
        if (majorSound != null) audioSource.PlayOneShot(majorSound, majorVolume);
        if (teleport != null && teleport.IsAiming) return;
        originalTimeScale = Time.timeScale;
        originalFixedDelta = Time.fixedDeltaTime;
        originalSize = worldCamera.orthographicSize;
        originalFov = worldCamera.fieldOfView;
        startedAt = Time.unscaledTime;
        active = true;
        SetTimeScale(originalTimeScale * impactTimeScale);
        SetZoom(1f);
    }

    private void Update()
    {
        // Remove last frame's additive shake before CameraFollow computes its new destination.
        RemoveShake();
        if (!active) return;
        if (GameInputKeys.IsGameplayBlocked || (runManager != null && runManager.IsRunOver) ||
            !Mathf.Approximately(Time.timeScale, lastTimeScale))
        {
            Cancel();
            return;
        }
        float elapsed = Time.unscaledTime - startedAt;
        float recovery = RecoveryProgress(elapsed, holdDuration, recoveryDuration);
        if (recovery >= 1f)
        {
            Cancel();
            return;
        }
        SetTimeScale(Mathf.Lerp(originalTimeScale * impactTimeScale, originalTimeScale, recovery));
        SetZoom(1f - recovery);
    }

    public static float RecoveryProgress(float elapsed, float hold, float recovery) =>
        Mathf.Clamp01((elapsed - Mathf.Max(0f, hold)) / Mathf.Max(0.01f, recovery));

    private void LateUpdate()
    {
        if (!active) return;
        float elapsed = Time.unscaledTime - startedAt;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, holdDuration + recoveryDuration));
        float envelope = shakeEnvelope != null ? Mathf.Max(0f, shakeEnvelope.Evaluate(progress)) : 1f - progress;
        float phase = elapsed * shakeFrequency * Mathf.PI * 2f;
        appliedShake = new Vector3(Mathf.Sin(phase + 0.8f), Mathf.Sin(phase * 1.37f + 2.1f), 0f)
            * (shakeStrength * envelope);
        transform.position += appliedShake;
    }

    private void SetTimeScale(float value)
    {
        lastTimeScale = Mathf.Max(0.001f, value);
        lastFixedDelta = originalFixedDelta * (lastTimeScale / Mathf.Max(0.001f, originalTimeScale));
        Time.timeScale = lastTimeScale;
        Time.fixedDeltaTime = lastFixedDelta;
    }

    private void SetZoom(float strength)
    {
        float multiplier = 1f - zoomAmount * Mathf.Clamp01(strength);
        worldCamera.orthographicSize = originalSize * multiplier;
        worldCamera.fieldOfView = originalFov * multiplier;
    }

    private void RemoveShake()
    {
        transform.position -= appliedShake;
        appliedShake = Vector3.zero;
    }

    // Call before another system snapshots time (pause, teleport, result screen).
    public static void CancelActive()
    {
        if (instance != null) instance.Cancel();
    }

    private void Cancel()
    {
        RemoveShake();
        if (!active) return;
        // An external pause/scene transition owns its new value; never overwrite it.
        if (Mathf.Approximately(Time.timeScale, lastTimeScale)) Time.timeScale = originalTimeScale;
        if (Mathf.Approximately(Time.fixedDeltaTime, lastFixedDelta)) Time.fixedDeltaTime = originalFixedDelta;
        if (worldCamera != null)
        {
            worldCamera.orthographicSize = originalSize;
            worldCamera.fieldOfView = originalFov;
        }
        active = false;
    }

    private void OnDisable()
    {
        Cancel();
        if (audioSource != null) audioSource.Stop();
        if (instance == this) instance = null;
    }
}
