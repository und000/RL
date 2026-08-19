using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IEnemyPoolLifecycle
{
    [Header("Enemy Data")]
    [SerializeField] private EnemyProfile profile;
    [Header("Legacy Fallback (used when Profile is empty)")]
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0)] private int defense;
    [SerializeField] private GameObject experienceGemPrefab;
    [SerializeField, Min(1)] private int myExperience = 1;

    private int currentHealth;
    private bool dying;

    public EnemyProfile Profile => profile;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnDied;

    private void Awake()
    {
        ResetHealth();
    }

    private void OnEnable()
    {
        ResetHealth();
    }

    public void TakeDamage(DamageData damageData)
    {
        if (currentHealth <= 0)
        {
            return;
        }

        float remainingDefense = Mathf.Max(
            0f,
            GetDefense() - damageData.FlatArmorPenetration
        );
        float effectiveDefense = remainingDefense *
            (1f - damageData.ArmorPenetrationRate);
        int normalDamage = Mathf.Max(
            damageData.MinimumDamage,
            Mathf.RoundToInt(damageData.NormalDamage - effectiveDefense)
        );
        int trueDamage = Mathf.Max(0, Mathf.RoundToInt(damageData.TrueDamage));
        int finalDamage = normalDamage + trueDamage;
        currentHealth = Mathf.Max(currentHealth - finalDamage, 0);
        if (finalDamage > 0)
        {
            OnDamaged?.Invoke();
        }
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        if (currentHealth == 0)
        {
            Die();
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return profile != null ? profile.MaxHealth : maxHealth;
    }

    public int GetDefense()
    {
        return profile != null ? profile.Defense : defense;
    }

    public void RegisterZeroDamageHit()
    {
        if (currentHealth > 0)
        {
            OnDamaged?.Invoke();
        }
    }

    private void Die()
    {
        if (dying) return;
        dying = true;
        OnDied?.Invoke();

        if (TryGetComponent(out EnemyLifecycleVisual lifecycleVisual) &&
            lifecycleVisual.TryPlayDeath(CompleteDeath))
        {
            return;
        }

        CompleteDeath();
    }

    private void CompleteDeath()
    {

        GameObject gemPrefab = profile != null
            ? profile.ExperienceGemPrefab : experienceGemPrefab;
        int experience = profile != null ? profile.Experience : myExperience;
        if (gemPrefab != null)
        {
            GameObject gemObject = Instantiate(
                gemPrefab,
                transform.position,
                Quaternion.identity
            );

            if (gemObject.TryGetComponent(out ExperienceGem gem))
            {
                gem.Initialize(experience);
            }
        }
        else
        {
            Debug.LogWarning($"{name}의 Experience Gem Prefab이 연결되지 않았습니다.");
        }

        if (TryGetComponent(out PooledEnemy pooledEnemy) && pooledEnemy.IsConfigured)
        {
            pooledEnemy.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnEnemySpawned()
    {
        ResetHealth();
    }

    public void OnEnemyDespawned()
    {
        dying = false;
    }

    private void ResetHealth()
    {
        currentHealth = GetMaxHealth();
        dying = false;
    }
}
