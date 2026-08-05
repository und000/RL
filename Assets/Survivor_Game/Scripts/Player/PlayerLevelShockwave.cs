using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerLevel), typeof(SpriteRenderer))]
public class PlayerLevelShockwave : MonoBehaviour
{
    [Header("충격파 범위")]
    [SerializeField, Min(0.1f)] private float shockwaveRadius = 7f;
    [SerializeField, Min(0.05f)] private float expansionDuration = 0.45f;

    [Header("충격파 효과")]
    [SerializeField, Min(0)] private int damage;
    [SerializeField, Min(0f)] private float baseAttackKnockbackStrength = 3f;
    [SerializeField, Min(0f)] private float knockbackStrengthMultiplier = 5f;
    [SerializeField, Min(0f)] private float knockbackDuration = 0.35f;

    [Header("충격파 연출")]
    [SerializeField] private Color shockwaveColor = new Color(0.5f, 0.9f, 1f, 0.9f);
    [SerializeField, Min(0.01f)] private float lineWidth = 0.12f;
    [SerializeField, Range(16, 128)] private int circleSegments = 64;

    private PlayerLevel playerLevel;
    private ContactFilter2D enemyFilter;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(32);

    private void Awake()
    {
        playerLevel = GetComponent<PlayerLevel>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(1 << enemyLayer);
        enemyFilter.useTriggers = false;
    }

    private void OnEnable()
    {
        if (playerLevel != null)
        {
            playerLevel.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (playerLevel != null)
        {
            playerLevel.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        StartCoroutine(PlayShockwaveAfterLevelUpPopup());
    }

    private IEnumerator PlayShockwaveAfterLevelUpPopup()
    {
        // LevelUpUI opens from OnProgressChanged later in the same frame.
        yield return null;

        // Wait for the slowdown and popup to finish, even when its minimum
        // time scale is configured above zero.
        yield return new WaitUntil(() => !LevelUpUI.IsPopupOpen);
        yield return PlayShockwave();
    }

    private IEnumerator PlayShockwave()
    {
        HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
        LineRenderer ring = CreateRing();
        float elapsedTime = 0f;

        while (elapsedTime < expansionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / expansionDuration);
            float currentRadius = shockwaveRadius * progress;

            UpdateRing(ring, currentRadius, 1f - progress);
            HitEnemiesInside(currentRadius, hitEnemies);
            yield return null;
        }

        HitEnemiesInside(shockwaveRadius, hitEnemies);
        if (ring != null)
        {
            Destroy(ring.gameObject);
        }
    }

    private void HitEnemiesInside(
        float currentRadius,
        HashSet<EnemyHealth> hitEnemies)
    {
        overlapResults.Clear();
        Physics2D.OverlapCircle(
            transform.position,
            currentRadius,
            enemyFilter,
            overlapResults
        );

        foreach (Collider2D enemyCollider in overlapResults)
        {
            if (!enemyCollider.TryGetComponent(out EnemyHealth enemyHealth) ||
                !hitEnemies.Add(enemyHealth))
            {
                continue;
            }

            Vector2 knockbackDirection =
                (enemyCollider.transform.position - transform.position).normalized;
            if (knockbackDirection == Vector2.zero)
            {
                knockbackDirection = Vector2.up;
            }

            if (enemyCollider.TryGetComponent(out EnemyKnockback enemyKnockback))
            {
                enemyKnockback.ApplyKnockback(
                    knockbackDirection,
                    baseAttackKnockbackStrength * knockbackStrengthMultiplier,
                    knockbackDuration
                );
            }

            if (damage > 0)
            {
                enemyHealth.TakeDamage(new DamageData(damage, 0f, 0, 0f, 1));
            }

            if (enemyCollider.TryGetComponent(out EnemyHitEffect hitEffect))
            {
                hitEffect.Play();
            }
        }
    }

    private LineRenderer CreateRing()
    {
        GameObject ringObject = new GameObject("LevelUpShockwave");
        ringObject.transform.SetParent(transform, false);

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = circleSegments;
        ring.startWidth = lineWidth;
        ring.endWidth = lineWidth;
        ring.startColor = shockwaveColor;
        ring.endColor = shockwaveColor;
        ring.sortingOrder = 30;
        ring.sharedMaterial = GetComponent<SpriteRenderer>().sharedMaterial;
        return ring;
    }

    private void UpdateRing(LineRenderer ring, float radius, float alphaMultiplier)
    {
        Color currentColor = shockwaveColor;
        currentColor.a *= alphaMultiplier;
        ring.startColor = currentColor;
        ring.endColor = currentColor;

        for (int index = 0; index < circleSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / circleSegments;
            ring.SetPosition(
                index,
                transform.position +
                new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius
            );
        }
    }
}
