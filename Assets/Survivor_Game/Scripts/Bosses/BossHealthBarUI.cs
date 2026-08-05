using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyHealth))]
public class BossHealthBarUI : MonoBehaviour
{
    [Header("위치와 크기")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -45f);
    [SerializeField, Min(100f)] private float barWidth = 600f;
    [SerializeField, Min(10f)] private float barHeight = 32f;

    [Header("색상")]
    [SerializeField] private Color backgroundColor = new Color(0.04f, 0.04f, 0.04f, 0.95f);
    [SerializeField] private Color fillColor = new Color(0.65f, 0.05f, 0.12f, 1f);
    [SerializeField] private Color damageTrailColor = Color.white;

    [Header("피해 잔상")]
    [SerializeField, Min(0f)] private float damageTrailDelay = 0.4f;
    [SerializeField, Min(0.01f)] private float damageTrailDuration = 0.8f;

    private EnemyHealth enemyHealth;
    private GameObject canvasObject;
    private RectTransform fillTransform;
    private RectTransform damageTrailTransform;
    private int currentHealth;
    private int maxHealth;
    private float displayedTrailHealth;
    private float trailMoveStartTime;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        currentHealth = enemyHealth.GetCurrentHealth();
        maxHealth = enemyHealth.GetMaxHealth();
        displayedTrailHealth = currentHealth;
        CreateHealthBar();
        UpdateBarFill();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (Time.time < trailMoveStartTime || displayedTrailHealth <= currentHealth)
        {
            return;
        }

        displayedTrailHealth = Mathf.MoveTowards(
            displayedTrailHealth,
            currentHealth,
            maxHealth / damageTrailDuration * Time.deltaTime
        );
        UpdateBarFill();
    }

    private void HandleHealthChanged(int newCurrentHealth, int newMaxHealth)
    {
        int previousHealth = currentHealth;
        currentHealth = newCurrentHealth;
        maxHealth = newMaxHealth;

        if (currentHealth < previousHealth)
        {
            displayedTrailHealth = Mathf.Max(displayedTrailHealth, previousHealth);
            trailMoveStartTime = Time.time + damageTrailDelay;
        }
        else if (currentHealth > displayedTrailHealth)
        {
            displayedTrailHealth = currentHealth;
        }

        UpdateBarFill();
    }

    public float GetRecoverableTrailHealth()
    {
        return Mathf.Max(0f, displayedTrailHealth - currentHealth);
    }

    private void CreateHealthBar()
    {
        canvasObject = new GameObject("BossHealthCanvas", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        GameObject barObject = new GameObject("BossHealthBar", typeof(RectTransform));
        RectTransform barTransform = barObject.GetComponent<RectTransform>();
        barTransform.SetParent(canvasObject.transform, false);
        barTransform.anchorMin = new Vector2(0.5f, 1f);
        barTransform.anchorMax = new Vector2(0.5f, 1f);
        barTransform.pivot = new Vector2(0.5f, 1f);
        barTransform.anchoredPosition = anchoredPosition;
        barTransform.sizeDelta = new Vector2(barWidth, barHeight);

        CreateImage("Background", barTransform, backgroundColor);
        damageTrailTransform = CreateImage("DamageTrail", barTransform, damageTrailColor).rectTransform;
        fillTransform = CreateImage("Fill", barTransform, fillColor).rectTransform;
        damageTrailTransform.pivot = new Vector2(0f, 0.5f);
        fillTransform.pivot = new Vector2(0f, 0.5f);
    }

    private void UpdateBarFill()
    {
        float healthRatio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
        float trailRatio = maxHealth > 0 ? Mathf.Clamp01(displayedTrailHealth / maxHealth) : 0f;
        fillTransform.anchorMax = new Vector2(healthRatio, 1f);
        damageTrailTransform.anchorMax = new Vector2(trailRatio, 1f);
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
        imageTransform.SetParent(parent, false);
        imageTransform.anchorMin = Vector2.zero;
        imageTransform.anchorMax = Vector2.one;
        imageTransform.offsetMin = Vector2.zero;
        imageTransform.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }
    }
}
