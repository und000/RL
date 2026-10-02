using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("VFX/Sprite Fracture Dissolve VFX")]
public sealed class SpriteFractureDissolveVFX : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("재생")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField, Min(0.01f)] private float duration = 0.18f;
    [SerializeField] private AnimationCurve dissolveCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private bool disableRendererOnComplete = true;
    [SerializeField] private bool randomizeNoiseOffset;

    [Header("애니메이션으로 진행도 제어")]
    [Tooltip("켜면 내부 타이머를 쓰지 않고 아래 진행도 값을 매 프레임 그대로 반영한다. " +
        "Animation 창에서 이 오브젝트의 진행도에만 키를 찍을 수 있고, " +
        "값은 MaterialPropertyBlock으로만 들어가므로 공유 머티리얼 에셋은 건드리지 않는다.")]
    [SerializeField] private bool driveFromAnimation;
    [Tooltip("0이면 온전하고 1이면 완전히 사라진다. Animation 창에서 키프레임할 값.")]
    [SerializeField, Range(0f, 1f)] private float animatedDissolve;

    [Header("선택적 머티리얼 값 덮어쓰기")]
    [SerializeField] private bool overrideFractureSettings;
    [SerializeField] private Texture2D noiseTexture;
    [SerializeField] private Vector2 noiseTiling = Vector2.one;
    [SerializeField, Range(-180f, 180f)] private float noiseAngle;
    [SerializeField, Range(-150f, 150f)] private float noiseArcBendAngle = 60f;
    [SerializeField] private Vector2 noiseArcBendCenter = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 noiseScrollSpeed = new Vector2(0.8f, 0f);
    [SerializeField, Range(0.1f, 4f)] private float noiseContrast = 1.35f;
    [SerializeField, Range(0.001f, 0.2f)] private float holeSoftness = 0.055f;
    [SerializeField, Range(0f, 1f)] private float directionalInfluence = 0.7f;
    [SerializeField] private Vector2 dissolveDirection = Vector2.right;
    [SerializeField] private bool reverse;
    [SerializeField, Range(0f, 1f)] private float shapeRoundness = 1f;
    [SerializeField] private Vector2 radialCenter = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 radialAspect = Vector2.one;
    [SerializeField] private bool radialOutsideIn;

    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    private static readonly int NoiseOffsetId = Shader.PropertyToID("_NoiseOffset");
    private static readonly int NoiseTextureId = Shader.PropertyToID("_NoiseTex");
    private static readonly int NoiseTilingId = Shader.PropertyToID("_NoiseTiling");
    private static readonly int NoiseAngleId = Shader.PropertyToID("_NoiseAngle");
    private static readonly int NoiseArcBendAngleId =
        Shader.PropertyToID("_NoiseArcBendAngle");
    private static readonly int NoiseArcBendCenterId =
        Shader.PropertyToID("_NoiseArcBendCenter");
    private static readonly int NoiseScrollSpeedId =
        Shader.PropertyToID("_NoiseScrollSpeed");
    private static readonly int NoiseContrastId = Shader.PropertyToID("_NoiseContrast");
    private static readonly int HoleSoftnessId = Shader.PropertyToID("_HoleSoftness");
    private static readonly int DirectionalInfluenceId =
        Shader.PropertyToID("_DirectionalInfluence");
    private static readonly int DissolveDirectionId =
        Shader.PropertyToID("_DissolveDirection");
    private static readonly int ReverseId = Shader.PropertyToID("_Reverse");
    private static readonly int ShapeRoundnessId = Shader.PropertyToID("_ShapeRoundness");
    private static readonly int RadialCenterId = Shader.PropertyToID("_RadialCenter");
    private static readonly int RadialAspectId = Shader.PropertyToID("_RadialAspect");
    private static readonly int RadialInvertId = Shader.PropertyToID("_RadialInvert");

    // MaterialPropertyBlock은 MonoBehaviour 필드 초기화 시점에 만들 수 없으므로
    // 실제로 필요할 때 지연 생성한다.
    private MaterialPropertyBlock propertyBlock;
    private float elapsed;
    private Vector2 noiseOffset;
    private bool playing;

    private MaterialPropertyBlock Properties
    {
        get
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            return propertyBlock;
        }
    }

    public bool IsPlaying => playing;

    public void Configure(WeaponDissolveSettings settings)
    {
        if (settings == null) return;
        duration = Mathf.Max(0.01f, settings.duration);
        noiseTexture = settings.noiseTexture;
        noiseTiling = settings.noiseTiling;
        noiseAngle = settings.noiseAngle;
        noiseArcBendAngle = settings.noiseArcBendAngle;
        noiseArcBendCenter = settings.noiseArcBendCenter;
        noiseScrollSpeed = settings.noiseScrollSpeed;
        noiseContrast = settings.noiseContrast;
        randomizeNoiseOffset = settings.randomizeNoiseOffset;
        noiseOffset = randomizeNoiseOffset
            ? new Vector2(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f))
            : Vector2.zero;
        holeSoftness = settings.softness;
        directionalInfluence = settings.progressSpread;
        dissolveDirection = settings.dissolveDirection;
        reverse = settings.reverseDirection;
        shapeRoundness = settings.shapeRoundness;
        radialCenter = settings.radialCenter;
        radialAspect = settings.radialAspect;
        radialOutsideIn = settings.radialOutsideIn;
        overrideFractureSettings = true;
        ApplyCurrentVisual();
    }

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (driveFromAnimation)
        {
            // 렌더러 on/off는 애니메이션(Sprite Renderer.Enabled)이 소유한다.
            // 여기서 강제로 켜면 클립의 m_Enabled 키와 충돌한다.
            playing = false;
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            Apply(animatedDissolve);
            return;
        }
        if (!Application.isPlaying) { ResetVisual(); return; }
        if (playOnEnable) Play();
        else ResetVisual();
    }

    /// <summary>
    /// 애니메이션 구동 모드에서는 Animator가 필드를 쓴 뒤에 반영해야 하므로
    /// Update가 아니라 LateUpdate에서 적용한다. 에디트 모드 미리보기도 여기서 처리된다.
    /// </summary>
    private void LateUpdate()
    {
        if (!driveFromAnimation) return;
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        Apply(animatedDissolve);
    }

    private void Update()
    {
        if (driveFromAnimation || !Application.isPlaying) return;
        if (!playing || targetRenderer == null) return;
        elapsed += Time.deltaTime;
        if (elapsed < startDelay) return;

        float progress = Mathf.Clamp01((elapsed - startDelay) / duration);
        float dissolve = dissolveCurve != null
            ? Mathf.Clamp01(dissolveCurve.Evaluate(progress))
            : progress;
        Apply(dissolve);
        if (progress < 1f) return;

        playing = false;
        if (disableRendererOnComplete) targetRenderer.enabled = false;
    }

    public void Play()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        elapsed = 0f;
        noiseOffset = randomizeNoiseOffset
            ? new Vector2(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f))
            : Vector2.zero;
        playing = true;
        targetRenderer.enabled = true;
        Apply(0f);
    }

    public void Play(float durationOverride)
    {
        duration = Mathf.Max(0.01f, durationOverride);
        Play();
    }

    public void ResetVisual()
    {
        playing = false;
        elapsed = 0f;
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null) return;
        targetRenderer.enabled = true;
        noiseOffset = Vector2.zero;
        Apply(0f);
    }

    private void Apply(float dissolve)
    {
        if (targetRenderer == null) return;
        MaterialPropertyBlock properties = Properties;
        targetRenderer.GetPropertyBlock(properties);
        properties.SetFloat(DissolveId, Mathf.Clamp01(dissolve));
        properties.SetVector(NoiseOffsetId, noiseOffset);
        if (overrideFractureSettings)
        {
            if (noiseTexture != null) properties.SetTexture(NoiseTextureId, noiseTexture);
            properties.SetVector(NoiseTilingId, noiseTiling);
            properties.SetFloat(NoiseAngleId, noiseAngle);
            properties.SetFloat(NoiseArcBendAngleId, noiseArcBendAngle);
            properties.SetVector(NoiseArcBendCenterId, noiseArcBendCenter);
            properties.SetVector(NoiseScrollSpeedId, noiseScrollSpeed);
            properties.SetFloat(NoiseContrastId, noiseContrast);
            properties.SetFloat(HoleSoftnessId, holeSoftness);
            properties.SetFloat(DirectionalInfluenceId, directionalInfluence);
            properties.SetVector(DissolveDirectionId, dissolveDirection);
            properties.SetFloat(ReverseId, reverse ? 1f : 0f);
            properties.SetFloat(ShapeRoundnessId, shapeRoundness);
            properties.SetVector(RadialCenterId, radialCenter);
            properties.SetVector(RadialAspectId, radialAspect);
            properties.SetFloat(RadialInvertId, radialOutsideIn ? 1f : 0f);
        }
        targetRenderer.SetPropertyBlock(properties);
    }

    private void OnDisable()
    {
        playing = false;
    }

    private void ApplyCurrentVisual()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null) return;
        float progress = playing && elapsed >= startDelay
            ? Mathf.Clamp01((elapsed - startDelay) / duration)
            : 0f;
        float dissolve = dissolveCurve != null
            ? Mathf.Clamp01(dissolveCurve.Evaluate(progress))
            : progress;
        Apply(dissolve);
    }

    private void OnValidate()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        startDelay = Mathf.Max(0f, startDelay);
        duration = Mathf.Max(0.01f, duration);
        noiseTiling.x = Mathf.Max(0.01f, Mathf.Abs(noiseTiling.x));
        noiseTiling.y = Mathf.Max(0.01f, Mathf.Abs(noiseTiling.y));
        noiseAngle = Mathf.Clamp(noiseAngle, -180f, 180f);
        noiseArcBendAngle = Mathf.Clamp(noiseArcBendAngle, -150f, 150f);
        noiseArcBendCenter.x = Mathf.Clamp01(noiseArcBendCenter.x);
        noiseArcBendCenter.y = Mathf.Clamp01(noiseArcBendCenter.y);
        noiseContrast = Mathf.Clamp(noiseContrast, 0.1f, 4f);
        holeSoftness = Mathf.Clamp(holeSoftness, 0.001f, 0.2f);
        directionalInfluence = Mathf.Clamp01(directionalInfluence);
        shapeRoundness = Mathf.Clamp01(shapeRoundness);
        radialCenter.x = Mathf.Clamp01(radialCenter.x);
        radialCenter.y = Mathf.Clamp01(radialCenter.y);
        radialAspect.x = Mathf.Max(0.01f, Mathf.Abs(radialAspect.x));
        radialAspect.y = Mathf.Max(0.01f, Mathf.Abs(radialAspect.y));
    }
}
