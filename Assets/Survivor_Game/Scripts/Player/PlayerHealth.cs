using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 5;

    [Header("레벨별 최대 체력 성장")]
    [Tooltip("첫 레벨업에 적용되는 최대 체력 증가율입니다.")]
    [SerializeField, Range(0f, 1f)] private float startingHealthGrowthRate = 0.1f;

    [Tooltip("레벨업할 때마다 감소하는 증가율입니다. 0.002는 0.2%p입니다.")]
    [SerializeField, Range(0f, 1f)] private float healthGrowthDecreasePerLevel = 0.002f;

    [Tooltip("최대 체력 증가율이 내려갈 수 있는 최솟값입니다.")]
    [SerializeField, Range(0f, 1f)] private float minimumHealthGrowthRate = 0.01f;

    private int currentHealth;
    private int baseMaxHealth;
    private bool isDead;
    private Rigidbody2D body;
    private PlayerMovement playerMovement;
    private AutoWeapon autoWeapon;
    private Collider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    private PlayerLevel playerLevel;

    public event Action OnHealthChanged;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        body = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        autoWeapon = GetComponent<AutoWeapon>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerLevel = GetComponent<PlayerLevel>();
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

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        OnHealthChanged?.Invoke();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (autoWeapon != null)
        {
            autoWeapon.enabled = false;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        if (isDead)
        {
            return;
        }

        maxHealth = Mathf.Max(
            1,
            Mathf.RoundToInt(baseMaxHealth * GetHealthGrowthMultiplier(newLevel))
        );
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke();
    }

    private float GetHealthGrowthMultiplier(int level)
    {
        float multiplier = 1f;

        for (int reachedLevel = 2; reachedLevel <= level; reachedLevel++)
        {
            multiplier *= 1f + GetHealthGrowthRate(reachedLevel);
        }

        return multiplier;
    }

    private float GetHealthGrowthRate(int reachedLevel)
    {
        int previousLevelUpCount = Mathf.Max(0, reachedLevel - 2);
        float growthRate = startingHealthGrowthRate -
            healthGrowthDecreasePerLevel * previousLevelUpCount;
        return Mathf.Max(minimumHealthGrowthRate, growthRate);
    }
}
