using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("VFX/Sprite Directional Fade VFX")]
public sealed class SpriteDirectionalFadeVFX : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("재생")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField, Min(0.01f)] private float duration = 0.18f;
    [Tooltip("0이면 완전히 보이고 1이면 완전히 사라진다.")]
    [SerializeField] private AnimationCurve fadeCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private bool disableRendererOnComplete = true;

    [Header("애니메이션으로 진행도 제어")]
    [Tooltip("켜면 내부 타이머를 쓰지 않고 아래 진행도 값을 매 프레임 그대로 반영한다. " +
        "Animation 창에서 이 오브젝트의 진행도에만 키를 찍을 수 있고, " +
        "값은 MaterialPropertyBlock으로만 들어가므로 공유 머티리얼 에셋은 건드리지 않는다.")]
    [SerializeField] private bool driveFromAnimation;
    [Tooltip("0이면 완전히 보이고 1이면 완전히 사라진다. Animation 창에서 키프레임할 값.")]
    [SerializeField, Range(0f, 1f)] private float animatedFadeProgress;

    [Header("선택적 머티리얼 값 덮어쓰기")]
    [Tooltip("끄면 머티리얼에 저장된 형태 값을 그대로 쓰고 진행도만 제어한다.")]
    [SerializeField] private bool overrideFadeSettings;
    [Tooltip("페이드가 진행되는 방향. 와이퍼 모드에서는 무시된다.")]
    [SerializeField] private Vector2 fadeDirection = Vector2.right;
    [Tooltip("경계가 번지는 폭.")]
    [SerializeField, Range(0.001f, 0.5f)] private float fadeSoftness = 0.12f;
    [Tooltip("1보다 크면 경계가 늦게, 작으면 빠르게 사라진다.")]
    [SerializeField, Range(0.1f, 8f)] private float fadePower = 1f;
    [SerializeField] private bool reverseDirection;

    [Header("피벗 와이퍼 페이드")]
    [Tooltip("켜면 방향 페이드 대신 피벗을 중심으로 회전하며 지워진다.")]
    [SerializeField] private bool wiperFade;
    [Tooltip("스프라이트 로컬 좌표 기준 회전 중심.")]
    [SerializeField] private Vector2 wiperPivotOffset;
    [SerializeField, Range(-180f, 180f)] private float wiperStartAngle;
    [SerializeField] private bool reverseWiperRotation;

    private static readonly int FadeProgressId = Shader.PropertyToID("_FadeProgress");
    private static readonly int FadeDirectionId = Shader.PropertyToID("_FadeDirection");
    private static readonly int FadeSoftnessId = Shader.PropertyToID("_FadeSoftness");
    private static readonly int FadePowerId = Shader.PropertyToID("_FadePower");
    private static readonly int ReverseId = Shader.PropertyToID("_Reverse");
    private static readonly int WiperFadeId = Shader.PropertyToID("_WiperFade");
    private static readonly int WiperPivotOffsetId =
        Shader.PropertyToID("_WiperPivotOffset");
    private static readonly int WiperStartAngleId =
        Shader.PropertyToID("_WiperStartAngle");
    private static readonly int WiperReverseRotationId =
        Shader.PropertyToID("_WiperReverseRotation");

    // MaterialPropertyBlock은 MonoBehaviour 필드 초기화 시점에 만들 수 없으므로
    // 실제로 필요할 때 지연 생성한다.
    private MaterialPropertyBlock propertyBlock;
    private float elapsed;
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
            Apply(animatedFadeProgress);
            return;
        }
        if (!Application.isPlaying) { ResetVisual(); return; }
        if (playOnEnable) Play();
        else ResetVisual();
    }

    private void OnDisable()
    {
        playing = false;
    }

    /// <summary>
    /// 애니메이션 구동 모드에서는 Animator가 필드를 쓴 뒤에 반영해야 하므로
    /// Update가 아니라 LateUpdate에서 적용한다. 에디트 모드 미리보기도 여기서 처리된다.
    /// </summary>
    private void LateUpdate()
    {
        if (!driveFromAnimation) return;
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        Apply(animatedFadeProgress);
    }

    private void Update()
    {
        if (driveFromAnimation || !Application.isPlaying) return;
        if (!playing || targetRenderer == null) return;
        elapsed += Time.deltaTime;
        if (elapsed < startDelay) return;

        float progress = Mathf.Clamp01((elapsed - startDelay) / duration);
        Apply(EvaluateFade(progress));
        if (progress < 1f) return;

        playing = false;
        if (disableRendererOnComplete) targetRenderer.enabled = false;
    }

    public void Play()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        elapsed = 0f;
        playing = true;
        targetRenderer.enabled = true;
        Apply(EvaluateFade(0f));
    }

    public void Play(float durationOverride)
    {
        duration = Mathf.Max(0.01f, durationOverride);
        Play();
    }

    /// <summary>재생을 멈추고 완전히 보이는 상태로 되돌린다.</summary>
    public void ResetVisual()
    {
        playing = false;
        elapsed = 0f;
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null) return;
        targetRenderer.enabled = true;
        Apply(0f);
    }

    private float EvaluateFade(float progress)
    {
        return fadeCurve != null
            ? Mathf.Clamp01(fadeCurve.Evaluate(progress))
            : progress;
    }

    private void Apply(float fadeProgress)
    {
        if (targetRenderer == null) return;
        MaterialPropertyBlock properties = Properties;
        targetRenderer.GetPropertyBlock(properties);
        properties.SetFloat(FadeProgressId, Mathf.Clamp01(fadeProgress));
        if (overrideFadeSettings)
        {
            properties.SetVector(FadeDirectionId, fadeDirection);
            properties.SetFloat(FadeSoftnessId, fadeSoftness);
            properties.SetFloat(FadePowerId, fadePower);
            properties.SetFloat(ReverseId, reverseDirection ? 1f : 0f);
            properties.SetFloat(WiperFadeId, wiperFade ? 1f : 0f);
            properties.SetVector(WiperPivotOffsetId, wiperPivotOffset);
            properties.SetFloat(WiperStartAngleId, wiperStartAngle);
            properties.SetFloat(WiperReverseRotationId, reverseWiperRotation ? 1f : 0f);
        }
        targetRenderer.SetPropertyBlock(properties);
    }

    private void OnValidate()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        startDelay = Mathf.Max(0f, startDelay);
        duration = Mathf.Max(0.01f, duration);
        fadeSoftness = Mathf.Clamp(fadeSoftness, 0.001f, 0.5f);
        fadePower = Mathf.Clamp(fadePower, 0.1f, 8f);
        wiperStartAngle = Mathf.Clamp(wiperStartAngle, -180f, 180f);
    }
}
