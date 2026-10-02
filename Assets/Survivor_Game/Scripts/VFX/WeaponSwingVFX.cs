using System;
using UnityEngine;

[Serializable]
public class WeaponSwingArcSettings
{
    [Header("사용 여부")]
    public bool enabled = true;

    [Header("배치")]
    public Vector2 localPosition;
    public float localRotation;
    public Vector2 effectSize = new Vector2(9f, 9f);

    [Header("원호 형태")]
    [Range(1f, 359f)] public float arcAngle = 150f;
    [Range(-180f, 180f)] public float centerAngle;
    [Range(0.05f, 1.2f)] public float radius = 0.62f;
    [Range(0.005f, 0.5f)] public float thickness = 0.12f;
    [Range(0.1f, 8f)] public float taperPower = 1.5f;
    [Range(0.001f, 0.2f)] public float edgeSoftness = 0.025f;
    public bool reverse;

    [Header("색상 및 발광")]
    [ColorUsage(true, true)] public Color outerColor =
        new Color(1f, 0.08f, 0.01f, 0.35f);
    [ColorUsage(true, true)] public Color mainColor =
        new Color(1f, 0.25f, 0.03f, 1f);
    [ColorUsage(true, true)] public Color coreColor =
        new Color(4f, 1.2f, 0.25f, 1f);

    [Header("재생 시간 (기본 공격 시간 기준)")]
    [Min(0.001f)] public float revealDuration = 0.08f;
    [Min(0f)] public float holdDuration = 0.02f;
    [Min(0.001f)] public float fadeDuration = 0.14f;

    [Header("속도 기반 방향성 블러")]
    [Min(0f)] public float minimumMotionSpeed = 1f;
    [Min(0.01f)] public float maximumMotionSpeed = 25f;
    [Range(0f, 0.5f)] public float maximumBlurLength = 0.1f;
    [Min(0.01f)] public float motionResponseSpeed = 24f;

    [Header("풀링 잔상")]
    public bool useAfterimages = true;
    [Min(0.01f)] public float afterimageInterval = 0.035f;
    [Min(0f)] public float afterimageMinimumDistance = 0.08f;
    [Min(0.01f)] public float afterimageLifetime = 0.12f;
    [Range(0f, 1f)] public float afterimageOpacity = 0.35f;

    public float TotalDuration =>
        Mathf.Max(0.001f, revealDuration) + Mathf.Max(0f, holdDuration) +
        Mathf.Max(0.001f, fadeDuration);

    public void Normalize()
    {
        effectSize.x = Mathf.Max(0.01f, effectSize.x);
        effectSize.y = Mathf.Max(0.01f, effectSize.y);
        arcAngle = Mathf.Clamp(arcAngle, 1f, 359f);
        centerAngle = Mathf.Clamp(centerAngle, -180f, 180f);
        radius = Mathf.Clamp(radius, 0.05f, 1.2f);
        thickness = Mathf.Clamp(thickness, 0.005f, 0.5f);
        taperPower = Mathf.Clamp(taperPower, 0.1f, 8f);
        edgeSoftness = Mathf.Clamp(edgeSoftness, 0.001f, 0.2f);
        revealDuration = Mathf.Max(0.001f, revealDuration);
        holdDuration = Mathf.Max(0f, holdDuration);
        fadeDuration = Mathf.Max(0.001f, fadeDuration);
        minimumMotionSpeed = Mathf.Max(0f, minimumMotionSpeed);
        maximumMotionSpeed = Mathf.Max(minimumMotionSpeed + 0.01f, maximumMotionSpeed);
        maximumBlurLength = Mathf.Clamp(maximumBlurLength, 0f, 0.5f);
        motionResponseSpeed = Mathf.Max(0.01f, motionResponseSpeed);
        afterimageInterval = Mathf.Max(0.01f, afterimageInterval);
        afterimageMinimumDistance = Mathf.Max(0f, afterimageMinimumDistance);
        afterimageLifetime = Mathf.Max(0.01f, afterimageLifetime);
        afterimageOpacity = Mathf.Clamp01(afterimageOpacity);
    }
}

