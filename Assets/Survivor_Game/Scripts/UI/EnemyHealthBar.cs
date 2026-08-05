using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("표시 시간")]
    [SerializeField, Min(0f)] private float visibleDuration = 2f;

    [Header("위치와 크기")]
    [SerializeField] private Vector2 barOffset = new Vector2(0f, 1.2f);
    [SerializeField, Min(0.1f)] private float barWidth = 1.4f;
    [SerializeField, Min(0.02f)] private float barHeight = 0.16f;

    [Header("색상")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
    [SerializeField] private Color fillColor = new Color(0.9f, 0.12f, 0.12f, 1f);
    [SerializeField] private int sortingOrder = 20;

    [Header("피해 잔상")]
    [SerializeField] private Color damageTrailColor = Color.white;
    [SerializeField, Min(0f)] private float damageTrailDelay = 0.25f;
    [SerializeField, Min(0.01f)] private float damageTrailDuration = 0.45f;

    private int currentHealth;
    private int maxHealth;
    private EnemyHealth enemyHealth;
    private float hideTime;
    private GameObject barRoot;
    private RectTransform fillTransform;
    private RectTransform damageTrailTransform;
    private float displayedTrailHealth;
    private float trailMoveStartTime;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth == null)
        {
            Debug.LogError($"{name}에 EnemyHealth가 없습니다.");
            enabled = false;
            return;
        }

        currentHealth = enemyHealth.GetCurrentHealth();
        maxHealth = enemyHealth.GetMaxHealth();
        displayedTrailHealth = currentHealth;
        CreateHealthBar();
        UpdateBarFill();
        barRoot.SetActive(false);
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
        if (Time.time >= trailMoveStartTime && displayedTrailHealth > currentHealth)
        {
            float speed = maxHealth / damageTrailDuration;
            displayedTrailHealth = Mathf.MoveTowards(
                displayedTrailHealth,
                currentHealth,
                speed * Time.deltaTime
            );
            UpdateBarFill();
        }

        if (barRoot != null && barRoot.activeSelf && Time.time >= hideTime)
        {
            barRoot.SetActive(false);
        }
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
        if (currentHealth > 0)
        {
            barRoot.SetActive(true);
            hideTime = Time.time + Mathf.Max(
                visibleDuration,
                damageTrailDelay + damageTrailDuration
            );
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetRecoverableTrailHealth() => Mathf.Max(0f, displayedTrailHealth - currentHealth);

    private void CreateHealthBar()
    {
        barRoot = new GameObject("EnemyHealthBar", typeof(RectTransform));
        RectTransform barTransform = barRoot.GetComponent<RectTransform>();
        barTransform.SetParent(transform, false);
        barTransform.localPosition = barOffset;
        barTransform.sizeDelta = new Vector2(100f, 10f);

        Vector3 parentScale = transform.lossyScale;
        barTransform.localScale = new Vector3(
            barWidth / (100f * Mathf.Max(Mathf.Abs(parentScale.x), 0.001f)),
            barHeight / (10f * Mathf.Max(Mathf.Abs(parentScale.y), 0.001f)),
            1f
        );

        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CreateImage("Background", barTransform, backgroundColor, Vector2.zero, Vector2.one);
        Image trailImage = CreateImage("DamageTrail", barTransform, damageTrailColor, Vector2.zero, Vector2.one);
        damageTrailTransform = trailImage.rectTransform;
        damageTrailTransform.pivot = new Vector2(0f, 0.5f);
        Image fillImage = CreateImage("Fill", barTransform, fillColor, Vector2.zero, Vector2.one);
        fillTransform = fillImage.rectTransform;
        fillTransform.pivot = new Vector2(0f, 0.5f);
    }

    private void UpdateBarFill()
    {
        float healthRatio = maxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / maxHealth)
            : 0f;
        float trailRatio = maxHealth > 0
            ? Mathf.Clamp01(displayedTrailHealth / maxHealth)
            : 0f;
        damageTrailTransform.anchorMax = new Vector2(trailRatio, 1f);
        fillTransform.anchorMax = new Vector2(healthRatio, 1f);
    }

    private static Image CreateImage(
        string objectName,
        Transform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
        imageTransform.SetParent(parent, false);
        imageTransform.anchorMin = anchorMin;
        imageTransform.anchorMax = anchorMax;
        imageTransform.offsetMin = Vector2.zero;
        imageTransform.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
