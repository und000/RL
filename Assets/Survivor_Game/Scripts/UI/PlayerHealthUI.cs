using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("UI/Player Health UI")]
public class PlayerHealthUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 Player 태그가 붙은 오브젝트에서 찾는다.")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("피해 잔상")]
    [Tooltip("줄어든 체력이 천천히 따라오는 띠. 프리팹에서 채움 막대 뒤에 깔아 두고 연결한다.")]
    [SerializeField] private RectTransform damageTrail;
    [SerializeField] private Color damageTrailColor = Color.white;
    [SerializeField, Min(0f)] private float damageTrailDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float damageTrailDuration = 0.6f;

    private float displayedTrailHealth;
    private float trailMoveStartTime;

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        GameFontManager.ApplyFont(healthText);
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            playerHealth = player != null
                ? player.GetComponentInChildren<PlayerHealth>(true) : null;
        }

        if (playerHealth == null || healthSlider == null || healthText == null ||
            damageTrail == null)
        {
            Debug.LogError(
                "Player Health UI에 필요한 오브젝트가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        DisplayOnlyUI.Configure(healthSlider);
        // 색은 인스펙터 값이 이기게 한다. 프리팹에 저장된 색만 쓰면
        // Damage Trail Color를 만져도 아무 일이 없어 헷갈린다.
        Image trailImage = damageTrail.GetComponent<Image>();
        if (trailImage != null) trailImage.color = damageTrailColor;
        displayedTrailHealth = playerHealth.GetCurrentHealth();
        playerHealth.OnHealthChanged += UpdateHealthUI;
        UpdateHealthUI();
    }

    private void Update()
    {
        if (playerHealth == null)
        {
            return;
        }

        float currentHealth = playerHealth.GetCurrentHealth();
        if (Time.time >= trailMoveStartTime && displayedTrailHealth > currentHealth)
        {
            float speed = playerHealth.GetMaxHealth() / damageTrailDuration;
            displayedTrailHealth = Mathf.MoveTowards(
                displayedTrailHealth,
                currentHealth,
                speed * Time.deltaTime
            );
            UpdateDamageTrail();
        }
    }

    private void UpdateHealthUI()
    {
        int currentHealth = playerHealth.GetCurrentHealth();
        int maxHealth = playerHealth.GetMaxHealth();

        if (currentHealth < displayedTrailHealth)
        {
            trailMoveStartTime = Time.time + damageTrailDelay;
        }
        else if (currentHealth > displayedTrailHealth)
        {
            displayedTrailHealth = currentHealth;
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        healthText.text = $"{currentHealth} / {maxHealth}";
        UpdateDamageTrail();
    }

    public float GetRecoverableTrailHealth()
    {
        return Mathf.Max(0f, displayedTrailHealth - playerHealth.GetCurrentHealth());
    }

    private void UpdateDamageTrail()
    {
        float maxHealth = playerHealth.GetMaxHealth();
        float ratio = maxHealth > 0f
            ? Mathf.Clamp01(displayedTrailHealth / maxHealth)
            : 0f;
        damageTrail.anchorMax = new Vector2(ratio, 1f);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }
}
