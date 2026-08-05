using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0)] private int defense;
    [SerializeField] private GameObject experienceGemPrefab;
    [SerializeField, Min(1)] private int myExperience = 1;

    private int currentHealth;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(DamageData damageData)
    {
        if (currentHealth <= 0)
        {
            return;
        }

        float remainingDefense = Mathf.Max(
            0f,
            defense - damageData.FlatArmorPenetration
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
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
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
        return maxHealth;
    }

    public int GetDefense()
    {
        return defense;
    }

    private void Die()
    {
        OnDied?.Invoke();

        if (experienceGemPrefab != null)
        {
            GameObject gemObject = Instantiate(
                experienceGemPrefab,
                transform.position,
                Quaternion.identity
            );

            if (gemObject.TryGetComponent(out ExperienceGem gem))
            {
                gem.Initialize(myExperience);
            }
        }
        else
        {
            Debug.LogWarning($"{name}의 Experience Gem Prefab이 연결되지 않았습니다.");
        }

        Destroy(gameObject);
    }
}