internal static class WeaponSwingShaderProperties
{
    private static readonly int OuterColor = Shader.PropertyToID("_OuterColor");
    private static readonly int MainColor = Shader.PropertyToID("_MainColor");
    private static readonly int CoreColor = Shader.PropertyToID("_CoreColor");
    private static readonly int ArcRadius = Shader.PropertyToID("_ArcRadius");
    private static readonly int ArcThickness = Shader.PropertyToID("_ArcThickness");
    private static readonly int ArcAngle = Shader.PropertyToID("_ArcAngle");
    private static readonly int CenterAngle = Shader.PropertyToID("_CenterAngle");
    private static readonly int TaperPower = Shader.PropertyToID("_TaperPower");
    private static readonly int EdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int Reveal = Shader.PropertyToID("_Reveal");
    private static readonly int Fade = Shader.PropertyToID("_Fade");
    private static readonly int Reverse = Shader.PropertyToID("_Reverse");
    private static readonly int Opacity = Shader.PropertyToID("_Opacity");
    private static readonly int MotionDirection = Shader.PropertyToID("_MotionDirection");
    private static readonly int MotionStrength = Shader.PropertyToID("_MotionStrength");
    private static readonly int BlurLength = Shader.PropertyToID("_BlurLength");

    // MaterialPropertyBlock은 MonoBehaviour 생성자/필드 초기화 시점에 만들 수 없다.
    // 실제 렌더 갱신이 처음 일어날 때 한 번만 만들고 궤적과 잔상이 함께 재사용한다.
    private static MaterialPropertyBlock sharedProperties;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sharedProperties = null;
    }

    public static void Apply(
        Renderer target,
        WeaponSwingArcSettings settings,
        float reveal,
        float fade,
        float opacity,
        Vector2 motionDirection,
        float motionStrength)
    {
        if (target == null || settings == null) return;

        MaterialPropertyBlock properties = sharedProperties;
        if (properties == null)
        {
            properties = new MaterialPropertyBlock();
            sharedProperties = properties;
        }

        target.GetPropertyBlock(properties);
        properties.SetColor(OuterColor, settings.outerColor);
        properties.SetColor(MainColor, settings.mainColor);
        properties.SetColor(CoreColor, settings.coreColor);
        properties.SetFloat(ArcRadius, settings.radius);
        properties.SetFloat(ArcThickness, settings.thickness);
        properties.SetFloat(ArcAngle, settings.arcAngle);
        properties.SetFloat(CenterAngle, settings.centerAngle);
        properties.SetFloat(TaperPower, settings.taperPower);
        properties.SetFloat(EdgeSoftness, settings.edgeSoftness);
        properties.SetFloat(Reveal, Mathf.Clamp01(reveal));
        properties.SetFloat(Fade, Mathf.Clamp01(fade));
        properties.SetFloat(Reverse, settings.reverse ? 1f : 0f);
        properties.SetFloat(Opacity, Mathf.Clamp01(opacity));
        properties.SetVector(MotionDirection, motionDirection);
        properties.SetFloat(MotionStrength, Mathf.Clamp01(motionStrength));
        properties.SetFloat(BlurLength, settings.maximumBlurLength);
        target.SetPropertyBlock(properties);
    }
}

