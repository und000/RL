using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("VFX/Weapon Swing Afterimage")]
public sealed class WeaponSwingAfterimage : MonoBehaviour, IPrefabPoolLifecycle
{
    [SerializeField] private MeshRenderer arcRenderer;

    private WeaponSwingArcSettings settings;
    private float reveal;
    private float fade;
    private float startingOpacity;
    private float lifetime;
    private float elapsed;
    private Vector2 motionDirection;
    private float motionStrength;
    private bool playing;

    public static void Spawn(
        WeaponSwingAfterimage prefab,
        Transform sourceRender,
        WeaponSwingArcSettings newSettings,
        float newReveal,
        float newFade,
        float opacity,
        Vector2 newMotionDirection,
        float newMotionStrength,
        float playbackSpeed)
    {
        if (prefab == null || sourceRender == null || opacity <= 0f) return;
        GameObject instance = PrefabPool.Spawn(
            prefab.gameObject,
            sourceRender.position,
            sourceRender.rotation);
        if (instance == null || !instance.TryGetComponent(out WeaponSwingAfterimage ghost))
        {
            if (instance != null) PrefabPool.Release(instance);
            return;
        }

        ghost.transform.localScale = sourceRender.lossyScale;
        ghost.Configure(
            newSettings,
            newReveal,
            newFade,
            opacity,
            newMotionDirection,
            newMotionStrength,
            newSettings.afterimageLifetime / Mathf.Max(0.01f, playbackSpeed));
    }

    private void Configure(
        WeaponSwingArcSettings newSettings,
        float newReveal,
        float newFade,
        float opacity,
        Vector2 newMotionDirection,
        float newMotionStrength,
        float newLifetime)
    {
        if (arcRenderer == null)
        {
            Debug.LogError(
                "WeaponSwingAfterimage 프리팹에 Arc Renderer가 없습니다.", this);
            PrefabPool.Release(gameObject);
            return;
        }

        settings = newSettings;
        reveal = newReveal;
        fade = newFade;
        startingOpacity = Mathf.Clamp01(opacity);
        lifetime = Mathf.Max(0.01f, newLifetime);
        elapsed = 0f;
        motionDirection = newMotionDirection;
        motionStrength = newMotionStrength;
        playing = true;
        arcRenderer.enabled = true;
        Apply(1f);
    }

    private void Update()
    {
        if (!playing) return;
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / lifetime);
        Apply(1f - progress);
        if (progress >= 1f) StopAndRelease();
    }

    private void Apply(float normalizedOpacity)
    {
        if (arcRenderer == null || settings == null) return;
        WeaponSwingShaderProperties.Apply(
            arcRenderer,
            settings,
            reveal,
            fade,
            startingOpacity * normalizedOpacity,
            motionDirection,
            motionStrength);
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
        if (arcRenderer != null) arcRenderer.enabled = false;
    }

    public void OnPrefabDespawned()
    {
        playing = false;
        settings = null;
        if (arcRenderer != null)
        {
            arcRenderer.enabled = false;
            arcRenderer.SetPropertyBlock(null);
        }
    }

    private void OnValidate()
    {
        if (arcRenderer == null) arcRenderer = GetComponentInChildren<MeshRenderer>();
    }
}
