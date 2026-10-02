using UnityEngine;

/// <summary>
/// 바닥에 놓인 물건이 자기 등급을 내보이는 연출. 등급이 높아질수록
/// 빛이 세지고 고리가 하나씩 붙는다. 색은 ItemGradeInfo에서만 온다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("VFX/Item Grade Visual")]
public class ItemGradeVisual : MonoBehaviour
{
    [Header("구성 요소")]
    [Tooltip("바닥에 퍼지는 빛. 항상 켜진다.")]
    [SerializeField] private Renderer glowRenderer;
    [Tooltip("천천히 도는 육각 고리. 일정 등급부터 켜진다.")]
    [SerializeField] private Renderer outerRingRenderer;
    [Tooltip("반대로 도는 점선 고리. 더 높은 등급부터 켜진다.")]
    [SerializeField] private Renderer innerRingRenderer;

    [Header("등급 문턱")]
    [Tooltip("이 티어부터 육각 고리가 붙는다. 규격 0, 특이 5.")]
    [SerializeField, Range(0, 5)] private int outerRingFromTier = 2;
    [Tooltip("이 티어부터 점선 고리가 하나 더 붙는다.")]
    [SerializeField, Range(0, 5)] private int innerRingFromTier = 4;

    [Header("빛")]
    [Tooltip("가장 낮은 등급에서의 빛 세기.")]
    [SerializeField, Range(0f, 1f)] private float minimumGlowAlpha = 0.18f;
    [Tooltip("가장 높은 등급에서의 빛 세기.")]
    [SerializeField, Range(0f, 1f)] private float maximumGlowAlpha = 0.62f;
    [Tooltip("빛이 커졌다 작아지는 폭. 0이면 숨쉬지 않는다.")]
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.16f;
    [Tooltip("가장 낮은 등급에서의 숨쉬는 속도.")]
    [SerializeField, Min(0f)] private float minimumPulseSpeed = 1.2f;
    [Tooltip("가장 높은 등급에서의 숨쉬는 속도.")]
    [SerializeField, Min(0f)] private float maximumPulseSpeed = 3.4f;

    [Header("고리")]
    [Tooltip("육각 고리가 도는 속도(도/초). 등급이 높아지면 빨라진다.")]
    [SerializeField] private float outerRingSpeed = 26f;
    [Tooltip("점선 고리가 도는 속도. 반대로 돌도록 부호를 반대로 둔다.")]
    [SerializeField] private float innerRingSpeed = -44f;
    [SerializeField, Range(0f, 1f)] private float ringAlpha = 0.75f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private ItemGrade grade = ItemGrade.Standard;
    private float strength;
    private float pulseSpeed;
    private float glowAlpha;
    private Vector3 glowBaseScale = Vector3.one;
    private bool configured;

    /// <summary>등급을 받아 색과 세기를 정한다. 놓이는 순간 한 번 부르면 된다.</summary>
    public void Apply(ItemGrade itemGrade)
    {
        grade = itemGrade;
        strength = ItemGradeInfo.Strength(grade);
        int tier = ItemGradeInfo.Tier(grade);
        Color color = ItemGradeInfo.Color(grade);

        pulseSpeed = Mathf.Lerp(minimumPulseSpeed, maximumPulseSpeed, strength);
        glowAlpha = Mathf.Lerp(minimumGlowAlpha, maximumGlowAlpha, strength);

        if (glowRenderer != null)
        {
            if (!configured) glowBaseScale = glowRenderer.transform.localScale;
            // 등급이 높으면 빛 자체도 조금 넓게 퍼진다.
            glowRenderer.transform.localScale =
                glowBaseScale * Mathf.Lerp(0.85f, 1.35f, strength);
            SetColor(glowRenderer, color, glowAlpha);
        }

        SetRing(outerRingRenderer, color, tier >= outerRingFromTier);
        SetRing(innerRingRenderer, color, tier >= innerRingFromTier);
        configured = true;
    }

    private void SetRing(Renderer renderer, Color color, bool visible)
    {
        if (renderer == null) return;

        renderer.enabled = visible;
        if (visible) SetColor(renderer, color, ringAlpha);
    }

    /// <summary>공유 머테리얼을 건드리지 않도록 이 렌더러에만 색을 넣는다.</summary>
    private void SetColor(Renderer renderer, Color color, float alpha)
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorId, new Color(color.r, color.g, color.b, alpha));
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void Update()
    {
        if (!configured) return;

        float wave = Mathf.Sin(Time.time * pulseSpeed);

        if (glowRenderer != null)
        {
            Color color = ItemGradeInfo.Color(grade);
            SetColor(glowRenderer, color, glowAlpha * (1f + pulseAmount * wave));
            glowRenderer.transform.localScale = glowBaseScale *
                Mathf.Lerp(0.85f, 1.35f, strength) * (1f + pulseAmount * 0.5f * wave);
        }

        float speedScale = Mathf.Lerp(0.6f, 1.6f, strength);
        Rotate(outerRingRenderer, outerRingSpeed * speedScale);
        Rotate(innerRingRenderer, innerRingSpeed * speedScale);
    }

    private void Rotate(Renderer renderer, float degreesPerSecond)
    {
        if (renderer == null || !renderer.enabled) return;
        renderer.transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
