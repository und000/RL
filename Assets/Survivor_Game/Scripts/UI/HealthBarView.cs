using UnityEngine;

public enum HealthBarType
{
    Normal,
    Elite,
    Boss
}

[RequireComponent(typeof(CanvasGroup))]
public class HealthBarView : MonoBehaviour
{
    [SerializeField] private RectTransform fillTransform;
    [SerializeField] private RectTransform damageTrailTransform;
    [SerializeField, Min(0f)] private float damageTrailDelay = 0.25f;
    [SerializeField, Min(0.01f)] private float damageTrailDuration = 0.45f;
    [SerializeField, Min(0f)] private float deathDisplayDuration = 0.35f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private EnemyHealth targetHealth;
    private EnemyHealthBarManager manager;
    private Camera worldCamera;
    private HealthBarType healthBarType;
    private Vector3 worldOffset;
    private float visibleDuration;
    private float visibleUntil;
    private float trailMoveStartTime;
    private float displayedTrailHealth;
    private int currentHealth;
    private int maxHealth;
    private Vector3 trackedWorldPosition;
    private float deathReleaseTime;
    private bool targetDied;
    private bool releasing;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Initialize(
        EnemyHealth newTarget,
        HealthBarType newType,
        Vector3 newWorldOffset,
        float newVisibleDuration,
        EnemyHealthBarManager owner,
        Camera camera)
    {
        targetHealth = newTarget;
        healthBarType = newType;
        worldOffset = newWorldOffset;
        visibleDuration = newVisibleDuration;
        manager = owner;
        worldCamera = camera;
        targetDied = false;
        releasing = false;

        currentHealth = targetHealth.GetCurrentHealth();
        maxHealth = targetHealth.GetMaxHealth();
        displayedTrailHealth = currentHealth;
        trailMoveStartTime = 0f;
        visibleUntil = 0f;
        deathReleaseTime = 0f;
        CacheTrackedWorldPosition();

        targetHealth.OnDamaged += HandleDamaged;
        targetHealth.OnHealthChanged += HandleHealthChanged;
        targetHealth.OnDied += HandleTargetDied;

        UpdateBarFill();
        UpdateVisibility();
    }

    private void Update()
    {
        if (targetDied)
        {
            if (Time.unscaledTime >= deathReleaseTime)
            {
                RequestRelease();
                return;
            }
        }
        else if (targetHealth == null || !targetHealth.gameObject.activeInHierarchy)
        {
            RequestRelease();
            return;
        }
        else
        {
            CacheTrackedWorldPosition();
        }

        if (Time.unscaledTime >= trailMoveStartTime &&
            displayedTrailHealth > currentHealth)
        {
            displayedTrailHealth = Mathf.MoveTowards(
                displayedTrailHealth,
                currentHealth,
                maxHealth / damageTrailDuration * Time.unscaledDeltaTime
            );
            UpdateBarFill();
        }

        if (healthBarType != HealthBarType.Boss)
        {
            UpdateTrackedPosition();
        }

        UpdateVisibility();
    }

    private void HandleDamaged()
    {
        CacheTrackedWorldPosition();
        visibleUntil = Mathf.Max(
            visibleUntil,
            Time.unscaledTime + Mathf.Max(
                visibleDuration,
                damageTrailDelay + damageTrailDuration
            )
        );
        UpdateVisibility();
    }

    private void HandleHealthChanged(int newCurrentHealth, int newMaxHealth)
    {
        CacheTrackedWorldPosition();
        int previousHealth = currentHealth;
        currentHealth = newCurrentHealth;
        maxHealth = newMaxHealth;

        if (currentHealth < previousHealth)
        {
            displayedTrailHealth = Mathf.Max(displayedTrailHealth, previousHealth);
            trailMoveStartTime = Time.unscaledTime + damageTrailDelay;
            visibleUntil = Time.unscaledTime + Mathf.Max(
                visibleDuration,
                damageTrailDelay + damageTrailDuration
            );
        }
        else if (currentHealth > displayedTrailHealth)
        {
            displayedTrailHealth = currentHealth;
        }

        UpdateBarFill();
        UpdateVisibility();
    }

    private void UpdateTrackedPosition()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            return;
        }

        rectTransform.position = worldCamera.WorldToScreenPoint(trackedWorldPosition);
    }

    private void UpdateVisibility()
    {
        bool shouldShow = healthBarType == HealthBarType.Boss ||
            Time.unscaledTime < visibleUntil;

        if (shouldShow && healthBarType != HealthBarType.Boss && worldCamera != null)
        {
            Vector3 viewportPosition = worldCamera.WorldToViewportPoint(trackedWorldPosition);
            shouldShow = viewportPosition.z > 0f &&
                viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
                viewportPosition.y >= 0f && viewportPosition.y <= 1f;
        }

        canvasGroup.alpha = shouldShow ? 1f : 0f;
    }

    private void UpdateBarFill()
    {
        float healthRatio = maxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / maxHealth)
            : 0f;
        float trailRatio = maxHealth > 0
            ? Mathf.Clamp01(displayedTrailHealth / maxHealth)
            : 0f;

        fillTransform.anchorMax = new Vector2(healthRatio, 1f);
        damageTrailTransform.anchorMax = new Vector2(trailRatio, 1f);
    }

    private void HandleTargetDied()
    {
        CacheTrackedWorldPosition();
        targetDied = true;
        deathReleaseTime = Time.unscaledTime + deathDisplayDuration;
        visibleUntil = Mathf.Max(visibleUntil, deathReleaseTime);
        UnsubscribeFromTarget();
        UpdateBarFill();
        UpdateVisibility();

        if (deathDisplayDuration <= 0f)
        {
            RequestRelease();
        }
    }

    private void CacheTrackedWorldPosition()
    {
        if (targetHealth != null)
        {
            trackedWorldPosition = targetHealth.transform.position + worldOffset;
        }
    }

    private void UnsubscribeFromTarget()
    {
        if (targetHealth == null)
        {
            return;
        }

        targetHealth.OnDamaged -= HandleDamaged;
        targetHealth.OnHealthChanged -= HandleHealthChanged;
        targetHealth.OnDied -= HandleTargetDied;
    }

    public bool IsTracking(EnemyHealth health)
    {
        return !releasing && targetHealth == health;
    }

    public bool IsHoldingDeathDisplayFor(EnemyHealth health)
    {
        return IsTracking(health) && targetDied &&
            Time.unscaledTime < deathReleaseTime;
    }

    private void RequestRelease()
    {
        if (releasing || manager == null)
        {
            return;
        }

        releasing = true;
        manager.Release(this, healthBarType);
    }

    public void ResetView()
    {
        UnsubscribeFromTarget();

        targetHealth = null;
        manager = null;
        targetDied = false;
        canvasGroup.alpha = 0f;
    }
}
