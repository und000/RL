using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField]
    [Min(1)]
    private int maxHealth = 3;

    [SerializeField]
    private GameObject experienceGemPrefab;
    [SerializeField]
    private int myExperience = 1;

    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        currentHealth -= damage;

        Debug.Log($"{name} 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (experienceGemPrefab != null)
        {
            GameObject gemObject = Instantiate(experienceGemPrefab, transform.position, Quaternion.identity);

            ExperienceGem gem = gemObject.GetComponent<ExperienceGem>();

            if (gem != null)
            {
                gem.Initialize(myExperience);
            }
        }
        Destroy(gameObject);
    }
}