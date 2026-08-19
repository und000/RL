using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("피해 잔상")]
    [SerializeField] private Color damageTrailColor = Color.white;
    [SerializeField, Min(0f)] private float damageTrailDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float damageTrailDuration = 0.6f;

    private RectTransform damageTrailTransform;
    private float displayedTrailHealth;
    private float trailMoveStartTime;

    private void Start()
    {
        if (playerHealth == null || healthSlider == null || healthText == null)
        {
            Debug.LogError("Player Health UI에 필요한 오브젝트가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        DisplayOnlyUI.Configure(healthSlider);
        CreateDamageTrail();
        displayedTrailHealth = playerHealth.GetCurrentHealth();
        playerHealth.OnHealthChanged += UpdateHealthUI;
        UpdateHealthUI();
    }

    private void Update()
    {
        if (damageTrailTransform == null || playerHealth == null)
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

    private void CreateDamageTrail()
    {
        RectTransform normalFill = healthSlider.fillRect;
        GameObject trailObject = new GameObject(
            "DamageTrail",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        damageTrailTransform = trailObject.GetComponent<RectTransform>();
        damageTrailTransform.SetParent(normalFill.parent, false);
        damageTrailTransform.SetSiblingIndex(normalFill.GetSiblingIndex());
        damageTrailTransform.anchorMin = Vector2.zero;
        damageTrailTransform.anchorMax = Vector2.one;
        damageTrailTransform.offsetMin = Vector2.zero;
        damageTrailTransform.offsetMax = Vector2.zero;

        Image trailImage = trailObject.GetComponent<Image>();
        trailImage.color = damageTrailColor;
        trailImage.raycastTarget = false;
    }

    private void UpdateDamageTrail()
    {
        float maxHealth = playerHealth.GetMaxHealth();
        float ratio = maxHealth > 0f
            ? Mathf.Clamp01(displayedTrailHealth / maxHealth)
            : 0f;
        damageTrailTransform.anchorMax = new Vector2(ratio, 1f);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }
}
