using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    private int experienceAmount;
    
    public void Initialize(int amount)
    {
        experienceAmount = amount;
    }

    private bool isCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
        {
            return;
        }

        if (!other.TryGetComponent(out PlayerLevel playerLevel))
        {
            return;
        }

        isCollected = true;

        playerLevel.AddExperience(experienceAmount);

        Destroy(gameObject);
    }
}