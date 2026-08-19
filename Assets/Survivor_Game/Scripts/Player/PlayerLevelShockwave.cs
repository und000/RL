using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerLevel))]
public class PlayerLevelShockwave : MonoBehaviour
{
    [Header("Shockwave Range")]
    [SerializeField, Min(0.1f)] private float shockwaveRadius = 7f;
    [SerializeField] private RadialImpactVisual shockwaveVisualPrefab;

    [Header("Shockwave Effect")]
    [SerializeField, Min(0)] private int damage;
    [SerializeField, Min(0f)] private float baseAttackKnockbackStrength = 3f;
    [SerializeField, Min(0f)] private float knockbackStrengthMultiplier = 5f;
    [SerializeField, Min(0f)] private float knockbackDuration = 0.35f;

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
        if (playerLevel != null) playerLevel.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (playerLevel != null) playerLevel.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        StartCoroutine(PlayAfterPopup());
    }

    private IEnumerator PlayAfterPopup()
    {
        yield return null;
        yield return new WaitUntil(() => !LevelUpUI.IsPopupOpen);

        HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
        if (shockwaveVisualPrefab == null)
        {
            HitEnemiesInside(shockwaveRadius, hitEnemies);
            yield break;
        }

        RadialImpactVisual visual = Instantiate(shockwaveVisualPrefab);
        visual.Play(transform.position, shockwaveRadius,
            radius => HitEnemiesInside(radius, hitEnemies));
    }

    private void HitEnemiesInside(float currentRadius, HashSet<EnemyHealth> hitEnemies)
    {
        overlapResults.Clear();
        Physics2D.OverlapCircle(transform.position, currentRadius, enemyFilter, overlapResults);
        foreach (Collider2D enemyCollider in overlapResults)
        {
            HitEnemy(enemyCollider, hitEnemies);
        }
    }

    private void HitEnemy(Collider2D enemyCollider, HashSet<EnemyHealth> hitEnemies)
    {
        if (!enemyCollider.TryGetComponent(out EnemyHealth enemyHealth) ||
            !hitEnemies.Add(enemyHealth)) return;

            Vector2 direction =
                (enemyCollider.transform.position - transform.position).normalized;
            if (direction == Vector2.zero) direction = Vector2.up;

            if (enemyCollider.TryGetComponent(out EnemyKnockback enemyKnockback))
            {
                enemyKnockback.ApplyKnockback(direction,
                    baseAttackKnockbackStrength * knockbackStrengthMultiplier,
                    knockbackDuration);
            }
            if (damage > 0)
            {
                enemyHealth.TakeDamage(new DamageData(damage, 0f, 0, 0f, 1));
            }
            if (enemyCollider.TryGetComponent(out EnemyHitEffect hitEffect)) hitEffect.Play();
    }
}
