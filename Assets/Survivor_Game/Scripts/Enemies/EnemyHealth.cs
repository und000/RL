using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField] private GameObject experienceGemPrefab;
    [SerializeField, Min(1)] private int myExperience = 1;

    private int currentHealth;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
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

    private void Die()
    {
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
