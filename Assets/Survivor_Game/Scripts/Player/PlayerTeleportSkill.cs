using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[RequireComponent(typeof(PlayerHealth), typeof(SpriteRenderer))]
[AddComponentMenu("Player/Player Teleport Skill")]
public class PlayerTeleportSkill : MonoBehaviour
{
    [Header("순간이동 거리")]
    [SerializeField, Min(0.1f)] private float maximumDistance = 10f;
    [SerializeField, Min(0f)] private float collisionSkin = 0.05f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("조준 범위 및 시간")]
    [SerializeField, Min(0.1f)] private float effectRadius = 3f;
    [SerializeField, Range(0.01f, 1f)] private float aimingTimeScale = 0.15f;
    [SerializeField, Min(0.01f)] private float slowDownDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float timeScaleRecoveryDuration = 1f;

    [Header("시각 효과 프로필")]
    [SerializeField] private TeleportVisualProfile visualProfile;

    [Header("도착 효과")]
    [SerializeField, Min(0)] private int damage;
    [SerializeField, Min(0f)] private float knockbackStrength = 15f;
    [SerializeField, Min(0f)] private float knockbackDuration = 0.35f;
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.6f;

    [Header("재사용 대기시간")]
    [SerializeField, Min(0f)] private float cooldown = 3f;

    [Header("Cooldown Ready Effect")]
    [SerializeField] private Color cooldownReadyColor = new Color(0.1f, 0.55f, 1f, 0.5f);
    [SerializeField, Min(0.01f)] private float cooldownReadyFlashDuration = 0.25f;

    public event Action<Vector2, float> OnArrivalAreaEffect;
    public TeleportVisualProfile VisualProfile => visualProfile;

    private readonly List<RaycastHit2D> castResults = new List<RaycastHit2D>(8);
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(32);
    private readonly HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private PlayerHealth playerHealth;
    private SpriteRenderer playerRenderer;
    private Camera worldCamera;
    private CameraFollow cameraFollow;
    private ContactFilter2D obstacleFilter;
    private ContactFilter2D enemyFilter;
    private TeleportTargetRangeVisual targetRangeVisual;
    private TeleportAimLineVisual aimLineVisual;
    private Vector2 targetPosition;
    private float defaultFixedDeltaTime;
    private float timeScaleBeforeAim;
    private float fixedDeltaBeforeAim;
    private float nextReadyTime;
    private float aimingElapsedTime;
    private bool aiming;
    private bool cooldownFlashPlayed = true;
    private Coroutine cooldownReadyFlashRoutine;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerHealth = GetComponent<PlayerHealth>();
        playerRenderer = GetComponent<SpriteRenderer>();
        worldCamera = Camera.main;
        cameraFollow = worldCamera != null ? worldCamera.GetComponent<CameraFollow>() : null;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        timeScaleBeforeAim = Time.timeScale;
        fixedDeltaBeforeAim = Time.fixedDeltaTime;

        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacleLayers);
        obstacleFilter.useTriggers = false;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(1 << enemyLayer);
        enemyFilter.useTriggers = false;
    }

    public void SetVisualProfile(TeleportVisualProfile newProfile)
    {
        if (visualProfile == newProfile) return;
        visualProfile = newProfile;
        if (!aiming) return;
        DestroyAimingVisuals();
        CreateAimingVisuals();
        targetRangeVisual?.SetCenter(targetPosition);
        aimLineVisual?.SetPoints(body.position, targetPosition);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (!aiming && !cooldownFlashPlayed && Time.unscaledTime >= nextReadyTime)
        {
            cooldownFlashPlayed = true;
            PlayReadyFlash();
        }

        if (!aiming && mouse.rightButton.wasPressedThisFrame &&
            Time.unscaledTime >= nextReadyTime && !LevelUpUI.IsPopupOpen)
        {
            BeginAiming();
        }

        if (!aiming) return;
        if (LevelUpUI.IsPopupOpen)
        {
            CancelAiming();
            return;
        }

        UpdateTarget(mouse.position.ReadValue());
        targetRangeVisual?.SetCenter(targetPosition);
        aimLineVisual?.SetPoints(body.position, targetPosition);
        UpdateSlowMotion();

        if (mouse.rightButton.wasReleasedThisFrame)
        {
            ExecuteTeleport();
        }
    }

    private void BeginAiming()
    {
        timeScaleBeforeAim = Time.timeScale;
        fixedDeltaBeforeAim = Time.fixedDeltaTime;
        aimingElapsedTime = 0f;
        aiming = true;
        if (worldCamera == null) worldCamera = Camera.main;
        CreateAimingVisuals();
        UpdateTarget(Mouse.current.position.ReadValue());
        cameraFollow?.SetPointerFocus(targetPosition, true);
    }

    private void UpdateTarget(Vector2 screenPosition)
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(screenPosition);
        Vector2 offset = (Vector2)mouseWorld - body.position;
        Vector2 direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.right;
        float distance = Mathf.Min(offset.magnitude, maximumDistance);
        targetPosition = GetReachablePosition(direction, distance);
    }

    private Vector2 GetReachablePosition(Vector2 direction, float distance)
    {
        castResults.Clear();
        int hitCount = playerCollider.Cast(direction, obstacleFilter, castResults,
            distance + collisionSkin);
        float allowedDistance = distance;
        for (int index = 0; index < hitCount; index++)
        {
            allowedDistance = Mathf.Min(allowedDistance,
                Mathf.Max(0f, castResults[index].distance - collisionSkin));
        }
        return body.position + direction * allowedDistance;
    }

    private void ExecuteTeleport()
    {
        aiming = false;
        RestoreTimeScale();
        cameraFollow?.SetPointerFocus(targetPosition, false);
        DestroyAimingVisuals();
        body.position = targetPosition;
        body.linearVelocity = Vector2.zero;
        BeginArrivalAreaEffect();
        StartCoroutine(GrantInvulnerability());
        nextReadyTime = Time.unscaledTime + cooldown;
        cooldownFlashPlayed = cooldown <= 0f;
        if (cooldownFlashPlayed) PlayReadyFlash();
    }

    private void BeginArrivalAreaEffect()
    {
        hitEnemies.Clear();
        OnArrivalAreaEffect?.Invoke(targetPosition, effectRadius);
        if (!PlayArrivalShockwave())
        {
            ApplyAreaEffectAtRadius(effectRadius);
        }
    }

    private void ApplyAreaEffectAtRadius(float currentRadius)
    {
        overlapResults.Clear();
        Physics2D.OverlapCircle(
            targetPosition,
            Mathf.Clamp(currentRadius, 0f, effectRadius),
            enemyFilter,
            overlapResults);
        foreach (Collider2D enemyCollider in overlapResults)
        {
            ApplyAreaEffectToEnemy(enemyCollider);
        }
    }

    private void ApplyAreaEffectToEnemy(Collider2D enemyCollider)
    {
        if (!enemyCollider.TryGetComponent(out EnemyHealth health) ||
            !hitEnemies.Add(health)) return;
            Vector2 direction = ((Vector2)enemyCollider.transform.position - targetPosition).normalized;
            if (direction == Vector2.zero) direction = Vector2.up;
            if (enemyCollider.TryGetComponent(out EnemyKnockback knockback))
            {
                knockback.ApplyKnockback(direction, knockbackStrength, knockbackDuration);
            }
            if (damage > 0)
            {
                health.TakeDamage(new DamageData(damage, 0f, 0, 0f, 1));
            }
            else
            {
                health.RegisterZeroDamageHit();
            }
            if (enemyCollider.TryGetComponent(out EnemyHitEffect hitEffect)) hitEffect.Play();
    }

    private IEnumerator GrantInvulnerability()
    {
        playerHealth.SetTeleportInvulnerable(true);
        yield return new WaitForSecondsRealtime(invulnerabilityDuration);
        playerHealth.SetTeleportInvulnerable(false);
    }

    private void UpdateSlowMotion()
    {
        aimingElapsedTime += Time.unscaledDeltaTime;
        float slowDuration = Mathf.Max(0.01f, slowDownDuration);
        float recoveryDuration = Mathf.Max(0.01f, timeScaleRecoveryDuration);

        if (aimingElapsedTime <= slowDuration)
        {
            float slowProgress = Mathf.Clamp01(aimingElapsedTime / slowDuration);
            Time.timeScale = Mathf.Lerp(timeScaleBeforeAim, aimingTimeScale, slowProgress);
        }
        else
        {
            float recoveryProgress = Mathf.Clamp01(
                (aimingElapsedTime - slowDuration) / recoveryDuration);
            Time.timeScale = Mathf.Lerp(aimingTimeScale, timeScaleBeforeAim, recoveryProgress);
        }

        float relativeScale = Time.timeScale / Mathf.Max(timeScaleBeforeAim, 0.01f);
        Time.fixedDeltaTime = fixedDeltaBeforeAim * Mathf.Max(relativeScale, 0.01f);
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = timeScaleBeforeAim;
        Time.fixedDeltaTime = fixedDeltaBeforeAim > 0f
            ? fixedDeltaBeforeAim : defaultFixedDeltaTime;
    }

    private void CancelAiming()
    {
        aiming = false;
        RestoreTimeScale();
        cameraFollow?.SetPointerFocus(targetPosition, false);
        DestroyAimingVisuals();
    }

    private void CreateAimingVisuals()
    {
        if (visualProfile == null) return;
        TeleportVisualProfile.VisualPrefabs visuals = visualProfile.Visuals;
        if (visuals.targetRange != null)
        {
            GameObject instance = Instantiate(visuals.targetRange);
            targetRangeVisual = instance.GetComponent<TeleportTargetRangeVisual>();
            if (targetRangeVisual != null)
            {
                targetRangeVisual.Initialize(effectRadius);
            }
            else
            {
                Debug.LogError("Teleport target-range prefab requires TeleportTargetRangeVisual on its root.", instance);
                Destroy(instance);
            }
        }

        if (visuals.aimLine != null)
        {
            GameObject instance = Instantiate(visuals.aimLine);
            aimLineVisual = instance.GetComponent<TeleportAimLineVisual>();
            if (aimLineVisual == null)
            {
                Debug.LogError("Teleport aim-line prefab requires TeleportAimLineVisual on its root.", instance);
                Destroy(instance);
            }
        }
    }

    private void DestroyAimingVisuals()
    {
        if (targetRangeVisual != null) Destroy(targetRangeVisual.gameObject);
        if (aimLineVisual != null) Destroy(aimLineVisual.gameObject);
        targetRangeVisual = null;
        aimLineVisual = null;
    }

    private bool PlayArrivalShockwave()
    {
        if (visualProfile == null) return false;
        GameObject prefab = visualProfile.Visuals.arrivalShockwave;
        if (prefab == null) return false;
        GameObject instance = Instantiate(prefab);
        RadialImpactVisual visual = instance.GetComponent<RadialImpactVisual>();
        if (visual == null)
        {
            Debug.LogError("Teleport arrival-shockwave prefab requires RadialImpactVisual on its root.", instance);
            Destroy(instance);
            return false;
        }
        visual.Play(targetPosition, effectRadius, ApplyAreaEffectAtRadius);
        return true;
    }

    private void PlayReadyFlash()
    {
        if (cooldownReadyFlashRoutine != null)
        {
            StopCoroutine(cooldownReadyFlashRoutine);
        }
        cooldownReadyFlashRoutine = StartCoroutine(PlayReadyFlashRoutine());
    }

    private IEnumerator PlayReadyFlashRoutine()
    {
        Color originalColor = playerRenderer.color;
        float elapsed = 0f;
        while (elapsed < cooldownReadyFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / cooldownReadyFlashDuration);
            float blend = cooldownReadyColor.a * fade;
            Color color = Color.Lerp(originalColor, cooldownReadyColor, blend);
            color.a = originalColor.a;
            playerRenderer.color = color;
            yield return null;
        }

        playerRenderer.color = originalColor;
        cooldownReadyFlashRoutine = null;
    }

    private void OnDisable()
    {
        if (cooldownReadyFlashRoutine != null)
        {
            StopCoroutine(cooldownReadyFlashRoutine);
            cooldownReadyFlashRoutine = null;
        }
        StopAllCoroutines();
        playerHealth?.SetTeleportInvulnerable(false);
        RestoreTimeScale();
        cameraFollow?.SetPointerFocus(targetPosition, false);
        DestroyAimingVisuals();
        aiming = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}
