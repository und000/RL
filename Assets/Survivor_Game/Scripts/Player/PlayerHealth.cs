using System;
using System.Collections;
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

    [Header("재기동")]
    [Tooltip("영구 개조로 얻은 재기동으로 살아날 때 채워지는 최대 체력 비율입니다.")]
    [SerializeField, Range(0.05f, 1f)] private float reviveHealthRatio = 0.5f;
    [Tooltip("일어난 직후 무적으로 버티는 시간입니다. " +
        "그대로 두면 같은 공격에 바로 다시 쓰러집니다.")]
    [SerializeField, Min(0f)] private float reviveInvulnerableDuration = 2f;

    private int currentHealth;
    private int baseMaxHealth;
    private int bonusMaxHealth;
    private int revivesUsed;
    private bool isReviveInvulnerable;
    private bool isDead;
    private bool isDodgeInvulnerable;
    private bool isTeleportInvulnerable;
    private Rigidbody2D body;
    private PlayerMovement playerMovement;
    private Collider2D playerCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private PlayerLevel playerLevel;
    private PlayerDodge playerDodge;
    private PlayerTeleportSkill playerTeleportSkill;
    private PlayerLowerbodyFacing lowerbodyFacing;

    public event Action OnHealthChanged;
    /// <summary>플레이어가 쓰러진 순간 한 번 호출된다.</summary>
    public event Action OnDied;
    /// <summary>재기동으로 다시 일어난 순간.</summary>
    public event Action OnRevived;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        body = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null)
        {
            Transform image = transform.Find("Body/Image");
            spriteRenderer = image != null ? image.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();
        }
        playerLevel = GetComponent<PlayerLevel>();
        playerDodge = GetComponent<PlayerDodge>();
        playerTeleportSkill = GetComponent<PlayerTeleportSkill>();
        lowerbodyFacing = GetComponentInChildren<PlayerLowerbodyFacing>(true);
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
    public bool IsDead => isDead;

    /// <summary>체력을 회복한다. 이미 가득 찼거나 쓰러진 뒤면 false를 돌려준다.</summary>
    public bool Heal(int amount)
    {
        if (amount <= 0 || isDead || currentHealth >= maxHealth) return false;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke();
        return true;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || isDodgeInvulnerable || isTeleportInvulnerable ||
            isReviveInvulnerable)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        lowerbodyFacing?.PlayHit();
        OnHealthChanged?.Invoke();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void SetDodgeInvulnerable(bool invulnerable)
    {
        isDodgeInvulnerable = invulnerable && !isDead;
    }

    public void SetTeleportInvulnerable(bool invulnerable)
    {
        isTeleportInvulnerable = invulnerable && !isDead;
    }

    /// <summary>지금 남은 재기동 횟수.</summary>
    public int RevivesRemaining =>
        Mathf.Max(0, MetaProgressRuntime.Bonuses.ReviveCount - revivesUsed);

    /// <summary>
    /// 영구 개조로 얻은 재기동이 남아 있으면 그 자리에서 다시 일어난다.
    /// 일어난 뒤 잠시 무적이라 같은 장판에 겹쳐 죽지 않는다.
    /// </summary>
    private bool TryRevive()
    {
        if (RevivesRemaining <= 0) return false;

        revivesUsed++;
        currentHealth = Mathf.Clamp(
            Mathf.RoundToInt(maxHealth * reviveHealthRatio), 1, maxHealth);
        OnHealthChanged?.Invoke();
        OnRevived?.Invoke();
        StartCoroutine(RunReviveInvulnerability());
        return true;
    }

    private IEnumerator RunReviveInvulnerability()
    {
        isReviveInvulnerable = true;
        yield return new WaitForSeconds(reviveInvulnerableDuration);
        isReviveInvulnerable = false;
    }

    private void Die()
    {
        if (TryRevive()) return;

        isDead = true;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (playerDodge != null)
        {
            playerDodge.enabled = false;
        }

        if (playerTeleportSkill != null)
        {
            playerTeleportSkill.enabled = false;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }

        OnDied?.Invoke();
    }

    private void HandleLevelUp(int newLevel)
    {
        if (isDead)
        {
            return;
        }

        maxHealth = ResolveMaxHealth(newLevel);
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke();
    }

    /// <summary>
    /// 코어 보드 같은 바깥 장비가 최대 체력을 더해 준다. 값은 덮어쓰기이므로 누적되지 않는다.
    /// 늘어난 만큼은 현재 체력에도 그대로 얹어 준다.
    /// </summary>
    public void SetBonusMaxHealth(int bonus)
    {
        if (isDead || bonusMaxHealth == bonus)
        {
            return;
        }

        bonusMaxHealth = bonus;

        int previousMax = maxHealth;
        int currentLevel = playerLevel != null ? playerLevel.GetCurrentLevel() : 1;
        maxHealth = ResolveMaxHealth(currentLevel);

        currentHealth = Mathf.Clamp(currentHealth + (maxHealth - previousMax), 1, maxHealth);
        OnHealthChanged?.Invoke();
    }

    private int ResolveMaxHealth(int level)
    {
        int grown = Mathf.RoundToInt(baseMaxHealth * GetHealthGrowthMultiplier(level));
        return Mathf.Max(1, grown + bonusMaxHealth);
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