/// <summary>
/// 풀에서 재사용되는 궤적 인스턴스를 안전하게 가리키는 소유권 토큰.
/// 재생이 자연 종료되어 풀로 돌아간 뒤에는 예전 소유자가 새 재생을 잘못 종료할 수 없다.
/// </summary>
public readonly struct WeaponSwingVfxHandle
{
    private readonly WeaponSwingVFX effect;
    private readonly int playbackId;

    internal WeaponSwingVfxHandle(WeaponSwingVFX newEffect, int newPlaybackId)
    {
        effect = newEffect;
        playbackId = newPlaybackId;
    }

    public bool IsActive => effect != null && effect.IsPlaybackActive(playbackId);

    public void Stop()
    {
        if (effect != null) effect.StopAndRelease(playbackId);
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("VFX/Weapon Swing VFX")]
public sealed class WeaponSwingVFX : MonoBehaviour, IPrefabPoolLifecycle
{
    [SerializeField] private MeshRenderer arcRenderer;
    [SerializeField] private Transform renderTransform;
    [SerializeField] private Transform motionPoint;
    [SerializeField] private WeaponSwingAfterimage afterimagePrefab;

    private WeaponSwingArcSettings settings;
    private float playbackSpeed = 1f;
    private float elapsed;
    private float motionStrength;
    private float nextAfterimageTime;
    private int playbackId;
    private Vector3 previousMotionPosition;
    private Vector3 lastAfterimagePosition;
    private Transform externalMotionPoint;
    private bool playing;

    public static WeaponSwingVfxHandle SpawnAttached(
        WeaponSwingVFX prefab,
        Transform parent,
        Transform newMotionPoint,
        WeaponSwingArcSettings newSettings,
        float newPlaybackSpeed)
    {
        if (prefab == null || parent == null || newSettings == null ||
            !newSettings.enabled) return default;

        GameObject instance = PrefabPool.Spawn(
            prefab.gameObject, parent.position, parent.rotation);
        if (instance == null || !instance.TryGetComponent(out WeaponSwingVFX effect))
        {
            if (instance != null) PrefabPool.Release(instance);
            return default;
        }

        effect.transform.SetParent(parent, true);
        effect.externalMotionPoint = newMotionPoint;
        if (!effect.Play(newSettings, newPlaybackSpeed))
        {
            PrefabPool.Release(effect.gameObject);
            return default;
        }
        return new WeaponSwingVfxHandle(effect, effect.playbackId);
    }

    private bool Play(WeaponSwingArcSettings newSettings, float newPlaybackSpeed)
    {
        if (arcRenderer == null || renderTransform == null)
        {
            Debug.LogError(
                "WeaponSwingVFX 프리팹에 Arc Renderer 또는 Render Transform이 없습니다.",
                this);
            return false;
        }

        settings = newSettings;
        playbackSpeed = Mathf.Max(0.01f, newPlaybackSpeed);
        elapsed = 0f;
        motionStrength = 0f;
        nextAfterimageTime = settings.afterimageInterval;
        playbackId++;
        transform.localPosition = settings.localPosition;
        transform.localRotation = Quaternion.Euler(0f, 0f, settings.localRotation);
        transform.localScale = Vector3.one;
        renderTransform.localPosition = Vector3.zero;
        renderTransform.localRotation = Quaternion.identity;
        renderTransform.localScale = new Vector3(
            settings.effectSize.x, settings.effectSize.y, 1f);
        if (motionPoint != null)
        {
            motionPoint.localPosition = new Vector3(
                settings.effectSize.x * settings.radius * 0.5f, 0f, 0f);
        }

        previousMotionPosition = GetMotionPosition();
        lastAfterimagePosition = previousMotionPosition;
        playing = true;
        arcRenderer.enabled = true;
        UpdateShader(0f, 0f, 1f, Vector2.right, 0f);
        return true;
    }

    private void Update()
    {
        if (!playing || settings == null) return;

        float deltaTime = Time.deltaTime;
        elapsed += deltaTime * playbackSpeed;
        Vector3 currentMotionPosition = GetMotionPosition();
        Vector3 worldVelocity = deltaTime > 0.00001f
            ? (currentMotionPosition - previousMotionPosition) / deltaTime
            : Vector3.zero;
        previousMotionPosition = currentMotionPosition;

        Vector3 localVelocity = renderTransform.InverseTransformVector(worldVelocity);
        Vector2 motionDirection = new Vector2(localVelocity.x, localVelocity.y);
        float speed = worldVelocity.magnitude;
        float targetStrength = Mathf.InverseLerp(
            settings.minimumMotionSpeed,
            settings.maximumMotionSpeed,
            speed);
        float response = 1f - Mathf.Exp(
            -settings.motionResponseSpeed * Mathf.Max(0f, deltaTime));
        motionStrength = Mathf.Lerp(motionStrength, targetStrength, response);
        if (motionDirection.sqrMagnitude < 0.0001f) motionDirection = Vector2.right;
        else motionDirection.Normalize();

        EvaluateTimeline(out float reveal, out float fade, out float opacity);
        UpdateShader(reveal, fade, opacity, motionDirection, motionStrength);
        TrySpawnAfterimage(currentMotionPosition, reveal, fade, motionDirection);

        if (elapsed >= settings.TotalDuration)
        {
            StopAndRelease();
        }
    }

    private void EvaluateTimeline(out float reveal, out float fade, out float opacity)
    {
        float revealDuration = Mathf.Max(0.001f, settings.revealDuration);
        float fadeStart = revealDuration + Mathf.Max(0f, settings.holdDuration);
        float fadeDuration = Mathf.Max(0.001f, settings.fadeDuration);
        reveal = Mathf.Clamp01(elapsed / revealDuration);
        fade = Mathf.Clamp01((elapsed - fadeStart) / fadeDuration);
        opacity = 1f - fade;
    }

    private void TrySpawnAfterimage(
        Vector3 motionPosition,
        float reveal,
        float fade,
        Vector2 motionDirection)
    {
        if (!settings.useAfterimages || afterimagePrefab == null ||
            motionStrength <= 0.01f || elapsed < nextAfterimageTime) return;
        if (Vector3.Distance(lastAfterimagePosition, motionPosition) <
            settings.afterimageMinimumDistance) return;

        nextAfterimageTime = elapsed + settings.afterimageInterval;
        lastAfterimagePosition = motionPosition;
        WeaponSwingAfterimage.Spawn(
            afterimagePrefab,
            renderTransform,
            settings,
            reveal,
            fade,
            settings.afterimageOpacity * motionStrength,
            motionDirection,
            motionStrength,
            playbackSpeed);
    }

    private void UpdateShader(
        float reveal,
        float fade,
        float opacity,
        Vector2 motionDirection,
        float strength)
    {
        WeaponSwingShaderProperties.Apply(
            arcRenderer,
            settings,
            reveal,
            fade,
            opacity,
            motionDirection,
            strength);
    }

    private Vector3 GetMotionPosition()
    {
        if (externalMotionPoint != null) return externalMotionPoint.position;
        return motionPoint != null ? motionPoint.position : transform.position;
    }

    public bool IsPlaybackActive(int expectedPlaybackId) =>
        playing && playbackId == expectedPlaybackId;

    public void StopAndRelease(int expectedPlaybackId)
    {
        if (!IsPlaybackActive(expectedPlaybackId)) return;
        StopAndRelease();
    }

    private void StopAndRelease()
    {
        if (!playing) return;
        playing = false;
        if (arcRenderer != null) arcRenderer.enabled = false;
        PrefabPool.Release(gameObject);
    }

    public void OnPrefabSpawned()
    {
        playing = false;
        settings = null;
        externalMotionPoint = null;
        if (arcRenderer != null) arcRenderer.enabled = false;
    }

    public void OnPrefabDespawned()
    {
        playing = false;
        settings = null;
        externalMotionPoint = null;
        if (arcRenderer != null)
        {
            arcRenderer.enabled = false;
            arcRenderer.SetPropertyBlock(null);
        }
    }

    private void OnValidate()
    {
        if (arcRenderer == null) arcRenderer = GetComponentInChildren<MeshRenderer>();
        if (renderTransform == null && arcRenderer != null)
        {
            renderTransform = arcRenderer.transform;
        }
    }
}
